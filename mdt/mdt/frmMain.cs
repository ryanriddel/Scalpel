using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using NATS.Client;
using System.Collections.Concurrent;


using System.Diagnostics;

namespace mdt
{
    public partial class frmMain : Form
    {
        Dictionary<string, EventHandler<MsgHandlerEventArgs>> natsMessageHandlerDict = new Dictionary<string, EventHandler<MsgHandlerEventArgs>>();
        Dictionary<string, IAsyncSubscription> natsSubscriptionDict = new Dictionary<string, IAsyncSubscription>();
        Dictionary<string, double> timeDifferenceAverageDict = new Dictionary<string, double>();

        List<string> natsSubjectList = new List<string>();

        uint testDurationMillis = 0;
        ulong numQuoteMsgReceived = 0;
        ulong numTradeMsgReceived = 0;

        long memoryUsed = 0;

        ConcurrentDictionary<string, ConcurrentQueue<string>> quoteMessages = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        ConcurrentDictionary<string, ConcurrentQueue<string>> tradeMessages = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        ConcurrentDictionary<ProtobufMessageType, ConcurrentDictionary<string, ConcurrentQueue<string>>> receivedMessageDictByType = 
            new ConcurrentDictionary<ProtobufMessageType, ConcurrentDictionary<string, ConcurrentQueue<string>>>();

        ConcurrentDictionary<ProtobufMessageType, ConcurrentDictionary<string, ulong>> receivedMessageCountByType =
            new ConcurrentDictionary<ProtobufMessageType, ConcurrentDictionary<string, ulong>>();

        ConcurrentDictionary<string, ProtobufMessageType> subjToMsgTypeDict = new ConcurrentDictionary<string, ProtobufMessageType>();
        NATSAdapter nats;

        string IPAddress = "";
        string Port = "";
        string Protocol = "";

        volatile Stopwatch stopWatch = new Stopwatch();

        Feeds.QuoteFeedClient client;


        Timer rbBroadcastTimer;

        public frmMain()
        {
            InitializeComponent();
            connectionDetailsChanged(this, null);

            rbBroadcastTimer = new Timer();
            rbBroadcastTimer.Tick += rbBroadcastTimer_Tick;
            rbBroadcastTimer.Interval = 20;
            
        }

        private void rbBroadcastTimer_Tick(object sender, EventArgs e)
        {
            Mktdatamessage.TradeMessage t = new Mktdatamessage.TradeMessage();
            Mktdatamessage.Instrument inst = new Mktdatamessage.Instrument();
            inst.UnderlyingSymbol = "TEST";
            inst.Strike = 3.14F;
            inst.Idx = 1;
            inst.ExpirationDay = 2;
            inst.ExpirationMonth = 1;
            inst.ExpirationYear = 2018;
            inst.InstrumentType = Mktdatamessage.Instrument.Types.InstrType.Option;
            inst.IsCallOption = true;

            t.Instruments.Add(inst);
            t.Price = 10;
            t.Size = 33;
            t.Timestamp = 070022111;
            t.ExchangeName = "CBOE";
            
            
            Mktdatamessage.RBTrade m = new Mktdatamessage.RBTrade();
            m.BaseMessage = t;
            m.OptionDelta = 1;
            m.OptionGamma = 2;
            m.OptionRho = 3;
            m.OptionTheta = 4;
            m.OptionVega = 5;

            m.StockPrice = 3.14F;

            int msize = m.CalculateSize();
            byte[] ar = Google.Protobuf.MessageExtensions.ToByteArray(m);

            if(nats.GetConnectionState() == ConnState.CONNECTED)
                nats.natsConnection.Publish("MKTDATA.TRADE.RBTRADE", ar);
        }

        private void btnConnect_Click(object sndr, EventArgs e)
        {
            closeConnections();

            connectionDetailsChanged(this, null);


            if(Protocol == "NATS")
            {
                if (IPAddress != "" & Port != "")
                    nats = new NATSAdapter(IPAddress, Port);
                else
                    txtConnectMsg.Text = "Invalid IP/Port";

                nats.OnConnStateUpdate += Nats_OnConnStateUpdate;
                nats.OnErrUpdate += Nats_OnErrUpdate;

                txtConnectMsg.Text = "NATS Connected! \n";
            }

            if (checkEditTestTCP.Checked)
            {
                
                try
                {
                    client = new Feeds.QuoteFeedClient("Option Quotes", Feeds.QuoteFeedClient.ClientType.Option);
                    client.Connect(txtPubSubIP.Text, 13000);
                }
                catch(Exception a)
                {
                    txtConnectMsg.Text = "Pubsub connect failed: " + a.Message;
                }
            }
        }

        void closeConnections()
        {
            if(nats != null)
            {
                nats.CloseConnection();
            }
            if(client != null)
            {
                client.Disconnect();
            }
        }

        private string InstrToString(Mktdatamessage.Instrument instr)
        {
            string instrString = instr.UnderlyingSymbol + "_" + instr.Strike +
            (instr.IsCallOption ? "C" : "P") + instr.ExpirationMonth + "/" + instr.ExpirationDay +
            "/" + instr.ExpirationYear;

            return instrString;
        }
        uint numErrors = 0;
        private void btnStartTest_Click(object sender, EventArgs e)
        {
            if (nats == null)
                return;
            if (nats.GetConnectionState() != ConnState.CONNECTED)
                return;

            natsMessageHandlerDict.Clear();
            natsSubscriptionDict.Clear();
            memoryUsed = GC.GetTotalMemory(false);

            testDurationMillis = (uint)updMilliseconds.Value;
            testDurationMillis += ((uint)updSeconds.Value) * 1000;
            tradeMessages.Clear();
            quoteMessages.Clear();

            numErrors = 0;

            receivedMessageDictByType.Clear();
            receivedMessageCountByType.Clear();
            quoteMessages = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

            for (int i = 0; i < natsSubjectList.Count; i++)
            {
                string subj = natsSubjectList[i];
                string[] subParts = subj.Split(new char[] { '.' });

                bool isTrade = (subParts[1] == "TRADE");
                bool isRB = (subParts[2] == "RBTRADE");
                bool isStats = (subParts[1] == "STATS");
                string thirdToken = (subParts[2]);
                

                ProtobufMessageType mtype;

                if (isTrade && isRB)
                {
                    mtype = ProtobufMessageType.RBTradeMessage;
                    
                }
                else if ((isTrade && !isRB) || subj == "MKTDATA.DEBUG.RBTRADE")
                {
                    mtype = ProtobufMessageType.TradeMessage;
                }
                else if (subParts[1] == "DEPTH")
                {
                    mtype = ProtobufMessageType.DepthMessage;
                }
                else if (isStats && subParts[2] == "PKTHANDLER")
                {
                    mtype = ProtobufMessageType.PacketHandlerStatsMessage;
                }
                else if (isStats && subParts[2] == "SINGLEMSG")
                    mtype = ProtobufMessageType.SingleMessageStatsMessage;
                else
                    mtype = ProtobufMessageType.UnknownMessageType;

                if (!receivedMessageDictByType.ContainsKey(mtype))
                    receivedMessageDictByType.TryAdd(mtype, new ConcurrentDictionary<string, ConcurrentQueue<string>>());

                if(!receivedMessageDictByType[mtype].ContainsKey(subj))
                    receivedMessageDictByType[mtype].TryAdd(subj, new ConcurrentQueue<string>());

                receivedMessageCountByType.TryAdd(mtype, new ConcurrentDictionary<string, ulong>());

                receivedMessageDictByType[mtype][subj] = new ConcurrentQueue<string>();
                subjToMsgTypeDict[subj] = mtype;


                timeDifferenceAverageDict[subj] = 0;

                if (!receivedMessageCountByType.ContainsKey(mtype))
                {
                    receivedMessageCountByType.TryAdd(mtype, new ConcurrentDictionary<string, ulong>());
                }

                if (!receivedMessageCountByType[mtype].ContainsKey(subj))
                {
                    receivedMessageCountByType[mtype].TryAdd(subj, 0);
                }

                EventHandler<MsgHandlerEventArgs> evHandler = new EventHandler<MsgHandlerEventArgs>((object o, MsgHandlerEventArgs a) => 
                {
                    //DateTime nTime = DateTime.Now;
                    //uint timestamp = (uint) (nTime.Hour * 60 * 60 * 1000 + nTime.Minute * 60 * 1000 + nTime.Second * 1000 + nTime.Millisecond);
                    

                    if (mtype == ProtobufMessageType.RBTradeMessage)
                    {
                        isTrade = true;
                        byte[] data = a.Message.Data;
                        Mktdatamessage.RBTrade2 rbMsg;

                        rbMsg = Mktdatamessage.RBTrade2.Parser.ParseFrom(data);
                        string rbstr = Google.Protobuf.JsonFormatter.ToDiagnosticString(rbMsg) +
                        Google.Protobuf.JsonFormatter.ToDiagnosticString(rbMsg.Instruments[0]) +
                        (rbMsg.Instruments[0].IsCallOption ? "C" : "P");
                        

                        
                        //baseMessage.MergeFrom(rbMsg.BaseMessage);




                        ulong cnt = ++receivedMessageCountByType[ProtobufMessageType.RBTradeMessage][subj];
                        if(rbstr.Length > 1 )
                        { 
                            //store message
                            /*string instrString = InstrToString(baseMessage.Instruments[0]);
                            string messageText;


                            messageText = "[" + baseMessage.Timestamp + "]: " + instrString + "===> [" + rbMsg.StockPrice + "] [" + rbMsg.AvgBBOAskSz + "] [" + rbMsg.AvgBBOBidSz + "] [" + rbMsg.OptionDelta + "] [" + rbMsg.OptionGamma + "] [" +
                            rbMsg.OptionRho + "] [" + rbMsg.OptionTheta + "] [" + rbMsg.OptionVega + "] ======= [" + baseMessage.Price + "] [" + baseMessage.Size + "] [" + baseMessage.ExchangeName + "]";
                            */
                            //receivedMessageDictByType[ProtobufMessageType.RBTradeMessage][subj].Enqueue(messageText);
                            receivedMessageDictByType[ProtobufMessageType.RBTradeMessage][subj].Enqueue(rbstr);
                        }
                        else
                            numErrors++;

                    }
                    else if(mtype == ProtobufMessageType.TradeMessage)
                    {
                        
                        byte[] data = a.Message.Data;
                        Mktdatamessage.TradeMessage tMsg;
                        

                        tMsg = Mktdatamessage.TradeMessage.Parser.ParseFrom(data);
                        

                        ulong cnt = ++receivedMessageCountByType[ProtobufMessageType.TradeMessage][subj];

                        if (cnt % 1 == 0)
                        {
                            //store message
                            string instrString = InstrToString(tMsg.Instruments[0]);
                            string messageText = instrString + "|" + tMsg.Timestamp + "|" + Google.Protobuf.JsonFormatter.ToDiagnosticString(tMsg);
                            //tMsg.Price + "] [" + tMsg.Size + "] [" + tMsg.ExchangeName + "]";

                            receivedMessageDictByType[ProtobufMessageType.TradeMessage][subj].Enqueue(messageText);
                        }


                    }
                    else if(mtype == ProtobufMessageType.DepthMessage)
                    {
                        byte[] data = a.Message.Data;
                        Mktdatamessage.BookDepthMessage qMsg;
                        
                        qMsg = Mktdatamessage.BookDepthMessage.Parser.ParseFrom(data);
                        ulong cnt = ++receivedMessageCountByType[ProtobufMessageType.DepthMessage][subj];

                        
                        
                        if (cnt % 1 == 0)
                        {
                            string instrString = InstrToString(qMsg.Instruments[0]);

                            string messageText = instrString + "|" + qMsg.Timestamp + "|" + Google.Protobuf.JsonFormatter.ToDiagnosticString(qMsg);


                            receivedMessageDictByType[ProtobufMessageType.DepthMessage][subj].Enqueue(messageText);
                            //quoteMessages.TryUpdate(subj,messageText);
                            Console.WriteLine(messageText);
                        }
                    }
                    else if(mtype == ProtobufMessageType.PacketHandlerStatsMessage)
                    {
                        byte[] data = a.Message.Data;
                        /*Mktdatamessage.PacketHandlerStats sMsg;


                        sMsg = Mktdatamessage.PacketHandlerStats.Parser.ParseFrom(data);


                        ulong cnt = ++receivedMessageCountByType[ProtobufMessageType.PacketHandlerStatsMessage][subj];

                        if (cnt % 1 == 0)
                        {
                            //store message
                            string messageText = sMsg.PacketHandlerDescription + ":  [" + DateTime.Now.ToShortDateString() + "] ===> [" + sMsg.LastLoggedData + "] [" + sMsg.LastLoggedGaps + "] [" +
                            sMsg.LastLoggedPackets + "] [" + sMsg.TotalDataProcessed + "] [" + sMsg.TotalGaps + "] [" + sMsg.TotalPacketsProcessed + "]";

                            receivedMessageDictByType[ProtobufMessageType.PacketHandlerStatsMessage][subj].Enqueue(messageText);
                        }*/
                    }
                    else if(mtype == ProtobufMessageType.UnknownMessageType)
                    {
                        //uhh...idk?
                        byte[] data = a.Message.Data;
                        Console.WriteLine("Weird message... ");
                    }
                    else
                    {
                        
                        
                    }
                });

                natsSubscriptionDict[subj] = nats.Subscribe(subj, evHandler);
                
            }

            foreach(IAsyncSubscription s in natsSubscriptionDict.Values)
            {
               
                s.Start();
            }
            

            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.startCount();
                    client.SubscribeAll();
                }
            }

            Timer t = new Timer();
            t.Interval = 250;
            t.Tick += (o, s) => 
            {
                if (stopWatch.IsRunning == false)
                    stopWatch.Start();

                if (stopWatch.ElapsedMilliseconds > testDurationMillis)
                {
                    stopWatch.Stop();
                    Task.Run(new Action(endTest));
                    t.Enabled = false;
                }
                
                Clock.Needles[0].Value = (float)(stopWatch.Elapsed.TotalSeconds % 60);
                Clock.Needles[1].Value = (float)(stopWatch.ElapsedMilliseconds % 1000);
                Clock.EndUpdate();
                Clock.Needles[0].Update();
                Clock.Needles[1].Update();
                System.Threading.Thread.Yield();

                
            };
            stopWatch.Restart();
            t.Enabled = true;
            t.Start();


        }

        //Timer t2;
        //private void button1_Click(object sender, EventArgs e)
        //{
        //    t2 = new Timer();
        //    t2.Interval = 5;
        //    t2.Tick += (o, s) =>
        //    {
        //        if (stopWatch.IsRunning == false)
        //            stopWatch.Start();

        //        if (stopWatch.ElapsedMilliseconds > testDurationMillis)
        //        {
        //            stopWatch.Reset();
        //            t2.Enabled = false;
        //        }

        //        /*
                
        //        Clock.BeginUpdate();
        //        Clock.Needles[0].Angle = (float)(6 * stopWatch.Elapsed.TotalSeconds);
        //        Clock.Needles[1].Angle = (float)(36.0 * stopWatch.ElapsedMilliseconds / 100);

        //        Clock.Needles[0].Update();
        //        Clock.Needles[1].Update();
        //        Clock.EndUpdate();
                
        //        */

        //    };
        //    stopWatch.Reset();
        //    t2.Enabled = true;
        //    t2.Start();
        //}

        public void endTest()
        {
            int pubSubAppMessageCount = 0;
            if (client != null)
            {
                pubSubAppMessageCount = client.stopCount();
                client.UnSubscribeAll();
            }

            setConnectMsgText("Finished sample collection...");
            string remsg = "Test results:" + Environment.NewLine;
            long totalDelivered = 0;
            long totalDropped = 0;
            long totalPending = 0;

            long memDiffKB = (GC.GetTotalMemory(false) - memoryUsed) / 1024;
            Console.WriteLine("Memory used: " + memDiffKB);

            for(int i=0; i<natsSubjectList.Count; i++)
            {
                string subj = natsSubjectList[i];
                IAsyncSubscription sub = natsSubscriptionDict[subj];

                remsg += "Subject: " + subj + Environment.NewLine + "Pending Messages:" + sub.PendingMessages.ToString() + Environment.NewLine +
                    "Dropped Messages: " + sub.Dropped.ToString() + Environment.NewLine + "Delivered Messages: " + sub.Delivered.ToString() + Environment.NewLine
                    + "Elapsed Milliseconds: " + stopWatch.ElapsedMilliseconds.ToString() + Environment.NewLine +
                    "Messages per Second: " + Math.Round((double) 1000* sub.Delivered / (stopWatch.ElapsedMilliseconds)).ToString() + Environment.NewLine + 
                    "Delivery Success Rate: " + Math.Round((double) 100 * sub.Delivered / (sub.Delivered + sub.Dropped)) + "%" +
                    Environment.NewLine + Environment.NewLine + Environment.NewLine;

                totalDelivered += sub.Delivered;
                totalDropped += sub.Dropped;
                totalPending += sub.PendingMessages;

                sub.Unsubscribe();
            }
            remsg += Environment.NewLine + Environment.NewLine + Environment.NewLine + "Total Delivered Messages: " + totalDelivered.ToString() + Environment.NewLine
                + "Total Dropped Messages: " + totalDropped.ToString() + Environment.NewLine + "Total Pending Messages: " + totalPending.ToString() + Environment.NewLine
                + Environment.NewLine + "Total Counted Trade Messages: " + numTradeMsgReceived.ToString() + Environment.NewLine + "Total Counted Quote Messages: " +
                numQuoteMsgReceived.ToString() + Environment.NewLine + "Pubsubapp message count: " + pubSubAppMessageCount + Environment.NewLine
                + "NATS to pubsub speed ratio: " + Convert.ToString(Math.Round((float)totalDelivered / pubSubAppMessageCount, 2) * 100).ToString() + "%";

            clearDataTabs();
            addDataTab("Test results", remsg);
            Console.WriteLine("Errors: " + numErrors);
            foreach(IAsyncSubscription ias in natsSubscriptionDict.Values)
            {
                string subj = ias.Subject;
                string[] subParts = subj.Split(new char[] { '.' });

                ProtobufMessageType mtype = subjToMsgTypeDict[subj];

                string str = "0";
                bool success = false;
                string allMessages = subj + " (" + receivedMessageCountByType[mtype][subj] + " messages received):" + Environment.NewLine;

                ConcurrentDictionary<string, ConcurrentQueue<string>> tempDict;
                tempDict = receivedMessageDictByType[mtype];

                int ct = tempDict[subj].Count;

                for (int i = 0; i < ct; i++)
                {
                    
                    success = tempDict[subj].TryDequeue(out str);
                    
                    if (success)
                        allMessages += str + Environment.NewLine; 
                }

                addDataTab(subj, allMessages);
                /*string strGuy = "" ;
                string theBigMessage = "";
                while(quoteMessages[subj].TryDequeue(out strGuy))
                {
                    theBigMessage += strGuy + Environment.NewLine;
                }

                addDataTab("Quotes", theBigMessage);*/
            }
            
            tradeMessages.Clear();
            natsSubscriptionDict.Clear();
            natsSubjectList.Clear();


            

            

            
        }

        void setConnectMsgText(string text)
        {
            txtConnectMsg.Invoke((MethodInvoker)delegate
           {
               txtConnectMsg.Text = text;
           });
        }

        void addDataTab(string header, string content)
        {
            tabData.Invoke((MethodInvoker)delegate
           {
               tabData.TabPages.Add(createDataTab(header, content));
               tabData.Refresh();
           });
        }

        void clearDataTabs()
        {
            tabData.Invoke((MethodInvoker)delegate
            {
                tabData.TabPages.Clear();
                tabData.Refresh();
            });
        }

        DevExpress.XtraTab.XtraTabPage createDataTab(string headerName, string content)
        {
            DevExpress.XtraTab.XtraTabPage tPageTemplate = new DevExpress.XtraTab.XtraTabPage();
            tPageTemplate.Text = headerName;

            tPageTemplate.Name = "tpgdata" + tabData.TabPages.Count.ToString();
            tPageTemplate.Size = new System.Drawing.Size(611, 192);

            TextBox textBox = new System.Windows.Forms.TextBox();
            textBox.BackColor = System.Drawing.SystemColors.MenuText;
            textBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            textBox.ForeColor = System.Drawing.Color.Lime;
            textBox.Location = new System.Drawing.Point(3, 3);
            textBox.Multiline = true;
            textBox.Name = "tboxdata" + tabData.TabPages.Count.ToString();
            textBox.Size = new System.Drawing.Size(605, 189);
            textBox.ScrollBars = ScrollBars.Both;
            textBox.WordWrap = false;

            textBox.Text = content;

            tPageTemplate.Controls.Add(textBox);

            return tPageTemplate;
        }

        private void connectionDetailsChanged(object sender, EventArgs e)
        {
            IPAddress = txtIP.Text;
            Port = txtPort.Text;
            Protocol = cboProtocol.Text;
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            string subject = txtSubject.Text;
            if (subject != "")
            {
                if(natsSubjectList.Contains(subject) == false)
                {
                    listboxSubjects.Items.Insert(0, subject);
                    natsSubjectList.Add(subject);
                }
            }
        }

        private void btnUnsubscribe_Click(object sender, EventArgs e)
        {
            if(listboxSubjects.SelectedItem != null)
            {
                listboxSubjects.Items.Remove(listboxSubjects.SelectedItem);
                natsSubjectList.Remove((string) listboxSubjects.SelectedItem);
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            listboxSubjects.Items.Clear();
            natsSubjectList.Clear();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void gaugeControl1_Click(object sender, EventArgs e)
        {

        }

        private void spreadsheetControl1_Click(object sender, EventArgs e)
        {

        }
        private void Nats_OnErrUpdate(string s)
        {
            txtConnectMsg.Invoke((MethodInvoker)delegate
           {
               txtConnectMsg.AppendText("\n" + s);
           });
        }

        private void Nats_OnConnStateUpdate(string s)
        {
            txtConnectMsg.Invoke((MethodInvoker)delegate
            {
                txtConnectMsg.AppendText("\n" + s);
            });
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            txtConnectMsg.Invoke((MethodInvoker)delegate
            {
                txtConnectMsg.ResetText();
            });
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeConnections();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            ConnState s = nats.GetConnectionState();
            Console.WriteLine(s.ToString());
            s = nats.natsConnection.State;
            Console.WriteLine(s.ToString());
            Exception a = nats.natsConnection.LastError;
            if (a != null)
                Console.WriteLine("Error: " + a.Message);
            if (nats.natsConnection.ConnectedUrl != null)
                Console.WriteLine(nats.natsConnection.ConnectedUrl);

            nats.natsConnection.Flush(1000);
            nats.natsConnection.ResetStats();
        }

        private void checkEditTestTCP_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked == true)
            {
                rbBroadcastTimer.Enabled = true;
            }
            else
            {
                rbBroadcastTimer.Enabled = false;
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            nats.PublishString(topicBox.Text, messageBox.Text);
            consoleBox.Text += "Message published" + Environment.NewLine;
        }


        void addSubscribeMsgText(string text)
        {
            subscribeTextBox.Invoke((MethodInvoker)delegate
            {
                subscribeTextBox.Text += text + Environment.NewLine;
            });
        }

        private void subscribeButton_Click(object sender, EventArgs e)
        {
            EventHandler<MsgHandlerEventArgs> evHandler = new EventHandler<MsgHandlerEventArgs>((object o, MsgHandlerEventArgs a) =>
            {
                addSubscribeMsgText(a.Message.ToString());
            });
            nats.Subscribe(subscribeBox.Text, evHandler);
            
            addSubscribeMsgText("Subscribed to " + subscribeBox.Text);
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            string returnVal = nats.RequestString(topicBox.Text, messageBox.Text);
            consoleBox.Text += "Message received: " + returnVal + Environment.NewLine;
        }
    }

    public enum ProtobufMessageType
    {
        DepthMessage,
        TradeMessage,
        RBTradeMessage,
        PacketHandlerStatsMessage,
        SingleMessageStatsMessage,
        UnknownMessageType
    };
}

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

        NATSAdapter nats;

        string IPAddress = "";
        string Port = "";
        string Protocol = "";

        volatile Stopwatch stopWatch = new Stopwatch();

        Feeds.QuoteFeedClient client;

        public frmMain()
        {
            InitializeComponent();
            connectionDetailsChanged(this, null);


        }

        private void btnConnect_Click(object sndr, EventArgs e)
        {
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
                client = new Feeds.QuoteFeedClient("Option Quotes", Feeds.QuoteFeedClient.ClientType.Option);
                client.OnConnectionStatusChanged += () =>
                {
                    if (client.IsConnected)
                    {
                        System.Threading.Thread.Sleep(1000);
                        client.SubscribeAll();
                    }
                };
                client.Connect("172.20.168.71", 13000);
            }
        }

        ulong messageCount = 0;
        ConcurrentDictionary<string, ConcurrentQueue<string>> messages = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private void btnStartTest_Click(object sender, EventArgs e)
        {
            if (nats.GetConnectionState() != ConnState.CONNECTED)
                return;
            messageCount = 0;
            natsMessageHandlerDict.Clear();
            natsSubscriptionDict.Clear();

            testDurationMillis = (uint)updMilliseconds.Value;
            testDurationMillis += ((uint)updSeconds.Value) * 1000;
            messages.Clear();

            for (int i = 0; i < natsSubjectList.Count; i++)
            {
                string subj = natsSubjectList[i];
                string[] subParts = subj.Split(new char[] { '.' });

                bool isTrade = (subParts[1] == "TRADE");
                messages[subj] = new ConcurrentQueue<string>();
                timeDifferenceAverageDict[subj] = 0;
                EventHandler<MsgHandlerEventArgs> evHandler = new EventHandler<MsgHandlerEventArgs>((object o, MsgHandlerEventArgs a) => 
                {
                    DateTime nTime = DateTime.Now;
                    uint timestamp = (uint) (nTime.Hour * 60 * 60 * 1000 + nTime.Minute * 60 * 1000 + nTime.Second * 1000 + nTime.Millisecond);

                    if(isTrade)
                    {
                        
                        byte[] data = a.Message.Data;
                        Streaminterface.TradeMessage tMsg;
                        if (subParts[0] == "TEST")
                        {
                            Google.Protobuf.ByteString newBS = Google.Protobuf.ByteString.CopyFrom(data);
                            tMsg = Streaminterface.TradeMessage.Parser.ParseFrom(newBS);
                        }
                        else
                        {
                            tMsg = Streaminterface.TradeMessage.Parser.ParseFrom(data);
                        }
                        try
                        {
                            Streaminterface.Instrument instr = tMsg.Instruments[0];

                            string instrString = "";

                            if (instr.InstrumentType == Streaminterface.Instrument.Types.InstrType.Option)
                                instrString = instr.UnderlyingSymbol + instr.Strike.ToString() + (instr.IsCallOption ? "C" : "P") +
                                "(" + instr.ExpirationMonth + "-" + instr.ExpirationDay + "-" + instr.ExpirationYear + ")";
                            else if (instr.InstrumentType == Streaminterface.Instrument.Types.InstrType.Equity)
                                instrString = instr.UnderlyingSymbol;

                            string m = instrString + ", " + tMsg.Timestamp.ToString() + ", " + tMsg.Price.ToString() + ", " + tMsg.Size.ToString() + ", " + timestamp.ToString();
                            messages[subj].Enqueue(m);
                           // this.txtMain.Invoke((MethodInvoker) delegate { txtMain.AppendText(m + "\n"); });

                        }
                        catch (Google.Protobuf.InvalidProtocolBufferException d)
                        {
                            Console.WriteLine(d.Message.ToString());
                            throw d;
                        }
                        
                        
                    }
                    else
                    {
                        byte[] data = a.Message.Data;
                       // Console.WriteLine(a.Message.ToString());
                        Streaminterface.BookDepthMessage qMsg;
                            
                        try
                        {
                            qMsg = Streaminterface.BookDepthMessage.Parser.ParseFrom(data);

                            Streaminterface.Instrument instr = qMsg.Instruments[0];

                            string instrString = "";

                            if (instr.InstrumentType == Streaminterface.Instrument.Types.InstrType.Option)
                                instrString = instr.UnderlyingSymbol + instr.Strike.ToString() + (instr.IsCallOption ? "C" : "P") +
                                "(" + instr.ExpirationMonth + "-" + instr.ExpirationDay + "-" + instr.ExpirationYear + ")";
                            else if (instr.InstrumentType == Streaminterface.Instrument.Types.InstrType.Equity)
                                instrString = instr.UnderlyingSymbol;

                            messageCount++;

                            //if we don't limit the number of messages we store, we'll run out of memory within 20 seconds
                            if(messageCount % 10 == 0)
                                messages[subj].Enqueue(Google.Protobuf.JsonFormatter.ToDiagnosticString(qMsg));
                            

                        }
                        catch (Google.Protobuf.InvalidProtocolBufferException d)
                        {
                            Console.WriteLine(d.Message.ToString());
                            throw d;
                        }
                    }
                });

                natsSubscriptionDict[subj] = nats.Subscribe(subj, evHandler);

                if (client != null)
                {
                    client.startCount();
                }

            }

            foreach(IAsyncSubscription s in natsSubscriptionDict.Values)
            {
                s.Start();
            }

            Timer t = new Timer();
            t.Interval = 5;
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
            if (client != null)
            {
                client.stopCount();
            }
            setConnectMsgText("Finished sample collection...");
            string remsg = "Test results:" + Environment.NewLine;
            long totalDelivered = 0;
            long totalDropped = 0;
            long totalPending = 0;
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
                + "Total Dropped Messages: " + totalDropped.ToString() + Environment.NewLine + "Total Pending Messages: " + totalPending.ToString();

            clearDataTabs();
            addDataTab("Test results", remsg);
            
            for (int i = 0; i < messages.Keys.Count; i++)
            {
                string subj = messages.Keys.ToArray()[i];

                string allMessages = "";
                string msg;
                while(messages[subj].TryDequeue(out msg))
                {
                    allMessages += msg + Environment.NewLine;
                }

                addDataTab(subj, allMessages);
            }


                MessageBox.Show("Test Complete!");

            
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
    }


}

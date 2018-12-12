using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using RBGRPC;
using Google.Protobuf;
using Grpc.Core;

namespace grpctester
{
    public partial class Form1: Form
    {
        Channel channel;
        RBGRPC.Credentials credentials;
        
        
        
        public Form1()
        {
            InitializeComponent();
            

            
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            requestComboBox.Items.Add("Subscribe to AAPL Option Quotes");
            requestComboBox.Items.Add("Subscribe to AAPL Option Trades");
        }

        private void grpcCallButton_Click(object sender, EventArgs e)
        {
            if(requestComboBox.Text == "")
            {
                MessageBox.Show("Invalid request.");
                return;
            }
            RBGRPC.SubscriptionRequest newRequest = new RBGRPC.SubscriptionRequest
            {
                IsSpread = false,
                IsList = false
            };

            RBGRPC.Instrument instr = new RBGRPC.Instrument
            {
                UnderlyingSymbol = "AAPL",
                ExpirationDay = 28,
                ExpirationMonth = 12,
                ExpirationYear = 2018,
                IsCallOption = true,
                InstrumentType = RBGRPC.Instrument.Types.InstrType.Option,
                IsLegOfSpread = false,
                Strike = 165
            };
            newRequest.Instruments.Add(instr);





            channel = new Channel("172.20.168.71", 50052, ChannelCredentials.Insecure, null);
            
            var client = new RBGRPC.SubscriptionManager.SubscriptionManagerClient(channel);
            CallOptions callOptions = new CallOptions();

            RBGRPC.SubscriptionResponse sr;
            if (requestComboBox.Text == "Subscribe to AAPL Option Quotes")
            {
                sr = client.SubscribeToQuotes(newRequest, callOptions);

            }
            else if (requestComboBox.Text == "Subscribe to AAPL Option Trades")
            {
                sr = client.SubscribeToTrades(newRequest, callOptions);

            }
            else
            {
                MessageBox.Show("Invalid request.");
                return;
            }
            
            if (sr.Message != "")
                replyTextBox.Text = sr.Message;
           

            
            channel.ShutdownAsync();
        }

        private void requestComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}

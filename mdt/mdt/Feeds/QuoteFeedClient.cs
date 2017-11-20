using MktSrvcAPI;
using System;
using System.Net.Sockets;

namespace mdt.Feeds
{
    public class QuoteFeedClient
    {
        public enum ClientType
        {
            Option,
            Equity,
            Spread
        }

        public delegate void ConnectionStatusChangedHandler();
        public event ConnectionStatusChangedHandler OnConnectionStatusChanged;

        public delegate void FeedMessageHandler(string message);
        public event FeedMessageHandler OnFeedMessage;

        //public delegate void QuoteBookReceivedHandler(QuoteBook book);
        //public event QuoteBookReceivedHandler OnQuoteBookReceived;

        private DepthOfBkClient client;

        public string Name { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }

        private bool isConnected = false;
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            private set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnConnectionStatusChanged?.Invoke();
                }
            }
        }

        public ClientType FeedType { get; private set; }

        private volatile bool countingTrades = false;

        private volatile int msgCount;
        public int MessageCount
        {
            get
            {
                return msgCount;
            }
        }

        public QuoteFeedClient(string name, ClientType type)
        {
            Name = name;
            FeedType = type;
            client = new DepthOfBkClient();
            client.RegisterSessHndlrs(HandleError, HandleConnectionFailed, HandleConnected, HandleDisconnected);
            //log.Info(String.Format("{0} quote feed \"{1}\" created", FeedType, Name));
        }

        private void HandleConnected(Socket s)
        {
            IsConnected = true;
            //log.Info(String.Format("{0} quote feed \"{1}\" connected to {2}:{3}", FeedType, Name, Host, Port));
            OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" connected to {2}:{3}", FeedType, Name, Host, Port));
        }

        private void HandleDisconnected(Socket s)
        {
            IsConnected = false;
            //log.Info(String.Format("{0} quote feed \"{1}\" disconnected", FeedType, Name));
            OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" disconnected", FeedType, Name));
        }

        private void HandleConnectionFailed(Socket s)
        {
            IsConnected = false;
            //log.Warn(String.Format("{0} quote feed \"{1}\" connection failed", FeedType, Name));
            OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" connection failed", FeedType, Name));
        }

        private void HandleError(string errstr)
        {
            //log.Error(String.Format("{0} quote feed \"{1}\" error: {2}", FeedType, Name, errstr));
            //OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" error: {2}", FeedType, Name, errstr));
        }

        public void Connect(string host, int port)
        {
            this.Host = host;
            this.Port = port;

            //log.Info(String.Format("{0} quote feed \"{1}\" attempting to connect to {2}:{3}", FeedType, Name, Host, Port));
            OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" attempting to connect to {2}:{3}", FeedType, Name, Host, Port));
            client.Connect(host, port);
        }

        public void Disconnect()
        {
            if (client.IsConnected())
            {
                //log.Info(String.Format("{0} quote feed \"{1}\" disconnecting", FeedType, Name));
                OnFeedMessage?.Invoke(String.Format("{0} quote feed \"{1}\" disconnecting", FeedType, Name));
                client.Disconnect();
            }
        }

        public void startCount()
        {
            msgCount = 0;
            countingTrades = true;
        }

        public void stopCount()
        {
            countingTrades = false;
            Console.WriteLine(Name + "messages received: " + msgCount);
        }

        public void SubscribeAll()
        {
            client.Subscribe(DepthOfBkHndlr);
        }

        public void SubscribeToInstrument(InstrInfo[] inst)
        {
            //log.Info(String.Format("{0} quote feed \"{1}\" subscribing to {2}", FeedType, Name, InstrumentUtilities.InstrArrayToRBString(inst)));
            client.Subscribe(inst, DepthOfBkHndlr);
        }

        public void UnsubscribeFromInstrument(InstrInfo[] inst)
        {
            //log.Info(String.Format("{0} quote feed \"{1}\" unsubscribing from {2}", FeedType, Name, InstrumentUtilities.InstrArrayToRBString(inst)));
            client.Unsubscribe(inst, DepthOfBkHndlr);
        }
        
        private void DepthOfBkHndlr(InstrInfo[] instr, uint ts, byte partid, int mod, byte numbid, byte numask, byte[] bidexch, byte[] askexch, QuoteInfo[] bidbk, QuoteInfo[] askbk)
        {
            //Console.WriteLine(instr[0].sym);
            if (countingTrades)
            {
                msgCount++;
            }

            //if (instr != null && ts != 0 && ((instr.Length == 1 && askexch != null && askbk != null) || instr.Length > 1))
            //{

            //}
        }
    }
}

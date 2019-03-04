using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NATS.Client;

namespace mdt
{
    public class NATSAdapter
    {
        //NATS.Client.Connection natsConnection;
        public NATS.Client.Options natsOptions;
        IStatistics natsStatistics;
       public  IConnection natsConnection;



        private ConnState State;
        public event OnMsgUpdateHandler OnMsgUpdate;
        public delegate void OnMsgUpdateHandler(int msgtype, object e);
        public event OnErrUpdateHandler OnErrUpdate;
        public delegate void OnErrUpdateHandler(string s);
        public event OnConnStateUpdateHandler OnConnStateUpdate;
        public delegate void OnConnStateUpdateHandler(string s);

        string IPAddress = "";
        string Port = "";

        

        public NATSAdapter(string _IPAddress, string _Port)
        {
            IPAddress = _IPAddress;
            Port = _Port;

            natsOptions = ConnectionFactory.GetDefaultOptions();
            natsOptions.Url = "nats://default:scalp@" + IPAddress + ":" + Port;

            natsOptions.DisconnectedEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.AsyncErrorEventHandler += (sender, args) => OnError(sender, args);
            natsOptions.ClosedEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.ServerDiscoveredEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.AllowReconnect = true;

            try
            {
                natsConnection = new ConnectionFactory().CreateConnection(natsOptions);
                natsStatistics = natsConnection.Stats;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            
        }



        public bool CloseConnection()
        {
            natsConnection.Close();
            return natsConnection.IsClosed();
        }

        public IAsyncSubscription Subscribe(string subject, EventHandler<MsgHandlerEventArgs> msgHandler)
        {
            return natsConnection.SubscribeAsync(subject, msgHandler);
        }

        public void PublishString(string subject, string message)
        {
            byte[] bytes = new byte[message.Length * sizeof(char)];
            System.Buffer.BlockCopy(message.ToCharArray(), 0, bytes, 0, bytes.Length);
            natsConnection.Publish(subject, bytes);
            Console.WriteLine(bytes.Length);
        }

        public string RequestString(string subject, string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            Console.WriteLine(System.Text.Encoding.Default.GetString(bytes));
            string response = "NO REPLY";
            try
            {
                Msg a = natsConnection.Request(subject, bytes, 5000);

                response = System.Text.Encoding.Default.GetString(a.Data);
            }
            catch(Exception e)
            {
                
            }
            return response;
        }

        public void Unsubscribe(IAsyncSubscription subscription)
        {
            subscription.Unsubscribe();
        }

        private void OnDisconnect(object s, NATS.Client.ConnEventArgs e)
        {
            OnConnStateUpdate?.Invoke(e.Conn.State.ToString());
        }
        private void OnError(object s, ErrEventArgs e)
        {
            OnErrUpdate?.Invoke(e.Error.ToString());
        }

        public ConnState GetConnectionState()
        {
            return natsConnection.State;
        }



    }
}

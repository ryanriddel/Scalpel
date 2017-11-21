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
            natsOptions.Url = "nats://" + IPAddress + ":" + Port;

            natsOptions.DisconnectedEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.AsyncErrorEventHandler += (sender, args) => OnError(sender, args);
            natsOptions.ClosedEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.ServerDiscoveredEventHandler += (sender, args) => OnDisconnect(sender, args);
            natsOptions.AllowReconnect = true;

            natsConnection = new ConnectionFactory().CreateConnection(natsOptions);
            natsStatistics = natsConnection.Stats;
            
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

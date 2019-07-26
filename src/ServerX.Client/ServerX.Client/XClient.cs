using Autofac;
using BeetleX.Clients;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerX.Client
{
    public class XClient : AsyncTcpClient
    {
        SubscrptionManager manager;
        readonly ClientStateMgr state_managr;
        readonly ILifetimeScope lift_scope;
        readonly string heartbeatname = typeof(HeartBeat).FullName;

        const string S_XCLIENT = "XCLIENT";
        readonly RequestMessage heartbeat_request = new RequestMessage("beat", null);
        public XClient(ILifetimeScope scope)
        {
            state_managr = new ClientStateMgr(this, 10, 10);
            lift_scope = scope /*?? throw new ArgumentNullException(nameof(ILifetimeScope), "need set ILifetimeScope")*/;
            manager = new SubscrptionManager();
            manager.Subscrption<HeartBeat, HeartBeatHandler>(new HeartBeatHandler(this));
            manager.RegisterServerEventNameToLocalEventName(FixPackageDeal);
            PacketReceive = Receive;
            Connected = OnConnected;
            Disconnected = OnDisConnected;
        }
        class ClientStateMgr
        {
            int _state = -1;
            readonly XClient _client;
            readonly int _heartbeatPeriod = 0;
            readonly int _reconnectPeriod = 0;
            public ClientStateMgr(XClient xClient, int heartbeatPeriod, int reconnectPeriod)
            {
                _client = xClient;
                _heartbeatPeriod = heartbeatPeriod;
                _reconnectPeriod = reconnectPeriod;
            }
            public void Switch(int state)
            {
                if (_state == -1)
                {
                    _state = state;
                    Task.Run(() =>
                    {
                        for (; ; )
                        {
                            while (_state == 1)
                            {
                                _client.SendHeartBeat();
                                Thread.Sleep(_heartbeatPeriod * 1000);
                            }
                            while (_state == 2)
                            {
                                _client.TryConnect(1, _reconnectPeriod);
                            }
                        }
                    });
                }
                else _state = state;
            }
        }
        private void OnDisConnected(IClient c)
        {
            state_managr.Switch(2);
        }

        private void OnConnected(IClient c)
        {
            state_managr.Switch(1);
        }
        internal void RaiseRecHeartBeat()
        {

        }
        public void Init(string host, int port)
        {
            Init(host, port, new Packet(manager));
            TryConnect(1, 10);
        }
        public void TryConnect(int step, int max)
        {
            int count = 0;
            var step_add_count = max / step;
            while (!SendHeartBeat().IsConnected)
            {
                int sleep = 1000 * (count >= step_add_count ? max : (count++) * step);
                Thread.Sleep(sleep);
            }
        }
        public AsyncTcpClient SendHeartBeat()
        {
            return Send(heartbeat_request);
        }
        void Receive(IClient client, object message)
        {
            var rsp = (ResponseMessage)message;
            foreach (var handler in rsp.Handlers)
            {
                if (handler.Instance != null)
                {
                    handler.HandlerType.GetMethod("Handle", new Type[] { rsp.DataType }).Invoke(handler.Instance, new object[] { rsp.Data });
                }
                else
                {
                    using (var scope = lift_scope.BeginLifetimeScope(S_XCLIENT))
                    {
                        var instance = scope.ResolveOptional(handler.HandlerType);
                        handler.HandlerType.GetMethod("Handle", new Type[] { rsp.DataType }).Invoke(instance, new object[] { rsp.Data });
                    }
                }
            }
        }
        private string FixPackageDeal(string arg)
        {
            if (arg == "beat") return heartbeatname;
            return arg;
        }

        public void Subscrption<T, THandler>() where THandler : IEventHandler<T>
        {
            manager.Subscrption<T, THandler>();
        }
    }
}

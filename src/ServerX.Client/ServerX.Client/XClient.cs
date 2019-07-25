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
        readonly ILifetimeScope lift_scope;
        readonly string heartbeatname = typeof(HeartBeat).FullName;
        const string S_XCLIENT = "XCLIENT";
        readonly RequestMessage heartbeat_request = new RequestMessage("beat", null);
        public XClient(ILifetimeScope scope)
        {
            lift_scope = scope ?? throw new ArgumentNullException(nameof(ILifetimeScope), "need set ILifetimeScope");
            manager = new SubscrptionManager();
            manager.Subscrption<HeartBeat, HeartBeatHandler>();
            manager.RegisterServerEventNameToLocalEventName(FixPackageDeal);
            PacketReceive = Receive;
            Connected = OnConnected;
        }

        private void OnConnected(IClient c)
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
                using (var scope = lift_scope.BeginLifetimeScope(S_XCLIENT))
                {
                    var instance = scope.ResolveOptional(handler);
                    handler.GetMethod("Handle", new Type[] { rsp.DataType }).Invoke(instance, new object[] { rsp.Data });
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

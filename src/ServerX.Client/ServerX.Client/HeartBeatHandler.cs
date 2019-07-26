namespace ServerX.Client
{
    public class HeartBeat
    {

    }
    internal class HeartBeatHandler : IEventHandler<HeartBeat>
    {
        readonly XClient client;
        public HeartBeatHandler(XClient client)
        {
            this.client = client;
        }
        public void Handle(HeartBeat @event)
        {
            client.RaiseRecHeartBeat();
        }
    }
}
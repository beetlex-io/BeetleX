namespace ServerX.Client
{
    public class HeartBeat
    {

    }
    internal class HeartBeatHandler : IEventHandler<HeartBeat>
    {
        public void Handle(HeartBeat @event)
        {
            throw new System.NotImplementedException();
        }
    }
}
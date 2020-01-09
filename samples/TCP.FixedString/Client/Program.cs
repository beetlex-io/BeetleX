using System;
using BeetleX;
using BeetleX.Buffers;
using BeetleX.Clients;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = SocketFactory.CreateClient<TcpClient, StringPacket>("127.0.0.1", 9090);
            while (true)
            {
                Console.Write("Enter Name:");
                var line = Console.ReadLine();
                client.SendMessage(line);
                var result = client.ReceiveMessage<string>();
                Console.WriteLine($"{DateTime.Now} {result}");
            }
        }
    }

    public class StringPacket : BeetleX.Packets.FixeHeaderClientPacket
    {
        public override IClientPacket Clone()
        {
            return new StringPacket();
        }

        protected override object OnRead(IClient client, PipeStream stream)
        {
            return stream.ReadString(CurrentSize);
        }

        protected override void OnWrite(object data, IClient client, PipeStream stream)
        {
            stream.Write((string)data);
        }
    }

}

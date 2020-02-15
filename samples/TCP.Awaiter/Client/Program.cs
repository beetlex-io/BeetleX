using BeetleX.Buffers;
using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Test();
            System.Threading.Thread.Sleep(-1);
        }

        static async void Test()
        {
            AwaiterClient client = new AwaiterClient("localhost", 9090, new StringPacket());
            while (true)
            {
                var name = Console.ReadLine();
                client.Send(name);
                var result = await client.Receive();
                Console.WriteLine(result);
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

using BeetleX;
using BeetleX.Clients;
using System;
using System.Net;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpClient client = SocketFactory.CreateClient<AsyncTcpClient>("127.0.0.1", 9090);
            client.DataReceive = (o, e) =>
            {
                var pipestream = e.Stream.ToPipeStream();
                if (pipestream.TryReadLine(out string line))
                {
                    Console.WriteLine(line);
                }
            };
            while (true)
            {
                Console.Write("Enter Name:");
                BytesHandler line = Console.ReadLine() + "\r\n";
                client.Send(line);
                if(!client.IsConnected)
                {
                    Console.WriteLine(client.LastError.Message);
                }
            }

        }
    }
}

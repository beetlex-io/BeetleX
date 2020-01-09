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
            TcpClient client = SocketFactory.CreateClient<TcpClient>("127.0.0.1", 9090);
            client.LocalEndPoint = new System.Net.IPEndPoint(IPAddress.Any, 9022);
            while (true)
            {
                Console.Write("Enter Name:");
                var line = Console.ReadLine();
                client.Stream.ToPipeStream().WriteLine(line);
                client.Stream.Flush();
                var reader = client.Receive();
                line = reader.ReadLine();
                Console.WriteLine($"{DateTime.Now} {line}");
            }

        }
    }
}

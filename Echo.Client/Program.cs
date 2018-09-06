using BeetleX;
using BeetleX.Clients;
using System;

namespace Echo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = SocketFactory.CreateTcpClient<TcpClient>("127.0.0.1", 9090);
            while (true)
            {
                Console.Write("Enter Name:");
                var line = Console.ReadLine();
                client.NetStream.WriteLine(line);
                client.NetStream.Flush();
                var reader = client.Read();
                line = reader.ReadLine();
                Console.WriteLine(line);
            }
            Console.WriteLine("Hello World!");
        }
    }
}

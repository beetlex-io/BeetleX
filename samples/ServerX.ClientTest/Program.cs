using ServerX.Client;
using System;

namespace ServerX.ClientTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            var client = new XClient(null);
            client.Init("127.0.0.1", 9090);
            Console.ReadKey();
        }
    }
}

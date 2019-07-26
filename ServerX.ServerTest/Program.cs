using System;

namespace ServerX.ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverX = new ServerX();
            serverX.Init(null);
            Console.ReadKey();
        }
    }
}

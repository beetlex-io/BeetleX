using BeetleX;
using BeetleX.Clients;
using BeetleX.Packets;
using Messages;
using System;
using System.Collections.Generic;

namespace IMessage.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            DefaultClientPacket packet = new DefaultClientPacket();
            packet.Register(typeof(Employee).Assembly);
            TcpClient client = SocketFactory.CreateClient<TcpClient>(packet, "127.0.0.1", 9090);
            //TcpClient client = SocketFactory.CreateClient<TcpClient>(packet, "127.0.0.1", 9090,"localhost");
            while (true)
            {
                Console.Write("enter get employee quantity:");
                string line = Console.ReadLine();
                int quantity;
                if (int.TryParse(line, out quantity))
                {
                    SearchEmployee search = new SearchEmployee();
                    search.Quantity = quantity;
                    client.SendMessage(search);
                    var result = client.ReceiveMessage<IList<Employee>>();
                    foreach (Employee item in result)
                    {
                        Console.WriteLine("\t{0} {1}", item.FirstName, item.LastName);
                    }
                }
                else
                {
                    Console.WriteLine("input not a number!");
                }

            }
        }
    }
}

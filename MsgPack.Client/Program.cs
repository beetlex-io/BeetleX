using BeetleX;
using BeetleX.Clients;
using MagPack.Messages;
using System;
using System.Collections.Generic;

namespace MsgPack.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ClientPacket packet = new ClientPacket();
            packet.Register(typeof(Employee).Assembly);
            TcpClient client = SocketFactory.CreateTcpClient<TcpClient>(packet, "127.0.0.1", 9090);
            while (true)
            {
                Console.Write("select search category 1.customer  2.employee :");
                string line = Console.ReadLine();
                int category;
                if (int.TryParse(line, out category))
                {
                    if (category == 1)
                    {
                        Console.Write("enter get customer quantity:");
                        line = Console.ReadLine();
                        int quantity;
                        if (int.TryParse(line, out quantity))
                        {
                            SearchCustomer search = new SearchCustomer();
                            search.Quantity = quantity;
                            client.SendMessage(search);
                            var result = client.ReadMessage<IList<Customer>>();
                            foreach (Customer item in result)
                            {
                                Console.WriteLine("\t{0}", item.CompanyName);
                            }
                        }
                        else
                            Console.WriteLine("input not a number!");

                    }
                    else if (category == 2)
                    {

                        Console.Write("enter get employee quantity:");
                        line = Console.ReadLine();
                        int quantity;
                        if (int.TryParse(line, out quantity))
                        {
                            SearchEmployee search = new SearchEmployee();
                            search.Quantity = quantity;
                            client.SendMessage(search);
                            var result = client.ReadMessage<IList<Employee>>();
                            foreach (Employee item in result)
                            {
                                Console.WriteLine("\t{0} {1}", item.FirstName, item.LastName);
                            }
                        }
                        else
                            Console.WriteLine("input not a number!");

                    }
                    else
                        Console.WriteLine("input category error!");

                }
                else
                    Console.WriteLine("input not a number!");


            }
        }
    }
}

using BeetleX;
using BeetleX.EventArgs;
using Json.Messages;
using System;
using System.Collections.Generic;

namespace Json.Server
{
    public class Program : ServerHandlerBase
    {
        private static IServer mServer;

        private static List<Employee> mEmployees;

        private static List<Customer> mCustomers;

        public static void Main(string[] args)
        {
            mEmployees = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Employee>>(Datas.Employees);
            mCustomers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Customer>>(Datas.Customers);

            NetConfig config = new NetConfig();
            mServer = SocketFactory.CreateTcpServer<Program, Messages.Packet>(config);
            mServer.Open();
            while (true)
            {
                Console.Write(mServer);
                System.Threading.Thread.Sleep(1000);
            }
            Console.Read();
        }

        public override void SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            if (e.Message is SearchEmployee)
            {
                OnSearchEmployee((SearchEmployee)e.Message, server, e.Session);
            }
            else if (e.Message is SearchCustomer)
            {
                OnSearchCustomer((SearchCustomer)e.Message, server, e.Session);
            }
        }

        private void OnSearchCustomer(SearchCustomer e, IServer server, ISession session)
        {
            if (e.Quantity > mCustomers.Count)
                e.Quantity = mCustomers.Count;
            List<Customer> result = new List<Customer>();
            for (int i = 0; i < e.Quantity; i++)
                result.Add(mCustomers[i]);
            server.Send(result, session);
        }

        private void OnSearchEmployee(SearchEmployee e, IServer server, ISession session)
        {
            if (e.Quantity > mEmployees.Count)
                e.Quantity = mEmployees.Count;
            List<Employee> result = new List<Employee>();
            for (int i = 0; i < e.Quantity; i++)
                result.Add(mEmployees[i]);
            server.Send(result, session);
        }

        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            base.SessionReceive(server, e);
        }

    }
}

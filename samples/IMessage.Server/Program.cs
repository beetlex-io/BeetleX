using BeetleX;
using BeetleX.EventArgs;
using BeetleX.Packets;
using Messages;
using System;
using System.Collections.Generic;

namespace IMessage.Server
{
    class Program : ServerHandlerBase
    {
        private static IServer mServer;

        private static IList<Employee> mEmployees;

        public static void Main(string[] args)
        {

            DefaultPacket packet = new DefaultPacket();
            packet.Register(typeof(Employee).Assembly);
            mEmployees = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Employee>>(Datas.Employees);
            mServer = SocketFactory.CreateTcpServer(new Program(), packet);
            //server.Options.DefaultListen.CertificateFile = "text.pfx";
            //server.Options.DefaultListen.SSL = true;
            //server.Options.DefaultListen.CertificatePassword = "123456";
            mServer.Open();
            Console.Write(mServer);
            Console.Read();
        }

        public override void SessionPacketDecodeCompleted(IServer server, PacketDecodeCompletedEventArgs e)
        {
            base.SessionPacketDecodeCompleted(server, e);
            List<Employee> items = new List<Employee>();
            SearchEmployee search = (SearchEmployee)e.Message;
            if (search.Quantity > mEmployees.Count)
                search.Quantity = mEmployees.Count;
            for (int i = 0; i < search.Quantity; i++)
            {
                items.Add(mEmployees[i]);
            }
            server.Send(items, e.Session);
        }
    }
}

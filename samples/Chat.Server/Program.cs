using BeetleX;
using BeetleX.EventArgs;
using System;

namespace Chat.Server
{
    class Program : ServerHandlerBase
    {
        private static IServer server;



        public static void Main(string[] args)
        {
            NetConfig config = new NetConfig();
            server = SocketFactory.CreateTcpServer<Program>(config);
            server.Open();
            Console.Write(server);
            Console.Read();
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {

            string line = e.Reader.ReadLine();
            Cmd cmd = ChatParse.Parse(line);
            string result;
            switch (cmd.Type)
            {
                case CmdType.LOGIN:
                    e.Session.Name = cmd.Text;
                    result = ChatParse.CreateCommand(CmdType.LOGIN, cmd.Text + " join chat room");
                    SendToOnlines(result, server);
                    break;
                case CmdType.SPEAK:
                    result = ChatParse.CreateCommand(CmdType.SPEAK, "["+ e.Session.Name + "]" + cmd.Text);
                    SendToOnlines(result, server);
                    break;
            }
            base.SessionReceive(server, e);
        }
        private void SendToOnlines(string cmd, IServer server)
        {
            foreach (ISession item in server.GetOnlines())
            {
                item.NetStream.WriteLine(cmd);
                item.NetStream.Flush();
            }
        }
        public override void Disconnect(IServer server, SessionEventArgs e)
        {
            base.Disconnect(server, e);
            if (!string.IsNullOrEmpty(e.Session.Name))
            {
                string result = ChatParse.CreateCommand(CmdType.QUIT, e.Session.Name + " quit chat room");
                SendToOnlines(result, server);
            }
        }
    }
}

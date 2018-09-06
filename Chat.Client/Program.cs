using BeetleX;
using BeetleX.Clients;
using System;

namespace Chat.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpClient client = SocketFactory.CreateTcpClient<AsyncTcpClient>("127.0.0.1", 9090);
            client.ClientError = (c, e, s) =>
            {
                Write(string.Format("client error {0} {1}\r\n", s, e.Message));
            };


            client.Receive = (o, e) =>
            {
                string line = e.Reader.ReadLine();
                Cmd cmd = ChatParse.Parse(line);
                Console.WriteLine(cmd.Text);
            };
            while (true)
            {
                Write("login enter you name:");
                string line = Console.ReadLine();
                string cmd = ChatParse.CreateCommand(CmdType.LOGIN, line);
                client.Connect();
                client.NetStream.WriteLine(cmd);
                client.NetStream.Flush();
                while (true)
                {
                    line = Console.ReadLine();
                    if (line == "quit")
                    {
                        client.DisConnect();
                        break;
                    }
                    cmd = ChatParse.CreateCommand(CmdType.SPEAK, line);
                    client.NetStream.WriteLine(cmd);
                    client.NetStream.Flush();
                }
            }

        }
        private static void Write(string value)
        {
            lock (typeof(Console))
            {
                Console.Write(value);
            }
        }
    }
}

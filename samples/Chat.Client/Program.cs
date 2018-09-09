using BeetleX;
using BeetleX.Clients;
using System;

namespace Chat.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpClient client = SocketFactory.CreateClient<AsyncTcpClient>("127.0.0.1", 9090);
            //AsyncTcpClient client = SocketFactory.CreateSslClient<AsyncTcpClient>("127.0.0.1", 9090, "localhost");
            client.ClientError = (c, e) =>
            {
                Write(string.Format("client error {0} {1}\r\n", e.Error, e.Message));
            };


            client.Receive = (o, e) =>
            {
                string line = e.Stream.ToPipeStream().ReadLine();
                Cmd cmd = ChatParse.Parse(line);
                Console.WriteLine(cmd.Text);
            };
            while (true)
            {
                Write("login enter you name:");
                string line = Console.ReadLine();
                string cmd = ChatParse.CreateCommand(CmdType.LOGIN, line);
                client.Connect();
                client.Stream.ToPipeStream().WriteLine(cmd);
                client.Stream.Flush();
                while (true)
                {
                    line = Console.ReadLine();
                    if (line == "quit")
                    {
                        client.DisConnect();
                        break;
                    }
                    cmd = ChatParse.CreateCommand(CmdType.SPEAK, line);
                    client.Stream.ToPipeStream().WriteLine(cmd);
                    client.Stream.Flush();
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

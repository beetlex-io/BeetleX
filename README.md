# BeetleX
high performance dotnet core socket tcp communication components support ssl

### Performance testing
Server:E3-1230V2
Bandwidth：10Gb
#### 1K connections
![](https://i.imgur.com/bNT7kI8.png)
#### 5K connections
![](https://i.imgur.com/l3gDs4k.png)
#### 10K connections
![](https://i.imgur.com/dX1U3ti.png)
### Create server
```
    class Program : ServerHandlerBase
    {
        private static IServer server;

        public static void Main(string[] args)
        {
            NetConfig config = new NetConfig();
            //ssl
            //config.SSL = true;
            //config.CertificateFile = @"c:\ssltest.pfx";
            //config.CertificatePassword = "123456";
            server = SocketFactory.CreateTcpServer<Program>(config);
            server.Open();
            Console.Write(server);
            Console.Read();
        }
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            string name = e.Stream.ToPipeStream().ReadLine();
            Console.WriteLine(name);
            e.Session.Stream.ToPipeStream().WriteLine("hello " + name);
            e.Session.Stream.Flush();
            base.SessionReceive(server, e);
        }
    }
```
### Create client
```
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = SocketFactory.CreateClient<TcpClient>("127.0.0.1", 9090);
            //ssl
            //TcpClient client = SocketFactory.CreateSslClient<TcpClient>("127.0.0.1", 9090, "localhost");
            while (true)
            {
                Console.Write("Enter Name:");
                var line = Console.ReadLine();
                client.Stream.ToPipeStream().WriteLine(line);
                client.Stream.Flush();
                var reader = client.Read();
                line = reader.ToPipeStream().ReadLine();
                Console.WriteLine(line);
            }
            Console.WriteLine("Hello World!");
        }
    }
```
### create async client
```
    class Program
    {
        static void Main(string[] args)
        {
            AsyncTcpClient client = SocketFactory.CreateClient<AsyncTcpClient>("127.0.0.1", 9090);
            //SSL
            //AsyncTcpClient client = SocketFactory.CreateSslClient<AsyncTcpClient>("127.0.0.1", 9090, "serviceName");
            client.ClientError = (o, e) =>
            {
                Console.WriteLine("client error {0}@{1}", e.Message, e.Error);
            };
            client.Receive = (o, e) =>
            {
                Console.WriteLine(e.Stream.ToPipeStream().ReadLine());
            };
            var pipestream = client.Stream.ToPipeStream();
            pipestream.WriteLine("hello henry");
            client.Stream.Flush();
            Console.Read();
        }
    }
```



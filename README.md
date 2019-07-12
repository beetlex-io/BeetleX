# BeetleX
high performance dotnet core socket tcp communication components support ssl
## Extended Components
- High performance and lightweight web api server components    
    [https://github.com/IKende/FastHttpApi](https://github.com/IKende/FastHttpApi)
- High performance webapi gateway components  
  [https://github.com/IKende/Bumblebee](https://github.com/IKende/Bumblebee)
- high-performance async/non-blocking  redis client components for dotnet core    
  [https://github.com/IKende/BeetleX.Redis](https://github.com/IKende/BeetleX.Redis)
- dotnet high performance remote interface invoke(RPC) communication components,implemente millions RPS remote interface method calls.
  [https://github.com/IKende/XRPC](https://github.com/IKende/XRPC)

## Nuget
https://www.nuget.org/packages/BeetleX/

## Framework benchmarks last test status
https://tfb-status.techempower.com/

### Performance testing
Server:E3-1230V2
Bandwidth：10Gb
#### 1K connections
![](https://github.com/IKende/BeetleX/blob/master/images/beetlex_1kc.png?raw=true)
#### 5K connections
![](https://github.com/IKende/BeetleX/blob/master/images/beetlex_5kc.png?raw=true)
#### 10K connections
![](https://github.com/IKende/BeetleX/blob/master/images/beetlex_10kc.png?raw=true)
### 50k connections
![](https://github.com/IKende/BeetleX/blob/master/images/beetlex_50kc.png?raw=true)
### 1m connections
![](https://github.com/IKende/BeetleX/blob/master/images/1mconnections.png?raw=true)
### Create server
```csharp
     class Program : ServerHandlerBase
    {
        private static IServer server;

        public static void Main(string[] args)
        {
           
            server = SocketFactory.CreateTcpServer<Program>();
            //server.Options.DefaultListen.CertificateFile = "text.pfx";
            //server.Options.DefaultListen.SSL = true;
            //server.Options.DefaultListen.CertificatePassword = "123456";
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
```csharp
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
```csharp
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



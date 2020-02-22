# BeetleX
high performance dotnet core socket tcp communication components support ssl
## Extended Components
- [High performance and lightweight http and websocket server components](https://github.com/IKende/FastHttpApi)
   
- [High performance http and websocket gateway components](https://github.com/IKende/Bumblebee)

- [High-performance async/non-blocking  redis client components](https://github.com/IKende/BeetleX.Redis)   
  
- [High performance remote interface invoke(RPC) communication components](https://github.com/IKende/XRPC)

- [Http and websocket clients](https://github.com/IKende/HttpClients)
 
## samples
[BeetleX's tcp, http, websocket, xprc ... Samples](https://github.com/IKende/BeetleX-Samples)
### Server
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
            var pipeStream = e.Stream.ToPipeStream();
            string name = pipeStream.ReadLine();
            Console.WriteLine(name);
            pipeStream.WriteLine("hello " + name);
            e.Session.Stream.Flush();
            base.SessionReceive(server, e);
        }
    }
```
### Client
```csharp
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
```
### Async client
```csharp

     AsyncTcpClient client = SocketFactory.CreateClient<AsyncTcpClient>("127.0.0.1", 9090);
     //SSL
     //AsyncTcpClient client = SocketFactory.CreateSslClient<AsyncTcpClient>("127.0.0.1", 9090, "serviceName");
     Client.AutoReceive = false;
     while (true)
     {
         var line = Console.ReadLine();
         var result = await Client.ReceiveFrom(s => s.WriteLine(line));
         //or
         //Client.Send(s => s.WriteLine(line));
         //result = await Client.Receive();
         Console.WriteLine(result.ReadLine());
     }

```

## Framework benchmarks last test status
https://tfb-status.techempower.com/
### 2019-08-01 result for .net
![](https://github.com/IKende/FastHttpApi/blob/master/images/20190801.png?raw=true)
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




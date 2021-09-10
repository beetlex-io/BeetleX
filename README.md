# BeetleX
high performance dotnet core socket tcp communication components,  support TCP, SSL, HTTP, HTTPS, WebSocket, RPC, Redis protocols ... and 1M connections problem solution


<a href="https://www.nuget.org/packages/BeetleX/" target="_blank"> <img src="https://img.shields.io/nuget/vpre/beetlex?label=BeetleX"> 
							  <img src="https://img.shields.io/nuget/dt/BeetleX">
							  </a>
## Extended Components
- [High performance lightweight http and websocket server components](https://github.com/beetlex-io/FastHttpApi)
   
- [High performance http and websocket gateway components](https://github.com/beetlex-io/Bumblebee)

- [High-performance async/non-blocking  redis client components](https://github.com/beetlex-io/BeetleX.Redis)   
  
- [High performance remote interface invoke(RPC) communication components](https://github.com/beetlex-io/XRPC)

- [Http and websocket clients](https://github.com/IKende/HttpClients)
 
## samples
[BeetleX's tcp, http, websocket, xprc ... Samples](https://github.com/beetlex-io/BeetleX-Samples)

## Base server
``` csharp
class Program : ServerHandlerBase
{
    private static IServer server;
    public static void Main(string[] args)
    {
        server = SocketFactory.CreateTcpServer<Program>();
        //server.Options.DefaultListen.Port =9090;
        //server.Options.DefaultListen.Host = "127.0.0.1";
        server.Open();
        Console.Read();
    }
    public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
    {
        var pipeStream = e.Stream.ToPipeStream();
        if (pipeStream.TryReadLine(out string name))
        {
            Console.WriteLine(name);
            e.Session.Stream.ToPipeStream().WriteLine("hello " + name);
            e.Session.Stream.Flush();
        }
        base.SessionReceive(server, e);
    }
}
```
## ssl server
``` csharp
class Program : ServerHandlerBase
{
    private static IServer server;
    public static void Main(string[] args)
    {
        server = SocketFactory.CreateTcpServer<Program, Messages.JsonPacket>();
        server.Options.LogLevel = LogType.Debug;
        server.Options.DefaultListen.SSL = true;
        server.Options.DefaultListen.CertificateFile = @"test.pfx";
        server.Options.DefaultListen.CertificatePassword = "123456";
        //server.Options.DefaultListen.Port =9090;
        //server.Options.DefaultListen.Host = "127.0.0.1";
        server.Open();
        Console.Read();
    }
    protected override void OnReceiveMessage(IServer server, ISession session, object message)
    {
        ((Messages.Register)message).DateTime = DateTime.Now;
        server.Send(message, session);
    }
}
```

## Web Framework Benchmarks
[Round 20](https://www.techempower.com/benchmarks/#section=data-r20&hw=ph&test=composite)
![benchmarks-round20](https://user-images.githubusercontent.com/2564178/107942248-eec41380-6fc5-11eb-94e4-410cadc8ae13.png)


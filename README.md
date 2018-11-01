# BeetleX
beetleX是基于dotnet core实现的轻量级高性能的TCP通讯组件，使用方便、性能高效和安全可靠是组件设计的出发点！开发人员可以在Beetlx组件的支持下快带地构建高性能的TCP通讯服务程序，在安全通讯方面只需要简单地设置一下SSL信息即可实现可靠安全的SSL服务。

### [基于BeetleX实现的官网](http://www.ikende.com)

### [性能比Go iris更出色的WEB服务](https://github.com/IKende/FastHttpApi)


[BeetleX实现单服千万级消息推送](http://www.ikende.com/Doc/1d887337760a47678be21ac9fb443d25.html)

[BeetleX实现单服务百万RPS吞吐](http://www.ikende.com/Doc/1ac8ead7308a485fa2ec6f83349b6b68.html)

### PipeStream
![PipeStream](https://i.imgur.com/16wjO0R.png) 

### 性能
beetleX的性能到底怎样呢，以下简单和DotNetty进行一个网络数据交换的性能测试,分别是1K,5K和10K连接数下数据请求并发测试
#### DotNetty测试代码
```
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            context.WriteAsync(message);
        }
```
#### Beetlex 测试代码
```
        public override void SessionReceive(IServer server, SessionReceiveEventArgs e)
        {
            server.Send(e.Stream.ToPipeStream().GetReadBuffers(), e.Session);
            base.SessionReceive(server, e);
        }
```
### 测试结果
#### 1K connections
![PipeStream](https://i.imgur.com/XlKeV9c.png) 
![PipeStream](https://i.imgur.com/JIaqGPD.png) 
#### 5K connections
![PipeStream](https://i.imgur.com/KzeUtOv.png) 
![PipeStream](https://i.imgur.com/ZBndSS6.png) 
#### 10K connections
![PipeStream](https://i.imgur.com/bc3UMeM.png) 
![PipeStream](https://i.imgur.com/VrffHGR.png) 
### 构建TCP Server
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
### 构建TCP Client
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
### 异步Client
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
### 实现一个Protobuf对象解释器
```
    public class Packet : FixedHeaderPacket
    {
        public Packet()
        {
            TypeHeader = new TypeHandler();
        }

        private PacketDecodeCompletedEventArgs mCompletedEventArgs = new PacketDecodeCompletedEventArgs();

        public void Register(params Assembly[] assemblies)
        {
            TypeHeader.Register(assemblies);
        }

        public IMessageTypeHeader TypeHeader { get; set; }

        public override IPacket Clone()
        {
            Packet result = new Packet();
            result.TypeHeader = TypeHeader;
            return result;
        }

        protected override object OnReader(ISession session, PipeStream reader)
        {
            Type type = TypeHeader.ReadType(reader);
            int bodySize = reader.ReadInt32();
            return reader.Stream.Deserialize(bodySize, type);
        }

        protected override void OnWrite(ISession session, object data, PipeStream writer)
        {
            TypeHeader.WriteType(data, writer);
            MemoryBlockCollection bodysize = writer.Allocate(4);
            int bodyStartlegnth = (int)writer.CacheLength;
            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(writer.Stream, data);
            bodysize.Full((int)writer.CacheLength - bodyStartlegnth);
        }
    }
```


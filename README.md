# BeetleX
beetleX是基于dotnet core实现的轻量级高性能的TCP通讯组件，其高性能和简便的使用方式可以快速低成地构建性能出色的TCP通讯服务程序！其高效和简便之处归功于BeetleX内部一个的PipeStream数据流处理器,PipeStream可以说是MemoryStream,NetworkStream和SocketAsyncEventArgs的一个结合体；它不仅提供了基于Stream标准操作的便利性，并且在读写上直接基于SocketAsyncEventArgs Buffer Pool作为载体，由于基于Buffer Pool作为内存块所以不存在类似于MemoryStream扩容构建内存和复制内存的问题；基于SocketAsyncEventArgs模型网络读写， 因此以在网络操作性能上高于传统的NetworkStream操作。

组件除了PipeStream外，还在其上层提供了一套IBinaryReader和IBinaryWriter读写规范，在这规范的基础之上进行数据协议分析和处理基本可以实现内存数据零拷贝，这对于在编写一些高并发的服务时将大大降低了内存复制的开销让性能更出色。为了更好地支持其他平台的通讯应用，BinaryReader和IBinaryWriter读写规范是支持 Big-Endian和Little-Endian机制

### 以下是PipeStream的结构
![PipeStream](https://github.com/IKende/BeetleX/blob/master/PipeStream.png) 

### 构建TCP Server
```
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
            string name = e.Reader.ReadLine();
            Console.WriteLine(name);
            var w = e.Session.NetStream;
            w.WriteLine("hello " + name);
            w.Flush();
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
            TcpClient client = SocketFactory.CreateTcpClient<TcpClient>("127.0.0.1", 9090);
            while (true)
            {
                Console.Write("Enter Name:");
                var line = Console.ReadLine();
                client.NetStream.WriteLine(line);
                client.NetStream.Flush();
                var reader = client.Read();
                line = reader.ReadLine();
                Console.WriteLine(line);
            }
            Console.WriteLine("Hello World!");
        }
    }
```

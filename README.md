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



## Web Framework Benchmarks
[Round 20](https://www.techempower.com/benchmarks/#section=data-r20&hw=ph&test=composite)
![benchmarks-round20](https://user-images.githubusercontent.com/2564178/107942248-eec41380-6fc5-11eb-94e4-410cadc8ae13.png)

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

## custom packet
``` csharp
    public abstract class FixedHeaderPacket : IPacket
    {

        public FixedHeaderPacket()
        {
            SizeType = FixedSizeType.INT;
        }

        public FixedSizeType SizeType
        { get; set; }

        public EventHandler<PacketDecodeCompletedEventArgs> Completed { get; set; }

        public abstract IPacket Clone();

        private PacketDecodeCompletedEventArgs mCompletedArgs = new PacketDecodeCompletedEventArgs();

        private int mSize;


        protected int CurrentSize => mSize;

        protected abstract object OnRead(ISession session, PipeStream stream);

        public void Decode(ISession session, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
        START:
            object data;
            if (mSize == 0)
            {
                if (SizeType == FixedSizeType.INT)
                {
                    if (pstream.Length < 4)
                        return;
                    mSize = pstream.ReadInt32();
                }
                else
                {
                    if (pstream.Length < 2)
                        return;
                    mSize = pstream.ReadInt16();
                }
            }
            if (pstream.Length < mSize)
                return;
            data = OnRead(session, pstream);
            mSize = 0;
            Completed?.Invoke(this, mCompletedArgs.SetInfo(session, data));
            goto START;

        }


        public virtual
            void Dispose()
        {

        }

        protected abstract void OnWrite(ISession session, object data, PipeStream stream);

        private void OnEncode(ISession session, object data, System.IO.Stream stream)
        {
            PipeStream pstream = stream.ToPipeStream();
            MemoryBlockCollection msgsize;
            if (SizeType == FixedSizeType.INT)
                msgsize = pstream.Allocate(4);
            else
                msgsize = pstream.Allocate(2);
            int length = (int)pstream.CacheLength;
            OnWrite(session, data, pstream);
            if (SizeType == FixedSizeType.INT)
            {
                int len = (int)pstream.CacheLength - length;
                if (!pstream.LittleEndian)
                    len = BitHelper.SwapInt32(len);
                msgsize.Full(len);
            }
            else
            {
                short len = (short)(pstream.CacheLength - length);
                if (!pstream.LittleEndian)
                    len = BitHelper.SwapInt16(len);
                msgsize.Full(len);
            }

        }

        public byte[] Encode(object data, IServer server)
        {
            byte[] result = null;
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                result = new byte[stream.Length];
                stream.Read(result, 0, result.Length);
            }
            return result;
        }

        public ArraySegment<byte> Encode(object data, IServer server, byte[] buffer)
        {
            using (Buffers.PipeStream stream = new PipeStream(server.SendBufferPool.Next(), server.Options.LittleEndian, server.Options.Encoding))
            {
                OnEncode(null, data, stream);
                stream.Position = 0;
                int count = (int)stream.Length;
                stream.Read(buffer, 0, count);
                return new ArraySegment<byte>(buffer, 0, count);
            }
        }

        public void Encode(object data, ISession session, System.IO.Stream stream)
        {
            OnEncode(session, data, stream);
        }
    }
    //json packet
    public class JsonPacket : BeetleX.Packets.FixedHeaderPacket
    {
        static JsonPacket()
        {
            TypeHeader.Register(typeof(JsonClientPacket).Assembly);
        }
        public static BeetleX.Packets.CustomTypeHeader TypeHeader { get; set; } = new BeetleX.Packets.CustomTypeHeader(BeetleX.Packets.MessageIDType.INT);

        public override IPacket Clone()
        {
            return new JsonPacket();
        }

        protected override object OnRead(ISession session, PipeStream stream)
        {
            Type type = TypeHeader.ReadType(stream);
            var size = CurrentSize - 4;
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);
            stream.Read(buffer, 0, size);
            try
            {
                return SpanJson.JsonSerializer.NonGeneric.Utf8.Deserialize(new ReadOnlySpan<byte>(buffer, 0, size), type);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override void OnWrite(ISession session, object data, PipeStream stream)
        {
            TypeHeader.WriteType(data, stream);
            var buffer = SpanJson.JsonSerializer.NonGeneric.Utf8.SerializeToArrayPool(data);
            try
            {
                stream.Write(buffer.Array, buffer.Offset, buffer.Count);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }
    }
```


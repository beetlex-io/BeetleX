namespace BeetleX.Buffers
{
    public static partial class SocketAsyncEventArgsXExtensions
    {
        public static void SetBuffer(this SocketAsyncEventArgsX socketAsyncEventArgsX, IBuffer bufferX, int length)
        {
#if NETSTANDARD2_0
            socketAsyncEventArgsX.SetBuffer(bufferX.Data, bufferX.Postion, length);
#else
            socketAsyncEventArgsX.SetBuffer(bufferX.Memory.Slice(bufferX.Postion, length));
#endif
        }

        public static void SetBuffer(this SocketAsyncEventArgsX socketAsyncEventArgsX, IBuffer bufferX)
        {
#if NETSTANDARD2_0
            socketAsyncEventArgsX.SetBuffer(bufferX.Data, 0, bufferX.Data.Length);
#else
            socketAsyncEventArgsX.SetBuffer(bufferX.Memory);
#endif
        }
    }
}
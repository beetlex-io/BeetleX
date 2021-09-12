using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Clients
{
    public class AwaiterClient : IClientSocketProcessHandler
    {
        static AwaiterClient()
        {

        }

        public AwaiterClient(string host, int port, IClientPacket packet, string sslServiceName = null)
        {
            if (sslServiceName == null)
            {
                mClient = SocketFactory.CreateClient<AsyncTcpClient>(packet, host, port);
            }
            else
            {
                mClient = SocketFactory.CreateSslClient<AsyncTcpClient>(packet, host, port, sslServiceName);
            }
            mClient.ClientError = OnError;
            mClient.SocketProcessHandler = this;
            mClient.PacketReceive = OnPacketReceive;
        }

        public static Dispatchs.DispatchCenter<(AwaiterClient, object)> AwaiterDispatchCenter { get; set; }

        private AsyncTcpClient mClient;

        public AsyncTcpClient Client => mClient;

        public int QueueMaxLength { get; set; } = 1000;

        private Queue<object> mQueues = new Queue<object>();


        private TaskCompletionSource<object> mReceiveCompletionSource;


        private void OnError(IClient c, ClientErrorArgs e)
        {
            lock (mQueues)
            {
                var completed = mReceiveCompletionSource;
                mReceiveCompletionSource = null;
                Task.Run(() => completed?.TrySetException(e.Error));
            }
        }

        private void OnPacketReceive(IClient client, object message)
        {
            lock (mQueues)
            {
                if (mReceiveCompletionSource != null)
                {
                    var completed = mReceiveCompletionSource;
                    mReceiveCompletionSource = null;
                    Task.Run(() => completed?.TrySetResult(message));
                }
                else
                {
                    if (mQueues.Count < QueueMaxLength)
                        mQueues.Enqueue(message);
                }
            }
        }

        public RemoteCertificateValidationCallback CertificateValidationCallback
        {
            get { return mClient?.CertificateValidationCallback; }

            set
            {
                if (mClient != null)
                    mClient.CertificateValidationCallback = value;

            }
        }

        public Func<AwaiterClient, Task> Connected { get; set; }

        public async Task<T> ReceiveFrom<T>(object data, bool autoConnect = true)
        {
            await Send(data);
            return await Receive<T>(autoConnect);
        }

        public async Task<T> Receive<T>(bool autoConnect = true)
        {
            object result = await Receive(autoConnect);
            return (T)result;
        }

        public async Task<object> Receive(bool autoConnect = true)
        {
            TaskCompletionSource<object> resultAwaiter;
            if (autoConnect)
            {
                var result = await mClient.Connect();
                if (result.Connected)
                {
                    if (result.NewConnection)
                    {
                        await Connected?.Invoke(this);

                    }
                }
                else
                {
                    throw mClient.LastError;
                }
            }
            lock (mQueues)
            {

                if (mQueues.Count > 0)
                {
                    return mQueues.Dequeue();
                }
                else
                {
                    mReceiveCompletionSource = new TaskCompletionSource<object>();
                    resultAwaiter = mReceiveCompletionSource;
                }
            }
            if (resultAwaiter != null)
            {
                var msg = await resultAwaiter.Task;
                return msg;
                
            }
            return null;
        }

        public async Task Send(object data)
        {
            var result = await mClient.Connect();
            if (result.Connected)
            {
                if (result.NewConnection)
                {
                    if (Connected != null)
                        await Connected(this);
                }
                await mClient.Send(data);
            }
            else
            {
                throw mClient.LastError;
            }
        }

        public virtual void ReceiveCompleted(IClient client, SocketAsyncEventArgs e)
        {
            EventReceiveCompleted?.Invoke(this, e);
        }

        public virtual void SendCompleted(IClient client, SocketAsyncEventArgs e, bool end)
        {
            EventSendCompleted?.Invoke(this, e, end);
        }

        public Action<AwaiterClient, SocketAsyncEventArgs> EventReceiveCompleted { get; set; }

        public Action<AwaiterClient, SocketAsyncEventArgs, bool> EventSendCompleted { get; set; }
    }
}

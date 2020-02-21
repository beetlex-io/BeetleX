using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Clients
{
    public class AwaiterClient : IAwaitObject
    {
        static AwaiterClient()
        {
            mAwaiterDispatchCenter = new Dispatchs.DispatchCenter<(AwaiterClient, object)>(OnProcess);
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
            mClient.PacketReceive = OnPacketReceive;
        }

        private static Dispatchs.DispatchCenter<(AwaiterClient, object)> mAwaiterDispatchCenter;

        private AsyncTcpClient mClient;

        public AsyncTcpClient Client => mClient;

        public int QueueMaxLength { get; set; } = 1000;

        private Queue<object> mQueues = new Queue<object>();

        private static void OnProcess((AwaiterClient client, object result) item)
        {
            item.client.Success(item.result);
        }

        private void OnError(IClient c, ClientErrorArgs e)
        {
            lock (mQueues)
            {
                if (Pending)
                {
                    Pending = false;
                    mAwaiterDispatchCenter.Enqueue((this, new Exception(e.Message, e.Error)));
                }
            }
        }

        private void OnPacketReceive(IClient client, object message)
        {
            lock (mQueues)
            {
                if (Pending)
                {
                    Pending = false;
                    mAwaiterDispatchCenter.Enqueue((this, message));
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

        public IAwaitObject Receive(bool autoConnect = true)
        {
            if (autoConnect)
            {
                bool isnew = false;
                if (mClient.Connect(out isnew))
                {
                    if (isnew)
                    {
                        var task = Connected?.Invoke(this);
                        task?.Wait();
                    }
                }
                else
                {
                    throw mClient.LastError;
                }
            }
            lock (mQueues)
            {
                if (Pending)
                {
                    throw new BXException($"Awaiter client on pending!");
                }
                BeginReceive();
                if (mQueues.Count > 0)
                {
                    Pending = false;
                    Success(mQueues.Dequeue());
                }
                return this;
            }
        }

        public async void Send(object data)
        {
            bool isnew = false;
            if (mClient.Connect(out isnew))
            {
                if (isnew)
                {
                    if (Connected != null)
                        await Connected(this);
                }
                mClient.Send(data);
            }
            else
            {
                throw mClient.LastError;
            }
        }

        #region awaiter

        private static readonly Action _callbackCompleted = () => { };

        private Action _callback;

        private Object mResult;

        private void BeginReceive()
        {
            mResult = null;
            _callback = null;
            Pending = true;

        }

        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public object Result
        {
            get
            {
                if (mResult is Exception error)
                    throw error;
                return mResult;
            }
        }

        public IAwaitObject GetAwaiter()
        {
            return this;
        }

        public object GetResult()
        {
            return Result;
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                continuation();
            }
        }

        public void Success(object data)
        {
            mResult = data;
            var action = Interlocked.Exchange(ref _callback, _callbackCompleted);
            if (action != null && action != _callbackCompleted)
            {
                action();
            }
        }

        public void Error(Exception error)
        {
            Success(error);
        }

        public bool Pending { get; set; } = false;

        #endregion
    }
}

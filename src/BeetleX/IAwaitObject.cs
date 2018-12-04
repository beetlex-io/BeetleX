using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace BeetleX
{
    public interface IAwaitCompletion : INotifyCompletion
    {

        object Result { get; }

        bool Pending { get; set; }

        bool IsCompleted { get; }

        void Success(object data);

        void Error(Exception error);

    }

    public interface IAwaitObject : IAwaitCompletion
    {

        IAwaitObject GetAwaiter();

        object GetResult();

    }

    public struct AwaitStruct<T> : INotifyCompletion
    {
        public AwaitStruct(IAwaitObject awaitCompletion)
        {
            AwaitCompletion = awaitCompletion;
        }

        public bool IsCompleted => AwaitCompletion.IsCompleted;

        public IAwaitObject AwaitCompletion { get; set; }

        public void OnCompleted(Action continuation)
        {
            AwaitCompletion.OnCompleted(continuation);
        }

        public AwaitStruct<T> GetAwaiter()
        {
            return this;
        }

        public T GetResult()
        {
            object data = AwaitCompletion.GetResult();
            if (data is Exception error)
                throw error;
            return (T)data;
        }
    }

    public class AwaitObject : IAwaitObject
    {
        public AwaitObject()
        {
            mResult = null;
            Pending = false;
            _callback = null;
        }

        private static readonly Action _callbackCompleted = () => { };

        private Action _callback;

        public bool Pending { get; set; }

        private int mCompletedStatus = 0;

        private Object mResult;

        public void Reset()
        {
            mResult = null;
            Pending = true;
            mCompletedStatus = 0;
            _callback = null;
        }

        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public object Result => mResult;

        public IAwaitObject GetAwaiter()
        {
            return this;
        }

        public virtual object GetResult()
        {
            return Result;
        }

        public Action<IAwaitObject> Executed { get; set; }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                try
                {
                    Pending = false;
                    Interlocked.Exchange(ref mCompletedStatus, 1);
                    continuation();
                }
                finally
                {
                    Executed?.Invoke(this);
                }
            }
        }

        public void Success(object data)
        {
            mResult = data;
            var action = Interlocked.Exchange(ref _callback, _callbackCompleted);
            Pending = false;
            if (action != null)
            {
                if (Interlocked.Exchange(ref mCompletedStatus, 1) == 0)
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        Executed?.Invoke(this);
                    }
                }

            }
        }

        public void Error(Exception error)
        {
            Success(error);
        }
    }
}

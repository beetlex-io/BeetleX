using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.Dispatchs
{
    class DispatchCenter<T> : IDisposable
    {
        List<object> mDispatchers = new List<object>();

        long mIndex = 1;

        public DispatchCenter(Action<T> process, int count)
        {
            for (int i = 0; i < count; i++)
            {
                mDispatchers.Add(new Dispatcher<T>(process));
            }

        }

        public Dispatcher<T> Next()
        {
            return (Dispatcher<T>)mDispatchers[(int)(System.Threading.Interlocked.Increment(ref mIndex) % mDispatchers.Count)];

        }

        public void Start()
        {
            foreach (Dispatcher<T> item in mDispatchers)
            {
                item.Start();
            }
        }

        public void Dispose()
        {
            foreach (Dispatcher<T> item in mDispatchers)
            {
                item.Dispose();
            }
            mDispatchers.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.Dispatchs
{
    public class DispatchCenter<T> : IDisposable
    {
        List<SingleThreadDispatcher<T>> mDispatchers = new List<SingleThreadDispatcher<T>>();

        long mIndex = 1;

        public DispatchCenter(Action<T> process, int count)
        {
            for (int i = 0; i < count; i++)
            {
                mDispatchers.Add(new SingleThreadDispatcher<T>(process));
            }
        }


        public SingleThreadDispatcher<T> Get(object data)
        {
            int id = Math.Abs(data.GetHashCode());
            return mDispatchers[id % mDispatchers.Count];
        }

        public SingleThreadDispatcher<T> Next()
        {
            return mDispatchers[(int)(System.Threading.Interlocked.Increment(ref mIndex) % mDispatchers.Count)];
        }

        public void Dispose()
        {
            foreach (SingleThreadDispatcher<T> item in mDispatchers)
            {
                item.Dispose();
            }
            mDispatchers.Clear();
        }
    }
}

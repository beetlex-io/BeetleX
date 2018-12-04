using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Blocks
{
    public interface IAwaitMessage<IDType>
    {
        IDType ID { get; set; }

        void Success(object data);

        void Error(Exception error);

        long TimeOut { get; set; }


    }
    public class AwaitMessage<IDType, T> : System.Threading.Tasks.TaskCompletionSource<T>, IAwaitMessage<IDType>
    {
        public AwaitMessage(IDType id)
        {
            ID = id;
        }

        public IDType ID { get; set; }

        public long TimeOut { get; set; }

        public void Error(Exception error)
        {
            this.SetException(error);
        }

        public void Success(object data)
        {
            this.SetResult((T)data);
        }
    }
}

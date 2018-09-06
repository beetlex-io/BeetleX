using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    public interface ILRUDetector
    {

        void Update(IDetectorItem item);

        void Detection(int timeout);

        IServer Server { get; }

        Action<IList<IDetectorItem>> Timeout { get; set; }

    }



    public interface IDetectorItem
    {
        double ActiveTime
        { get; set; }

        LinkedListNode<IDetectorItem> DetectorNode
        {
            get;
            set;
        }
    }


    class LRUDetector : ILRUDetector, IDisposable
    {

        public LRUDetector()
        {

        }

        private LinkedList<IDetectorItem> mItems = new LinkedList<IDetectorItem>();

        public IServer Server
        {
            get; internal set;
        }

        public Action<IList<IDetectorItem>> Timeout
        {
            get; set;
        }

        public void Detection(int timeout)
        {
            double time = Server.GetRunTime();
            List<IDetectorItem> result = new List<IDetectorItem>();
            lock (this)
            {
                LinkedListNode<IDetectorItem> last = mItems.Last;
                while (last != null && (time - last.Value.ActiveTime) > timeout)
                {
                    mItems.Remove(last);
                    result.Add(last.Value);
                    last.Value.DetectorNode = null;
                    last = mItems.Last;
                }
            }
            if (Timeout != null && result.Count > 0)
                Timeout(result);
        }

        public void Update(IDetectorItem item)
        {
            lock (this)
            {
                if (item.DetectorNode == null)
                    item.DetectorNode = new LinkedListNode<IDetectorItem>(item);
                item.ActiveTime = Server.GetRunTime();
                if (item.DetectorNode.List == mItems)
                    mItems.Remove(item.DetectorNode);
                mItems.AddFirst(item);
            }
        }

        public void Dispose()
        {
            mItems.Clear();
        }
    }


}

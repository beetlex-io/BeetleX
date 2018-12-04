using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX
{
    //public interface ILRUDetector
    //{
    //    void Update(IDetector item);

    //    void Detection(int timeout);

    //    double GetTime();

    //    Action<IList<IDetector>> Timeout { get; set; }
    //}

    //public interface IDetector
    //{
       
    //    LinkedListNode<IDetector> DetectorNode
    //    {
    //        get;
    //        set;
    //    }
    //}


    //class LRUDetector : ILRUDetector, IDisposable
    //{
    //    public LRUDetector()
    //    {
    //        mTimeWatch = new System.Diagnostics.Stopwatch();
    //        mTimeWatch.Restart();
    //    }

    //    private Buffers.XSpinLock xSpinLock = new Buffers.XSpinLock();

    //    private System.Diagnostics.Stopwatch mTimeWatch;

    //    private LinkedList<IDetector> mItems = new LinkedList<IDetector>();

    //    public Action<IList<IDetector>> Timeout
    //    {
    //        get; set;
    //    }

    //    public void Detection(int timeout)
    //    {
    //        double time = GetTime();
    //        List<IDetector> result = new List<IDetector>();
    //        using (xSpinLock.Enter())
    //        {
    //            LinkedListNode<IDetector> last = mItems.Last;
    //            while (last != null && (time - last.Value.ActiveTime) > timeout)
    //            {
    //                mItems.Remove(last);
    //                result.Add(last.Value);
    //                last.Value.DetectorNode = null;
    //                last = mItems.Last;
    //            }
    //        }
    //        if (Timeout != null && result.Count > 0)
    //            Timeout(result);
    //    }

    //    public void Update(IDetector item)
    //    {
    //        using (xSpinLock.Enter())
    //        {
    //            item.ActiveTime = GetTime();
    //            if (item.DetectorNode == null)
    //            {
    //                item.DetectorNode = mItems.AddFirst(item);  //new LinkedListNode<IDetector>(item);
    //            }
    //            else
    //            {
    //                mItems.Remove(item.DetectorNode);
    //                mItems.AddFirst(item.DetectorNode);
    //            }
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        mItems.Clear();
    //    }

    //    public double GetTime()
    //    {
    //        return mTimeWatch.Elapsed.TotalMilliseconds;
    //    }
    //}


}

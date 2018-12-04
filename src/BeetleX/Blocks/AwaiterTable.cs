using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Blocks
{
    class AwaiterTable<IDType> : IDisposable
    {

        private LinkedList<IAwaitMessage<IDType>> mAwaitMessageLRU = new LinkedList<IAwaitMessage<IDType>>();

        private System.Collections.Concurrent.ConcurrentDictionary<IDType, LinkedListNode<IAwaitMessage<IDType>>> mAwaitMessages
            = new System.Collections.Concurrent.ConcurrentDictionary<IDType, LinkedListNode<IAwaitMessage<IDType>>>();

        private Buffers.XSpinLock mLock = new Buffers.XSpinLock();

        public IList<IAwaitMessage<IDType>> TimeOut(long time)
        {
            List<IAwaitMessage<IDType>> result = new List<IAwaitMessage<IDType>>();
            using (mLock.Enter())
            {
                LinkedListNode<IAwaitMessage<IDType>> last = mAwaitMessageLRU.Last;
                while (last != null && time > last.Value.TimeOut)
                {
                    result.Add(last.Value);
                    last = last.Previous;
                }
            }
            return result;
        }

        public void Add(IAwaitMessage<IDType> item)
        {
            using (mLock.Enter())
            {
                LinkedListNode<IAwaitMessage<IDType>> listNode = new LinkedListNode<IAwaitMessage<IDType>>(item);
                mAwaitMessageLRU.AddFirst(listNode);
                mAwaitMessages[item.ID] = listNode;
            }
        }

        public void Dispose()
        {
            using (mLock.Enter())
            {
                mAwaitMessageLRU.Clear();
                mAwaitMessages.Clear();
            }
        }

        public IAwaitMessage<IDType> GetAwait(IDType id)
        {
            using (mLock.Enter())
            {
                if (mAwaitMessages.TryRemove(id, out LinkedListNode<IAwaitMessage<IDType>> item))
                {
                    mAwaitMessageLRU.Remove(item);
                    return item.Value;
                }
                return null;
            }
        }

        public IAwaitMessage<IDType> GetAwait(IUniqueMessage<IDType> message)
        {
            return GetAwait(message._UniqueID);
        }
    }

    class AwaiterManager<IDType>
    {
        public AwaiterManager()
        {
            for (int i = 0; i < Math.Min(System.Environment.ProcessorCount, 16); i++)
            {
                awaiterTables.Add(new AwaiterTable<IDType>());
            }
        }

        private List<AwaiterTable<IDType>> awaiterTables = new List<AwaiterTable<IDType>>();

        public List<AwaiterTable<IDType>> AwaiterTables => awaiterTables;

        public void Add(IAwaitMessage<IDType> item)
        {
            awaiterTables[item.ID.GetHashCode() % awaiterTables.Count].Add(item);
        }

        public IAwaitMessage<IDType> Get(IDType id)
        {
            return awaiterTables[id.GetHashCode() % awaiterTables.Count].GetAwait(id);
        }
    }
}

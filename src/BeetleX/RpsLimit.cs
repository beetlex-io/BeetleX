using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX
{
    public class RpsLimit
    {
        public RpsLimit(int max)
        {
            mMax = max;
        }

        private int mMax;

        private long mRpsCount;

        private long mLastRpsTime;

        public void SetMaxRpx(int value)
        {
            this.mMax = value;
        }

        public bool Check(int max = 0)
        {
            if (max > 0)
                mMax = max;
            if (mMax <= 0)
                return false;
            else
            {
                mRpsCount = System.Threading.Interlocked.Increment(ref mRpsCount);
                long now = TimeWatch.GetElapsedMilliseconds();
                long time = now - mLastRpsTime;
                if (time >= 1000)
                {
                    System.Threading.Interlocked.Exchange(ref mRpsCount, 0);
                    System.Threading.Interlocked.Exchange(ref mLastRpsTime, now);
                }
                else
                {
                    if (mRpsCount > mMax)
                        return true;
                }
            }
            return false;
        }
    }

    public class RpsCounter
    {

        public RpsCounter()
        {
            mLastTime = TimeWatch.GetTotalSeconds();
        }

        private long mLastCount;

        private double mLastTime;

        public Value Next(long count)
        {
            double now = TimeWatch.GetTotalSeconds();
            double time = now - mLastTime;
            Value result = new Value();
            result.RPS = (long)((count - mLastCount) / time);
            result.Count = count;
            mLastCount = count;
            mLastTime = now;
            return result;
        }

        public struct Value
        {
            public long Count { get; set; }

            public long RPS { get; set; }
        }

    }
}

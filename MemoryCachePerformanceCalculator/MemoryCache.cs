using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryCachePerformanceCalculator
{
    abstract class MemoryCacheSimulator
    {
        public int BitSize { get; protected set; }
        public int BytesPerBlock { get; protected set; }
        public int NumberOfRows { get; protected set; }

        public abstract bool getMem(int address);
        public abstract string getCacheStatusAsCsv(bool verbose = false);

        public int getMemAccessTime(int address)
        {
            return (getMem(address)) ? getHitTime() : getMissTime();
        }
        public int getHitTime()
        {
            return 1;
        }
        public int getMissTime()
        {
            return 18 + (BytesPerBlock * 3);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryCachePerformanceCalculator
{
    class FullyAssociativeCacheSimulator : SetAssociativeSetCacheSimulator
    {
        public FullyAssociativeCacheSimulator(int bytesPerBlock, int maxCacheBitSize) : base(maxCacheBitSize: maxCacheBitSize, numberOfRows: 1, bytesPerBlock: bytesPerBlock)
        {

        }

        public override string getCacheStatusAsCsv(bool verbose = false)
        {
            string csv =
                "Fully Associated Cache\n" +
                "Size (bits), " + BitSize + "\n" +
                "Block Size (bits), " + (BytesPerBlock * 8) + "\n" +
                "# of Rows, " + SetsPerRow + "\n" +
                "Hit Time (cycles), " + this.getHitTime() + "\n" +
                "Miss Time (cycles), " + this.getMissTime() + "\n";

            if (!verbose) { return csv; }

            csv +=
                "\n" +
                "Tag #, Offset, LRU, Valid\n";

            int rowNumber = 0;
            int binTagLength = AddressBitSize - floorLog2(BytesPerBlock);
            int binOffsetLength = floorLog2(BytesPerBlock);
            int binLruLength = ceilLog2(SetsPerRow);

            string offset = Enumerable.Repeat("X", binOffsetLength).Aggregate((a, b) => a + b);
            foreach (CacheRow row in Cache)
            {

                int[] arrayCacheSet = row.CacheSet.ToArray();
                arrayCacheSet.Reverse(); //LRU Will Be Be Bigger The More Recently It Was Accesed
                for (int i = 0; i < SetsPerRow; i++)
                {
                    string lru = toBin(SetsPerRow - (1 + i), binLruLength);

                    string tag;
                    string valid;
                    if (i < arrayCacheSet.Length)
                    {
                        tag = toBin(arrayCacheSet[i], binTagLength);
                        valid = "1";
                    }
                    else
                    {
                        lru = Enumerable.Repeat("-", binLruLength).Aggregate((a, b) => a + b);
                        tag = Enumerable.Repeat("-", binTagLength).Aggregate((a, b) => a + b);
                        valid = "0";
                    }

                    csv += tag + ", " + offset + ", " + lru + ", " + valid + "\n";
                }
                rowNumber++;
            }

            return csv;
        }
    }
}

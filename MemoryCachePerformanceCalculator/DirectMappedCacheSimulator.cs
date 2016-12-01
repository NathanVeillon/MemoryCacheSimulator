using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryCachePerformanceCalculator
{
    class DirectMappedCacheSimulator : SetAssociativeSetCacheSimulator
    {
        public DirectMappedCacheSimulator(int bytesPerBlock, int maxCacheBitSize) : base(maxCacheBitSize: maxCacheBitSize, setsPerRow: 1, bytesPerBlock: bytesPerBlock)
        {

        }

        public override string getCacheStatusAsCsv(bool verbose = false)
        {
            string csv =
                "Direct Mapped Cache\n" +
                "Size (bits), " + BitSize + "\n" +
                "Block Size (bits), " + (BytesPerBlock * 8) + "\n" +
                "# of Rows, " + NumberOfRows + "\n" +
                "Hit Time (cycles), " + this.getHitTime() + "\n" +
                "Miss Time (cycles), " + this.getMissTime() + "\n";

            if (!verbose) { return csv; }

            csv +=
                "\n" +
                "Tag #, Row #, Offset, Valid\n";

            int rowNumber = 0;
            int binTagLength = AddressBitSize - floorLog2(NumberOfRows) - floorLog2(BytesPerBlock);
            int binRowLength = floorLog2(Cache.Length);
            int binOffsetLength = floorLog2(BytesPerBlock);
            foreach (CacheRow row in Cache)
            {
                string rowText = (Cache.Length == 1) ? "-" : toBin(rowNumber, binRowLength);

                string offset = Enumerable.Repeat("X", binOffsetLength).Aggregate((a, b) => a + b);

                int[] arrayCacheSet = row.CacheSet.ToArray();
                for (int i = 0; i < 1; i++)
                {
                    string tag;
                    string valid;
                    if (i < arrayCacheSet.Length)
                    {
                        tag = toBin(arrayCacheSet[i], binTagLength);
                        valid = "1";
                    }
                    else
                    {
                        tag = Enumerable.Repeat("-", binTagLength).Aggregate((a, b) => a + b);
                        valid = "0";
                    }

                    csv += tag + ", " + rowText + ", " + offset + ", " + valid + "\n";
                }
                rowNumber++;
            }

            return csv;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryCachePerformanceCalculator
{
    class SetAssociativeSetCacheSimulator : MemoryCacheSimulator
    {
        protected int AddressBitSize;
        protected int SetsPerRow;

        protected CacheRow[] Cache;

        protected struct CacheRow
        {
            private Queue<int> _CacheSet;
            public Queue<int> CacheSet
            {
                get { if (_CacheSet == null) { _CacheSet = new Queue<int>(); } return _CacheSet; }

                set { _CacheSet = value; }
            }
        }

        public SetAssociativeSetCacheSimulator(int maxCacheBitSize, int setsPerRow, int bytesPerBlock)
        {
            AddressBitSize = 16;
            BytesPerBlock = bytesPerBlock;
            SetsPerRow = setsPerRow;
            NumberOfRows = calculateMaximumNumberOfRows(maxCacheBitSize, setsPerRow, bytesPerBlock);

            BitSize = calculateBitSize(NumberOfRows, SetsPerRow, BytesPerBlock);

            Cache = new CacheRow[NumberOfRows];
        }

        public SetAssociativeSetCacheSimulator(int bytesPerBlock = -1, int numberOfRows = -1, int setsPerRow = -1, int maxCacheBitSize = -1)
        {
            int[] theFeilds = { maxCacheBitSize, numberOfRows, setsPerRow, bytesPerBlock };

            if (!confirmExactlyOneNonPositiveFeild(theFeilds))
            {
                throw new Exception("Exactly One Paramater Must Be Calculated, All Other Feilds Must Be Positive");
            }

            AddressBitSize = 16;
            BytesPerBlock = bytesPerBlock;
            NumberOfRows = numberOfRows;
            SetsPerRow = setsPerRow;

            if (bytesPerBlock <= 0)
            {
                throw new Exception("Calculating Maximum Bytes Per Block Is Not Yet Implemented");
            }

            if (numberOfRows <= 0)
            {
                NumberOfRows = calculateMaximumNumberOfRows(maxCacheBitSize, setsPerRow, bytesPerBlock);
            }

            if (setsPerRow <= 0)
            {
                SetsPerRow = calculateMaximumNumberOfSetsPerRow(maxCacheBitSize, numberOfRows, bytesPerBlock);
            }

            BitSize = calculateBitSize(NumberOfRows, SetsPerRow, BytesPerBlock);

            Cache = new CacheRow[NumberOfRows];
        }

        private bool confirmExactlyOneNonPositiveFeild(int[] feilds)
        {
            int numberOfNonPositiveFeilds = 0;

            foreach(int feild in feilds)
            {
                if(feild <= 0) { numberOfNonPositiveFeilds++; }
            }

            return numberOfNonPositiveFeilds == 1;
        }

        public override string getCacheStatusAsCsv(bool verbose = false)
        {
            string csv =
                SetsPerRow + "-Way Set Associative Cache\n" +
                "Size (bits), " + BitSize + "\n" +
                "Block Size (bits), " + (BytesPerBlock * 8) + "\n" +
                "# of Rows, " + NumberOfRows + "\n" +
                "Tags Per Row, " + SetsPerRow + "\n" +
                "Hit Time (cycles), " + this.getHitTime() + "\n" +
                "Miss Time (cycles), " + this.getMissTime() + "\n";

            if (!verbose) { return csv; }

            csv +=
                "\n" +
                "Tag #, Row #, Offset, LRU, Valid\n";

            int rowNumber = 0;
            int binTagLength = AddressBitSize - floorLog2(NumberOfRows) - floorLog2(BytesPerBlock);
            int binRowLength = floorLog2(Cache.Length);
            int binOffsetLength = floorLog2(BytesPerBlock);
            int binLruLength = ceilLog2(SetsPerRow);
            foreach (CacheRow row in Cache)
            {
                string rowText = (Cache.Length == 1) ? "-" : toBin(rowNumber, binRowLength);

                string offset = Enumerable.Repeat("X", binOffsetLength).Aggregate((a, b) => a + b);

                int[] arrayCacheSet = row.CacheSet.ToArray();
                arrayCacheSet.Reverse(); //LRU Will Be Be Bigger The More Recently It Was Accesed
                for (int i = 0; i < SetsPerRow; i++)
                {
                    string lru = toBin(SetsPerRow - (1 + i), binLruLength);
                    
                    string tag;
                    string valid;
                    if ( i < arrayCacheSet.Length)
                    {
                        tag = toBin(arrayCacheSet[i], binTagLength);
                        valid = "1";
                    }
                    else
                    {
                        lru = Enumerable.Repeat("-", Math.Max(binLruLength, 1)).Aggregate((a, b) => a + b);
                        tag = Enumerable.Repeat("-", binTagLength).Aggregate((a, b) => a + b);
                        valid = "0";
                    }

                    csv += tag + ", " + rowText + ", " + offset + ", " + lru + ", " + valid + "\n";
                }
                rowNumber++;
            }

            return csv;
        }

        public override bool getMem(int address)
        {
            int rowNumber = (address / (BytesPerBlock)) % NumberOfRows;
            int tag = address / (BytesPerBlock * NumberOfRows);
            
            CacheRow possibleCacheRow = Cache[rowNumber];
             
            bool isCacheHit = tagIsInCacheRow(tag, possibleCacheRow);

            addAddressToCache(address);
            
            string stringAddress = toBin(address, 16);
            string stringRowNumber = toBin(rowNumber, ceilLog2(NumberOfRows));
            string stringTag = toBin(tag, 16 - ceilLog2(NumberOfRows) - ceilLog2(BytesPerBlock));

            return isCacheHit;
        }

        private bool tagIsInCacheRow(int tag, CacheRow row)
        {
            foreach (int cacheTag in row.CacheSet)
            {
                if (tag == cacheTag)
                {
                    return true;
                }
            }

            return false;
        }

        private void addAddressToCache(int address)
        {
            int rowNumber = (address / (BytesPerBlock)) % NumberOfRows;
            int tag = address / (BytesPerBlock * NumberOfRows);

            CacheRow row = Cache[rowNumber];

            //Removes The Tag From The Queue If It Is There So It Can Be At The End Of The Queue;
            for (int i = 0; i < row.CacheSet.Count; i++)
            {
                int cacheTag = row.CacheSet.Dequeue();
                if(cacheTag == tag) { continue; }
                row.CacheSet.Enqueue(cacheTag);
            }

            // Forgets About The Least Used Tag From The Cache Set
            if (row.CacheSet.Count == SetsPerRow)
            {
                row.CacheSet.Dequeue();
            }

            // Adds Tag To The Cache Set As The Last Used Item
            row.CacheSet.Enqueue(tag);

            Cache[rowNumber] = row;
        }
        
        private int calculateBitSize(int numberOfRows, int setsPerRow, int bytesPerBlock)
        {
            bool includeLru = setsPerRow != 1; // Don't Use Lru If Only 1 Set in Each Row
            int lruBits = includeLru ? ceilLog2(setsPerRow) : 0;

            int bitsPerRow = setsPerRow * (AddressBitSize - floorLog2(numberOfRows) - floorLog2(bytesPerBlock) + 1 + ((bytesPerBlock * 8) + lruBits));
            return numberOfRows * bitsPerRow;
        }

        private int calculateMaximumNumberOfRows(int maxCacheBitSize, int setsPerRow, int bytesPerBlock)
        {
            bool includeLru = setsPerRow != 1; // Don't Use Lru If Only 1 Set in Each Row
            int aproxRowBitSize = (includeLru) ? setsPerRow*(1 + (bytesPerBlock * 8) + ceilLog2(setsPerRow)) : (bytesPerBlock * 8);
            // Gets A Bigger Aprox Row Amount Then Possible
            int aproxRowsAmount = maxCacheBitSize / aproxRowBitSize;

            int bigestPossibleRowAmount = shrinkRowSizeUntilItFitsInMaxBitSize(maxCacheBitSize, aproxRowsAmount, setsPerRow, bytesPerBlock);

            //Make Sure That The Number Of Rows Is A Power Of Two;
            return 1 << floorLog2(bigestPossibleRowAmount); // Equivalent to 2^floorLog2(aproxRowsAmount)
        }
        
        private int calculateMaximumNumberOfSetsPerRow(int maxCacheBitSize, int numberOfRows, int bytesPerBlock)
        {
            int aproxSetBitSize = (AddressBitSize + 1 + (bytesPerBlock * 8) - floorLog2(numberOfRows) - floorLog2(bytesPerBlock));
            int aproxSetsPerRow = maxCacheBitSize / (aproxSetBitSize * numberOfRows);

            int biggestPossibleSetsPerRow = shrinkSetsPerRowUntilItFitsInMaxBitSize(maxCacheBitSize, numberOfRows, aproxSetsPerRow, bytesPerBlock);

            return biggestPossibleSetsPerRow;
        }

        private int shrinkRowSizeUntilItFitsInMaxBitSize(int maxCacheBitSize, int aproxRowSize, int setSize, int blockByteSize)
        {

            if (aproxRowSize <= 0)
            {
                throw new Exception("There Is No Row Size That Will Be Less Or Equal Too The Max Bit Size");
            }

            int bitSize = calculateBitSize(aproxRowSize, setSize, blockByteSize);

            if (bitSize > maxCacheBitSize)
            {
                return shrinkRowSizeUntilItFitsInMaxBitSize(maxCacheBitSize, aproxRowSize - 1, setSize, blockByteSize);
            }

            return aproxRowSize;
        }

        private int shrinkSetsPerRowUntilItFitsInMaxBitSize(int maxCacheBitSize, int numberOfRows, int aproxSetsPerRow, int blockByteSize)
        {

            if (aproxSetsPerRow <= 0)
            {
                throw new Exception("There Is No Tags Per Row That Will Be Less Or Equal Too The Max Bit Size");
            }

            int bitSize = calculateBitSize(numberOfRows, aproxSetsPerRow, blockByteSize);

            if (bitSize > maxCacheBitSize)
            {
                return shrinkSetsPerRowUntilItFitsInMaxBitSize(maxCacheBitSize, numberOfRows, aproxSetsPerRow - 1, blockByteSize);
            }

            return aproxSetsPerRow;
        }


        protected static int floorLog2(int num)
        {
            int logFloor = -1;
            while (num > 0)
            {
                num = num / 2;
                logFloor++;
            }

            return logFloor;
        }

        protected static int ceilLog2(int num)
        {
            if (num <= 0) { return -1; }

            int logFloor = floorLog2(num);

            bool numIsPowerOf2 = (num & (num - 1)) == 0;
            return (numIsPowerOf2) ? logFloor : logFloor + 1;
        }

        protected static string toBin(int value, int len)
        {
            return (len > 1 ? toBin(value >> 1, len - 1) : null) + "01"[value & 1];
        }
    }
}

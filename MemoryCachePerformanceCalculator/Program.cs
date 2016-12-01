using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryCachePerformanceCalculator
{
    class Program
    {
        struct CacheResult
        {
            public MemoryCacheSimulator MemCache { get; private set; }
            public int CycleUse { get; private set; }
            public int AmountOfMemoryCalls { get; private set; }
            public Dictionary<int, bool> HitMissRecord { get; private set; }


            public CacheResult(MemoryCacheSimulator memCache, int cycleUse, Dictionary<int, bool> hitMissRecord)
            {
                MemCache = memCache;
                CycleUse = cycleUse;
                AmountOfMemoryCalls = hitMissRecord.Count;
                HitMissRecord = hitMissRecord;
            }

            public string ToCsvString(bool verbose)
            {
                string results =
                    "Cycles Taken, " + CycleUse + "\n" +
                    "Average CPI, " + (double)CycleUse / (double)AmountOfMemoryCalls + "\n"+
                    MemCache.getCacheStatusAsCsv(verbose);

                if (!verbose) { return results; }

                results += 
                    "\n" + 
                    getInstructionHitsAndMissesCsv(HitMissRecord);
                    
                return results;
            }

        }

        static void Main(string[] args)
        {
            int[] addresses = { 4, 8, 20, 24, 28, 36, 44, 20, 24, 28, 36, 40, 44, 68, 72, 92, 96, 100, 104, 108, 112, 100, 112, 116, 120, 128, 140 };
            List<CacheResult> bestDirectMappedCaches = simulateDirectMappedCaches(addresses, 800, 64);
            List<CacheResult> bestSetAssociativeCaches = simulateSetAssociativeCaches(addresses, 800, 16, 64);
            List<CacheResult> bestFullyAssociativeCaches = simulateFullyAssociativeCaches(addresses, 800, 64);

            Console.WriteLine("#####################################");
            Console.WriteLine("##   Best Direct Mapped Cache(s)   ##");
            Console.WriteLine("#####################################");
            foreach(CacheResult directResult in bestDirectMappedCaches)
            {
                Console.WriteLine(directResult.ToCsvString(true));
            }

            Console.WriteLine("#####################################");
            Console.WriteLine("##  Best Set Associative Cache(s)  ##");
            Console.WriteLine("#####################################");
            foreach (CacheResult setAssocResult in bestFullyAssociativeCaches)
            {
                Console.WriteLine(setAssocResult.ToCsvString(true));
            }

            Console.WriteLine("#####################################");
            Console.WriteLine("## Best Fully Associative Cache(s) ##");
            Console.WriteLine("#####################################");
            foreach (CacheResult fullyAssocResult in bestFullyAssociativeCaches)
            {
                Console.WriteLine(fullyAssocResult.ToCsvString(true));
            }

            Console.ReadLine();
        }

        static List<CacheResult> simulateDirectMappedCaches(int[] addresses, int maxSize, int maxBlockSize)
        {
            List<CacheResult> bestCacheResults = new List<CacheResult>();

            Dictionary<int, bool> cacheHitsMissRecord = new Dictionary<int, bool>(addresses.Length);

            for (int blockSize = 4; blockSize <= maxBlockSize; blockSize *= 2)
            {

                DirectMappedCacheSimulator aCacheSimulator;
                try
                {
                    aCacheSimulator = new DirectMappedCacheSimulator(bytesPerBlock: blockSize, maxCacheBitSize: maxSize);
                }
                catch
                {
                    break;
                }

                CacheResult newCacheResult = simulateCacheUses(addresses, aCacheSimulator);

                Console.WriteLine(newCacheResult.ToCsvString(false));

                updateBestCacheResults(bestCacheResults, newCacheResult);
            }

            return bestCacheResults;

        }

        static List<CacheResult> simulateSetAssociativeCaches(int[] addresses, int maxSize, int maxRows, int maxBlockSize)
        {
            List<CacheResult> bestCacheResults = new List<CacheResult>();

            Dictionary<int, bool> cacheHitsMissRecord = new Dictionary<int, bool>(addresses.Length);

            for (int numberOfRows = 1; numberOfRows <= maxRows; numberOfRows *= 2)
            {
                for (int blockSize = 4; blockSize <= maxBlockSize; blockSize *= 2)
                {

                    SetAssociativeSetCacheSimulator aCacheSimulator;
                    try
                    {
                        aCacheSimulator = new SetAssociativeSetCacheSimulator(numberOfRows: numberOfRows, bytesPerBlock: blockSize, maxCacheBitSize: maxSize);
                    }
                    catch
                    {
                        break;
                    }

                    CacheResult newCacheResult = simulateCacheUses(addresses, aCacheSimulator);

                    updateBestCacheResults(bestCacheResults, newCacheResult);
                }
            }

            return bestCacheResults;

        }

        static List<CacheResult> simulateFullyAssociativeCaches(int[] addresses, int maxSize, int maxBlockSize)
        {
            List<CacheResult> bestCacheResults = new List<CacheResult>();

            Dictionary<int, bool> cacheHitsMissRecord = new Dictionary<int, bool>(addresses.Length);

            for (int blockSize = 4; blockSize <= maxBlockSize; blockSize *= 2)
            {

                FullyAssociativeCacheSimulator aCacheSimulator;
                try
                {
                    aCacheSimulator = new FullyAssociativeCacheSimulator(bytesPerBlock: blockSize, maxCacheBitSize: maxSize);
                }
                catch
                {
                    break;
                }

                CacheResult newCacheResult = simulateCacheUses(addresses, aCacheSimulator);

                updateBestCacheResults(bestCacheResults, newCacheResult);
            }

            return bestCacheResults;

        }

        static CacheResult simulateCacheUses(int[] addresses, MemoryCacheSimulator aCacheSimulator)
        {
            Dictionary<int, bool> hitMissRecord = new Dictionary<int, bool>();
            int cyclesTaken = -1;
            for (int i = 0; i < 2; i++)
            {
                cyclesTaken = 0;
                foreach (int address in addresses)
                {
                    bool isCacheHit = aCacheSimulator.getMem(address);
                    hitMissRecord[address] = isCacheHit;
                    cyclesTaken += (isCacheHit) ? aCacheSimulator.getHitTime() : aCacheSimulator.getMissTime();
                }
            }

            return new CacheResult(aCacheSimulator, cyclesTaken, hitMissRecord);
        }

        static void updateBestCacheResults(List<CacheResult> bestCacheResults, CacheResult newCacheResults)
        {
            if (bestCacheResults.Count == 0)
            {
                bestCacheResults.Add(newCacheResults);
                return;
            }

            if (bestCacheResults[0].CycleUse == newCacheResults.CycleUse)
            {
                bestCacheResults.Add(newCacheResults);
                return;
            }

            if (bestCacheResults[0].CycleUse > newCacheResults.CycleUse)
            {
                bestCacheResults.Clear();
                bestCacheResults.Add(newCacheResults);
                return;
            }
        }

        static string getInstructionHitsAndMissesCsv(Dictionary<int, bool> cacheHitsMissRecord)
        {
            string csv = "Instruction, Is Hit\n";
            foreach(KeyValuePair<int, bool> record in cacheHitsMissRecord)
            {
                csv += record.Key + ", " + record.Value + "\n";
            }

            return csv;
        }
    }
}

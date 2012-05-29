using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using DictionarySpeedTests.BitShiftingSearchDictionaries;
using DictionarySpeedTests.StringNormalisers;
using DictionarySpeedTests.TernarySearchTreeDictionaries;

namespace DictionarySpeedTests
{
    class Program
    {
        // ========================================================================================================================================================
        // MAIN
        // ========================================================================================================================================================
        static void Main(string[] args)
        {
            // The file "SampleData.dat" is derived from data obtained by querying the New York Times Article Search API in my FullTextIndexer project (and then
            // manipulated to remove dependencies to any classes in that work). From what I understand of the API's Terms of Use it's fine to distribute this
            // data here (it's essentially just a set of words that appear in some of their articles). I used that so that there was a real set of data to
            // operate on (there's 86,000-ish keys so it's not a small set but not enormous by any stretch).
            var keyNormaliser = new DefaultStringNormaliser();
            var data = new Dictionary<string, float>(keyNormaliser);
            foreach (var token in File.ReadAllText("TokenList.txt").Split('\n').Select(t => t.Trim()))
            {
                var normalisedToken = keyNormaliser.GetNormalisedString(token);
                if ((normalisedToken != "") && !data.ContainsKey(normalisedToken))
                    data.Add(normalisedToken, data.Count);
            }

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data1..");
            var data1 = new BitShiftingSearchDictionary<string, float>(data);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data2..");
            var data2 = new BitShiftingStructSearchDictionaryWithKeyNotFoundNode<string, float>(data);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data3..");
            var data3 = new BitShiftingStructSearchDictionary<string, float>(data);
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data4 [TernarySearchTree-Unsorted]..");
            var data4 = new TernarySearchTreeDictionary<float>(data, new DefaultStringNormaliser());
            Console.WriteLine(" > BalanceFactor: " + data4.GetBalanceFactor());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data5 [TernarySearchTree-Alphabetical]..");
            var data5 = new TernarySearchTreeDictionary<float>(GetAlphabeticalSortedData(data), new DefaultStringNormaliser());
            Console.WriteLine(" > BalanceFactor: " + data5.GetBalanceFactor());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data6 [TernarySearchTree-RandomSorted]..");
            var data6 = new TernarySearchTreeDictionary<float>(GetRandomSortedData(data, 0), new DefaultStringNormaliser());
            Console.WriteLine(" > BalanceFactor: " + data6.GetBalanceFactor());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data7 [TernarySearchTree-SearchTreeSortedData]..");
            var data7 = new TernarySearchTreeDictionary<float>(GetSearchTreeSortedData<float>(data), new DefaultStringNormaliser());
            Console.WriteLine(" > BalanceFactor: " + data7.GetBalanceFactor());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Generating data8..");
            var data8 = new TernarySearchTreeStructDictionary<float>(GetSearchTreeSortedData<float>(data), new DefaultStringNormaliser());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - Done");

            var outerLoopRepeatCount = 50;
            var innerLoopsCount = 1000;

            // The reverseKeyChance value specifies how many of the keys that are taken as a random subset of the input data are reversed such that some of the requested
            // keys will exist in the data and some won't (a value of 0 means that no keys are reversed, 1 means that they all are). Having run these tests a few times
            // I've found the following results (in Release builds, which show better results for the search tree than Debug).
            //  - reverseKeyChance 1 (none of the searched-for keys exist in the data): TernarySearchTree is slightly slower, 0.97x the speed of the standard dictionary
            //  - reverseKeyChance 0.99: TernarySearchTree is slightly slower, 0.99x
            //  - reverseKeyChance 0.98: TernarySearchTree is slightly faster, almost 1.05x
            //  - reverseKeyChance 0.97: TernarySearchTree is slightly faster, almost 1.1x
            //  - reverseKeyChance 0.9: TernarySearchTree is faster, over 1.2x
            //  - reverseKeyChance 0.75: TernarySearchTree is faster, over 1.42x
            //  - reverseKeyChance 0.5: TernarySearchTree is faster, over 1.9x
            //  - reverseKeyChance 0 (all of the searched-for keys exist in the data): TernarySearchTree is faster, over 2.8x the speed of the standard dictionary
            var reverseKeyChance = 0.5f;
            var keysToRetrieve = GetKeysToRetrieve(data.Keys, 100, 0, reverseKeyChance);

            var TotalTime0 = new TimeSpan(0);
            var TotalTime1 = new TimeSpan(0);
            var TotalTime2 = new TimeSpan(0);
            var TotalTime3 = new TimeSpan(0);
            var TotalTime4 = new TimeSpan(0);
            var TotalTime5 = new TimeSpan(0);
            var TotalTime6 = new TimeSpan(0);
            var TotalTime7 = new TimeSpan(0);
            var TotalTime8 = new TimeSpan(0);
            for (var index = 0; index < outerLoopRepeatCount; index++)
            {
                var time8 = GetTimeForRetrievals<string, float>(data8, keysToRetrieve, innerLoopsCount);
                TotalTime8 = TotalTime8.Add(time8.TimeTaken);
                var time7 = GetTimeForRetrievals<string, float>(data7, keysToRetrieve, innerLoopsCount);
                TotalTime7 = TotalTime7.Add(time7.TimeTaken);
                var time6 = GetTimeForRetrievals<string, float>(data6, keysToRetrieve, innerLoopsCount);
                TotalTime6 = TotalTime6.Add(time6.TimeTaken);
                var time5 = GetTimeForRetrievals<string, float>(data5, keysToRetrieve, innerLoopsCount);
                TotalTime5 = TotalTime5.Add(time5.TimeTaken);
                var time4 = GetTimeForRetrievals<string, float>(data4, keysToRetrieve, innerLoopsCount);
                TotalTime4 = TotalTime4.Add(time4.TimeTaken);
                var time3 = GetTimeForRetrievals<string, float>(data3, keysToRetrieve, innerLoopsCount);
                TotalTime3 = TotalTime3.Add(time3.TimeTaken);
                var time2 = GetTimeForRetrievals<string, float>(data2, keysToRetrieve, innerLoopsCount);
                TotalTime2 = TotalTime2.Add(time2.TimeTaken);
                var time1 = GetTimeForRetrievals<string, float>(data1, keysToRetrieve, innerLoopsCount);
                TotalTime1 = TotalTime1.Add(time1.TimeTaken);
                var time0 = GetTimeForRetrievals<string, float>(data, keysToRetrieve, innerLoopsCount);
                TotalTime0 = TotalTime0.Add(time0.TimeTaken);
                Console.WriteLine(((float)((index + 1) * 100) / (float)outerLoopRepeatCount).ToString("0.000") + "% complete..");
            }
            Console.WriteLine("reverseKeyChance: " + reverseKeyChance);
            var improvement1 = TotalTime0.TotalMilliseconds / TotalTime1.TotalMilliseconds;
            Console.WriteLine("improvement1 [BitShiftDictionary]: " + improvement1);
            var improvement2 = TotalTime0.TotalMilliseconds / TotalTime2.TotalMilliseconds;
            Console.WriteLine("improvement2 [BitShiftStructDictionaryWithKeyNotFound]: " + improvement2);
            var improvement3 = TotalTime0.TotalMilliseconds / TotalTime3.TotalMilliseconds;
            Console.WriteLine("improvement3 [BitShiftStructDictionary]: " + improvement3);
            var improvement4 = TotalTime0.TotalMilliseconds / TotalTime4.TotalMilliseconds;
            Console.WriteLine("improvement4 [TernarySearchTree-InsertedInOrder]: " + improvement4);
            var improvement5 = TotalTime0.TotalMilliseconds / TotalTime5.TotalMilliseconds;
            Console.WriteLine("improvement5 [TernarySearchTree-Alphabetical]: " + improvement5);
            var improvement6 = TotalTime0.TotalMilliseconds / TotalTime6.TotalMilliseconds;
            Console.WriteLine("improvement6 [TernarySearchTree-RandomSorted]: " + improvement6);
            var improvement7 = TotalTime0.TotalMilliseconds / TotalTime7.TotalMilliseconds;
            Console.WriteLine("improvement7 [TernarySearchTree-SearchTreeSortedData]: " + improvement7);
            var improvement8 = TotalTime0.TotalMilliseconds / TotalTime8.TotalMilliseconds;
            Console.WriteLine("improvement8 [TernarySearchTree-SearchTreeSortedData, struct store]: " + improvement8);

            Console.WriteLine("Press [Enter] to continue..");
            Console.ReadLine();
		}

        private static string[] GetKeysToRetrieve(IEnumerable<string> keys, int count, int seed, double flipValueChance)
		{
			if (keys == null)
				throw new ArgumentNullException("data");
			if (count <= 0)
				throw new ArgumentOutOfRangeException("count", "must be greater than zero");
			if ((flipValueChance < 0) || (flipValueChance > 1))
				throw new ArgumentOutOfRangeException("flipValueChance", "must be between zero and one (inclusive)");

            if (!keys.Any())
                return new string[0];
            
            var rnd = new Random(seed);
			var allKeys = keys.ToArray();
			var keysToRetrieve = new string[count];
			for (var index = 0; index < keysToRetrieve.Length; index++)
			{
				var key = allKeys[rnd.Next(0, allKeys.Length)];
				if (rnd.NextDouble() < flipValueChance)
					key = new String(key.Reverse().ToArray());
				keysToRetrieve[index] = key;
			}
			return keysToRetrieve;
		}

        private static Results<TValue> GetTimeForRetrievals<TKey, TValue>(Dictionary<TKey, TValue> data, TKey[] keysToRetrieve, int loopsToPerform)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (keysToRetrieve == null)
                throw new ArgumentNullException("keysToRetrieve");
            if (loopsToPerform <= 0)
                throw new ArgumentOutOfRangeException("loopsToPerform", "must be greater than zero");

            var valuesRetrieved = new TValue[keysToRetrieve.Length];
            TValue valueRetrieved;
            var timer = new Stopwatch();
            timer.Start();
            for (var loopIndex = 0; loopIndex < loopsToPerform; loopIndex++)
            {
                for (var keyIndex = 0; keyIndex < keysToRetrieve.Length; keyIndex++)
                {
					var keyPresent = data.TryGetValue(keysToRetrieve[keyIndex], out valueRetrieved);
                    valuesRetrieved[keyIndex] = valueRetrieved;
                }
            }
            timer.Stop();
            return new Results<TValue>(valuesRetrieved, timer.Elapsed, loopsToPerform);
        }

        private static Results<TValue> GetTimeForRetrievals<TKey, TValue>(ILookup<TKey, TValue> data, TKey[] keysToRetrieve, int loopsToPerform)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (keysToRetrieve == null)
                throw new ArgumentNullException("keysToRetrieve");
            if (loopsToPerform <= 0)
                throw new ArgumentOutOfRangeException("loopsToPerform", "must be greater than zero");

            var valuesRetrieved = new TValue[keysToRetrieve.Length];
            TValue valueRetrieved;
            var timer = new Stopwatch();
            timer.Start();
            for (var loopIndex = 0; loopIndex < loopsToPerform; loopIndex++)
            {
                for (var keyIndex = 0; keyIndex < keysToRetrieve.Length; keyIndex++)
                {
					var keyPresent = data.TryGetValue(keysToRetrieve[keyIndex], out valueRetrieved);
                    valuesRetrieved[keyIndex] = valueRetrieved;
                }
            }
            timer.Stop();
            return new Results<TValue>(valuesRetrieved, timer.Elapsed, loopsToPerform);
        }

        private class Results<TValue>
        {
            public Results(IEnumerable<TValue> values, TimeSpan timeTaken, int loopsPerformed)
            {
                if (values == null)
                    throw new ArgumentNullException("values");
                if (timeTaken.Ticks < 0)
                    throw new ArgumentOutOfRangeException("timeTaken", "must be a non-negative value");
                if (loopsPerformed <= 0)
                    throw new ArgumentOutOfRangeException("loopsPerformed", "must be greater than zero");

                var valuesTidied = values.ToList();
                if (!valuesTidied.Any())
                    throw new ArgumentException("No entries in values set");

                Values = valuesTidied.AsReadOnly();
                TimeTaken = timeTaken;
                LoopsPerformed = loopsPerformed;
            }

            /// <summary>
            /// This will never be null nor an empty set (it may feasibly contain null values)
            /// </summary>
            public ReadOnlyCollection<TValue> Values { get; private set; }
            
            /// <summary>
            /// This will never be a negative TimeSpan
            /// </summary>
            public TimeSpan TimeTaken { get; private set; }
            
            /// <summary>
            /// This will always be greater than zero
            /// </summary>
            public int LoopsPerformed { get; private set; }
        }

        // ========================================================================================================================================================
        // DATA SORTING
        // ========================================================================================================================================================
        private static Dictionary<string, TValue> GetAlphabeticalSortedData<TValue>(Dictionary<string, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var sortedDataTemp = new SortedDictionary<string, TValue>(data);
            var sortedData = new Dictionary<string, TValue>(data.Count, data.Comparer);
            foreach (var key in sortedDataTemp.Keys)
                sortedData.Add(key, sortedDataTemp[key]);
            return sortedData;
        }

        private static Dictionary<string, TValue> GetRandomSortedData<TValue>(Dictionary<string, TValue> data, int seed)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var rnd = new Random(seed);
            var randomSortedDataTemp = new SortedDictionary<int, List<KeyValuePair<string, TValue>>>();
            foreach (var entry in data)
            {
                var randomKey = rnd.Next();
                if (randomSortedDataTemp.ContainsKey(randomKey))
                    randomSortedDataTemp[randomKey].Add(entry);
                else
                    randomSortedDataTemp.Add(randomKey, new List<KeyValuePair<string, TValue>> { entry });
            }
            var randomSortedData = new Dictionary<string, TValue>(data.Count, data.Comparer);
            foreach (var keyValuePairSets in randomSortedDataTemp.Values)
            {
                foreach (var keyValuePair in keyValuePairSets)
                    randomSortedData.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return randomSortedData;
        }

        /// <summary>
        /// Try to pre-sort data that will be inserted into a TernarySearchTreeDictionary such that the internal tree data will be balanced (the keys are sorted
        /// alphabetically and then then middle key is taken as the first item to insert while the sets of keys to left and right are taken as separate sets,
        /// these sets then have their middle keys taken as the second and third items to insert and the keys either side taken to form four new sets - this
        /// continues until there are no more keys to assign)
        /// </summary>
        private static Dictionary<string, TValue> GetSearchTreeSortedData<TValue>(Dictionary<string, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var orderedKeys = new List<string>();
            var keySets = new List<string[]>
			{
				data.Keys.OrderBy(k => k).ToArray()
			};
            while (keySets.Any())
            {
                var keySetsNew = new List<string[]>();
                foreach (var keySet in keySets)
                {
                    var midPointIndex = keySet.Length / 2;
                    var leftItems = keySet.Take(midPointIndex).ToArray();
                    var rightItems = keySet.Skip(midPointIndex + 1).ToArray();
                    orderedKeys.Add(keySet[midPointIndex]);
                    if (leftItems.Any())
                        keySetsNew.Add(leftItems);
                    if (rightItems.Any())
                        keySetsNew.Add(rightItems);
                }
                keySets = keySetsNew;
            }

            var sortedData = new Dictionary<string, TValue>();
            foreach (var key in orderedKeys)
                sortedData.Add(key, data[key]);
            return sortedData;
        }
    }
}

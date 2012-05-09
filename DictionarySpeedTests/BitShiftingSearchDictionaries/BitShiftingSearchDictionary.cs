using System;
using System.Collections.Generic;
using System.Linq;

namespace DictionarySpeedTests.BitShiftingSearchDictionaries
{
    public class BitShiftingSearchDictionary<TKey, TValue> : ILookup<TKey, TValue>
    {
        private Node _topNode;
        private KeyValuePair<TKey, TValue>[] _values;
        private IEqualityComparer<TKey> _keyComparer;
        public BitShiftingSearchDictionary(Dictionary<TKey, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build a tree with paths for each Key, ending at a Node with index values into a Values set
            var values = new List<KeyValuePair<TKey, TValue>>();
            var topNode = new Node();
            foreach (var key in data.Keys)
            {
                var node = topNode;
                var hash = data.Comparer.GetHashCode(key) & int.MaxValue;
                while (hash > 0)
                {
                    var isEven = (hash & 1) == 0;
                    if (isEven)
                    {
                        if (node.EvenNode == null)
                            node.EvenNode = new Node();
                        node = node.EvenNode;
                    }
                    else
                    {
                        if (node.OddNode == null)
                            node.OddNode = new Node();
                        node = node.OddNode;
                    }
                    hash = hash >> 1;
                }
                values.Add(new KeyValuePair<TKey, TValue>(key, data[key]));
                node.ValueIndices = (node.ValueIndices ?? new int[0]).Concat(new[] { values.Count - 1 }).ToArray();
            }

            _topNode = topNode;
            _values = values.ToArray();
            _keyComparer = data.Comparer;
        }

        private class Node
        {
            public Node EvenNode { get; set; }
            public Node OddNode { get; set; }
            public int[] ValueIndices { get; set; }
        }

        /// <summary>
        /// This will return true if the specified key was found and will set the value output parameter to the corresponding value. If it return false then the
        /// value output parameter should not be considered to be defined.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var node = _topNode;
            var hash = _keyComparer.GetHashCode(key) & int.MaxValue;
            while (hash > 0)
            {
                var isEven = (hash & 1) == 0;
                if (isEven)
                {
					if (node.EvenNode == null)
					{
						value = default(TValue);
						return false;
					}
                    node = node.EvenNode;
                }
                else
                {
                    if (node.OddNode == null)
					{
						value = default(TValue);
						return false;
					}
					node = node.OddNode;
                }
                hash = hash >> 1;
            }

            if (node.ValueIndices != null)
            {
                foreach (var valueIndex in node.ValueIndices)
                {
                    var entry = _values[valueIndex];
                    if (_keyComparer.Equals(key, entry.Key))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// This will throw an exception for a key not present in the data
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
        }
    }
}

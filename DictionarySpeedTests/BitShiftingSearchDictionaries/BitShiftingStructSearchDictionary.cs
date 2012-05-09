using System;
using System.Collections.Generic;

namespace DictionarySpeedTests.BitShiftingSearchDictionaries
{
    public class BitShiftingStructSearchDictionary<TKey, TValue> : ILookup<TKey, TValue>
    {
        private Node[] _nodes;
        private KeyValuePair<TKey, TValue>[] _values;
        private IEqualityComparer<TKey> _keyComparer;
        public BitShiftingStructSearchDictionary(Dictionary<TKey, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build a tree with paths for each Key, ending at a Node with index values into a Values set
            var values = new List<KeyValuePair<TKey, TValue>>();
            var nodes = new List<Node>
            {
                GetUnintialisedNode()
            };
            foreach (var key in data.Keys)
            {
                var nodeIndex = 0;
                var hash = data.Comparer.GetHashCode(key) & int.MaxValue;
                while (hash > 0)
                {
                    var isEven = (hash & 1) == 0;
                    if (isEven)
                    {
                        if (nodes[nodeIndex].EvenNodeIndex == -1)
                        {
                            nodes.Add(GetUnintialisedNode());
                            nodes[nodeIndex] = SetEvenNodeIndex(nodes[nodeIndex], nodes.Count - 1);
                            nodeIndex = nodes.Count - 1;
                        }
                        else
                            nodeIndex = nodes[nodeIndex].EvenNodeIndex;
                    }
                    else
                    {
                        if (nodes[nodeIndex].OddNodeIndex == -1)
                        {
                            nodes.Add(GetUnintialisedNode());
                            nodes[nodeIndex] = SetOddNodeIndex(nodes[nodeIndex], nodes.Count - 1);
                            nodeIndex = nodes.Count - 1;
                        }
                        else
                            nodeIndex = nodes[nodeIndex].OddNodeIndex;
                    }
                    hash = hash >> 1;
                }
                values.Add(new KeyValuePair<TKey,TValue>(key, data[key]));
                nodes[nodeIndex] = ExtendNodeValueIndices(nodes[nodeIndex], values.Count - 1);
            }

            _nodes = nodes.ToArray();
            _values = values.ToArray();
            _keyComparer = data.Comparer;
        }

        private struct Node
        {
            public int EvenNodeIndex;
            public int OddNodeIndex;
            public int[] ValueIndices;
        }

        private static Node GetUnintialisedNode()
        {
            return new Node() { EvenNodeIndex = -1, OddNodeIndex = -1, ValueIndices = null };
        }

        private static Node SetEvenNodeIndex(Node node, int index)
        {
            return new Node() { EvenNodeIndex = index, OddNodeIndex = node.OddNodeIndex, ValueIndices = node.ValueIndices };
        }

        private static Node SetOddNodeIndex(Node node, int index)
        {
            return new Node() { EvenNodeIndex = node.EvenNodeIndex, OddNodeIndex = index, ValueIndices = node.ValueIndices };
        }

        private static Node ExtendNodeValueIndices(Node node, int valueIndex)
        {
            var values = node.ValueIndices ?? new int[0];
            var valuesExtended = new int[values.Length + 1];
            values.CopyTo(valuesExtended, 0);
            valuesExtended[valuesExtended.Length - 1] = valueIndex;
            return new Node() { EvenNodeIndex = node.EvenNodeIndex, OddNodeIndex = node.OddNodeIndex, ValueIndices = valuesExtended };
        }

        /// <summary>
        /// This will return true if the specified key was found and will set the value output parameter to the corresponding value. If it return false then the
        /// value output parameter should not be considered to be defined.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var nodeIndex = 0;
            var hash = _keyComparer.GetHashCode(key) & int.MaxValue;
            while (hash > 0)
            {
                var isEven = (hash & 1) == 0;
                nodeIndex = isEven ? _nodes[nodeIndex].EvenNodeIndex : _nodes[nodeIndex].OddNodeIndex;
                if (nodeIndex == -1)
                {
                    value = default(TValue);
                    return false;
                }
                hash = hash >> 1;
            }

            var valueIndices = _nodes[nodeIndex].ValueIndices;
            if (valueIndices != null)
            {
                foreach (var valueIndex in valueIndices)
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

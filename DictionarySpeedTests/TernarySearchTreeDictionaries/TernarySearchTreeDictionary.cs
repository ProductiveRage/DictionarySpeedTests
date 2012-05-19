using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DictionarySpeedTests.TernarySearchTreeDictionaries
{
    [Serializable]
    public class TernarySearchTreeDictionary<TValue> : ILookup<string, TValue>, IEnumerable<string>
    {
        private Node _root;
        private IStringNormaliser _keyNormaliser;
        private ReadOnlyCollection<string> _keys;

        public TernarySearchTreeDictionary(IEnumerable<KeyValuePair<string, TValue>> data, IStringNormaliser keyNormaliser)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (keyNormaliser == null)
                throw new ArgumentNullException("keyNormaliser");

            Node root = null;
            var nodeCount = 0;
            var keys = new HashSet<string>(keyNormaliser);
            var depthToKeyLengthRatio = new List<float>();
            foreach (var entry in data)
            {
                var key = entry.Key;
                if (key == null)
                    throw new ArgumentException("Null key encountered in data");
                var normalisedKey = keyNormaliser.GetNormalisedString(key);
                if (normalisedKey == "")
                    throw new ArgumentException("key value results in blank string when normalised: " + key);
                if (keys.Contains(normalisedKey))
                    throw new ArgumentException("key value results in duplicate normalised key:" + key);
                keys.Add(key);

                if (root == null)
                {
                    root = new Node() { Character = normalisedKey[0] };
                    nodeCount++;
                }

                var node = root;
                var index = 0;
                var depth = 1;
                while (true)
                {
                    if (node.Character == normalisedKey[index])
                    {
                        index++;
                        if (index == normalisedKey.Length)
                        {
                            node.IsKey = true;
                            node.Value = entry.Value;
                            depthToKeyLengthRatio.Add(depth / normalisedKey.Length);
                            break;
                        }
                        if (node.MiddleChild == null)
                        {
                            node.MiddleChild = new Node() { Character = normalisedKey[index] };
                            nodeCount++;
                        }
                        node = node.MiddleChild;
                    }
                    else if (normalisedKey[index] < node.Character)
                    {
                        if (node.LeftChild == null)
                        {
                            node.LeftChild = new Node() { Character = normalisedKey[index] };
                            nodeCount++;
                        }
                        node = node.LeftChild;
                    }
                    else
                    {
                        if (node.RightChild == null)
                        {
                            node.RightChild = new Node() { Character = normalisedKey[index] };
                            nodeCount++;
                        }
                        node = node.RightChild;
                    }
                    depth++;
                }
            }

            _root = root;
            _keyNormaliser = keyNormaliser;
            _keys = keys.ToList().AsReadOnly();
            BalanceFactor = depthToKeyLengthRatio.Any() ? depthToKeyLengthRatio.Average() : 0;
        }

        [Serializable]
        private class Node
        {
            public char Character { get; set; }
            public Node LeftChild { get; set; }
            public Node MiddleChild { get; set; }
            public Node RightChild { get; set; }
            public bool IsKey { get; set; }
            public TValue Value { get; set; }
        }

        public int Count
        {
            get { return _keys.Count; }
        }

        /// <summary>
        /// This will never be null nor contain any null or empty strings
        /// </summary>
        public IEnumerable<string> Keys
        {
            get { return _keys; }
        }

        /// <summary>
        /// Get the average ratio of Depth-to-Key-Length (Depth will always be greater or equal to the Key Length). The lower the value, the better balanced the
        /// tree and the better the performance should be (1 would the lowest value and would mean that all paths were optimal but this is not realistic with
        /// real data - less than 2.5 should yield excellent performance). This will be zero if there are no keys.
        /// </summary>
        public float BalanceFactor { get; private set; }

        /// <summary>
        /// This will return true if the specified key was found and will set the value output parameter to the corresponding value. If it return false then the
        /// value output parameter should not be considered to be defined.
        /// </summary>
        public bool TryGetValue(string key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            var normalisedKey = _keyNormaliser.GetNormalisedString(key);
            if (normalisedKey != "")
            {
                var node = _root;
                var index = 0;
                while (true)
                {
                    if (node.Character == normalisedKey[index])
                    {
                        index++;
                        if (index == normalisedKey.Length)
                        {
                            if (node.IsKey)
                            {
                                value = node.Value;
                                return true;
                            }
                            break;
                        }
                        node = node.MiddleChild;
                    }
                    else if (normalisedKey[index] < node.Character)
                        node = node.LeftChild;
                    else
                        node = node.RightChild;
                    if (node == null)
                        break;
                }
            }
            value = default(TValue);
            return false;
        }

        /// <summary>
        /// This will throw an exception for a key not present in the data
        /// </summary>
        public TValue this[string key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException();
                return value;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

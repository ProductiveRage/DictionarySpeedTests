using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DictionarySpeedTests.StringNormalisers;

namespace DictionarySpeedTests.TernarySearchTreeDictionaries
{
    [Serializable]
	public class TernarySearchTreeStructDictionary<TValue> : ILookup<string, TValue>, IEnumerable<string>
	{
		private Node[] _nodes;
		private TValue[] _values;
        private IStringNormaliser _keyNormaliser;
        private ReadOnlyCollection<string> _keys;

        public TernarySearchTreeStructDictionary(IEnumerable<KeyValuePair<string, TValue>> data, IStringNormaliser keyNormaliser)
		{
            if (data == null)
                throw new ArgumentNullException("data");
            if (keyNormaliser == null)
                throw new ArgumentNullException("keyNormaliser");

			var nodes = new List<Node> { GetUnintialisedNode() };
			var values = new List<TValue>();
            var keys = new HashSet<string>(keyNormaliser);
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

				if (nodes[0].Character == (char)0)
					nodes[0] = SetCharacter(nodes[0], normalisedKey[0]);

				var nodeIndex = 0;
				var keyIndex = 0;
                while (true)
                {
                    if (nodes[nodeIndex].Character == normalisedKey[keyIndex])
                    {
                        keyIndex++;
                        if (keyIndex == normalisedKey.Length)
                        {
							var newValueIndex = values.Count;
							values.Add(entry.Value);
							nodes[nodeIndex] = SetValueIndex(nodes[nodeIndex], newValueIndex);
                            break;
                        }
						if (nodes[nodeIndex].MiddleChildIndex == -1)
						{
							var newNode = SetCharacter(GetUnintialisedNode(), normalisedKey[keyIndex]);
							var newNodeIndex = nodes.Count;
							nodes.Add(newNode);
							nodes[nodeIndex] = SetMiddleChildIndex(nodes[nodeIndex], newNodeIndex);
							nodeIndex = newNodeIndex;
						}
						else
							nodeIndex = nodes[nodeIndex].MiddleChildIndex;
                        continue;
                    }
					else if (normalisedKey[keyIndex] < nodes[nodeIndex].Character)
                    {
						if (nodes[nodeIndex].LeftChildIndex == -1)
						{
							var newNode = SetCharacter(GetUnintialisedNode(), normalisedKey[keyIndex]);
							var newNodeIndex = nodes.Count;
							nodes.Add(newNode);
							nodes[nodeIndex] = SetLeftChildIndex(nodes[nodeIndex], newNodeIndex);
							nodeIndex = newNodeIndex;
						}
						else
							nodeIndex = nodes[nodeIndex].LeftChildIndex;
					}
                    else
                    {
						if (nodes[nodeIndex].RightChildIndex == -1)
						{
							var newNode = SetCharacter(GetUnintialisedNode(), normalisedKey[keyIndex]);
							var newNodeIndex = nodes.Count;
							nodes.Add(newNode);
							nodes[nodeIndex] = SetRightChildIndex(nodes[nodeIndex], newNodeIndex);
							nodeIndex = newNodeIndex;
						}
						else
							nodeIndex = nodes[nodeIndex].RightChildIndex;
					}
                }
            }

			_nodes = nodes.ToArray();
			_values = values.ToArray();
            _keyNormaliser = keyNormaliser;
            _keys = keys.ToList().AsReadOnly();
		}

		private static Node GetUnintialisedNode()
		{
			return new Node() { Character = (char)0, LeftChildIndex = -1, MiddleChildIndex = -1, RightChildIndex = -1, IsKey = false, ValueIndex = -1 };
		}

		private static Node SetCharacter(Node node, char character)
		{
			return new Node()
			{
				Character = character,
				LeftChildIndex = node.LeftChildIndex,
				MiddleChildIndex = node.MiddleChildIndex,
				RightChildIndex = node.RightChildIndex,
				IsKey = node.IsKey,
				ValueIndex = node.ValueIndex
			};
		}

		private static Node SetLeftChildIndex(Node node, int index)
		{
			return new Node()
			{
				Character = node.Character,
				LeftChildIndex = index,
				MiddleChildIndex = node.MiddleChildIndex,
				RightChildIndex = node.RightChildIndex,
				IsKey = node.IsKey,
				ValueIndex = node.ValueIndex
			};
		}

		private static Node SetMiddleChildIndex(Node node, int index)
		{
			return new Node()
			{
				Character = node.Character,
				LeftChildIndex = node.LeftChildIndex,
				MiddleChildIndex = index,
				RightChildIndex = node.RightChildIndex,
				IsKey = node.IsKey,
				ValueIndex = node.ValueIndex
			};
		}

		private static Node SetRightChildIndex(Node node, int index)
		{
			return new Node()
			{
				Character = node.Character,
				LeftChildIndex = node.LeftChildIndex,
				MiddleChildIndex = node.MiddleChildIndex,
				RightChildIndex = index,
				IsKey = node.IsKey,
				ValueIndex = node.ValueIndex
			};
		}

		/// <summary>
		/// This will always mark IsKey as true
		/// </summary>
		private static Node SetValueIndex(Node node, int index)
		{
			return new Node()
			{
				Character = node.Character,
				LeftChildIndex = node.LeftChildIndex,
				MiddleChildIndex = node.MiddleChildIndex,
				RightChildIndex = node.RightChildIndex,
				IsKey = true,
				ValueIndex = index
			};
		}

		[Serializable]
        private struct Node
        {
            public char Character { get; set; }
            public int LeftChildIndex { get; set; }
			public int MiddleChildIndex { get; set; }
			public int RightChildIndex { get; set; }
            public bool IsKey { get; set; }
            public int ValueIndex { get; set; }
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
				var nodeIndex = 0;
                var keyIndex = 0;
                while (true)
                {
                    if (_nodes[nodeIndex].Character == normalisedKey[keyIndex])
                    {
                        keyIndex++;
                        if (keyIndex == normalisedKey.Length)
                        {
							if (_nodes[nodeIndex].IsKey)
                            {
                                value = _values[_nodes[nodeIndex].ValueIndex];
                                return true;
                            }
                            break;
                        }
						if (_nodes[nodeIndex].MiddleChildIndex == -1)
							break;
						nodeIndex = _nodes[nodeIndex].MiddleChildIndex;
                    }
					else if (normalisedKey[keyIndex] < _nodes[nodeIndex].Character)
					{
						if (_nodes[nodeIndex].LeftChildIndex == -1)
							break;
						nodeIndex = _nodes[nodeIndex].LeftChildIndex;
					}
					else
					{
						if (_nodes[nodeIndex].RightChildIndex == -1)
							break;
						nodeIndex = _nodes[nodeIndex].RightChildIndex;
					}
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

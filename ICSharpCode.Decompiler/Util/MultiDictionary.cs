#nullable enable
// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Represents a mutable key-to-multiple-values map.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Values are grouped per key and preserved in insertion order within each key's backing list.
	/// </para>
	/// <para>
	/// The type also implements <see cref="ILookup{TKey, TElement}"/> so it can be consumed through standard
	/// LINQ grouping APIs.
	/// </para>
	/// </remarks>
	public class MultiDictionary<TKey, TValue> : ILookup<TKey, TValue> where TKey : notnull
	{
		readonly Dictionary<TKey, List<TValue>> dict;

		/// <summary>
		/// Initializes an empty dictionary that uses the default key comparer.
		/// </summary>
		public MultiDictionary()
		{
			dict = new Dictionary<TKey, List<TValue>>();
		}

		/// <summary>
		/// Initializes an empty dictionary that uses a caller-specified key comparer.
		/// </summary>
		/// <param name="comparer">Comparer used to test key equality; <see langword="null"/> uses the default comparer.</param>
		public MultiDictionary(IEqualityComparer<TKey>? comparer)
		{
			dict = new Dictionary<TKey, List<TValue>>(comparer);
		}

		/// <summary>
		/// Adds a value to the group associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key that identifies the value group.</param>
		/// <param name="value">Value to append.</param>
		public void Add(TKey key, TValue value)
		{
			if (!dict.TryGetValue(key, out List<TValue>? valueList))
			{
				valueList = new List<TValue>();
				dict.Add(key, valueList);
			}
			valueList.Add(value);
		}

		/// <summary>
		/// Removes a specific value from the group associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key identifying the value group.</param>
		/// <param name="value">Value to remove.</param>
		/// <returns><see langword="true"/> when a matching value was removed; otherwise <see langword="false"/>.</returns>
		/// <remarks>
		/// If the removal leaves the group empty, the key is removed from the dictionary.
		/// </remarks>
		public bool Remove(TKey key, TValue value)
		{
			if (dict.TryGetValue(key, out List<TValue>? valueList))
			{
				if (valueList.Remove(value))
				{
					if (valueList.Count == 0)
						dict.Remove(key);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes all entries with the specified key.
		/// </summary>
		/// <returns>Returns true if at least one entry was removed.</returns>
		public bool RemoveAll(TKey key)
		{
			return dict.Remove(key);
		}

		/// <summary>
		/// Removes all entries from the dictionary.
		/// </summary>
		public void Clear()
		{
			dict.Clear();
		}

		/// <summary>
		/// Gets all values associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key whose values should be returned.</param>
		/// <returns>
		/// A read-only list of values for <paramref name="key"/>. Returns an empty list when the key is absent.
		/// </returns>
		public IReadOnlyList<TValue> this[TKey key] {
			get {
				if (dict.TryGetValue(key, out var list))
					return list;
				else
					return EmptyList<TValue>.Instance;
			}
		}

		/// <summary>
		/// Attempts to retrieve all values associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key to resolve.</param>
		/// <param name="values">
		/// Receives the values associated with <paramref name="key"/>, or an empty list when the key is absent.
		/// </param>
		/// <returns><see langword="true"/> when the key exists; otherwise <see langword="false"/>.</returns>
		public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
		{
			values = EmptyList<TValue>.Instance;
			if (dict.TryGetValue(key, out var list))
			{
				values = list;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns the number of different keys.
		/// </summary>
		public int Count {
			get { return dict.Count; }
		}

		/// <summary>
		/// Gets all keys currently present in the dictionary.
		/// </summary>
		public ICollection<TKey> Keys {
			get { return dict.Keys; }
		}

		/// <summary>
		/// Enumerates all values across all keys.
		/// </summary>
		public IEnumerable<TValue> Values {
			get { return dict.Values.SelectMany(list => list); }
		}

		IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key] {
			get { return this[key]; }
		}

		/// <summary>
		/// Determines whether the dictionary contains at least one value for <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Key to test.</param>
		/// <returns><see langword="true"/> when a group exists for <paramref name="key"/>; otherwise <see langword="false"/>.</returns>
		public bool Contains(TKey key)
		{
			return dict.ContainsKey(key);
		}

		/// <summary>
		/// Returns an enumerator over key/value groups.
		/// </summary>
		/// <returns>
		/// An enumerator that yields one <see cref="IGrouping{TKey, TElement}"/> per key currently stored.
		/// </returns>
		public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
		{
			foreach (var pair in dict)
				yield return new Grouping(pair.Key, pair.Value);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		sealed class Grouping : IGrouping<TKey, TValue>
		{
			readonly TKey key;
			readonly List<TValue> values;

			public Grouping(TKey key, List<TValue> values)
			{
				this.key = key;
				this.values = values;
			}

			public TKey Key {
				get { return key; }
			}

			public IEnumerator<TValue> GetEnumerator()
			{
				return values.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return values.GetEnumerator();
			}
		}
	}
}

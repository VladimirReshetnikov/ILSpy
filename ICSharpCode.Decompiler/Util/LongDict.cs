// Copyright (c) 2017 Daniel Grunwald
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Provides factory and comparer helpers for <see cref="LongDict{T}"/>.
	/// </summary>
	static class LongDict
	{
		/// <summary>
		/// Builds an immutable lookup from long-key intervals to values.
		/// </summary>
		/// <typeparam name="T">Value type stored for each covered interval.</typeparam>
		/// <param name="entries">
		/// Sequence of interval sets paired with values. Earlier entries take precedence when intervals overlap.
		/// </param>
		/// <returns>A normalized <see cref="LongDict{T}"/> that can be queried by individual keys.</returns>
		public static LongDict<T> Create<T>(IEnumerable<(LongSet, T)> entries)
		{
			return new LongDict<T>(entries);
		}

		internal static readonly KeyComparer<LongInterval, long> StartComparer = KeyComparer.Create((LongInterval i) => i.Start);
	}

	/// <summary>
	/// Represents an immutable mapping from <see cref="long"/> keys to values.
	/// </summary>
	/// <typeparam name="T">Type of values stored in the dictionary.</typeparam>
	/// <remarks>
	/// <para>
	/// Input entries are interpreted as sets of numeric intervals. When multiple entries overlap,
	/// the first entry wins for all keys in the overlap and later entries only contribute keys that are still uncovered.
	/// </para>
	/// <para>
	/// Internally, intervals are flattened and sorted by start offset, which allows
	/// <see cref="TryGetValue(long, out T)"/> to resolve keys using binary search.
	/// </para>
	/// </remarks>
	struct LongDict<T> : IEnumerable<KeyValuePair<LongInterval, T>>
	{
		readonly LongInterval[] keys;
		readonly T[] values;

		/// <summary>
		/// Initializes a new immutable dictionary from interval/value entries.
		/// </summary>
		/// <param name="entries">
		/// Sequence of interval sets paired with values. If two sets contain the same key,
		/// the value from the earliest sequence element is kept.
		/// </param>
		public LongDict(IEnumerable<(LongSet, T)> entries)
		{
			LongSet available = LongSet.Universe;
			var keys = new List<LongInterval>();
			var values = new List<T>();
			foreach (var (key, val) in entries)
			{
				foreach (var interval in key.IntersectWith(available).Intervals)
				{
					keys.Add(interval);
					values.Add(val);
				}
				available = available.ExceptWith(key);
			}
			this.keys = keys.ToArray();
			this.values = values.ToArray();
			Array.Sort(this.keys, this.values, LongDict.StartComparer);
		}

		/// <summary>
		/// Attempts to resolve the value assigned to <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Numeric key to look up.</param>
		/// <param name="value">Receives the mapped value when a covering interval is found.</param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="key"/> is contained in one of the stored intervals;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool TryGetValue(long key, out T value)
		{
			int pos = Array.BinarySearch(this.keys, new LongInterval(key, key), LongDict.StartComparer);
			// If the element isn't found, BinarySearch returns the complement of "insertion position".
			// We use this to find the previous element (if there wasn't any exact match).
			if (pos < 0)
				pos = ~pos - 1;
			if (pos >= 0 && this.keys[pos].Contains(key))
			{
				value = this.values[pos];
				return true;
			}
			value = default(T);
			return false;
		}

		/// <summary>
		/// Gets the value for <paramref name="key"/> or <c>default</c> when no interval covers the key.
		/// </summary>
		/// <param name="key">Numeric key to look up.</param>
		/// <returns>
		/// The mapped value if present; otherwise <c>default(T)</c>.
		/// </returns>
		public T GetOrDefault(long key)
		{
			TryGetValue(key, out T val);
			return val;
		}

		/// <summary>
		/// Returns an enumerator over the flattened interval/value pairs.
		/// </summary>
		/// <returns>
		/// An enumerator that yields each stored <see cref="LongInterval"/> with its associated value,
		/// ordered by interval start.
		/// </returns>
		public IEnumerator<KeyValuePair<LongInterval, T>> GetEnumerator()
		{
			for (int i = 0; i < this.keys.Length; ++i)
			{
				yield return new KeyValuePair<LongInterval, T>(this.keys[i], this.values[i]);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

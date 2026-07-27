// Copyright (c) 2016 Daniel Grunwald
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
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Represents a fixed-size mutable set of non-negative integers backed by packed 64-bit words.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type is used by control-flow and data-flow analyses where the domain size is known in advance (for example
	/// node IDs or variable IDs). The constructor rounds capacity up to full words, so storage can include trailing bits
	/// outside the caller's logical range.
	/// </para>
	/// <para>
	/// Set operations assume both operands were created with compatible capacities. Some methods assert this in debug
	/// builds, while release builds rely on callers to preserve that invariant.
	/// </para>
	/// </remarks>
	public class BitSet
	{
		const int BitsPerWord = 64;
		const int Log2BitsPerWord = 6;
		const ulong Mask = 0xffffffffffffffffUL;

		readonly ulong[] words;

		static int WordIndex(int bitIndex)
		{
			Debug.Assert(bitIndex >= 0);
			return bitIndex >> Log2BitsPerWord;
		}

		/// <summary>
		/// Creates a new bitset, where initially all bits are zero.
		/// </summary>
		/// <param name="capacity">The logical number of addressable bits expected by callers.</param>
		public BitSet(int capacity)
		{
			this.words = new ulong[Math.Max(1, WordIndex(capacity + BitsPerWord - 1))];
		}

		private BitSet(ulong[] bits)
		{
			this.words = bits;
		}

		/// <summary>
		/// Creates a deep copy of this bitset.
		/// </summary>
		/// <returns>A new <see cref="BitSet"/> with the same bit pattern.</returns>
		public BitSet Clone()
		{
			return new BitSet((ulong[])words.Clone());
		}

		/// <summary>
		/// Gets or sets whether the bit at <paramref name="index"/> is set.
		/// </summary>
		/// <param name="index">Zero-based bit index.</param>
		/// <value><see langword="true"/> when the bit is set; otherwise <see langword="false"/>.</value>
		/// <exception cref="IndexOutOfRangeException">
		/// <paramref name="index"/> is outside the storage allocated by this instance.
		/// </exception>
		public bool this[int index] {
			get {
				return (words[WordIndex(index)] & (1UL << index)) != 0;
			}
			set {
				if (value)
					Set(index);
				else
					Clear(index);
			}
		}

		/// <summary>
		/// Gets whether at least one bit is set.
		/// </summary>
		public bool Any()
		{
			for (int i = 0; i < words.Length; i++)
			{
				if (words[i] != 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Gets whether all bits in the specified range are set.
		/// </summary>
		public bool All(int startIndex, int endIndex)
		{
			Debug.Assert(startIndex <= endIndex);
			if (startIndex >= endIndex)
			{
				return true;
			}
			int startWordIndex = WordIndex(startIndex);
			int endWordIndex = WordIndex(endIndex - 1);
			ulong startMask = Mask << startIndex;
			ulong endMask = Mask >> -endIndex; // same as (Mask >> (64 - (endIndex % 64)))
			if (startWordIndex == endWordIndex)
			{
				return (words[startWordIndex] & (startMask & endMask)) == (startMask & endMask);
			}
			else
			{
				if ((words[startWordIndex] & startMask) != startMask)
					return false;
				for (int i = startWordIndex + 1; i < endWordIndex; i++)
				{
					if (words[i] != ulong.MaxValue)
						return false;
				}
				return (words[endWordIndex] & endMask) == endMask;
			}
		}

		/// <summary>
		/// Gets whether both bitsets have the same content.
		/// </summary>
		/// <param name="other">The set to compare with this instance.</param>
		/// <returns><see langword="true"/> when all stored words are equal; otherwise <see langword="false"/>.</returns>
		public bool SetEquals(BitSet other)
		{
			Debug.Assert(words.Length == other.words.Length);
			for (int i = 0; i < words.Length; i++)
			{
				if (words[i] != other.words[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Gets whether this set is a subset of other, or equal.
		/// </summary>
		/// <param name="other">The candidate superset.</param>
		/// <returns>
		/// <see langword="true"/> when each set bit in this instance is also set in <paramref name="other"/>;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsSubsetOf(BitSet other)
		{
			for (int i = 0; i < words.Length; i++)
			{
				if ((words[i] & ~other.words[i]) != 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Gets whether this set is a superset of other, or equal.
		/// </summary>
		/// <param name="other">The candidate subset.</param>
		/// <returns>
		/// <see langword="true"/> when each set bit in <paramref name="other"/> is set in this instance;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsSupersetOf(BitSet other)
		{
			return other.IsSubsetOf(this);
		}

		/// <summary>
		/// Gets whether this set is a strict subset of <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The candidate strict superset.</param>
		/// <returns>
		/// <see langword="true"/> when this set is a subset of <paramref name="other"/> but not equal to it;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsProperSubsetOf(BitSet other)
		{
			return IsSubsetOf(other) && !SetEquals(other);
		}

		/// <summary>
		/// Gets whether this set is a strict superset of <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The candidate strict subset.</param>
		/// <returns>
		/// <see langword="true"/> when this set is a superset of <paramref name="other"/> but not equal to it;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsProperSupersetOf(BitSet other)
		{
			return IsSupersetOf(other) && !SetEquals(other);
		}

		/// <summary>
		/// Gets whether at least one bit is set in both bitsets.
		/// </summary>
		public bool Overlaps(BitSet other)
		{
			for (int i = 0; i < words.Length; i++)
			{
				if ((words[i] & other.words[i]) != 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Sets this instance to the union of itself and <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The set whose bits are added to this instance.</param>
		public void UnionWith(BitSet other)
		{
			Debug.Assert(words.Length == other.words.Length);
			for (int i = 0; i < words.Length; i++)
			{
				words[i] |= other.words[i];
			}
		}

		/// <summary>
		/// Sets this instance to the intersection of itself and <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The set used to filter this instance.</param>
		public void IntersectWith(BitSet other)
		{
			for (int i = 0; i < words.Length; i++)
			{
				words[i] &= other.words[i];
			}
		}

		/// <summary>
		/// Sets the bit at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Zero-based bit index.</param>
		public void Set(int index)
		{
			words[WordIndex(index)] |= (1UL << index);
		}

		/// <summary>
		/// Sets all bits i; where startIndex &lt;= i &lt; endIndex.
		/// </summary>
		/// <param name="startIndex">Inclusive start of the range.</param>
		/// <param name="endIndex">Exclusive end of the range.</param>
		public void Set(int startIndex, int endIndex)
		{
			Debug.Assert(startIndex <= endIndex);
			if (startIndex >= endIndex)
			{
				return;
			}
			int startWordIndex = WordIndex(startIndex);
			int endWordIndex = WordIndex(endIndex - 1);
			ulong startMask = Mask << startIndex;
			ulong endMask = Mask >> -endIndex; // same as (Mask >> (64 - (endIndex % 64)))
			if (startWordIndex == endWordIndex)
			{
				words[startWordIndex] |= (startMask & endMask);
			}
			else
			{
				words[startWordIndex] |= startMask;
				for (int i = startWordIndex + 1; i < endWordIndex; i++)
				{
					words[i] = ulong.MaxValue;
				}
				words[endWordIndex] |= endMask;
			}
		}

		// Note: intentionally no SetAll(), because it would also set the
		// extra bits (due to the capacity being rounded up to a full word).

		/// <summary>
		/// Clears the bit at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Zero-based bit index.</param>
		public void Clear(int index)
		{
			words[WordIndex(index)] &= ~(1UL << index);
		}

		/// <summary>
		/// Clear all bits i; where startIndex &lt;= i &lt; endIndex.
		/// </summary>
		/// <param name="startIndex">Inclusive start of the range.</param>
		/// <param name="endIndex">Exclusive end of the range.</param>
		public void Clear(int startIndex, int endIndex)
		{
			Debug.Assert(startIndex <= endIndex);
			if (startIndex >= endIndex)
			{
				return;
			}
			int startWordIndex = WordIndex(startIndex);
			int endWordIndex = WordIndex(endIndex - 1);
			ulong startMask = Mask << startIndex;
			ulong endMask = Mask >> -endIndex; // same as (Mask >> (64 - (endIndex % 64)))
			if (startWordIndex == endWordIndex)
			{
				words[startWordIndex] &= ~(startMask & endMask);
			}
			else
			{
				words[startWordIndex] &= ~startMask;
				for (int i = startWordIndex + 1; i < endWordIndex; i++)
				{
					words[i] = 0;
				}
				words[endWordIndex] &= ~endMask;
			}
		}

		/// <summary>
		/// Clears every stored bit in this instance.
		/// </summary>
		public void ClearAll()
		{
			for (int i = 0; i < words.Length; i++)
			{
				words[i] = 0;
			}
		}

		/// <summary>
		/// Finds the first set bit in the specified range.
		/// </summary>
		/// <param name="startIndex">Inclusive start of the search range.</param>
		/// <param name="endIndex">Exclusive end of the search range.</param>
		/// <returns>
		/// The index of the first set bit in <c>[<paramref name="startIndex"/>, <paramref name="endIndex"/>)</c>,
		/// or <c>-1</c> when the range contains no set bit.
		/// </returns>
		public int NextSetBit(int startIndex, int endIndex)
		{
			Debug.Assert(startIndex <= endIndex);
			if (startIndex >= endIndex)
			{
				return -1;
			}

			int startWordIndex = WordIndex(startIndex);
			int endWordIndex = WordIndex(endIndex - 1);
			ulong startMask = Mask << startIndex;
			ulong endMask = Mask >> -endIndex; // same as (Mask >> (64 - (endIndex % 64)))
			if (startWordIndex == endWordIndex)
			{
				ulong maskedWord = words[startWordIndex] & startMask & endMask;
				if (maskedWord != 0)
				{
					return startWordIndex * 64 + BitOperations.TrailingZeroCount(maskedWord);
				}
			}
			else
			{
				ulong maskedWord = words[startWordIndex] & startMask;
				if (maskedWord != 0)
				{
					return startWordIndex * 64 + BitOperations.TrailingZeroCount(maskedWord);
				}
				for (int i = startWordIndex + 1; i < endWordIndex; i++)
				{
					maskedWord = words[i];
					if (maskedWord != 0)
					{
						return i * 64 + BitOperations.TrailingZeroCount(maskedWord);
					}
				}
				maskedWord = words[endWordIndex] & endMask;
				if (maskedWord != 0)
				{
					return endWordIndex * 64 + BitOperations.TrailingZeroCount(maskedWord);
				}
			}

			return -1;
		}

		/// <summary>
		/// Enumerates set-bit indices in ascending order within the specified range.
		/// </summary>
		/// <param name="startIndex">Inclusive start of the range.</param>
		/// <param name="endIndex">Exclusive end of the range.</param>
		/// <returns>A deferred sequence that yields each set index once.</returns>
		public IEnumerable<int> SetBits(int startIndex, int endIndex)
		{
			while (true)
			{
				int next = NextSetBit(startIndex, endIndex);
				if (next == -1)
					break;
				yield return next;
				startIndex = next + 1;
			}
		}

		/// <summary>
		/// Replaces this instance with a copy of <paramref name="incoming"/>.
		/// </summary>
		/// <param name="incoming">The source set to copy from.</param>
		public void ReplaceWith(BitSet incoming)
		{
			Debug.Assert(words.Length == incoming.words.Length);
			Array.Copy(incoming.words, 0, words, 0, words.Length);
		}

		/// <summary>
		/// Returns a diagnostic representation of set indices.
		/// </summary>
		/// <returns>A comma-separated list enclosed in braces, truncated for very large sets.</returns>
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append('{');
			for (int i = 0; i < words.Length * BitsPerWord; i++)
			{
				if (this[i])
				{
					if (b.Length > 1)
						b.Append(", ");
					if (b.Length > 500)
					{
						b.Append("...");
						break;
					}
					b.Append(i);
				}
			}
			b.Append('}');
			return b.ToString();
		}
	}
}

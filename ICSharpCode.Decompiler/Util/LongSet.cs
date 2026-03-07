#nullable enable
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Represents an immutable set of <see cref="long"/> values stored as normalized, non-overlapping intervals.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The interval representation keeps dense ranges compact and enables set algebra without materializing each value.
	/// This is especially important in the decompiler's control-flow analysis where domains can cover very large numeric
	/// ranges (for example switch labels and state-machine states).
	/// </para>
	/// <para>
	/// Every constructor normalizes its input so intervals are sorted, non-empty, non-overlapping, and non-touching.
	/// The <see cref="Intervals"/> field therefore provides a canonical representation for equality checks.
	/// </para>
	/// </remarks>
	public struct LongSet : IEquatable<LongSet>
	{
		/// <summary>
		/// The intervals in this set of longs.
		/// </summary>
		/// <remarks>
		/// Invariant: the intervals in this array are non-empty, non-overlapping, non-touching, and sorted.
		/// 
		/// This invariant ensures every LongSet is always in a normalized representation.
		/// </remarks>
		public readonly ImmutableArray<LongInterval> Intervals;

		private LongSet(ImmutableArray<LongInterval> intervals)
		{
			this.Intervals = intervals;
#if DEBUG
			// Check invariant
			long minValue = long.MinValue;
			for (int i = 0; i < intervals.Length; i++)
			{
				Debug.Assert(!intervals[i].IsEmpty);
				Debug.Assert(minValue <= intervals[i].Start);
				if (intervals[i].InclusiveEnd == long.MaxValue - 1 || intervals[i].InclusiveEnd == long.MaxValue)
				{
					// An inclusive end of long.MaxValue-1 or long.MaxValue means (after the gap of 1 element),
					// there isn't any room for more non-empty intervals.
					Debug.Assert(i == intervals.Length - 1);
				}
				else
				{
					minValue = checked(intervals[i].End + 1); // enforce least 1 gap between intervals
				}
			}
#endif
		}

		/// <summary>
		/// Creates a set containing exactly <paramref name="value"/>.
		/// </summary>
		/// <param name="value">The single value to include.</param>
		public LongSet(long value)
			: this(ImmutableArray.Create(LongInterval.Inclusive(value, value)))
		{
		}

		/// <summary>
		/// Create a new LongSet that contains the values from the interval.
		/// </summary>
		/// <param name="interval">The interval whose values are included in the resulting set.</param>
		public LongSet(LongInterval interval)
			: this(interval.IsEmpty ? Empty.Intervals : ImmutableArray.Create(interval))
		{
		}

		/// <summary>
		/// Creates a new LongSet the contains the values from the specified intervals.
		/// </summary>
		/// <param name="intervals">
		/// Intervals to include. Empty intervals are ignored; overlapping or adjacent intervals are merged.
		/// </param>
		public LongSet(IEnumerable<LongInterval> intervals)
			: this(MergeOverlapping(intervals.Where(i => !i.IsEmpty).OrderBy(i => i.Start)).ToImmutableArray())
		{
		}

		/// <summary>
		/// The empty LongSet.
		/// </summary>
		public static readonly LongSet Empty = new LongSet(ImmutableArray.Create<LongInterval>());

		/// <summary>
		/// The LongSet that contains all possible long values.
		/// </summary>
		public static readonly LongSet Universe = new LongSet(LongInterval.Inclusive(long.MinValue, long.MaxValue));

		/// <summary>
		/// Gets whether this set contains no values.
		/// </summary>
		public bool IsEmpty {
			get { return Intervals.IsEmpty; }
		}

		/// <summary>
		/// Gets the number of values in this LongSet.
		/// Note: for <c>LongSet.Universe</c>, the number of values does not fit into <c>ulong</c>.
		/// Instead, this property returns the off-by-one value <c>ulong.MaxValue</c> to avoid overflow.
		/// </summary>
		/// <returns>
		/// The number of represented values, or <see cref="ulong.MaxValue"/> for the full universe because
		/// <c>2^64</c> cannot be represented by <see cref="ulong"/>.
		/// </returns>
		public ulong Count()
		{
			unchecked
			{
				ulong count = 0;
				foreach (var interval in Intervals)
				{
					count += (ulong)(interval.End - interval.Start);
				}
				if (count == 0 && !Intervals.IsEmpty)
					return ulong.MaxValue;
				else
					return count;
			}
		}

		IEnumerable<LongInterval> DoIntersectWith(LongSet other)
		{
			var enumA = this.Intervals.GetEnumerator();
			var enumB = other.Intervals.GetEnumerator();
			bool moreA = enumA.MoveNext();
			bool moreB = enumB.MoveNext();
			while (moreA && moreB)
			{
				LongInterval a = enumA.Current;
				LongInterval b = enumB.Current;
				LongInterval intersection = a.Intersect(b);
				if (!intersection.IsEmpty)
				{
					yield return intersection;
				}
				if (a.InclusiveEnd < b.InclusiveEnd)
				{
					moreA = enumA.MoveNext();
				}
				else
				{
					moreB = enumB.MoveNext();
				}
			}
		}

		/// <summary>
		/// Determines whether this set and <paramref name="other"/> share at least one value.
		/// </summary>
		/// <param name="other">The set to test for overlap.</param>
		/// <returns><see langword="true"/> when the intersection is non-empty; otherwise <see langword="false"/>.</returns>
		public bool Overlaps(LongSet other)
		{
			return DoIntersectWith(other).Any();
		}

		/// <summary>
		/// Creates a set containing the intersection of this set and <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The set to intersect with this instance.</param>
		/// <returns>A normalized set that contains values present in both sets.</returns>
		public LongSet IntersectWith(LongSet other)
		{
			return new LongSet(DoIntersectWith(other).ToImmutableArray());
		}

		/// <summary>
		/// Given an enumerable of non-empty intervals sorted by the starting position,
		/// merges overlapping or touching intervals to create a valid interval array for LongSet.
		/// </summary>
		static IEnumerable<LongInterval> MergeOverlapping(IEnumerable<LongInterval> input)
		{
			long start = long.MinValue;
			long end = long.MinValue;
			bool empty = true;
			foreach (var element in input)
			{
				Debug.Assert(start <= element.Start);
				Debug.Assert(!element.IsEmpty);

				if (!empty && element.Start <= end)
				{
					// element overlaps or touches [start, end), so combine the intervals:
					if (element.End == long.MinValue)
					{
						// special case: element goes all the way up to long.MaxValue inclusive
						end = long.MinValue;
					}
					else
					{
						end = Math.Max(end, element.End);
					}
				}
				else
				{
					// flush existing interval:
					if (!empty)
					{
						yield return new LongInterval(start, end);
					}
					else
					{
						empty = false;
					}
					start = element.Start;
					end = element.End;
				}
				if (end == long.MinValue)
				{
					// special case: element goes all the way up to long.MaxValue inclusive
					// all further intervals in the input must be contained in [start, end),
					// so ignore them (and avoid trouble due to the overflow in `end`).
					break;
				}
			}
			if (!empty)
			{
				yield return new LongInterval(start, end);
			}
		}

		/// <summary>
		/// Creates a set containing the union of this set and <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The set to union with this instance.</param>
		/// <returns>A normalized set that contains values present in either input.</returns>
		public LongSet UnionWith(LongSet other)
		{
			var mergedIntervals = this.Intervals.Merge(other.Intervals, (a, b) => a.Start.CompareTo(b.Start));
			return new LongSet(MergeOverlapping(mergedIntervals).ToImmutableArray());
		}

		/// <summary>
		/// Creates a new LongSet where val is added to each element of this LongSet.
		/// </summary>
		/// <param name="val">Offset added to every element using unchecked arithmetic.</param>
		/// <returns>
		/// A new set where each original value <c>x</c> is transformed to <c>unchecked(x + val)</c>.
		/// </returns>
		/// <remarks>
		/// Overflow wraps around, so a single interval can split into two intervals near numeric boundaries.
		/// </remarks>
		public LongSet AddOffset(long val)
		{
			if (val == 0)
			{
				return this;
			}
			var newIntervals = new List<LongInterval>(Intervals.Length + 1);
			foreach (var element in Intervals)
			{
				long newStart = unchecked(element.Start + val);
				long newInclusiveEnd = unchecked(element.InclusiveEnd + val);
				if (newStart <= newInclusiveEnd)
				{
					newIntervals.Add(LongInterval.Inclusive(newStart, newInclusiveEnd));
				}
				else
				{
					// interval got split by integer overflow
					newIntervals.Add(LongInterval.Inclusive(newStart, long.MaxValue));
					newIntervals.Add(LongInterval.Inclusive(long.MinValue, newInclusiveEnd));
				}
			}
			newIntervals.Sort((a, b) => a.Start.CompareTo(b.Start));
			return new LongSet(MergeOverlapping(newIntervals).ToImmutableArray());
		}

		/// <summary>
		/// Creates a new set that contains all values that are in <c>this</c>, but not in <c>other</c>.
		/// </summary>
		/// <param name="other">The set whose values should be removed.</param>
		/// <returns>A set representing <c>this \ other</c>.</returns>
		public LongSet ExceptWith(LongSet other)
		{
			return IntersectWith(other.Invert());
		}

		/// <summary>
		/// Creates a new LongSet that contains all elements not contained in this LongSet.
		/// </summary>
		/// <returns>The complement of this set relative to <see cref="Universe"/>.</returns>
		public LongSet Invert()
		{
			// The loop below assumes a non-empty LongSet, so handle the empty case specially.
			if (IsEmpty)
			{
				return Universe;
			}
			List<LongInterval> newIntervals = new List<LongInterval>(Intervals.Length + 1);
			long prevEnd = long.MinValue; // previous exclusive end
			foreach (var interval in Intervals)
			{
				if (interval.Start > prevEnd)
				{
					newIntervals.Add(new LongInterval(prevEnd, interval.Start));
				}
				prevEnd = interval.End;
			}
			// create a final interval up to long.MaxValue inclusive
			if (prevEnd != long.MinValue)
			{
				newIntervals.Add(new LongInterval(prevEnd, long.MinValue));
			}
			return new LongSet(newIntervals.ToImmutableArray());
		}

		/// <summary>
		/// Gets whether this set is a subset of other, or equal.
		/// </summary>
		/// <param name="other">The candidate superset.</param>
		/// <returns>
		/// <see langword="true"/> when every value in this set is contained in <paramref name="other"/>;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsSubsetOf(LongSet other)
		{
			// TODO: optimize IsSubsetOf -- there's no need to build a temporary set
			return this.UnionWith(other).SetEquals(other);
		}

		/// <summary>
		/// Gets whether this set is a superset of other, or equal.
		/// </summary>
		/// <param name="other">The candidate subset.</param>
		/// <returns>
		/// <see langword="true"/> when every value in <paramref name="other"/> is contained in this set;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsSupersetOf(LongSet other)
		{
			return other.IsSubsetOf(this);
		}

		/// <summary>
		/// Gets whether this set is a strict subset of <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The candidate strict superset.</param>
		/// <returns>
		/// <see langword="true"/> when this set is a subset of <paramref name="other"/> and both are not equal;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsProperSubsetOf(LongSet other)
		{
			return IsSubsetOf(other) && !SetEquals(other);
		}

		/// <summary>
		/// Gets whether this set is a strict superset of <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The candidate strict subset.</param>
		/// <returns>
		/// <see langword="true"/> when this set is a superset of <paramref name="other"/> and both are not equal;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool IsProperSupersetOf(LongSet other)
		{
			return IsSupersetOf(other) && !SetEquals(other);
		}

		/// <summary>
		/// Determines whether <paramref name="val"/> is contained in this set.
		/// </summary>
		/// <param name="val">The value to test.</param>
		/// <returns><see langword="true"/> when <paramref name="val"/> is present; otherwise <see langword="false"/>.</returns>
		public bool Contains(long val)
		{
			int index = upper_bound(val);
			return index > 0 && Intervals[index - 1].Contains(val);
		}

		internal int upper_bound(long val)
		{
			int min = 0, max = Intervals.Length - 1;
			while (max >= min)
			{
				int m = min + (max - min) / 2;
				LongInterval i = Intervals[m];
				if (val < i.Start)
				{
					max = m - 1;
					continue;
				}
				if (val > i.End)
				{
					min = m + 1;
					continue;
				}
				return m + 1;
			}
			return min;
		}

		/// <summary>
		/// Enumerates all represented values in ascending order.
		/// </summary>
		/// <remarks>
		/// The sequence is deferred and can be extremely large for wide intervals.
		/// </remarks>
		public IEnumerable<long> Values {
			get { return Intervals.SelectMany(i => i.Range()); }
		}

		/// <summary>
		/// Computes the smallest interval that contains every value in this set.
		/// </summary>
		/// <returns>
		/// A half-open interval from the first interval's start to the last interval's end, or the default empty interval
		/// when this set is empty.
		/// </returns>
		public LongInterval ContainingInterval()
		{
			if (IsEmpty)
				return default;
			return new LongInterval(Intervals[0].Start, Intervals[Intervals.Length - 1].End);
		}

		/// <summary>
		/// Returns a string representation of the normalized interval list.
		/// </summary>
		/// <returns>A comma-separated list of interval fragments.</returns>
		public override string ToString()
		{
			return string.Join(",", Intervals);
		}

		#region Equals and GetHashCode implementation
		public override bool Equals(object? obj)
		{
			return obj is LongSet && SetEquals((LongSet)obj);
		}

		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}

		[Obsolete("Explicitly call SetEquals() instead.")]
		public bool Equals(LongSet other)
		{
			return SetEquals(other);
		}

		/// <summary>
		/// Determines whether this set and <paramref name="other"/> represent the same normalized interval sequence.
		/// </summary>
		/// <param name="other">The set to compare against this instance.</param>
		/// <returns><see langword="true"/> when both sets contain identical intervals; otherwise <see langword="false"/>.</returns>
		public bool SetEquals(LongSet other)
		{
			if (Intervals.Length != other.Intervals.Length)
				return false;
			for (int i = 0; i < Intervals.Length; i++)
			{
				if (Intervals[i] != other.Intervals[i])
					return false;
			}
			return true;
		}
		#endregion
	}
}

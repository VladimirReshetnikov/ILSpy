#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Provides allocation-conscious helper extensions used throughout the decompiler pipeline.
	/// </summary>
	static class CollectionExtensions
	{
		/// <summary>
		/// Supports tuple deconstruction syntax for <see cref="KeyValuePair{TKey, TValue}"/> values.
		/// </summary>
		/// <param name="pair">Pair to deconstruct.</param>
		/// <param name="key">Receives <see cref="KeyValuePair{TKey, TValue}.Key"/>.</param>
		/// <param name="value">Receives <see cref="KeyValuePair{TKey, TValue}.Value"/>.</param>
		public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value)
		{
			key = pair.Key;
			value = pair.Value;
		}

#if !NET8_0_OR_GREATER
		public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> input1, IEnumerable<B> input2)
		{
			return input1.Zip(input2, (a, b) => (a, b));
		}
#endif

		/// <summary>
		/// Zips two sequences and emits the zero-based tuple index alongside each pair.
		/// </summary>
		/// <param name="input1">First source sequence.</param>
		/// <param name="input2">Second source sequence.</param>
		/// <returns>A lazy sequence of <c>(index, first, second)</c> tuples.</returns>
		public static IEnumerable<(int, A, B)> ZipWithIndex<A, B>(this IEnumerable<A> input1, IEnumerable<B> input2)
		{
			int index = 0;
			return input1.Zip(input2, (a, b) => (index++, a, b));
		}

		/// <summary>
		/// Zips two sequences until both are exhausted, filling missing items with <see langword="default"/>.
		/// </summary>
		/// <param name="input1">First source sequence.</param>
		/// <param name="input2">Second source sequence.</param>
		/// <returns>A lazy sequence whose length equals the longer input sequence.</returns>
		public static IEnumerable<(A?, B?)> ZipLongest<A, B>(this IEnumerable<A> input1, IEnumerable<B> input2)
		{
			using (var it1 = input1.GetEnumerator())
			{
				using (var it2 = input2.GetEnumerator())
				{
					bool hasElements1 = true;
					bool hasElements2 = true;
					while (true)
					{
						if (hasElements1)
							hasElements1 = it1.MoveNext();
						if (hasElements2)
							hasElements2 = it2.MoveNext();
						if (!(hasElements1 || hasElements2))
							break;
						yield return ((hasElements1 ? it1.Current : default), (hasElements2 ? it2.Current : default));
					}
				}
			}
		}

		public static IEnumerable<T> Slice<T>(this IReadOnlyList<T> input, int offset, int length)
		{
			for (int i = offset; i < offset + length; i++)
			{
				yield return input[i];
			}
		}

		public static IEnumerable<T> Slice<T>(this IReadOnlyList<T> input, int offset)
		{
			int length = input.Count;
			for (int i = offset; i < length; i++)
			{
				yield return input[i];
			}
		}

#if !NET8_0_OR_GREATER
		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> input)
		{
			return new HashSet<T>(input);
		}
#endif

		public static IEnumerable<T> SkipLast<T>(this IReadOnlyCollection<T> input, int count)
		{
			return input.Take(input.Count - count);
		}

		public static IEnumerable<T> TakeLast<T>(this IReadOnlyCollection<T> input, int count)
		{
			return input.Skip(input.Count - count);
		}

		public static T? PopOrDefault<T>(this Stack<T> stack)
		{
			if (stack.Count == 0)
				return default(T);
			return stack.Pop();
		}

		public static T? PeekOrDefault<T>(this Stack<T> stack)
		{
			if (stack.Count == 0)
				return default(T);
			return stack.Peek();
		}

		/// <summary>
		/// Computes the maximum projected value, or a fallback for empty sequences.
		/// </summary>
		/// <param name="input">Source sequence.</param>
		/// <param name="selector">Projection that maps each element to an <see cref="int"/> key.</param>
		/// <param name="defaultValue">Value returned when <paramref name="input"/> is empty.</param>
		/// <returns>The largest projected value or <paramref name="defaultValue"/>.</returns>
		public static int MaxOrDefault<T>(this IEnumerable<T> input, Func<T, int> selector, int defaultValue = 0)
		{
			int max = defaultValue;
			foreach (var element in input)
			{
				int value = selector(element);
				if (value > max)
					max = value;
			}
			return max;
		}

		public static int IndexOf<T>(this IReadOnlyList<T> collection, T value)
		{
			var comparer = EqualityComparer<T>.Default;
			int index = 0;
			foreach (T item in collection)
			{
				if (comparer.Equals(item, value))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> input)
		{
			foreach (T item in input)
				collection.Add(item);
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToArray()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input collection that provides the elements and target array size.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>An array containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static U[] SelectArray<T, U>(this ICollection<T> collection, Func<T, U> func)
		{
			U[] result = new U[collection.Count];
			int index = 0;
			foreach (var element in collection)
			{
				result[index++] = func(element);
			}
			return result;
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToImmutableArray()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input collection that provides the elements and builder capacity hint.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>An immutable array containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static ImmutableArray<U> SelectImmutableArray<T, U>(this IReadOnlyCollection<T> collection, Func<T, U> func)
		{
			var builder = ImmutableArray.CreateBuilder<U>(collection.Count);
			foreach (var element in collection)
			{
				builder.Add(func(element));
			}
			return builder.MoveToImmutable();
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToArray()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input collection that provides the elements and target array size.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>An array containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static U[] SelectReadOnlyArray<T, U>(this IReadOnlyCollection<T> collection, Func<T, U> func)
		{
			U[] result = new U[collection.Count];
			int index = 0;
			foreach (var element in collection)
			{
				result[index++] = func(element);
			}
			return result;
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToArray()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input list that provides the elements and target array size.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>An array containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static U[] SelectArray<T, U>(this List<T> collection, Func<T, U> func)
		{
			U[] result = new U[collection.Count];
			int index = 0;
			foreach (var element in collection)
			{
				result[index++] = func(element);
			}
			return result;
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToArray()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input array that provides the elements and target array size.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>An array containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static U[] SelectArray<T, U>(this T[] collection, Func<T, U> func)
		{
			U[] result = new U[collection.Length];
			int index = 0;
			foreach (var element in collection)
			{
				result[index++] = func(element);
			}
			return result;
		}

		/// <summary>
		/// Equivalent to <code>collection.Select(func).ToList()</code>, but more efficient as it makes
		/// use of the input collection's known size.
		/// </summary>
		/// <param name="collection">Input collection that provides the elements and initial list capacity.</param>
		/// <param name="func">Projection applied to every input element.</param>
		/// <returns>A list containing one projected value for each element in <paramref name="collection"/>.</returns>
		public static List<U> SelectList<T, U>(this ICollection<T> collection, Func<T, U> func)
		{
			List<U> result = new List<U>(collection.Count);
			foreach (var element in collection)
			{
				result.Add(func(element));
			}
			return result;
		}

		public static IEnumerable<U> SelectWithIndex<T, U>(this IEnumerable<T> source, Func<int, T, U> func)
		{
			int index = 0;
			foreach (var element in source)
				yield return func(index++, element);
		}

		public static IEnumerable<(int, T)> WithIndex<T>(this IEnumerable<T> source)
		{
			int index = 0;
			foreach (var item in source)
			{
				yield return (index, item);
				index++;
			}
		}

		/// <summary>
		/// The merge step of merge sort.
		/// </summary>
		/// <param name="input1">First input sequence, expected to be sorted with the same ordering as <paramref name="comparison"/>.</param>
		/// <param name="input2">Second input sequence, expected to be sorted with the same ordering as <paramref name="comparison"/>.</param>
		/// <param name="comparison">Ordering function used to interleave values from the two sorted input sequences.</param>
		/// <returns>A lazily-evaluated merged sequence that preserves sorted order.</returns>
		public static IEnumerable<T> Merge<T>(this IEnumerable<T> input1, IEnumerable<T> input2, Comparison<T> comparison)
		{
			using (var enumA = input1.GetEnumerator())
			using (var enumB = input2.GetEnumerator())
			{
				bool moreA = enumA.MoveNext();
				bool moreB = enumB.MoveNext();
				while (moreA && moreB)
				{
					if (comparison(enumA.Current, enumB.Current) <= 0)
					{
						yield return enumA.Current;
						moreA = enumA.MoveNext();
					}
					else
					{
						yield return enumB.Current;
						moreB = enumB.MoveNext();
					}
				}
				while (moreA)
				{
					yield return enumA.Current;
					moreA = enumA.MoveNext();
				}
				while (moreB)
				{
					yield return enumB.Current;
					moreB = enumB.MoveNext();
				}
			}
		}

		/// <summary>
		/// Returns the minimum element.
		/// </summary>
		/// <param name="source">Sequence to scan.</param>
		/// <param name="keySelector">Selector that produces the key used for ordering elements.</param>
		/// <returns>The element whose key is smallest according to the default comparer for <typeparamref name="K"/>.</returns>
		/// <exception cref="InvalidOperationException">The input sequence is empty</exception>
		public static T MinBy<T, K>(this IEnumerable<T> source, Func<T, K> keySelector) where K : IComparable<K>
		{
			return source.MinBy(keySelector, Comparer<K>.Default);
		}

		/// <summary>
		/// Returns the minimum element.
		/// </summary>
		/// <param name="source">Sequence to scan.</param>
		/// <param name="keySelector">Selector that produces the key used for ordering elements.</param>
		/// <param name="keyComparer">Comparer used to compare keys. If <see langword="null"/>, <see cref="Comparer{T}.Default"/> is used.</param>
		/// <returns>The element whose key is smallest according to <paramref name="keyComparer"/>.</returns>
		/// <exception cref="InvalidOperationException">The input sequence is empty</exception>
		public static T MinBy<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, IComparer<K>? keyComparer)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			if (keyComparer == null)
				keyComparer = Comparer<K>.Default;
			using (var enumerator = source.GetEnumerator())
			{
				if (!enumerator.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");
				T minElement = enumerator.Current;
				K minKey = keySelector(minElement);
				while (enumerator.MoveNext())
				{
					T element = enumerator.Current;
					K key = keySelector(element);
					if (keyComparer.Compare(key, minKey) < 0)
					{
						minElement = element;
						minKey = key;
					}
				}
				return minElement;
			}
		}

#if !NET8_0_OR_GREATER
		/// <summary>
		/// Returns the maximum element.
		/// </summary>
		/// <param name="source">Sequence to scan.</param>
		/// <param name="keySelector">Selector that produces the key used for ordering elements.</param>
		/// <returns>The element whose key is largest according to the default comparer for <typeparamref name="K"/>.</returns>
		/// <exception cref="InvalidOperationException">The input sequence is empty</exception>
		public static T MaxBy<T, K>(this IEnumerable<T> source, Func<T, K> keySelector) where K : IComparable<K>
		{
			return source.MaxBy(keySelector, Comparer<K>.Default);
		}
#endif
		/// <summary>
		/// Returns the maximum element.
		/// </summary>
		/// <param name="source">Sequence to scan.</param>
		/// <param name="keySelector">Selector that produces the key used for ordering elements.</param>
		/// <param name="keyComparer">Comparer used to compare keys. If <see langword="null"/>, <see cref="Comparer{T}.Default"/> is used.</param>
		/// <returns>The element whose key is largest according to <paramref name="keyComparer"/>.</returns>
		/// <exception cref="InvalidOperationException">The input sequence is empty</exception>
		public static T MaxBy<T, K>(this IEnumerable<T> source, Func<T, K> keySelector, IComparer<K>? keyComparer)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			if (keyComparer == null)
				keyComparer = Comparer<K>.Default;
			using (var enumerator = source.GetEnumerator())
			{
				if (!enumerator.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");
				T maxElement = enumerator.Current;
				K maxKey = keySelector(maxElement);
				while (enumerator.MoveNext())
				{
					T element = enumerator.Current;
					K key = keySelector(element);
					if (keyComparer.Compare(key, maxKey) > 0)
					{
						maxElement = element;
						maxKey = key;
					}
				}
				return maxElement;
			}
		}

		public static void RemoveLast<T>(this IList<T> list)
		{
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			list.RemoveAt(list.Count - 1);
		}

		/// <summary>
		/// Returns the only element in a sequence that matches <paramref name="predicate"/>, or <see langword="default"/>.
		/// </summary>
		/// <param name="source">Source sequence.</param>
		/// <param name="predicate">Predicate used to select candidate elements.</param>
		/// <returns>
		/// The matching element when exactly one item satisfies the predicate; otherwise <see langword="default"/>.
		/// </returns>
		public static T? OnlyOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate) => OnlyOrDefault(source.Where(predicate));

		/// <summary>
		/// Returns the only element in a sequence, or <see langword="default"/> when cardinality is not one.
		/// </summary>
		/// <param name="source">Source sequence.</param>
		/// <returns>
		/// The single sequence element when exactly one element exists; otherwise <see langword="default"/>.
		/// </returns>
		public static T? OnlyOrDefault<T>(this IEnumerable<T> source)
		{
			bool any = false;
			T? first = default;
			foreach (var t in source)
			{
				if (any)
					return default(T);
				first = t;
				any = true;
			}

			return first;
		}

#if !NET8_0_OR_GREATER
		public static int EnsureCapacity<T>(this List<T> list, int capacity)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity));
			if (list.Capacity < capacity)
			{
				const int DefaultCapacity = 4;
				const int MaxLength = 0X7FFFFFC7;

				int newcapacity = list.Capacity == 0 ? DefaultCapacity : 2 * list.Capacity;

				if ((uint)newcapacity > MaxLength)
					newcapacity = MaxLength;

				if (newcapacity < capacity)
					newcapacity = capacity;

				list.Capacity = newcapacity;
			}

			return list.Capacity;
		}
#endif

		#region Aliases/shortcuts for Enumerable extension methods
		public static bool Any<T>(this ICollection<T> list) => list.Count > 0;
		public static bool Any<T>(this T[] array, Predicate<T> match) => Array.Exists(array, match);
		public static bool Any<T>(this List<T> list, Predicate<T> match) => list.Exists(match);

		public static bool All<T>(this T[] array, Predicate<T> match) => Array.TrueForAll(array, match);
		public static bool All<T>(this List<T> list, Predicate<T> match) => list.TrueForAll(match);

		public static T? FirstOrDefault<T>(this T[] array, Predicate<T> predicate) => Array.Find(array, predicate);
		public static T? FirstOrDefault<T>(this List<T> list, Predicate<T> predicate) => list.Find(predicate);

		public static T Last<T>(this IList<T> list) => list[list.Count - 1];
		#endregion
	}
}

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

using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Creates comparers that compare complex elements by a selected key.
	/// </summary>
	/// <remarks>
	/// The decompiler uses these helpers when collections contain tuples or rich objects but ordering/equality should
	/// be driven by one specific field (for example queue deduplication in reference resolution).
	/// </remarks>
	public static class KeyComparer
	{
		/// <summary>
		/// Creates a comparer/equality-comparer pair that uses default key comparers.
		/// </summary>
		/// <typeparam name="TElement">Element type to compare.</typeparam>
		/// <typeparam name="TKey">Key type extracted from each element.</typeparam>
		/// <param name="keySelector">Function that extracts the comparison key from an element.</param>
		/// <returns>A <see cref="KeyComparer{TElement, TKey}"/> configured with default key comparers.</returns>
		public static KeyComparer<TElement, TKey> Create<TElement, TKey>(Func<TElement, TKey> keySelector)
		{
			return new KeyComparer<TElement, TKey>(keySelector, Comparer<TKey>.Default, EqualityComparer<TKey>.Default);
		}

		/// <summary>
		/// Creates a comparer/equality-comparer pair with explicit key comparers.
		/// </summary>
		/// <typeparam name="TElement">Element type to compare.</typeparam>
		/// <typeparam name="TKey">Key type extracted from each element.</typeparam>
		/// <param name="keySelector">Function that extracts the comparison key from an element.</param>
		/// <param name="comparer">Comparer used for ordering comparisons.</param>
		/// <param name="equalityComparer">Comparer used for equality and hash code calculation.</param>
		/// <returns>A <see cref="KeyComparer{TElement, TKey}"/> with caller-supplied key comparers.</returns>
		public static KeyComparer<TElement, TKey> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, IEqualityComparer<TKey> equalityComparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, comparer, equalityComparer);
		}

		/// <summary>
		/// Creates an <see cref="IComparer{T}"/> that orders elements by key.
		/// </summary>
		public static IComparer<TElement> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, comparer, EqualityComparer<TKey>.Default);
		}

		/// <summary>
		/// Creates an <see cref="IEqualityComparer{T}"/> that compares elements by key.
		/// </summary>
		public static IEqualityComparer<TElement> Create<TElement, TKey>(Func<TElement, TKey> keySelector, IEqualityComparer<TKey> equalityComparer)
		{
			return new KeyComparer<TElement, TKey>(keySelector, Comparer<TKey>.Default, equalityComparer);
		}

		/// <summary>
		/// Sorts a list in-place by a projected key using default key ordering.
		/// </summary>
		/// <typeparam name="TElement">Element type stored in the list.</typeparam>
		/// <typeparam name="TKey">Key type used for ordering.</typeparam>
		/// <param name="list">List to sort.</param>
		/// <param name="keySelector">Function that extracts sort keys.</param>
		public static void SortBy<TElement, TKey>(this List<TElement> list, Func<TElement, TKey> keySelector)
		{
			list.Sort(Create(keySelector));
		}
	}

	/// <summary>
	/// Implements ordering and equality by applying comparers to keys projected from elements.
	/// </summary>
	/// <typeparam name="TElement">Element type being compared.</typeparam>
	/// <typeparam name="TKey">Key type used for ordering/equality.</typeparam>
	public class KeyComparer<TElement, TKey> : IComparer<TElement>, IEqualityComparer<TElement>
	{
		readonly Func<TElement, TKey> keySelector;
		readonly IComparer<TKey> keyComparer;
		readonly IEqualityComparer<TKey> keyEqualityComparer;

		/// <summary>
		/// Initializes a key-based comparer.
		/// </summary>
		/// <param name="keySelector">Function that extracts keys from elements.</param>
		/// <param name="keyComparer">Comparer used for ordering keys.</param>
		/// <param name="keyEqualityComparer">Comparer used for key equality and hash codes.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="keySelector"/>, <paramref name="keyComparer"/>, or <paramref name="keyEqualityComparer"/>
		/// is <see langword="null"/>.
		/// </exception>
		public KeyComparer(Func<TElement, TKey> keySelector, IComparer<TKey> keyComparer, IEqualityComparer<TKey> keyEqualityComparer)
		{
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			if (keyComparer == null)
				throw new ArgumentNullException(nameof(keyComparer));
			if (keyEqualityComparer == null)
				throw new ArgumentNullException(nameof(keyEqualityComparer));
			this.keySelector = keySelector;
			this.keyComparer = keyComparer;
			this.keyEqualityComparer = keyEqualityComparer;
		}

		/// <summary>
		/// Compares two elements by comparing their projected keys.
		/// </summary>
		public int Compare(TElement? x, TElement? y)
		{
			return keyComparer.Compare(keySelector(x!), keySelector(y!));
		}

		/// <summary>
		/// Determines equality by comparing projected keys.
		/// </summary>
		public bool Equals(TElement? x, TElement? y)
		{
			return keyEqualityComparer.Equals(keySelector(x!), keySelector(y!));
		}

		/// <summary>
		/// Produces a hash code from the projected key.
		/// </summary>
		public int GetHashCode(TElement obj)
		{
			var key = keySelector(obj)!;
			return keyEqualityComparer.GetHashCode(key);
		}
	}
}

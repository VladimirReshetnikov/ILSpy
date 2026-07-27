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
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Represents a singleton, allocation-free empty list that can be reused anywhere an <see cref="IList{T}"/> is required.
	/// </summary>
	/// <typeparam name="T">Element type of the list.</typeparam>
	/// <remarks>
	/// <para>
	/// This type intentionally implements both collection and enumerator interfaces on the same singleton instance.
	/// Consumers can obtain <see cref="Instance"/> repeatedly without allocating either a backing store or a separate enumerator.
	/// </para>
	/// <para>
	/// Mutating APIs follow the usual read-only collection behavior:
	/// <list type="bullet">
	/// <item><description>indexers throw <see cref="ArgumentOutOfRangeException"/> because no valid position exists,</description></item>
	/// <item><description>members that would add or remove by position throw <see cref="NotSupportedException"/>, while <see cref="ICollection{T}.Remove"/> simply returns <see langword="false"/>,</description></item>
	/// <item><description><see cref="ICollection{T}.Clear"/> is a no-op because the collection is already empty.</description></item>
	/// </list>
	/// </para>
	/// </remarks>
	[Serializable]
	public sealed class EmptyList<T> : IList<T>, IEnumerator<T>, IReadOnlyList<T>
	{
		/// <summary>
		/// Gets the singleton empty list instance for <typeparamref name="T"/>.
		/// </summary>
		public static readonly EmptyList<T> Instance = new EmptyList<T>();

		private EmptyList() { }

		/// <summary>
		/// Gets or sets an item by index.
		/// </summary>
		/// <param name="index">Requested index.</param>
		/// <returns>Never returns because the list is always empty.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Always thrown for both getter and setter.</exception>
		public T this[int index] {
			get { throw new ArgumentOutOfRangeException(nameof(index)); }
			set { throw new ArgumentOutOfRangeException(nameof(index)); }
		}

		/// <summary>
		/// Gets the number of elements in the list.
		/// </summary>
		/// <value>Always <c>0</c>.</value>
		public int Count {
			get { return 0; }
		}

		bool ICollection<T>.IsReadOnly {
			get { return true; }
		}

		/// <summary>
		/// Determines the index of an item in the list.
		/// </summary>
		/// <param name="item">Item to locate.</param>
		/// <returns>Always <c>-1</c> because no item can be present.</returns>
		int IList<T>.IndexOf(T item)
		{
			return -1;
		}

		/// <summary>
		/// Inserts an item at a specific index.
		/// </summary>
		/// <param name="index">Insertion index.</param>
		/// <param name="item">Item to insert.</param>
		/// <exception cref="NotSupportedException">Always thrown because the collection is immutable.</exception>
		void IList<T>.Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes the item at the specified index.
		/// </summary>
		/// <param name="index">Index to remove.</param>
		/// <exception cref="NotSupportedException">Always thrown because the collection is immutable.</exception>
		void IList<T>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">Item to add.</param>
		/// <exception cref="NotSupportedException">Always thrown because the collection is immutable.</exception>
		void ICollection<T>.Add(T item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		/// <remarks>
		/// This method is intentionally a no-op because the collection is already empty.
		/// </remarks>
		void ICollection<T>.Clear()
		{
		}

		/// <summary>
		/// Determines whether the collection contains a specific value.
		/// </summary>
		/// <param name="item">Item to look up.</param>
		/// <returns>Always <see langword="false"/>.</returns>
		bool ICollection<T>.Contains(T item)
		{
			return false;
		}

		/// <summary>
		/// Copies the elements of the collection to an array.
		/// </summary>
		/// <param name="array">Destination array.</param>
		/// <param name="arrayIndex">Index in <paramref name="array"/> where copying would start.</param>
		/// <remarks>
		/// This method performs no writes because the collection contains no elements.
		/// </remarks>
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the collection.
		/// </summary>
		/// <param name="item">Item to remove.</param>
		/// <returns>Always <see langword="false"/> because there is nothing to remove.</returns>
		bool ICollection<T>.Remove(T item)
		{
			return false;
		}

		/// <summary>
		/// Returns a generic enumerator for the collection.
		/// </summary>
		/// <returns>The singleton instance itself, acting as an empty enumerator.</returns>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return this;
		}

		/// <summary>
		/// Returns a non-generic enumerator for the collection.
		/// </summary>
		/// <returns>The singleton instance itself, acting as an empty enumerator.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this;
		}

		/// <summary>
		/// Gets the current element in the enumeration.
		/// </summary>
		/// <value>Never returns because the enumeration is always empty.</value>
		/// <exception cref="NotSupportedException">Always thrown.</exception>
		T IEnumerator<T>.Current {
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the current element in the enumeration.
		/// </summary>
		/// <value>Never returns because the enumeration is always empty.</value>
		/// <exception cref="NotSupportedException">Always thrown.</exception>
		object IEnumerator.Current {
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Releases resources used by the enumerator.
		/// </summary>
		/// <remarks>
		/// This method is intentionally a no-op because the enumerator does not hold external resources.
		/// </remarks>
		[SuppressMessage("Usage", "CA1063:Implement IDisposable Correctly",
			Justification = "Explicit IDisposable implementation for IEnumerator<T>; intentional no-op for the singleton.")]
		void IDisposable.Dispose()
		{
		}

		/// <summary>
		/// Advances the enumerator to the next element.
		/// </summary>
		/// <returns>Always <see langword="false"/> because no elements exist.</returns>
		bool IEnumerator.MoveNext()
		{
			return false;
		}

		/// <summary>
		/// Sets the enumerator to its initial position.
		/// </summary>
		/// <remarks>
		/// This method is a no-op because the enumerator has no state beyond being permanently exhausted.
		/// </remarks>
		void IEnumerator.Reset()
		{
		}
	}

	/// <summary>
	/// Provides cached empty values for a type.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	public static class Empty<T>
	{
		/// <summary>
		/// Gets the shared zero-length array instance for <typeparamref name="T"/>.
		/// </summary>
		public static readonly T[] Array = System.Array.Empty<T>();
	}

	/// <summary>
	/// Represents a value-less placeholder type used for generic contexts that require a type argument.
	/// </summary>
	/// <remarks>
	/// This mirrors the "unit" concept from functional programming: the type has exactly one meaningful value.
	/// </remarks>
	public struct Unit { }
}

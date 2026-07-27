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
	/// Projects elements from an input list on demand and caches each projected value.
	/// </summary>
	/// <typeparam name="TInput">Element type stored in the source list.</typeparam>
	/// <typeparam name="TOutput">Reference type produced by the projection.</typeparam>
	/// <remarks>
	/// This type is used in the type-system layer to expose resolved objects as an <see cref="IReadOnlyList{T}"/>
	/// without eagerly materializing every projection result. Each index is computed on first access and the
	/// published result is reused; under concurrent first access the projection may run more than once, but only
	/// one result is published.
	/// </remarks>
	public sealed class ProjectedList<TInput, TOutput> : IReadOnlyList<TOutput> where TOutput : class
	{
		readonly IList<TInput> input;
		readonly Func<TInput, TOutput> projection;
		readonly TOutput?[] items;

		/// <summary>
		/// Initializes a new lazy projected list.
		/// </summary>
		/// <param name="input">Source list whose elements are projected on demand.</param>
		/// <param name="projection">Function that converts a source element into an output element.</param>
		/// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="projection"/> is <see langword="null"/>.</exception>
		public ProjectedList(IList<TInput> input, Func<TInput, TOutput> projection)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (projection == null)
				throw new ArgumentNullException(nameof(projection));
			this.input = input;
			this.projection = projection;
			this.items = new TOutput?[input.Count];
		}

		/// <summary>
		/// Gets the projected value at the specified index.
		/// </summary>
		/// <param name="index">Zero-based index in the source list.</param>
		/// <returns>
		/// The cached projection result for that index, computing and storing it the first time the index is accessed.
		/// </returns>
		public TOutput this[int index] {
			get {
				TOutput? output = LazyInit.VolatileRead(ref items[index]);
				if (output != null)
				{
					return output;
				}
				return LazyInit.GetOrSet(ref items[index], projection(input[index]));
			}
		}

		/// <summary>
		/// Gets the number of items in the projected list.
		/// </summary>
		public int Count {
			get { return items.Length; }
		}

		/// <summary>
		/// Returns an enumerator that projects elements in source order.
		/// </summary>
		public IEnumerator<TOutput> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>
	/// Projects elements from an input list using an additional shared context and caches each result.
	/// </summary>
	/// <typeparam name="TContext">Auxiliary context type captured once for all projections.</typeparam>
	/// <typeparam name="TInput">Element type stored in the source list.</typeparam>
	/// <typeparam name="TOutput">Reference type produced by the projection.</typeparam>
	/// <remarks>
	/// This variant avoids per-item closure allocations when projection logic needs both an external context object and
	/// the current source item.
	/// </remarks>
	public sealed class ProjectedList<TContext, TInput, TOutput> : IReadOnlyList<TOutput> where TOutput : class
	{
		readonly IList<TInput> input;
		readonly TContext context;
		readonly Func<TContext, TInput, TOutput> projection;
		readonly TOutput?[] items;

		/// <summary>
		/// Initializes a new lazy projected list with shared context.
		/// </summary>
		/// <param name="context">Context passed to each projection call.</param>
		/// <param name="input">Source list whose elements are projected on demand.</param>
		/// <param name="projection">Function that converts <c>(context, sourceElement)</c> into an output element.</param>
		/// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="projection"/> is <see langword="null"/>.</exception>
		public ProjectedList(TContext context, IList<TInput> input, Func<TContext, TInput, TOutput> projection)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (projection == null)
				throw new ArgumentNullException(nameof(projection));
			this.input = input;
			this.context = context;
			this.projection = projection;
			this.items = new TOutput?[input.Count];
		}

		/// <summary>
		/// Gets the projected value at the specified index.
		/// </summary>
		/// <param name="index">Zero-based index in the source list.</param>
		/// <returns>
		/// The cached projection result for that index, computing and storing it the first time the index is accessed.
		/// </returns>
		public TOutput this[int index] {
			get {
				TOutput? output = LazyInit.VolatileRead(ref items[index]);
				if (output != null)
				{
					return output;
				}
				return LazyInit.GetOrSet(ref items[index], projection(context, input[index]));
			}
		}

		/// <summary>
		/// Gets the number of items in the projected list.
		/// </summary>
		public int Count {
			get { return items.Length; }
		}

		/// <summary>
		/// Returns an enumerator that projects elements in source order.
		/// </summary>
		public IEnumerator<TOutput> GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

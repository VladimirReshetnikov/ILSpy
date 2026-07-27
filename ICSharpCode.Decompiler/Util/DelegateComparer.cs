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
	/// Adapts a comparison delegate to <see cref="IComparer{T}"/>.
	/// </summary>
	/// <typeparam name="T">Element type to compare.</typeparam>
	public class DelegateComparer<T> : IComparer<T>
	{
		private readonly Func<T?, T?, int> func;

		/// <summary>
		/// Initializes a comparer that delegates comparison to <paramref name="func"/>.
		/// </summary>
		/// <param name="func">
		/// Comparison function that must follow standard comparer semantics:
		/// negative for less-than, zero for equality, positive for greater-than.
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
		public DelegateComparer(Func<T?, T?, int> func)
		{
			this.func = func ?? throw new ArgumentNullException(nameof(func));
		}

		/// <summary>
		/// Compares two values using the delegate provided at construction time.
		/// </summary>
		/// <param name="x">First value to compare.</param>
		/// <param name="y">Second value to compare.</param>
		/// <returns>
		/// A signed integer whose sign indicates relative ordering between <paramref name="x"/> and <paramref name="y"/>.
		/// </returns>
		public int Compare(T? x, T? y)
		{
			return func(x, y);
		}
	}
}

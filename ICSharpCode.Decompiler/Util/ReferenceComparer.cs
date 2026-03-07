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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Compares objects using reference identity semantics.
	/// </summary>
	/// <remarks>
	/// Hash codes are produced with <see cref="RuntimeHelpers.GetHashCode(object)"/>, which remains stable for the object's lifetime and ignores
	/// custom <see cref="object.GetHashCode"/> implementations.
	/// </remarks>
	public sealed class ReferenceComparer : IEqualityComparer<object?>
	{
		/// <summary>
		/// Shared singleton instance.
		/// </summary>
		public readonly static ReferenceComparer Instance = new ReferenceComparer();

		/// <summary>
		/// Determines whether two references point to the same object instance.
		/// </summary>
		/// <param name="x">First object reference.</param>
		/// <param name="y">Second object reference.</param>
		/// <returns><see langword="true"/> when <paramref name="x"/> and <paramref name="y"/> are the same reference; otherwise <see langword="false"/>.</returns>
		public new bool Equals(object? x, object? y)
		{
			return x == y;
		}

		/// <summary>
		/// Returns an identity-based hash code for <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">Reference whose identity hash code should be returned.</param>
		/// <returns>The identity hash code used by hash-based collections configured with this comparer.</returns>
		public int GetHashCode(object? obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}

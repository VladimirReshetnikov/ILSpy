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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Defines structural identity used by <see cref="InterningProvider"/> when canonicalizing type-system objects.
	/// </summary>
	/// <remarks>
	/// Implementations should expose equality semantics that are stable for the immutable state used as interning key data.
	/// The methods on this interface are intentionally separate from ordinary <see cref="object.Equals(object?)"/> and
	/// <see cref="object.GetHashCode()"/> so the type system can keep user-facing equality behavior independent from memory-
	/// deduplication behavior.
	/// </remarks>
	public interface ISupportsInterning
	{
		/// <summary>
		/// Returns a hash code that groups potentially equal instances for interning lookup.
		/// </summary>
		/// <returns>A hash code derived from the fields that participate in <see cref="EqualsForInterning"/>.</returns>
		int GetHashCodeForInterning();

		/// <summary>
		/// Determines whether this instance is interning-equivalent to <paramref name="other"/>.
		/// </summary>
		/// <param name="other">The candidate instance from the same interning domain.</param>
		/// <returns><see langword="true"/> when both instances represent the same canonical value; otherwise <see langword="false"/>.</returns>
		bool EqualsForInterning(ISupportsInterning other);
	}
}

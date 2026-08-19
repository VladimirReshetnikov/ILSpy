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

using System.Globalization;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents semantic classification for by-reference expressions such as <c>ref x</c>, <c>in x</c>, and <c>out x</c>.
	/// </summary>
	public class ByReferenceResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the by-reference modifier kind (<c>ref</c>, <c>in</c>, or <c>out</c>).
		/// </summary>
		public ReferenceKind ReferenceKind { get; }

		/// <summary>
		/// Gets the wrapped expression when this node originates from source-level by-reference syntax.
		/// </summary>
		/// <remarks>
		/// This field is <see langword="null"/> for temporary instances created through the internal constructor.
		/// </remarks>
		public readonly ResolveResult ElementResult;

		/// <summary>
		/// Initializes a by-reference resolve result that wraps an existing expression.
		/// </summary>
		/// <param name="elementResult">The underlying expression referenced by the by-ref operation.</param>
		/// <param name="kind">The by-reference modifier kind.</param>
		public ByReferenceResolveResult(ResolveResult elementResult, ReferenceKind kind)
			: this(elementResult.Type, kind)
		{
			this.ElementResult = elementResult;
		}

		/// <summary>
		/// Initializes a temporary by-reference result from an element type only.
		/// </summary>
		/// <param name="elementType">The element type carried by the by-reference wrapper.</param>
		/// <param name="kind">The by-reference modifier kind.</param>
		/// <remarks>
		/// Should only be used for temporary resolve results in type inference and conversion classification paths.
		/// </remarks>
		internal ByReferenceResolveResult(IType elementType, ReferenceKind kind)
			: base(new ByReferenceType(elementType))
		{
			this.ReferenceKind = kind;
		}

		/// <summary>
		/// Gets the element type referenced by this by-reference expression.
		/// </summary>
		public IType ElementType {
			get { return ((ByReferenceType)this.Type).ElementType; }
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1} {2}]", GetType().Name, ReferenceKind.ToString().ToLowerInvariant(), ElementType);
		}
	}
}

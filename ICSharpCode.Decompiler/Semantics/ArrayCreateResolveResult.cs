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
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents semantic array creation, including optional dimension sizes and initializer values.
	/// </summary>
	public class ArrayCreateResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets resolved expressions that specify the length of each declared dimension.
		/// </summary>
		public readonly IReadOnlyList<ResolveResult> SizeArguments;

		/// <summary>
		/// Gets resolved initializer elements, or <see langword="null"/> when no initializer is present.
		/// </summary>
		public readonly IReadOnlyList<ResolveResult> InitializerElements;

		/// <summary>
		/// Initializes an array-creation semantic node.
		/// </summary>
		/// <param name="arrayType">The produced array type.</param>
		/// <param name="sizeArguments">Dimension-size expressions. This list can be empty for implicitly-sized initializers.</param>
		/// <param name="initializerElements">Initializer values, or <see langword="null"/> when no initializer is specified.</param>
		/// <exception cref="ArgumentNullException"><paramref name="sizeArguments"/> is <see langword="null"/>.</exception>
		public ArrayCreateResolveResult(IType arrayType, IReadOnlyList<ResolveResult> sizeArguments, IReadOnlyList<ResolveResult> initializerElements)
			: base(arrayType)
		{
			if (sizeArguments == null)
				throw new ArgumentNullException(nameof(sizeArguments));
			this.SizeArguments = sizeArguments;
			this.InitializerElements = initializerElements;
		}

		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (InitializerElements != null)
				return SizeArguments.Concat(InitializerElements);
			else
				return SizeArguments;
		}
	}
}

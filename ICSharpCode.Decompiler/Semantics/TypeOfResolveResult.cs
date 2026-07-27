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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents semantic classification for a C# <c>typeof</c> expression.
	/// </summary>
	/// <remarks>
	/// <see cref="ResolveResult.Type"/> is the compile-time type of the <c>typeof</c> expression itself
	/// (normally <c>System.Type</c>), while <see cref="ReferencedType"/> stores the operand type inside the
	/// parentheses.
	/// </remarks>
	public class TypeOfResolveResult : ResolveResult
	{
		readonly IType referencedType;

		/// <summary>
		/// Initializes a <c>typeof</c> resolve result.
		/// </summary>
		/// <param name="systemType">The resulting expression type (typically <c>System.Type</c>).</param>
		/// <param name="referencedType">The operand type referenced by <c>typeof</c>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="referencedType"/> is <see langword="null"/>.</exception>
		public TypeOfResolveResult(IType systemType, IType referencedType)
			: base(systemType)
		{
			if (referencedType == null)
				throw new ArgumentNullException(nameof(referencedType));
			this.referencedType = referencedType;
		}

		/// <summary>
		/// Gets the operand type referenced by <c>typeof</c>.
		/// </summary>
		public IType ReferencedType {
			get { return referencedType; }
		}
	}
}

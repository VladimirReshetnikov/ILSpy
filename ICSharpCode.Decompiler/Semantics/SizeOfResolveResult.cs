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
	/// Represents semantic classification for a C# <c>sizeof</c> expression.
	/// </summary>
	/// <remarks>
	/// <see cref="ResolveResult.Type"/> is the integer result type (<c>System.Int32</c>).
	/// <see cref="ReferencedType"/> stores the operand type, and <see cref="ConstantValue"/> is populated only
	/// when the resolver can determine a compile-time size.
	/// </remarks>
	public class SizeOfResolveResult : ResolveResult
	{
		readonly IType referencedType;
		readonly int? constantValue;

		/// <summary>
		/// Initializes a <c>sizeof</c> resolve result.
		/// </summary>
		/// <param name="int32">The result type used for the expression.</param>
		/// <param name="referencedType">The operand type whose storage size is queried.</param>
		/// <param name="constantValue">Compile-time size in bytes when known; otherwise <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="referencedType"/> is <see langword="null"/>.</exception>
		public SizeOfResolveResult(IType int32, IType referencedType, int? constantValue)
			: base(int32)
		{
			if (referencedType == null)
				throw new ArgumentNullException(nameof(referencedType));
			this.referencedType = referencedType;
			this.constantValue = constantValue;
		}

		/// <summary>
		/// Gets the operand type referenced by <c>sizeof</c>.
		/// </summary>
		public IType ReferencedType {
			get { return referencedType; }
		}

		/// <summary>
		/// Gets whether <see cref="ConstantValue"/> contains a compile-time byte size.
		/// </summary>
		public override bool IsCompileTimeConstant {
			get {
				return constantValue != null;
			}
		}

		/// <summary>
		/// Gets the compile-time byte size when known; otherwise <see langword="null"/>.
		/// </summary>
		public override object ConstantValue {
			get {
				return constantValue;
			}
		}

		/// <summary>
		/// Gets whether the operand type is known to be a reference type, which is invalid for unmanaged-size evaluation.
		/// </summary>
		public override bool IsError {
			get {
				return referencedType.IsReferenceType != false;
			}
		}
	}
}

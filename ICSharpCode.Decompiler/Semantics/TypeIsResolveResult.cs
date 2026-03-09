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
	/// Represents semantic classification for a C# <c>is</c> type-test expression.
	/// </summary>
	/// <remarks>
	/// This result models expressions of the form <c>input is TargetType</c> and always reports the
	/// expression type supplied to the constructor (typically <c>System.Boolean</c>).
	/// </remarks>
	public class TypeIsResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the expression being tested.
		/// </summary>
		public readonly ResolveResult Input;

		/// <summary>
		/// Gets the tested type from the right-hand side of the <c>is</c> expression.
		/// </summary>
		public readonly IType TargetType;

		/// <summary>
		/// Initializes a type-test resolve result.
		/// </summary>
		/// <param name="input">The resolved left-hand side expression to test.</param>
		/// <param name="targetType">The type used as the test target.</param>
		/// <param name="booleanType">The result type for the expression.</param>
		/// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="targetType"/> is <see langword="null"/>.</exception>
		public TypeIsResolveResult(ResolveResult input, IType targetType, IType booleanType)
			: base(booleanType)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (targetType == null)
				throw new ArgumentNullException(nameof(targetType));
			this.Input = input;
			this.TargetType = targetType;
		}
	}
}

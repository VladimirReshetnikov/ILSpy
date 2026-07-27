// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Extends <see cref="ITypeResolveContext"/> with local-symbol visibility information for expression/body analysis.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> This context models the lexical state at a specific position within executable code.
	/// In addition to module/type/member scope, it exposes locally declared variables and lambda-state information used
	/// by resolver components when binding identifiers.
	/// </para>
	/// <para>
	/// <b>Usage.</b> Implementations are typically transient snapshots created while traversing AST or IL bodies.
	/// Consumers should treat <see cref="LocalVariables"/> as the variables visible at that source location, not as a
	/// complete method-level declaration list.
	/// </para>
	/// </remarks>
	public interface ICodeContext : ITypeResolveContext
	{
		/// <summary>
		/// Gets the set of locals and lambda parameters visible at the current code location.
		/// </summary>
		/// <value>
		/// An enumeration of visible block-scoped variables and lambda parameters.
		/// Method parameters are intentionally excluded.
		/// </value>
		IEnumerable<IVariable> LocalVariables { get; }

		/// <summary>
		/// Gets whether this context is nested inside a lambda expression or anonymous method body.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when lookup semantics should treat lambda capture and parameter rules as active;
		/// otherwise <see langword="false"/>.
		/// </value>
		bool IsWithinLambdaExpression { get; }
	}
}

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
	/// Represents the semantic classification of a resolved expression.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Instances capture semantic information that survives syntax rewrites: the expression type, whether the expression is known
	/// to be a compile-time constant, and (for subclasses) operation-specific details such as member targets or invocation arguments.
	/// </para>
	/// <para>
	/// <see cref="ResolveResult"/> values are attached to C# syntax nodes throughout the decompiler pipeline. Many consumers only inspect
	/// <see cref="Type"/> and <see cref="IsError"/>, but transformations that need richer semantic trees can traverse
	/// <see cref="GetChildResults"/> recursively.
	/// </para>
	/// </remarks>
	public class ResolveResult
	{
		readonly IType type;

		/// <summary>
		/// Initializes a semantic result with the specified expression type.
		/// </summary>
		/// <param name="type">The effective type of the resolved expression.</param>
		/// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
		public ResolveResult(IType type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			this.type = type;
		}

		/// <summary>
		/// Gets the effective type of the resolved expression.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
										 Justification = "Unrelated to object.GetType()")]
		public IType Type {
			get { return type; }
		}

		/// <summary>
		/// Gets a value indicating whether the expression is known to be a compile-time constant.
		/// </summary>
		/// <remarks>
		/// This property describes semantic constness, not syntax shape. For example, a member access can return
		/// <see langword="true"/> when it targets a <c>const</c> field.
		/// </remarks>
		public virtual bool IsCompileTimeConstant {
			get { return false; }
		}

		/// <summary>
		/// Gets the compile-time constant value when <see cref="IsCompileTimeConstant"/> is <see langword="true"/>.
		/// </summary>
		/// <value>
		/// The constant value, or <see langword="null"/> when this instance does not represent a constant expression.
		/// </value>
		public virtual object ConstantValue {
			get { return null; }
		}

		/// <summary>
		/// Gets a value indicating whether this result represents an unresolved or invalid semantic state.
		/// </summary>
		public virtual bool IsError {
			get { return false; }
		}

		public override string ToString()
		{
			return "[" + GetType().Name + " " + type + "]";
		}

		/// <summary>
		/// Enumerates immediate semantic child nodes that belong to this resolve result.
		/// </summary>
		/// <returns>
		/// A sequence of direct child results. The default implementation returns an empty sequence.
		/// </returns>
		public virtual IEnumerable<ResolveResult> GetChildResults()
		{
			return Enumerable.Empty<ResolveResult>();
		}

		/// <summary>
		/// Creates a shallow copy of this semantic node.
		/// </summary>
		/// <returns>A clone produced by <see cref="object.MemberwiseClone"/>.</returns>
		/// <remarks>
		/// Reference-typed fields are shared between the original and clone.
		/// </remarks>
		public virtual ResolveResult ShallowClone()
		{
			return (ResolveResult)MemberwiseClone();
		}
	}
}

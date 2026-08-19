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

using System.Collections.Generic;
using System.Globalization;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Identifies the syntactic shape of a dynamic invocation operation.
	/// </summary>
	public enum DynamicInvocationType
	{
		/// <summary>
		/// A call expression such as <c>target(args)</c>.
		/// </summary>
		Invocation,

		/// <summary>
		/// An indexer-style access such as <c>target[index]</c>.
		/// </summary>
		Indexing,

		/// <summary>
		/// A dynamic object-creation expression such as <c>new target(args)</c>.
		/// </summary>
		ObjectCreation,
	}

	/// <summary>
	/// Represents an invocation performed through C# dynamic binding.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The result type is always <see cref="SpecialType.Dynamic"/> because the final member and return type are established
	/// at runtime by the binder.
	/// </para>
	/// <para>
	/// This node is used for dynamic method calls, dynamic index operations, and dynamic constructor invocations.
	/// </para>
	/// </remarks>
	public class DynamicInvocationResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the invocation target.
		/// </summary>
		/// <remarks>
		/// The target can be a dynamic expression or a <see cref="MethodGroupResolveResult"/> when the operation starts from
		/// a method-group-like shape before dynamic dispatch.
		/// </remarks>
		public readonly ResolveResult Target;

		/// <summary>
		/// Gets the invocation shape represented by this node.
		/// </summary>
		public readonly DynamicInvocationType InvocationType;

		/// <summary>
		/// Gets the invocation arguments in source order.
		/// </summary>
		/// <remarks>
		/// Named arguments are represented as <see cref="NamedArgumentResolveResult"/> instances.
		/// </remarks>
		public readonly IList<ResolveResult> Arguments;

		/// <summary>
		/// Gets object or collection initializer statements attached to this invocation result.
		/// </summary>
		/// <remarks>
		/// Initializer statements are only meaningful for <see cref="DynamicInvocationType.ObjectCreation"/>.
		/// When they are present, references to the newly created object are represented by
		/// <see cref="InitializedObjectResolveResult"/> nodes.
		/// </remarks>
		public readonly IList<ResolveResult> InitializerStatements;

		/// <summary>
		/// Synthesized member (a <c>dynamic</c> method on the <c>dynamic</c> type) representing the invoked
		/// member, so the member reference carries a navigable symbol / hover tooltip. Only set for an
		/// invoke-member (<c>a.Method(b)</c>); null for a plain invoke or an indexer. May be null.
		/// </summary>
		public readonly IMember Symbol;

		/// <summary>
		/// Initializes a new dynamic invocation resolve result.
		/// </summary>
		/// <param name="target">The invocation target.</param>
		/// <param name="invocationType">The syntactic invocation shape.</param>
		/// <param name="arguments">The argument list in source order.</param>
		/// <param name="initializerStatements">Optional initializer statements associated with object creation.</param>
		/// <param name="symbol">Optional synthesized member standing in for the member the binder will pick.</param>
		public DynamicInvocationResolveResult(ResolveResult target, DynamicInvocationType invocationType, IList<ResolveResult> arguments, IList<ResolveResult> initializerStatements = null, IMember symbol = null) : base(SpecialType.Dynamic)
		{
			this.Target = target;
			this.InvocationType = invocationType;
			this.Arguments = arguments ?? EmptyList<ResolveResult>.Instance;
			this.InitializerStatements = initializerStatements ?? EmptyList<ResolveResult>.Instance;
			this.Symbol = symbol;
		}

		/// <summary>
		/// Returns a debugger-oriented textual representation of this resolve result.
		/// </summary>
		/// <returns>A fixed label identifying the node as a dynamic invocation.</returns>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[Dynamic invocation ]");
		}
	}
}

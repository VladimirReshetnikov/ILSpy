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
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents the semantic result of invoking a method, constructor, delegate, or indexer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="Arguments"/> stores arguments in source-evaluation order. Call-argument order can differ when named arguments,
	/// optional parameters, or <c>params</c> expansion are involved; use <see cref="GetArgumentsForCall"/> when consumer logic requires
	/// parameter-mapped order.
	/// </para>
	/// <para>
	/// <see cref="InitializerStatements"/> captures semantic operations produced by object and collection initializers that execute after
	/// the invocation result has been created.
	/// </para>
	/// </remarks>
	public class InvocationResolveResult : MemberResolveResult
	{
		/// <summary>
		/// Gets the arguments as they are evaluated at the call site.
		/// </summary>
		public readonly IList<ResolveResult> Arguments;

		/// <summary>
		/// Gets semantic operations that initialize the created value after invocation.
		/// </summary>
		/// <remarks>
		/// Entries typically include assignment-like statements that reference <see cref="InitializedObjectResolveResult"/>.
		/// </remarks>
		public readonly IList<ResolveResult> InitializerStatements;

		/// <summary>
		/// Initializes a new invocation resolve result.
		/// </summary>
		/// <param name="targetResult">The resolved invocation target expression, or <see langword="null"/> for static calls.</param>
		/// <param name="member">The callable member selected by overload resolution.</param>
		/// <param name="arguments">Arguments in source-evaluation order. If <see langword="null"/>, an empty list is used.</param>
		/// <param name="initializerStatements">
		/// Object/collection initializer operations that execute after construction. If <see langword="null"/>, an empty list is used.
		/// </param>
		/// <param name="returnTypeOverride">
		/// An optional type to expose from <see cref="ResolveResult.Type"/> instead of the computed member type.
		/// </param>
		public InvocationResolveResult(ResolveResult targetResult, IParameterizedMember member,
									   IList<ResolveResult> arguments = null,
									   IList<ResolveResult> initializerStatements = null,
									   IType returnTypeOverride = null)
			: base(targetResult, member, returnTypeOverride)
		{
			this.Arguments = arguments ?? EmptyList<ResolveResult>.Instance;
			this.InitializerStatements = initializerStatements ?? EmptyList<ResolveResult>.Instance;
		}

		/// <summary>
		/// Gets the invoked callable member.
		/// </summary>
		public new IParameterizedMember Member {
			get { return (IParameterizedMember)base.Member; }
		}

		/// <summary>
		/// Gets arguments in call-parameter order.
		/// </summary>
		/// <returns>
		/// A list whose entries correspond to parameter positions. For expanded <c>params</c> calls, implementations can synthesize an
		/// <see cref="ArrayCreateResolveResult"/> to represent packed trailing arguments.
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
									 Justification = "Derived methods may be expensive and create new lists")]
		public virtual IList<ResolveResult> GetArgumentsForCall()
		{
			return Arguments;
		}

		/// <inheritdoc/>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return base.GetChildResults().Concat(this.Arguments).Concat(this.InitializerStatements);
		}
	}
}

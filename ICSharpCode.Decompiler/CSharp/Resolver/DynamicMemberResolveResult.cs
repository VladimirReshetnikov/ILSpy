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

using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Resolver
{
	/// <summary>
	/// Represents a dynamic member-access operation (for example, <c>target.Member</c> where the runtime binder decides the member).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The result type is always <see cref="SpecialType.Dynamic"/>, because compile-time overload and member selection are deferred
	/// to the C# dynamic runtime binder.
	/// </para>
	/// <para>
	/// This node captures syntactic intent only: it records the access target and member name without proving that the member exists.
	/// </para>
	/// </remarks>
	public class DynamicMemberResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the dynamic access target expression.
		/// </summary>
		public readonly ResolveResult Target;

		/// <summary>
		/// Gets the member name requested from <see cref="Target"/>.
		/// </summary>
		public readonly string Member;

		/// <summary>
		/// Initializes a dynamic member-access resolve result.
		/// </summary>
		/// <param name="target">The expression on which the member is accessed.</param>
		/// <param name="member">The member name that will be looked up by the runtime binder.</param>
		public DynamicMemberResolveResult(ResolveResult target, string member) : base(SpecialType.Dynamic)
		{
			this.Target = target;
			this.Member = member;
		}

		/// <summary>
		/// Returns a debugger-oriented textual representation of this resolve result.
		/// </summary>
		/// <returns>A string containing the dynamic member name.</returns>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[Dynamic member '{0}']", Member);
		}

		/// <summary>
		/// Returns child semantic results used by this dynamic member-access node.
		/// </summary>
		/// <returns>A single-item sequence containing <see cref="Target"/>.</returns>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new[] { Target };
		}
	}
}

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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents a type lookup that produced multiple viable candidates.
	/// </summary>
	/// <remarks>
	/// This result keeps the best-known type payload while signaling failure through <see cref="ResolveResult.IsError"/>.
	/// It is used when member/type lookup cannot choose a unique declaration.
	/// </remarks>
	public class AmbiguousTypeResolveResult : TypeResolveResult
	{
		/// <summary>
		/// Initializes an ambiguous type resolve result.
		/// </summary>
		/// <param name="type">A representative candidate type retained for diagnostics and downstream heuristics.</param>
		public AmbiguousTypeResolveResult(IType type) : base(type)
		{
		}

		/// <summary>
		/// Always <see langword="true"/>: an ambiguous lookup is an error regardless of the
		/// representative candidate.
		/// </summary>
		public override bool IsError {
			get { return true; }
		}
	}

	/// <summary>
	/// Represents an ambiguous non-method member access (field, property, or event).
	/// </summary>
	/// <remarks>
	/// The result preserves both the selected target expression and one candidate member symbol so error reporting can remain contextual.
	/// </remarks>
	public class AmbiguousMemberResolveResult : MemberResolveResult
	{
		/// <summary>
		/// Initializes an ambiguous member resolve result.
		/// </summary>
		/// <param name="targetResult">The resolved target expression, or <see langword="null"/> for static access.</param>
		/// <param name="member">A representative candidate member from the ambiguous lookup set.</param>
		public AmbiguousMemberResolveResult(ResolveResult targetResult, IMember member) : base(targetResult, member)
		{
		}

		/// <summary>
		/// Always <see langword="true"/>: an ambiguous lookup is an error regardless of the
		/// representative candidate.
		/// </summary>
		public override bool IsError {
			get { return true; }
		}
	}
}

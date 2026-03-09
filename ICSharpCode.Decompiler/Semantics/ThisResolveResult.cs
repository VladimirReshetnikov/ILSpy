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
	/// Represents semantic classification for <c>this</c> and <c>base</c> receiver expressions.
	/// </summary>
	/// <remarks>
	/// The same type is used for both keywords. <see cref="CausesNonVirtualInvocation"/> distinguishes
	/// the <c>base</c> case where member dispatch must bypass virtual override lookup.
	/// </remarks>
	public class ThisResolveResult : ResolveResult
	{
		bool causesNonVirtualInvocation;

		/// <summary>
		/// Initializes a receiver resolve result.
		/// </summary>
		/// <param name="type">The receiver type used for member lookup and typing.</param>
		/// <param name="causesNonVirtualInvocation">
		/// <see langword="true"/> when this receiver enforces non-virtual dispatch semantics (as with <c>base</c>).
		/// </param>
		public ThisResolveResult(IType type, bool causesNonVirtualInvocation = false) : base(type)
		{
			this.causesNonVirtualInvocation = causesNonVirtualInvocation;
		}

		/// <summary>
		/// Gets whether this resolve result causes member invocations to be non-virtual.
		/// </summary>
		public bool CausesNonVirtualInvocation {
			get { return causesNonVirtualInvocation; }
		}
	}
}

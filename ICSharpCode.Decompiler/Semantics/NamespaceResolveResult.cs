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

using System.Globalization;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents a name lookup that resolved to a namespace symbol.
	/// </summary>
	/// <remarks>
	/// Namespace resolve results are frequently attached to type references built by <c>TypeSystemAstBuilder</c> so later transforms can
	/// keep namespace/type ambiguity decisions stable.
	/// </remarks>
	public class NamespaceResolveResult : ResolveResult
	{
		readonly INamespace ns;

		/// <summary>
		/// Initializes a namespace resolve result.
		/// </summary>
		/// <param name="ns">The resolved namespace symbol.</param>
		public NamespaceResolveResult(INamespace ns) : base(SpecialType.NoType)
		{
			this.ns = ns;
		}

		/// <summary>
		/// Gets the resolved namespace symbol.
		/// </summary>
		public INamespace Namespace {
			get { return ns; }
		}

		/// <summary>
		/// Gets the fully qualified namespace name.
		/// </summary>
		public string NamespaceName {
			get { return ns.FullName; }
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1}]", GetType().Name, ns);
		}
	}
}

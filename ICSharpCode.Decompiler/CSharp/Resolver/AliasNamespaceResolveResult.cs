//
// AliasResolveResult.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using ICSharpCode.Decompiler.Semantics;

namespace ICSharpCode.Decompiler.CSharp.Resolver
{
	/// <summary>
	/// Represents a namespace resolution produced through a C# using-alias.
	/// </summary>
	/// <remarks>
	/// Like <see cref="AliasTypeResolveResult"/>, this type records alias-origin information that is not present
	/// in plain <see cref="NamespaceResolveResult"/>. This allows downstream consumers to preserve source intent.
	/// </remarks>
	public class AliasNamespaceResolveResult : NamespaceResolveResult
	{
		/// <summary>
		/// Gets the alias token that was bound to the resolved namespace.
		/// </summary>
		public string Alias {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new alias-based namespace resolve result.
		/// </summary>
		/// <param name="alias">The alias token that appeared in source.</param>
		/// <param name="underlyingResult">The namespace resolve result that the alias expands to.</param>
		public AliasNamespaceResolveResult(string alias, NamespaceResolveResult underlyingResult) : base(underlyingResult.Namespace)
		{
			this.Alias = alias;
		}
	}
}

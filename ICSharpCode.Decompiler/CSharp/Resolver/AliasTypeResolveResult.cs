//
// AliasTypeResolveResult.cs
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
	/// Represents a type-name resolution that was written through a C# using-alias.
	/// </summary>
	/// <remarks>
	/// This resolve result preserves the original alias token in addition to the resolved target type.
	/// Consumers that care about source fidelity (for example, semantic highlighting or refactoring hints)
	/// can therefore distinguish an alias reference from a direct type reference that resolves to the same symbol.
	/// </remarks>
	public class AliasTypeResolveResult : TypeResolveResult
	{
		/// <summary>
		/// Gets the alias token used in source code.
		/// </summary>
		public string Alias {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new alias-based type resolve result.
		/// </summary>
		/// <param name="alias">The alias token that appeared in source.</param>
		/// <param name="underlyingResult">
		/// The resolved type bound to <paramref name="alias"/>. Its <see cref="TypeResolveResult.Type"/> value is exposed as this result's type.
		/// </param>
		public AliasTypeResolveResult(string alias, TypeResolveResult underlyingResult) : base(underlyingResult.Type)
		{
			this.Alias = alias;
		}
	}
}

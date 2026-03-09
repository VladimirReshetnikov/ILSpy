// Copyright (c) 2018 Daniel Grunwald
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
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Resolve result for a tuple literal expression.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The computed <see cref="ResolveResult.Type"/> is a <see cref="TupleType"/> when every element has a concrete type.
	/// If any element is <see cref="TypeKind.None"/> or <see cref="TypeKind.Null"/>, the result type falls back to
	/// <see cref="SpecialType.NoType"/> so callers can propagate an error/incomplete-state signal.
	/// </para>
	/// <para>
	/// Element names are consumed only when constructing the tuple type; per-element expression semantics are carried by
	/// <see cref="Elements"/>.
	/// </para>
	/// </remarks>
	public class TupleResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets element expressions in tuple-literal order.
		/// </summary>
		public ImmutableArray<ResolveResult> Elements { get; }

		/// <summary>
		/// Initializes a tuple-literal resolve result.
		/// </summary>
		/// <param name="compilation">Compilation used to resolve <see cref="TupleType"/> and <c>System.ValueTuple</c> symbols.</param>
		/// <param name="elements">Tuple element expressions in source order.</param>
		/// <param name="elementNames">Optional tuple element names aligned with <paramref name="elements"/>.</param>
		/// <param name="valueTupleAssembly">Optional assembly override used to locate tuple framework definitions.</param>
		public TupleResolveResult(ICompilation compilation,
			ImmutableArray<ResolveResult> elements,
			ImmutableArray<string> elementNames = default(ImmutableArray<string>),
			IModule valueTupleAssembly = null)
		: base(GetTupleType(compilation, elements, elementNames, valueTupleAssembly))
		{
			this.Elements = elements;
		}

		/// <summary>
		/// Enumerates tuple element expressions as child resolve results.
		/// </summary>
		/// <returns><see cref="Elements"/> in declaration order.</returns>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return Elements;
		}

		static IType GetTupleType(ICompilation compilation, ImmutableArray<ResolveResult> elements, ImmutableArray<string> elementNames, IModule valueTupleAssembly)
		{
			if (elements.Any(e => e.Type.Kind == TypeKind.None || e.Type.Kind == TypeKind.Null))
				return SpecialType.NoType;
			else
				return new TupleType(compilation, elements.Select(e => e.Type).ToImmutableArray(), elementNames, valueTupleAssembly);
		}
	}
}

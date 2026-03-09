// Copyright (c) 2018 Siegfried Pammer
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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents a C# interpolated-string expression before any target-type conversion is applied.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The resolver keeps interpolated strings as a dedicated node so later conversion classification can decide between
	/// plain <see cref="KnownTypeCode.String"/> materialization and conversions such as
	/// <see cref="Conversion.ImplicitInterpolatedStringConversion"/>.
	/// </para>
	/// <para>
	/// <see cref="FormatString"/> and <see cref="Arguments"/> follow the same shape expected by
	/// <see cref="string.Format(string, object[])"/>-style formatting: placeholders in the format map to entries in
	/// <see cref="Arguments"/> by index.
	/// </para>
	/// </remarks>
	public class InterpolatedStringResolveResult : ResolveResult
	{
		/// <summary>
		/// Composite formatting template generated for the interpolated string.
		/// </summary>
		public readonly string FormatString;

		/// <summary>
		/// Expression results that supply placeholder values for <see cref="FormatString"/>.
		/// </summary>
		public readonly ResolveResult[] Arguments;

		/// <summary>
		/// Initializes a resolve result for an interpolated-string expression.
		/// </summary>
		/// <param name="stringType">The semantic type associated with string expressions in the current compilation.</param>
		/// <param name="formatString">Composite formatting string synthesized from interpolation holes and literal segments.</param>
		/// <param name="arguments">Per-hole value expressions in placeholder order.</param>
		public InterpolatedStringResolveResult(IType stringType, string formatString, params ResolveResult[] arguments)
			: base(stringType)
		{
			FormatString = formatString;
			Arguments = arguments;
		}

		/// <summary>
		/// Enumerates child resolve results that participate in the interpolated expression.
		/// </summary>
		/// <returns>The argument resolve results in placeholder order.</returns>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return Arguments;
		}
	}
}

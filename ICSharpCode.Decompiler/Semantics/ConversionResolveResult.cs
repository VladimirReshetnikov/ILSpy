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

using System;
using System.Collections.Generic;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents classification of an implicit or explicit conversion applied to an input expression.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The source type is available as <see cref="Input"/>.<see cref="ResolveResult.Type"/>, while the target type is
	/// the inherited <see cref="ResolveResult.Type"/> on this instance.
	/// </para>
	/// <para>
	/// <see cref="Conversion"/> captures the conversion category and metadata (for example selected user-defined operator),
	/// but this node does not execute conversion semantics itself.
	/// </para>
	/// </remarks>
	public class ConversionResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the expression being converted.
		/// </summary>
		public readonly ResolveResult Input;

		/// <summary>
		/// Gets the conversion classification selected by the resolver.
		/// </summary>
		public readonly Conversion Conversion;

		/// <summary>
		/// For numeric conversions, specifies whether overflow checking is enabled.
		/// </summary>
		public readonly bool CheckForOverflow;

		/// <summary>
		/// Initializes a conversion resolve result.
		/// </summary>
		/// <param name="targetType">Target type of the conversion.</param>
		/// <param name="input">Expression being converted.</param>
		/// <param name="conversion">Conversion category and metadata selected by the resolver.</param>
		/// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="conversion"/> is <see langword="null"/>.</exception>
		public ConversionResolveResult(IType targetType, ResolveResult input, Conversion conversion)
			: base(targetType)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			if (conversion == null)
				throw new ArgumentNullException(nameof(conversion));
			this.Input = input;
			this.Conversion = conversion;
		}

		/// <summary>
		/// Initializes a conversion resolve result with explicit overflow-checking metadata.
		/// </summary>
		/// <param name="targetType">Target type of the conversion.</param>
		/// <param name="input">Expression being converted.</param>
		/// <param name="conversion">Conversion category and metadata selected by the resolver.</param>
		/// <param name="checkForOverflow"><see langword="true"/> when numeric conversion semantics are in checked context.</param>
		public ConversionResolveResult(IType targetType, ResolveResult input, Conversion conversion, bool checkForOverflow)
			: this(targetType, input, conversion)
		{
			this.CheckForOverflow = checkForOverflow;
		}

		/// <summary>
		/// Gets whether conversion classification reported an error.
		/// </summary>
		/// <returns><see langword="true"/> when <see cref="Conversion.IsValid"/> is <see langword="false"/>; otherwise <see langword="false"/>.</returns>
		public override bool IsError {
			get { return !Conversion.IsValid; }
		}

		/// <summary>
		/// Enumerates child resolve results for this conversion node.
		/// </summary>
		/// <returns>A single-element sequence containing <see cref="Input"/>.</returns>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new[] { Input };
		}

	}
}

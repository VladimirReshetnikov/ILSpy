// Copyright (c) 2017 Siegfried Pammer
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

#nullable enable

using System.Collections.Generic;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
	/// <summary>
	/// Represents a C# interpolated string expression (<c>$"..."</c>).
	/// <c>interpolated_string_expression ::= interpolated_string_content*</c> (C# grammar section 12.8.3)
	/// </summary>
	/// <remarks>
	/// The expression stores its payload as an ordered <see cref="Content"/> collection containing either literal
	/// segments (<see cref="InterpolatedStringText"/>) or interpolation holes (<see cref="Interpolation"/>).
	/// </remarks>
	[DecompilerAstNode]
	public sealed partial class InterpolatedStringExpression : Expression
	{
		/// <summary>
		/// The opening interpolated-string token (<c>$"</c>).
		/// </summary>
		public const string OpenQuote = "$\"";
		/// <summary>
		/// The closing interpolated-string token (<c>"</c>).
		/// </summary>
		public const string CloseQuote = "\"";

		/// <summary>
		/// Gets the ordered sequence of literal and interpolation nodes that compose the string body.
		/// </summary>
		[Slot("Content")]
		public partial AstNodeCollection<InterpolatedStringContent> Content { get; }

		/// <summary>
		/// Initializes an interpolated string from existing content nodes.
		/// </summary>
		/// <param name="content">Content nodes to append in lexical order.</param>
		public InterpolatedStringExpression(IList<InterpolatedStringContent> content)
		{
			Content.AddRange(content);
		}
	}

	/// <summary>
	/// Base type for nodes that can appear inside an <see cref="InterpolatedStringExpression"/>.
	/// <code>
	/// interpolated_string_content ::=
	///       interpolation
	///     | interpolated_string_text
	/// </code>
	/// (C# grammar section 12.8.3)
	/// </summary>
	[DecompilerAstNode]
	public abstract partial class InterpolatedStringContent : AstNode
	{
	}

	/// <summary>
	/// Represents one interpolation hole inside an interpolated string body.
	/// <c>interpolation ::= '{' expression ( ',' alignment )? ( ':' format )? '}'</c> (C# grammar section 12.8.3)
	/// </summary>
	/// <remarks>
	/// <see cref="Alignment"/> and <see cref="Suffix"/> are values carried over from the IL string-format
	/// pattern rather than child nodes. Only <see cref="Expression"/> occupies a slot, so pattern matching
	/// compares the embedded expression subtree and ignores the alignment and format suffix.
	/// </remarks>
	[DecompilerAstNode]
	public sealed partial class Interpolation : InterpolatedStringContent
	{
		[Slot("Expression")]
		public partial Expression Expression { get; set; }

		/// <summary>
		/// Gets the alignment component that follows the expression in <c>{expr,alignment}</c> forms.
		/// </summary>
		public int Alignment { get; }

		/// <summary>
		/// Gets the format-suffix text that follows a colon in <c>{expr:format}</c> forms.
		/// </summary>
		public string? Suffix { get; }

		/// <summary>
		/// Initializes an interpolation hole.
		/// </summary>
		/// <param name="expression">Expression to evaluate and format.</param>
		/// <param name="alignment">Optional alignment width supplied in source or reconstructed during transforms.</param>
		/// <param name="suffix">Optional format suffix (without the leading colon).</param>
		public Interpolation(Expression expression, int alignment = 0, string? suffix = null)
		{
			Expression = expression;
			Alignment = alignment;
			Suffix = suffix;
		}
	}

	/// <summary>
	/// Represents a literal text segment within an <see cref="InterpolatedStringExpression"/>.
	/// <c>interpolated_string_text ::= text_character+</c> (C# lexical grammar section 12.8.3)
	/// </summary>
	[DecompilerAstNode]
	public sealed partial class InterpolatedStringText : InterpolatedStringContent
	{
		/// <summary>
		/// Gets or sets the literal text payload for this segment.
		/// </summary>
		public string Text { get; set; } = string.Empty;

		public InterpolatedStringText()
		{
		}

		/// <summary>
		/// Initializes a literal interpolated-string text segment.
		/// </summary>
		/// <param name="text">Literal content for this segment.</param>
		public InterpolatedStringText(string text)
		{
			Text = text;
		}
	}
}

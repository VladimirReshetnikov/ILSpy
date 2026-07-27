// 
// PreProcessorDirective.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
#nullable enable

using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
	/// <summary>
	/// Enumerates preprocessor directive kinds represented in the syntax tree.
	/// </summary>
	public enum PreProcessorDirectiveType : byte
	{
		Invalid = 0,
		Region = 1,
		Endregion = 2,

		If = 3,
		Endif = 4,
		Elif = 5,
		Else = 6,

		Define = 7,
		Undef = 8,
		Error = 9,
		Warning = 10,
		Pragma = 11,
		Line = 12,
		Nullable = 13
	}

	/// <summary>
	/// Represents a <c>#line</c> directive.
	/// <c>line_directive ::= '#' 'line' ( decimal_digit+ string_literal? | 'default' | 'hidden' )</c> (C# lexical grammar)
	/// </summary>
	[DecompilerAstNode]
	public sealed partial class LinePreprocessorDirective : PreProcessorDirective
	{
		/// <summary>
		/// Initializes a <see cref="LinePreprocessorDirective"/> with explicit source span information.
		/// </summary>
		/// <param name="startLocation">Source location where the directive starts.</param>
		/// <param name="endLocation">Source location where the directive ends.</param>
		public LinePreprocessorDirective(TextLocation startLocation, TextLocation endLocation) : base(PreProcessorDirectiveType.Line, startLocation, endLocation)
		{
		}

		/// <summary>
		/// Initializes a <see cref="LinePreprocessorDirective"/> from already parsed argument text.
		/// </summary>
		/// <param name="argument">Directive argument text, excluding the leading <c>#line</c> token.</param>
		public LinePreprocessorDirective(string? argument = null) : base(PreProcessorDirectiveType.Line, argument)
		{
		}
	}

	/// <summary>
	/// Represents a <c>#pragma warning</c> directive and its warning-id list.
	/// <c>pragma_warning_directive ::= '#' 'pragma' 'warning' ( 'disable' | 'restore' ) expression*</c> (C# lexical grammar)
	/// </summary>
	[DecompilerAstNode]
	public sealed partial class PragmaWarningPreprocessorDirective : PreProcessorDirective
	{
		/// <summary>
		/// Gets the warning IDs declared by the directive.
		/// </summary>
		[Slot("Warning")]
		public partial AstNodeCollection<PrimitiveExpression> Warnings { get; }

		public override TextLocation EndLocation {
			get {
				var child = LastChild;
				if (child == null)
					return base.EndLocation;
				return child.EndLocation;
			}
		}

		/// <summary>
		/// Initializes a pragma-warning directive with explicit source span information.
		/// </summary>
		/// <param name="startLocation">Source location where the directive starts.</param>
		/// <param name="endLocation">Source location where the directive ends.</param>
		public PragmaWarningPreprocessorDirective(TextLocation startLocation, TextLocation endLocation) : base(PreProcessorDirectiveType.Pragma, startLocation, endLocation)
		{
		}

		/// <summary>
		/// Initializes a pragma-warning directive from already parsed argument text.
		/// </summary>
		/// <param name="argument">Directive argument text, excluding the leading <c>#pragma</c> token.</param>
		public PragmaWarningPreprocessorDirective(string? argument = null) : base(PreProcessorDirectiveType.Pragma, argument)
		{
		}

		/// <summary>
		/// Determines whether the directive contains a specific warning ID.
		/// </summary>
		/// <param name="pragmaWarning">Warning ID to probe.</param>
		/// <returns><see langword="true"/> when <paramref name="pragmaWarning"/> appears in <see cref="Warnings"/>.</returns>
		public bool IsDefined(int pragmaWarning)
		{
			return Warnings.Select(w => (int)w.Value).Any(n => n == pragmaWarning);
		}
	}

	/// <summary>
	/// Represents a preprocessor directive token sequence attached to syntax trivia.
	/// <c>pp_directive ::= '#' pp_kind new_line</c> (C# lexical grammar section 6.5.1)
	/// </summary>
	/// <remarks>
	/// Directives are <see cref="Trivia"/>, so they participate in the same tree traversal and output
	/// infrastructure as comments while staying outside expression/statement semantics and off the
	/// child-slot space of the node they annotate.
	/// </remarks>
	[DecompilerAstNode]
	public partial class PreProcessorDirective : Trivia
	{
		/// <summary>
		/// Gets or sets the directive kind.
		/// </summary>
		public PreProcessorDirectiveType Type { get; set; }

		/// <summary>
		/// Gets or sets the raw directive payload text (without the leading directive keyword token).
		/// </summary>
		public string? Argument { get; set; }

		/// <summary>
		/// Initializes a directive with explicit source span information.
		/// </summary>
		/// <param name="type">Directive kind.</param>
		/// <param name="startLocation">Source location where the directive starts.</param>
		/// <param name="endLocation">Source location where the directive ends.</param>
		public PreProcessorDirective(PreProcessorDirectiveType type, TextLocation startLocation, TextLocation endLocation) : base(startLocation, endLocation)
		{
			this.Type = type;
		}

		/// <summary>
		/// Initializes a directive from type and payload text when source coordinates are not tracked.
		/// </summary>
		/// <param name="type">Directive kind.</param>
		/// <param name="argument">Directive payload text.</param>
		public PreProcessorDirective(PreProcessorDirectiveType type, string? argument = null)
		{
			this.Type = type;
			this.Argument = argument;
		}

		protected internal override bool DoMatch(AstNode? other, PatternMatching.Match match)
		{
			PreProcessorDirective? o = other as PreProcessorDirective;
			return o != null && Type == o.Type && MatchString(Argument, o.Argument);
		}
	}
}

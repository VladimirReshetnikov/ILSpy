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
using System.IO;

using ICSharpCode.Decompiler.CSharp.Syntax;

namespace ICSharpCode.Decompiler.CSharp.OutputVisitor
{
	/// <summary>
	/// Defines the low-level token sink used by <see cref="CSharpOutputVisitor"/> while traversing the C# syntax tree.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unlike <see cref="TextWriter"/>-style output APIs, this abstraction receives semantic categories (identifier, keyword, punctuation,
	/// comments, and preprocessor directives) and explicit node-boundary notifications via <see cref="StartNode"/> and <see cref="EndNode"/>.
	/// Decorators can use those callbacks to inject whitespace, attach source locations, or preserve trivia without changing the visitor.
	/// </para>
	/// <para>
	/// Implementations are expected to preserve call order. The visitor drives <see cref="Indent"/>, <see cref="Unindent"/>, and
	/// <see cref="NewLine"/> explicitly; token writers should not apply additional structural formatting beyond what the call stream requests.
	/// </para>
	/// </remarks>
	public abstract class TokenWriter
	{
		/// <summary>
		/// Notifies the writer that emission of <paramref name="node"/> is starting.
		/// </summary>
		/// <param name="node">The AST node whose token sequence is about to be written.</param>
		public abstract void StartNode(AstNode node);

		/// <summary>
		/// Notifies the writer that emission of <paramref name="node"/> has finished.
		/// </summary>
		/// <param name="node">The AST node whose token sequence has just been written.</param>
		public abstract void EndNode(AstNode node);

		/// <summary>
		/// Writes an identifier.
		/// </summary>
		/// <param name="identifier">The identifier node to emit.</param>
		public abstract void WriteIdentifier(Identifier identifier);

		/// <summary>
		/// Writes a keyword to the output.
		/// </summary>
		/// <param name="role">The syntactic role that selected this keyword token.</param>
		/// <param name="keyword">The keyword text to emit.</param>
		public abstract void WriteKeyword(Role role, string keyword);

		/// <summary>
		/// Writes a token to the output.
		/// </summary>
		/// <param name="role">The syntactic role represented by <paramref name="token"/>.</param>
		/// <param name="token">The token text to emit.</param>
		public abstract void WriteToken(Role role, string token);

		/// <summary>
		/// Writes a primitive or literal value.
		/// </summary>
		/// <param name="value">The literal value to format.</param>
		/// <param name="format">Additional literal-formatting hints supplied by the caller.</param>
		public abstract void WritePrimitiveValue(object value, LiteralFormat format = LiteralFormat.None);

		/// <summary>
		/// Writes a primitive type keyword such as <c>int</c> or <c>string</c>.
		/// </summary>
		/// <param name="type">The primitive type token text.</param>
		public abstract void WritePrimitiveType(string type);

		/// <summary>
		/// Write a piece of text in an interpolated string literal.
		/// </summary>
		/// <param name="text">The raw interpolation text segment to encode and emit.</param>
		public abstract void WriteInterpolatedText(string text);

		/// <summary>
		/// Emits a single formatting space.
		/// </summary>
		public abstract void Space();

		/// <summary>
		/// Increases indentation depth for subsequent lines.
		/// </summary>
		public abstract void Indent();

		/// <summary>
		/// Decreases indentation depth for subsequent lines.
		/// </summary>
		public abstract void Unindent();

		/// <summary>
		/// Emits a line break and resets line-start state in the underlying writer.
		/// </summary>
		public abstract void NewLine();

		/// <summary>
		/// Writes a comment token.
		/// </summary>
		/// <param name="commentType">The concrete comment syntax to emit.</param>
		/// <param name="content">The comment body text, excluding start/end markers.</param>
		public abstract void WriteComment(CommentType commentType, string content);

		/// <summary>
		/// Writes a preprocessor directive.
		/// </summary>
		/// <param name="type">The directive kind.</param>
		/// <param name="argument">The directive argument text, or an empty string when no argument is present.</param>
		public abstract void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument);

		/// <summary>
		/// Creates the default token-writer pipeline for plain text output.
		/// </summary>
		/// <param name="writer">The destination text writer.</param>
		/// <param name="indentation">The indentation token used for each indentation level.</param>
		/// <returns>
		/// A writer chain that emits tokens, inserts required spacing, and preserves trivia (comments/directives) from the syntax tree.
		/// </returns>
		public static TokenWriter Create(TextWriter writer, string indentation = "\t")
		{
			return new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(new TextWriterTokenWriter(writer) { IndentationString = indentation }));
		}

		/// <summary>
		/// Creates a token-writer pipeline that also assigns source locations to generated AST token nodes.
		/// </summary>
		/// <param name="writer">The destination text writer.</param>
		/// <param name="indentation">The indentation token used for each indentation level.</param>
		/// <returns>
		/// A writer chain equivalent to <see cref="Create"/>, extended with <see cref="InsertMissingTokensDecorator"/> so emitted tokens receive coordinates.
		/// </returns>
		public static TokenWriter CreateWriterThatSetsLocationsInAST(TextWriter writer, string indentation = "\t")
		{
			var target = new TextWriterTokenWriter(writer) { IndentationString = indentation };
			return new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(new InsertMissingTokensDecorator(target, target)));
		}

		/// <summary>
		/// Wraps an existing writer with spacing logic that inserts mandatory separators between adjacent tokens.
		/// </summary>
		/// <param name="writer">The writer to decorate.</param>
		/// <returns>A decorator that emits required spaces before delegating token output to <paramref name="writer"/>.</returns>
		public static TokenWriter InsertRequiredSpaces(TokenWriter writer)
		{
			return new InsertRequiredSpacesDecorator(writer);
		}

		/// <summary>
		/// Wraps an existing writer so emitted tokens are materialized into the AST with source locations.
		/// </summary>
		/// <param name="writer">The writer to decorate. Must also implement <see cref="ILocatable"/>.</param>
		/// <returns>A decorator that inserts missing token nodes and updates their coordinates.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when <paramref name="writer"/> does not implement <see cref="ILocatable"/>, because location information is required.
		/// </exception>
		public static TokenWriter WrapInWriterThatSetsLocationsInAST(TokenWriter writer)
		{
			if (!(writer is ILocatable))
				throw new InvalidOperationException("writer does not provide locations!");
			return new InsertMissingTokensDecorator(writer, (ILocatable)writer);
		}
	}

	/// <summary>
	/// Exposes the current output location for token writers that can report line/column coordinates.
	/// </summary>
	public interface ILocatable
	{
		/// <summary>
		/// Gets the line/column location where the next emitted character would be written.
		/// </summary>
		TextLocation Location { get; }

		/// <summary>
		/// Gets the total number of emitted characters, including line terminators and indentation.
		/// </summary>
		int Length { get; }
	}

	/// <summary>
	/// Base class for token-writer decorators that forward all operations to an inner writer by default.
	/// </summary>
	public abstract class DecoratingTokenWriter : TokenWriter
	{
		readonly TokenWriter decoratedWriter;

		/// <summary>
		/// Initializes a new decorator around <paramref name="decoratedWriter"/>.
		/// </summary>
		/// <param name="decoratedWriter">The inner writer that receives forwarded calls.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="decoratedWriter"/> is <see langword="null"/>.</exception>
		protected DecoratingTokenWriter(TokenWriter decoratedWriter)
		{
			if (decoratedWriter == null)
				throw new ArgumentNullException(nameof(decoratedWriter));
			this.decoratedWriter = decoratedWriter;
		}

		public override void StartNode(AstNode node)
		{
			decoratedWriter.StartNode(node);
		}

		public override void EndNode(AstNode node)
		{
			decoratedWriter.EndNode(node);
		}

		public override void WriteIdentifier(Identifier identifier)
		{
			decoratedWriter.WriteIdentifier(identifier);
		}

		public override void WriteKeyword(Role role, string keyword)
		{
			decoratedWriter.WriteKeyword(role, keyword);
		}

		public override void WriteToken(Role role, string token)
		{
			decoratedWriter.WriteToken(role, token);
		}

		public override void WritePrimitiveValue(object value, LiteralFormat format = LiteralFormat.None)
		{
			decoratedWriter.WritePrimitiveValue(value, format);
		}

		public override void WritePrimitiveType(string type)
		{
			decoratedWriter.WritePrimitiveType(type);
		}

		public override void WriteInterpolatedText(string text)
		{
			decoratedWriter.WriteInterpolatedText(text);
		}

		public override void Space()
		{
			decoratedWriter.Space();
		}

		public override void Indent()
		{
			decoratedWriter.Indent();
		}

		public override void Unindent()
		{
			decoratedWriter.Unindent();
		}

		public override void NewLine()
		{
			decoratedWriter.NewLine();
		}

		public override void WriteComment(CommentType commentType, string content)
		{
			decoratedWriter.WriteComment(commentType, content);
		}

		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			decoratedWriter.WritePreProcessorDirective(type, argument);
		}
	}
}

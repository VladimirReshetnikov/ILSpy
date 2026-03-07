// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Defines the output sink used by decompiler and disassembler components to emit formatted text and semantic references.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations receive both plain text and rich reference writes. Rich writes carry symbol handles that callers such as
	/// the WPF frontend can map to navigation targets, while plain writes are used for punctuation and text fragments that do not
	/// represent a navigable symbol.
	/// </para>
	/// <para>
	/// Callers rely on explicit <see cref="Indent"/>, <see cref="Unindent"/>, and <see cref="WriteLine"/> calls instead of implicit
	/// indentation logic. Implementations are expected to be thread-compatible: the decompiler writes from a single logical writer
	/// at a time, and concurrent use requires external synchronization.
	/// </para>
	/// </remarks>
	public interface ITextOutput
	{
		/// <summary>
		/// Gets or sets the text emitted for one indentation level.
		/// </summary>
		/// <value>
		/// The indentation token used when a new line begins at a non-zero indentation depth.
		/// </value>
		string IndentationString { get; set; }

		/// <summary>
		/// Increases the current indentation depth for subsequent lines.
		/// </summary>
		void Indent();

		/// <summary>
		/// Decreases the current indentation depth for subsequent lines.
		/// </summary>
		void Unindent();

		/// <summary>
		/// Writes a single character to the output.
		/// </summary>
		/// <param name="ch">The character to emit.</param>
		void Write(char ch);

		/// <summary>
		/// Writes text to the output without appending a line terminator.
		/// </summary>
		/// <param name="text">The text fragment to emit.</param>
		void Write(string text);

		/// <summary>
		/// Writes a line terminator and prepares indentation for the next line.
		/// </summary>
		void WriteLine();

		/// <summary>
		/// Writes an IL opcode token and optionally removes the suffix after the final dot.
		/// </summary>
		/// <param name="opCode">The opcode metadata used for display text and optional navigation metadata.</param>
		/// <param name="omitSuffix">
		/// <see langword="true"/> to omit the trailing opcode suffix (for example <c>ldarg.s</c> to <c>ldarg.</c>);
		/// otherwise, <see langword="false"/>.
		/// </param>
		void WriteReference(OpCodeInfo opCode, bool omitSuffix = false);

		/// <summary>
		/// Writes text representing a metadata handle and associates it with a navigation reference.
		/// </summary>
		/// <param name="metadata">The metadata file that owns <paramref name="handle"/>.</param>
		/// <param name="handle">The target metadata handle represented by <paramref name="text"/>.</param>
		/// <param name="text">The text displayed to the user.</param>
		/// <param name="protocol">
		/// The reference protocol name used by consumers to route navigation (for example <c>decompile</c>).
		/// </param>
		/// <param name="isDefinition">
		/// <see langword="true"/> if the text denotes a definition site; otherwise <see langword="false"/> for a usage site.
		/// </param>
		void WriteReference(MetadataFile metadata, Handle handle, string text, string protocol = "decompile", bool isDefinition = false);

		/// <summary>
		/// Writes text representing a type symbol and associates it with type navigation metadata.
		/// </summary>
		/// <param name="type">The referenced type symbol.</param>
		/// <param name="text">The text displayed to the user.</param>
		/// <param name="isDefinition">
		/// <see langword="true"/> if the write represents the definition location; otherwise <see langword="false"/>.
		/// </param>
		void WriteReference(IType type, string text, bool isDefinition = false);

		/// <summary>
		/// Writes text representing a member symbol and associates it with member navigation metadata.
		/// </summary>
		/// <param name="member">The referenced member symbol.</param>
		/// <param name="text">The text displayed to the user.</param>
		/// <param name="isDefinition">
		/// <see langword="true"/> if the write represents the definition location; otherwise <see langword="false"/>.
		/// </param>
		void WriteReference(IMember member, string text, bool isDefinition = false);

		/// <summary>
		/// Writes text associated with a local, method-scoped reference.
		/// </summary>
		/// <param name="text">The text displayed to the user.</param>
		/// <param name="reference">
		/// An implementation-defined identity object that lets consumers connect local definitions and uses.
		/// </param>
		/// <param name="isDefinition">
		/// <see langword="true"/> when this token introduces the local reference; otherwise <see langword="false"/>.
		/// </param>
		void WriteLocalReference(string text, object reference, bool isDefinition = false);

		/// <summary>
		/// Marks the start of a foldable region.
		/// </summary>
		/// <param name="collapsedText">Placeholder text shown when the region is collapsed.</param>
		/// <param name="defaultCollapsed">
		/// <see langword="true"/> to request that viewers collapse the region by default; otherwise <see langword="false"/>.
		/// </param>
		/// <param name="isDefinition">
		/// <see langword="true"/> when the region encloses a definition (for example a type or method body).
		/// </param>
		void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false, bool isDefinition = false);

		/// <summary>
		/// Marks the end of the current foldable region.
		/// </summary>
		void MarkFoldEnd();
	}

	/// <summary>
	/// Provides formatting helpers for <see cref="ITextOutput"/> implementations.
	/// </summary>
	public static class TextOutputExtensions
	{
		/// <summary>
		/// Formats text with <see cref="string.Format(string, object[])"/> and writes the resulting fragment.
		/// </summary>
		/// <param name="output">The destination output.</param>
		/// <param name="format">The composite format string.</param>
		/// <param name="args">The format arguments.</param>
		public static void Write(this ITextOutput output, string format, params object[] args)
		{
			output.Write(string.Format(format, args));
		}

		/// <summary>
		/// Writes <paramref name="text"/> followed by a line terminator.
		/// </summary>
		/// <param name="output">The destination output.</param>
		/// <param name="text">The text fragment to emit before the line break.</param>
		public static void WriteLine(this ITextOutput output, string text)
		{
			output.Write(text);
			output.WriteLine();
		}

		/// <summary>
		/// Formats text with <see cref="string.Format(string, object[])"/>, writes it, and appends a line terminator.
		/// </summary>
		/// <param name="output">The destination output.</param>
		/// <param name="format">The composite format string.</param>
		/// <param name="args">The format arguments.</param>
		public static void WriteLine(this ITextOutput output, string format, params object[] args)
		{
			output.WriteLine(string.Format(format, args));
		}
	}
}

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Minimal <see cref="ITextOutput"/> implementation that writes plain text to a <see cref="TextWriter"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type deliberately ignores navigation metadata and fold markers. Calls such as <see cref="WriteReference(MetadataFile, Handle, string, string, bool)"/>
	/// and <see cref="ITextOutput.MarkFoldStart(string, bool, bool)"/> degrade to plain textual output so the same decompiler pipeline can target both rich and plain sinks.
	/// </para>
	/// <para>
	/// <see cref="Location"/> tracks the logical write position in terms of line and column, including deferred indentation that has been requested but not
	/// emitted yet. The implementation is not thread-safe.
	/// </para>
	/// </remarks>
	public sealed class PlainTextOutput : ITextOutput, IDisposable
	{
		readonly TextWriter writer;
		readonly bool ownsWriter;
		int indent;
		bool needsIndent;

		int line = 1;
		int column = 1;

		/// <summary>
		/// Gets or sets the string written for each indentation level.
		/// </summary>
		public string IndentationString { get; set; } = "\t";

		/// <summary>
		/// Initializes a plain-text output wrapper over an existing <see cref="TextWriter"/>.
		/// </summary>
		/// <param name="writer">The destination writer.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is <see langword="null"/>.</exception>
		public PlainTextOutput(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			this.writer = writer;
			this.ownsWriter = false;
		}

		/// <summary>
		/// Initializes a plain-text output instance backed by an in-memory <see cref="StringWriter"/>.
		/// </summary>
		public PlainTextOutput()
		{
			this.writer = new StringWriter();
			this.ownsWriter = true;
		}

		public void Dispose()
		{
			if (ownsWriter)
				writer.Dispose();
		}

		/// <summary>
		/// Gets the current write location in the output stream.
		/// </summary>
		/// <value>
		/// A one-based line and column pair that reflects the next emitted character.
		/// </value>
		public TextLocation Location {
			get {
				return new TextLocation(line, column + (needsIndent ? indent : 0));
			}
		}

		/// <summary>
		/// Returns the text written so far when this instance owns an in-memory writer.
		/// </summary>
		/// <returns>
		/// The buffered text for an instance created with the parameterless constructor; otherwise whatever the
		/// wrapped <see cref="TextWriter"/> returns from <see cref="object.ToString"/>, which is normally its type name.
		/// </returns>
		public override string ToString()
		{
			return writer.ToString();
		}

		/// <summary>
		/// Increases indentation depth for subsequent lines.
		/// </summary>
		public void Indent()
		{
			indent++;
		}

		/// <summary>
		/// Decreases indentation depth for subsequent lines.
		/// </summary>
		public void Unindent()
		{
			indent--;
		}

		/// <summary>
		/// Writes any pending indentation prefix for the current line.
		/// </summary>
		void WriteIndent()
		{
			if (needsIndent)
			{
				needsIndent = false;
				for (int i = 0; i < indent; i++)
				{
					writer.Write(IndentationString);
				}
				column += indent;
			}
		}

		/// <summary>
		/// Writes a single character.
		/// </summary>
		/// <param name="ch">The character to emit.</param>
		public void Write(char ch)
		{
			WriteIndent();
			writer.Write(ch);
			column++;
		}

		/// <summary>
		/// Writes a text fragment.
		/// </summary>
		/// <param name="text">The text to emit.</param>
		public void Write(string text)
		{
			WriteIndent();
			writer.Write(text);
			column += text.Length;
		}

		/// <summary>
		/// Writes a line terminator and marks the next line as indentation-pending.
		/// </summary>
		public void WriteLine()
		{
			writer.WriteLine();
			needsIndent = true;
			line++;
			column = 1;
		}

		/// <summary>
		/// Writes an opcode display name.
		/// </summary>
		/// <param name="opCode">The opcode descriptor.</param>
		/// <param name="omitSuffix">
		/// <see langword="true"/> to truncate text after the final dot in <see cref="Disassembler.OpCodeInfo.Name"/>;
		/// otherwise, <see langword="false"/>.
		/// </param>
		public void WriteReference(Disassembler.OpCodeInfo opCode, bool omitSuffix = false)
		{
			if (omitSuffix)
			{
				int lastDot = opCode.Name.LastIndexOf('.');
				if (lastDot > 0)
				{
					Write(opCode.Name.Remove(lastDot + 1));
				}
			}
			else
			{
				Write(opCode.Name);
			}
		}

		/// <summary>
		/// Writes metadata-reference text without preserving navigation metadata.
		/// </summary>
		/// <param name="module">The metadata module that owns <paramref name="handle"/>.</param>
		/// <param name="handle">The metadata handle represented by <paramref name="text"/>.</param>
		/// <param name="text">The visible text to emit.</param>
		/// <param name="protocol">Unused by this implementation.</param>
		/// <param name="isDefinition">Unused by this implementation.</param>
		public void WriteReference(MetadataFile module, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
		{
			Write(text);
		}

		/// <summary>
		/// Writes type-reference text without preserving navigation metadata.
		/// </summary>
		/// <param name="type">The referenced type.</param>
		/// <param name="text">The visible text to emit.</param>
		/// <param name="isDefinition">Unused by this implementation.</param>
		public void WriteReference(IType type, string text, bool isDefinition = false)
		{
			Write(text);
		}

		/// <summary>
		/// Writes member-reference text without preserving navigation metadata.
		/// </summary>
		/// <param name="member">The referenced member.</param>
		/// <param name="text">The visible text to emit.</param>
		/// <param name="isDefinition">Unused by this implementation.</param>
		public void WriteReference(IMember member, string text, bool isDefinition = false)
		{
			Write(text);
		}

		/// <summary>
		/// Writes local-reference text without preserving definition/use linkage metadata.
		/// </summary>
		/// <param name="text">The visible text to emit.</param>
		/// <param name="reference">Unused by this implementation.</param>
		/// <param name="isDefinition">Unused by this implementation.</param>
		/// <param name="isHoverOnly">Unused by this implementation.</param>
		public void WriteLocalReference(string text, object reference, bool isDefinition = false, bool isHoverOnly = false)
		{
			Write(text);
		}

		/// <summary>
		/// Ignored in <see cref="PlainTextOutput"/>.
		/// </summary>
		void ITextOutput.MarkFoldStart(string collapsedText, bool defaultCollapsed, bool isDefinition)
		{
		}

		/// <summary>
		/// Ignored in <see cref="PlainTextOutput"/>.
		/// </summary>
		void ITextOutput.MarkFoldEnd()
		{
		}
	}

	/// <summary>
	/// Buffers <see cref="ITextOutput"/> operations so callers can either commit or drop the buffered writes.
	/// </summary>
	/// <remarks>
	/// The instance records operations as deferred delegates. Buffered operations are replayed against the target output in original order
	/// only when <see cref="Commit"/> is invoked.
	/// </remarks>
	internal class TextOutputWithRollback : ITextOutput
	{
		List<Action<ITextOutput>> actions;
		ITextOutput target;

		/// <summary>
		/// Initializes a rollback-capable output wrapper.
		/// </summary>
		/// <param name="target">The output that receives operations after <see cref="Commit"/>.</param>
		public TextOutputWithRollback(ITextOutput target)
		{
			this.target = target;
			this.actions = new List<Action<ITextOutput>>();
		}

		/// <summary>
		/// Gets or sets the indentation token delegated to the target output.
		/// </summary>
		string ITextOutput.IndentationString {
			get {
				return target.IndentationString;
			}
			set {
				target.IndentationString = value;
			}
		}

		/// <summary>
		/// Replays all buffered operations to the target output.
		/// </summary>
		public void Commit()
		{
			foreach (var action in actions)
			{
				action(target);
			}
		}

		/// <summary>
		/// Buffers an indentation increment operation.
		/// </summary>
		public void Indent()
		{
			actions.Add(target => target.Indent());
		}

		/// <summary>
		/// Buffers a fold-end operation.
		/// </summary>
		public void MarkFoldEnd()
		{
			actions.Add(target => target.MarkFoldEnd());
		}

		/// <summary>
		/// Buffers a fold-start operation.
		/// </summary>
		/// <param name="collapsedText">Collapsed placeholder text.</param>
		/// <param name="defaultCollapsed">Whether viewers should collapse the region by default.</param>
		/// <param name="isDefinition">Not forwarded to the target output when the buffer is committed.</param>
		public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false, bool isDefinition = false)
		{
			actions.Add(target => target.MarkFoldStart(collapsedText, defaultCollapsed));
		}

		/// <summary>
		/// Buffers an indentation decrement operation.
		/// </summary>
		public void Unindent()
		{
			actions.Add(target => target.Unindent());
		}

		/// <summary>
		/// Buffers a character write.
		/// </summary>
		/// <param name="ch">The character to emit when committed.</param>
		public void Write(char ch)
		{
			actions.Add(target => target.Write(ch));
		}

		/// <summary>
		/// Buffers a text write.
		/// </summary>
		/// <param name="text">The text to emit when committed.</param>
		public void Write(string text)
		{
			actions.Add(target => target.Write(text));
		}

		/// <summary>
		/// Buffers a line-break write.
		/// </summary>
		public void WriteLine()
		{
			actions.Add(target => target.WriteLine());
		}

		/// <summary>
		/// Buffers a local-reference write.
		/// </summary>
		/// <param name="text">The text to emit when committed.</param>
		/// <param name="reference">Identity object connecting the local's definition and uses.</param>
		/// <param name="isDefinition">Whether this token introduces the local reference.</param>
		/// <param name="isHoverOnly">Not forwarded to the target output when the buffer is committed.</param>
		public void WriteLocalReference(string text, object reference, bool isDefinition = false, bool isHoverOnly = false)
		{
			actions.Add(target => target.WriteLocalReference(text, reference, isDefinition));
		}

		/// <summary>
		/// Buffers an opcode reference write.
		/// </summary>
		/// <param name="opCode">The opcode to emit when committed.</param>
		/// <param name="omitSuffix">Not forwarded to the target output when the buffer is committed.</param>
		public void WriteReference(OpCodeInfo opCode, bool omitSuffix = false)
		{
			actions.Add(target => target.WriteReference(opCode));
		}

		/// <summary>
		/// Buffers a metadata-handle reference write.
		/// </summary>
		public void WriteReference(MetadataFile module, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
		{
			actions.Add(target => target.WriteReference(module, handle, text, protocol, isDefinition));
		}

		/// <summary>
		/// Buffers a type reference write.
		/// </summary>
		public void WriteReference(IType type, string text, bool isDefinition = false)
		{
			actions.Add(target => target.WriteReference(type, text, isDefinition));
		}

		/// <summary>
		/// Buffers a member reference write.
		/// </summary>
		public void WriteReference(IMember member, string text, bool isDefinition = false)
		{
			actions.Add(target => target.WriteReference(member, text, isDefinition));
		}
	}
}

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
using System.Diagnostics;

namespace ICSharpCode.Decompiler.DebugInfo
{
	/// <summary>
	/// Represents a mapping between an IL offset range and a source-text span.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Instances are used by both readers (when consuming existing symbols) and writers
	/// (when generating Portable PDB data from decompiled syntax trees).
	/// </para>
	/// <para>
	/// The IL range follows half-open semantics: <see cref="Offset"/> is inclusive and
	/// <see cref="EndOffset"/> is exclusive.
	/// </para>
	/// </remarks>
	[DebuggerDisplay("SequencePoint IL_{Offset,h}-IL_{EndOffset,h}, {StartLine}:{StartColumn}-{EndLine}:{EndColumn}, IsHidden={IsHidden}")]
	public class SequencePoint
	{
		/// <summary>
		/// Gets or sets the inclusive start offset of the covered IL range.
		/// </summary>
		public int Offset { get; set; }

		/// <summary>
		/// Gets or sets the exclusive end offset of the covered IL range.
		/// </summary>
		/// <remarks>
		/// Portable PDB sequence point records encode only the starting offset of each point.
		/// ILSpy tracks an explicit end offset while constructing and normalizing ranges so it can
		/// fill uncovered gaps with hidden points before writing the final metadata stream.
		/// </remarks>
		public int EndOffset { get; set; }

		/// <summary>
		/// Gets or sets the 1-based source start line.
		/// </summary>
		public int StartLine { get; set; }

		/// <summary>
		/// Gets or sets the 1-based source start column.
		/// </summary>
		public int StartColumn { get; set; }

		/// <summary>
		/// Gets or sets the 1-based source end line.
		/// </summary>
		public int EndLine { get; set; }

		/// <summary>
		/// Gets or sets the 1-based source end column.
		/// </summary>
		public int EndColumn { get; set; }

		/// <summary>
		/// Gets a value indicating whether this sequence point is hidden from source stepping.
		/// </summary>
		/// <remarks>
		/// Hidden points use the sentinel line value <c>0xFEEFEE</c>, matching
		/// <see cref="System.Reflection.Metadata.SequencePoint.HiddenLine"/>.
		/// </remarks>
		public bool IsHidden {
			get { return StartLine == 0xfeefee && StartLine == EndLine; }
		}

		/// <summary>
		/// Gets or sets the source document URL/path stored for this point.
		/// </summary>
		/// <value>
		/// The document name supplied by the symbol provider. The value may be empty when
		/// a sequence point does not reference a concrete document record.
		/// </value>
		public string DocumentUrl { get; set; }

		/// <summary>
		/// Marks this sequence point as hidden by assigning the Portable PDB hidden-line sentinel.
		/// </summary>
		internal void SetHidden()
		{
			StartLine = EndLine = 0xfeefee;
		}
	}
}

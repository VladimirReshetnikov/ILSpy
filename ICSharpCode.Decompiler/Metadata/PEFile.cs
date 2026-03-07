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

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Represents a managed Portable Executable (PE) image and exposes PE-specific services such as section access and method body lookup.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="PEFile"/> extends <see cref="MetadataFile"/> with direct access to the underlying <see cref="PEReader"/>.
	/// This enables consumers such as the disassembler and control-flow recovery passes to map RVAs, inspect section headers,
	/// and read raw method bodies.
	/// </para>
	/// <para>
	/// Instances own the <see cref="Reader"/> lifetime and must be disposed when no longer needed.
	/// </para>
	/// </remarks>
	[DebuggerDisplay("{FileName}")]
	public class PEFile : MetadataFile, IDisposable, IModuleReference
	{
		/// <summary>
		/// Gets the underlying PE reader used to serve section and method-body requests.
		/// </summary>
		public PEReader Reader { get; }

		/// <summary>
		/// Opens a PE file from disk and initializes metadata access.
		/// </summary>
		/// <param name="fileName">Path of the PE file to open.</param>
		/// <param name="streamOptions">Stream ownership and prefetch behavior options for <see cref="PEReader"/>.</param>
		/// <param name="metadataOptions">Options controlling how metadata tables and handles are interpreted.</param>
		/// <param name="utf8Decoder">Optional UTF-8 decoder used for metadata string decoding.</param>
		public PEFile(string fileName, PEStreamOptions streamOptions = PEStreamOptions.Default, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
			: this(fileName, new PEReader(new FileStream(fileName, FileMode.Open, FileAccess.Read), streamOptions), metadataOptions, utf8Decoder)
		{
		}

		/// <summary>
		/// Initializes a PE-backed metadata file from an existing stream.
		/// </summary>
		/// <param name="fileName">Display path associated with the stream content.</param>
		/// <param name="stream">Readable stream positioned at the beginning of a PE image.</param>
		/// <param name="streamOptions">Stream ownership and prefetch behavior options for <see cref="PEReader"/>.</param>
		/// <param name="metadataOptions">Options controlling how metadata tables and handles are interpreted.</param>
		/// <param name="utf8Decoder">Optional UTF-8 decoder used for metadata string decoding.</param>
		public PEFile(string fileName, Stream stream, PEStreamOptions streamOptions = PEStreamOptions.Default, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
			: this(fileName, new PEReader(stream, streamOptions), metadataOptions, utf8Decoder)
		{
		}

		/// <summary>
		/// Initializes a PE-backed metadata file from an existing <see cref="PEReader"/> instance.
		/// </summary>
		/// <param name="fileName">Display path associated with the reader content.</param>
		/// <param name="reader">PE reader exposing managed metadata and section data.</param>
		/// <param name="metadataOptions">Options controlling how metadata tables and handles are interpreted.</param>
		/// <param name="utf8Decoder">Optional UTF-8 decoder used for metadata string decoding.</param>
		public PEFile(string fileName, PEReader reader, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
			: base(MetadataFileKind.PortableExecutable, fileName, reader, metadataOptions, utf8Decoder)
		{
			this.Reader = reader;
		}

		/// <summary>
		/// Gets <see langword="false"/> because top-level PE files are never treated as embedded metadata payloads.
		/// </summary>
		public override bool IsEmbedded => false;

		/// <summary>
		/// Gets the offset of the metadata root within the PE file.
		/// </summary>
		public override int MetadataOffset => Reader.PEHeaders.MetadataStartOffset;

		/// <summary>
		/// Gets <see langword="false"/> because PE images include executable sections and method bodies.
		/// </summary>
		public override bool IsMetadataOnly => false;

		/// <summary>
		/// Disposes the underlying <see cref="PEReader"/> and releases associated file/stream resources.
		/// </summary>
		public void Dispose()
		{
			Reader.Dispose();
		}

		IModule TypeSystem.IModuleReference.Resolve(ITypeResolveContext context)
		{
			return new MetadataModule(context.Compilation, this, TypeSystemOptions.Default);
		}

		/// <summary>
		/// Reads and decodes the method body at the specified RVA.
		/// </summary>
		/// <param name="rva">Relative virtual address of the method body.</param>
		/// <returns>The decoded method body block.</returns>
		public override MethodBodyBlock GetMethodBody(int rva)
		{
			return Reader.GetMethodBody(rva);
		}

		/// <summary>
		/// Returns the PE section data that contains the specified RVA.
		/// </summary>
		/// <param name="rva">Relative virtual address to locate.</param>
		/// <returns>A <see cref="SectionData"/> view over the containing section.</returns>
		public override SectionData GetSectionData(int rva)
		{
			return new SectionData(Reader.GetSectionData(rva));
		}

		/// <summary>
		/// Returns the zero-based PE section index that contains the specified RVA.
		/// </summary>
		/// <param name="rva">Relative virtual address to locate.</param>
		/// <returns>The containing section index, or <c>-1</c> if no section contains <paramref name="rva"/>.</returns>
		public override int GetContainingSectionIndex(int rva)
		{
			return Reader.PEHeaders.GetContainingSectionIndex(rva);
		}

		ImmutableArray<SectionHeader> sectionHeaders;
		/// <summary>
		/// Gets PE section headers projected into the decompiler's <see cref="SectionHeader"/> representation.
		/// </summary>
	/// <remarks>
		/// The value is materialized lazily from <see cref="PEHeaders.SectionHeaders"/> and cached for subsequent calls.
		/// </remarks>
		public override ImmutableArray<SectionHeader> SectionHeaders {
			get {
				var value = sectionHeaders;
				if (value.IsDefault)
				{
					value = Reader.PEHeaders.SectionHeaders
						.Select(h => new SectionHeader {
							Name = h.Name,
							RawDataPtr = unchecked((uint)h.PointerToRawData),
							RawDataSize = unchecked((uint)h.SizeOfRawData),
							VirtualAddress = unchecked((uint)h.VirtualAddress),
							VirtualSize = unchecked((uint)h.VirtualSize)
						}).ToImmutableArray();
					sectionHeaders = value;
				}
				return value;
			}
		}

		/// <summary>
		/// Gets the CLI header from the PE headers, if present.
		/// </summary>
		public override CorHeader? CorHeader => Reader.PEHeaders.CorHeader;
	}
}

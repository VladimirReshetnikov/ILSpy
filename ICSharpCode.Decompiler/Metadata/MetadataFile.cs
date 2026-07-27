// Copyright (c) 2024 Siegfried Pammer
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// MetadataFile is the main class the decompiler uses to represent a metadata assembly/module.
	/// Every file on disk can be loaded into a standalone MetadataFile instance.
	/// 
	/// A MetadataFile can be combined with its referenced assemblies/modules to form a type system,
	/// in that case the <see cref="MetadataModule"/> class is used instead.
	/// </summary>
	/// <remarks>
	/// In addition to wrapping a <c>System.Reflection.Metadata.MetadataReader</c>, this class
	/// contains a few decompiler-specific caches to allow efficiently constructing a type
	/// system from multiple MetadataFiles. This allows the caches to be shared across multiple
	/// decompiled type systems.
	/// </remarks>
	[DebuggerDisplay("{Kind}: {FileName}")]
	public class MetadataFile : IDisposable
	{
		/// <summary>
		/// Identifies the physical container format that produced a <see cref="MetadataFile"/> instance.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The value is selected by the file-loader pipeline and is consumed by host layers to determine iconography,
		/// feature availability, and diagnostics wording.
		/// </para>
		/// <para>
		/// The enum does not describe whether the file contains method bodies. Use <see cref="IsMetadataOnly"/> for that decision.
		/// </para>
		/// </remarks>
		public enum MetadataFileKind
		{
			/// <summary>
			/// Standard .NET PE image (for example <c>.dll</c> or <c>.exe</c>) loaded through <see cref="PEReader"/>.
			/// </summary>
			PortableExecutable,

			/// <summary>
			/// Portable PDB or equivalent standalone debug metadata payload.
			/// </summary>
			ProgramDebugDatabase,

			/// <summary>
			/// WebCIL image embedded in a WebAssembly payload.
			/// </summary>
			WebCIL,

			/// <summary>
			/// Raw metadata stream that is not represented as a PE image.
			/// </summary>
			Metadata
		}

		/// <summary>
		/// Gets the source path supplied by the loader for this metadata instance.
		/// </summary>
		public string FileName { get; }

		/// <summary>
		/// Gets the format classification assigned when this metadata file was created.
		/// </summary>
		public MetadataFileKind Kind { get; }

		/// <summary>
		/// Gets the underlying metadata reader used for all table and blob access.
		/// </summary>
		public MetadataReader Metadata { get; }

		/// <summary>
		/// Gets the byte offset, relative to the original file, where the managed metadata root begins.
		/// </summary>
		public virtual int MetadataOffset { get; }

		/// <summary>
		/// Gets whether this metadata was read from an embedded source (for example embedded PDB metadata)
		/// instead of a top-level file payload.
		/// </summary>
		public virtual bool IsEmbedded { get; }

		/// <summary>
		/// Gets whether the file carries metadata only and therefore cannot provide PE section data or method bodies.
		/// </summary>
		public virtual bool IsMetadataOnly { get; } = true;

		/// <summary>
		/// Gets whether the metadata root represents an assembly definition instead of a module-only payload.
		/// </summary>
		public bool IsAssembly => Metadata.IsAssembly;

		string? name;

		/// <summary>
		/// Gets the simple name of the assembly or module represented by this metadata.
		/// </summary>
		/// <remarks>
		/// For standalone debug metadata that has no module table, this property returns the literal string
		/// <c>debug metadata</c>.
		/// </remarks>
		public string Name {
			get {
				var value = LazyInit.VolatileRead(ref name);
				if (value == null)
				{
					var metadata = Metadata;
					if (metadata.IsAssembly)
						value = metadata.GetString(metadata.GetAssemblyDefinition().Name);
					else if (metadata.DebugMetadataHeader == null) // standalone debug metadata does not contain module table
						value = metadata.GetString(metadata.GetModuleDefinition().Name);
					else
						value = "debug metadata";
					value = LazyInit.GetOrSet(ref name, value);
				}
				return value;
			}
		}

		string? fullName;

		/// <summary>
		/// Gets the display name used by the decompiler for this metadata root.
		/// </summary>
		/// <remarks>
		/// For assemblies this is the full assembly identity, including version, culture, and public key token.
		/// Non-assembly payloads reuse <see cref="Name"/>.
		/// </remarks>
		public string FullName {
			get {
				var value = LazyInit.VolatileRead(ref fullName);
				if (value == null)
				{
					var metadata = Metadata;
					value = metadata.IsAssembly ? metadata.GetFullAssemblyName() : Name;
					value = LazyInit.GetOrSet(ref fullName, value);
				}
				return value;
			}
		}

		/// <summary>
		/// Infers the target CLR generation from <see cref="MetadataReader.MetadataVersion"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="TargetRuntime"/> value inferred from the metadata version string, or
		/// <see cref="TargetRuntime.Unknown"/> if the version cannot be recognized.
		/// </returns>
		public TargetRuntime GetRuntime()
		{
			string version = Metadata.MetadataVersion;
			if (version == null || version.Length <= 1)
				return TargetRuntime.Unknown;
			switch (version[1])
			{
				case '1':
					if (version.Length <= 3)
						return TargetRuntime.Unknown;
					if (version[3] == 1)
						return TargetRuntime.Net_1_0;
					else
						return TargetRuntime.Net_1_1;
				case '2':
					return TargetRuntime.Net_2_0;
				case '4':
					return TargetRuntime.Net_4_0;
				default:
					return TargetRuntime.Unknown;
			}
		}

		ImmutableArray<AssemblyReference> assemblyReferences;
		/// <summary>
		/// Gets the assembly references declared in this metadata file.
		/// </summary>
		/// <remarks>
		/// The array is computed lazily and cached. Entries preserve metadata table order.
		/// </remarks>
		public ImmutableArray<AssemblyReference> AssemblyReferences {
			get {
				var value = assemblyReferences;
				if (value.IsDefault)
				{
					value = Metadata.AssemblyReferences.Select(r => new AssemblyReference(this.Metadata, r)).ToImmutableArray();
					assemblyReferences = value;
				}
				return value;
			}
		}

		ImmutableArray<ModuleReferenceMetadata> moduleReferences;
		/// <summary>
		/// Gets the module references declared in this metadata file.
		/// </summary>
		/// <remarks>
		/// The result is built on first access and then reused.
		/// </remarks>
		public ImmutableArray<ModuleReferenceMetadata> ModuleReferences {
			get {
				var value = moduleReferences;
				if (value.IsDefault)
				{
					value = Metadata.GetModuleReferences()
							.Select(m => new ModuleReferenceMetadata(this.Metadata, m))
							.ToImmutableArray();

					moduleReferences = value;
				}
				return value;
			}
		}

		/// <summary>
		/// Gets all manifest resources declared by this metadata root.
		/// </summary>
		/// <remarks>
		/// The property returns a freshly materialized immutable array on each access.
		/// </remarks>
		public ImmutableArray<Resource> Resources => GetResources().ToImmutableArray();

		IEnumerable<Resource> GetResources()
		{
			var metadata = Metadata;
			foreach (var h in metadata.ManifestResources)
			{
				yield return new MetadataResource(this, h);
			}
		}

		Dictionary<TopLevelTypeName, TypeDefinitionHandle>? typeLookup;

		/// <summary>
		/// Finds the top-level-type with the specified name.
		/// </summary>
		/// <param name="typeName">Namespace-qualified top-level type name to search for.</param>
		/// <returns>
		/// A <see cref="TypeDefinitionHandle"/> for the matching top-level type, or the default handle when no match exists.
		/// </returns>
		public TypeDefinitionHandle GetTypeDefinition(TopLevelTypeName typeName)
		{
			var lookup = LazyInit.VolatileRead(ref typeLookup);
			if (lookup == null)
			{
				lookup = new Dictionary<TopLevelTypeName, TypeDefinitionHandle>();
				foreach (var handle in Metadata.TypeDefinitions)
				{
					var td = Metadata.GetTypeDefinition(handle);
					if (!td.GetDeclaringType().IsNil)
					{
						continue; // nested type
					}
					var nsHandle = td.Namespace;
					string ns = nsHandle.IsNil ? string.Empty : Metadata.GetString(nsHandle);
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(Metadata.GetString(td.Name), out int typeParameterCount);
					lookup[new TopLevelTypeName(ns, name, typeParameterCount)] = handle;
				}
				lookup = LazyInit.GetOrSet(ref typeLookup, lookup);
			}
			if (lookup.TryGetValue(typeName, out var resultHandle))
				return resultHandle;
			else
				return default;
		}

		Dictionary<FullTypeName, ExportedTypeHandle>? typeForwarderLookup;

		/// <summary>
		/// Finds the type forwarder with the specified name.
		/// </summary>
		/// <param name="typeName">Fully qualified forwarded type name.</param>
		/// <returns>
		/// The matching <see cref="ExportedTypeHandle"/>, or the default handle if the metadata does not forward that type.
		/// </returns>
		public ExportedTypeHandle GetTypeForwarder(FullTypeName typeName)
		{
			var lookup = LazyInit.VolatileRead(ref typeForwarderLookup);
			if (lookup == null)
			{
				lookup = new Dictionary<FullTypeName, ExportedTypeHandle>();
				foreach (var handle in Metadata.ExportedTypes)
				{
					var td = Metadata.GetExportedType(handle);
					lookup[td.GetFullTypeName(Metadata)] = handle;
				}
				lookup = LazyInit.GetOrSet(ref typeForwarderLookup, lookup);
			}
			if (lookup.TryGetValue(typeName, out var resultHandle))
				return resultHandle;
			else
				return default;
		}

		MethodSemanticsLookup? methodSemanticsLookup;

		internal MethodSemanticsLookup MethodSemanticsLookup {
			get {
				var r = LazyInit.VolatileRead(ref methodSemanticsLookup);
				if (r != null)
					return r;
				else
					return LazyInit.GetOrSet(ref methodSemanticsLookup, new MethodSemanticsLookup(Metadata));
			}
		}

		PropertyAndEventBackingFieldLookup? propertyAndEventBackingFieldLookup;

		internal PropertyAndEventBackingFieldLookup PropertyAndEventBackingFieldLookup {
			get {
				var r = LazyInit.VolatileRead(ref propertyAndEventBackingFieldLookup);
				if (r != null)
					return r;
				else
					return LazyInit.GetOrSet(ref propertyAndEventBackingFieldLookup, new PropertyAndEventBackingFieldLookup(Metadata));
			}
		}

		/// <summary>
		/// Initializes a metadata file from a <see cref="MetadataReaderProvider"/>.
		/// </summary>
		/// <param name="kind">Container format classification for this instance.</param>
		/// <param name="fileName">Display path associated with the metadata.</param>
		/// <param name="metadata">Reader provider used to create the metadata reader.</param>
		/// <param name="metadataOptions">Metadata-reader options used when obtaining <see cref="Metadata"/>.</param>
		/// <param name="metadataOffset">Offset of the metadata root within the original file payload.</param>
		/// <param name="isEmbedded">Indicates whether metadata originated from an embedded payload.</param>
		/// <param name="utf8Decoder">Optional UTF-8 decoder applied to metadata string heaps.</param>
		public MetadataFile(MetadataFileKind kind, string fileName, MetadataReaderProvider metadata, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, int metadataOffset = 0, bool isEmbedded = false, MetadataStringDecoder? utf8Decoder = null)
		{
			this.Kind = kind;
			this.FileName = fileName;
			this.Metadata = metadata.GetMetadataReader(metadataOptions, utf8Decoder);
			this.MetadataOffset = metadataOffset;
			this.IsEmbedded = isEmbedded;
		}

		/// <summary>
		/// Initializes a metadata file from an existing <see cref="MetadataReader"/>.
		/// </summary>
		/// <param name="kind">Container format classification for this instance.</param>
		/// <param name="fileName">Display path associated with the metadata.</param>
		/// <param name="metadataReader">Reader that exposes metadata tables and heaps.</param>
		/// <param name="metadataOffset">Offset of the metadata root within the original file payload.</param>
		/// <param name="isEmbedded">Indicates whether metadata originated from an embedded payload.</param>
		public MetadataFile(MetadataFileKind kind, string fileName, MetadataReader metadataReader, int metadataOffset = 0, bool isEmbedded = false)
		{
			this.Kind = kind;
			this.FileName = fileName;
			this.Metadata = metadataReader;
			this.MetadataOffset = metadataOffset;
			this.IsEmbedded = isEmbedded;
		}

		/// <summary>
		/// Initializes a metadata file backed by a <see cref="PEReader"/>.
		/// </summary>
		/// <param name="kind">Container format classification for this instance.</param>
		/// <param name="fileName">Display path associated with the metadata.</param>
		/// <param name="reader">PE reader that provides access to managed metadata.</param>
		/// <param name="metadataOptions">Metadata-reader options used when obtaining <see cref="Metadata"/>.</param>
		/// <param name="utf8Decoder">Optional UTF-8 decoder applied to metadata string heaps.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileName"/> or <paramref name="reader"/> is <see langword="null"/>.</exception>
		/// <exception cref="MetadataFileNotSupportedException">The supplied PE image does not contain managed metadata.</exception>
		private protected MetadataFile(MetadataFileKind kind, string fileName, PEReader reader, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
		{
			this.Kind = kind;
			this.FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			_ = reader ?? throw new ArgumentNullException(nameof(reader));
			if (!reader.HasMetadata)
				throw new MetadataFileNotSupportedException("PE file does not contain any managed metadata.");
			this.Metadata = reader.GetMetadataReader(metadataOptions, utf8Decoder);
		}

		/// <summary>
		/// Gets the method body located at the supplied relative virtual address (RVA).
		/// </summary>
		/// <param name="rva">RVA of the method body within the underlying image.</param>
		/// <returns>The decoded method body block.</returns>
		/// <exception cref="BadImageFormatException">The current metadata source does not provide method bodies.</exception>
		public virtual MethodBodyBlock GetMethodBody(int rva)
		{
			throw new BadImageFormatException("This metadata file does not contain method bodies.");
		}

		/// <summary>
		/// Gets raw section bytes that contain the supplied RVA.
		/// </summary>
		/// <param name="rva">RVA to locate.</param>
		/// <returns>A <see cref="SectionData"/> view over the containing section.</returns>
		/// <exception cref="BadImageFormatException">The current metadata source does not expose section data.</exception>
		public virtual SectionData GetSectionData(int rva)
		{
			throw new BadImageFormatException("This metadata file does not support sections.");
		}

		/// <summary>
		/// Gets the zero-based index of the section that contains the supplied RVA.
		/// </summary>
		/// <param name="rva">RVA to locate.</param>
		/// <returns>The section index.</returns>
		/// <exception cref="BadImageFormatException">The current metadata source does not expose section metadata.</exception>
		public virtual int GetContainingSectionIndex(int rva)
		{
			throw new BadImageFormatException("This metadata file does not support sections.");
		}

		/// <summary>
		/// Gets section headers when the underlying metadata source is section-based.
		/// </summary>
		/// <exception cref="BadImageFormatException">The current metadata source does not expose section metadata.</exception>
		[SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations",
			Justification = "Throw signals that this MetadataFileKind has no PE sections; derived PE-like kinds override.")]
		public virtual ImmutableArray<SectionHeader> SectionHeaders => throw new BadImageFormatException("This metadata file does not support sections.");

		/// <summary>
		/// Gets the CLI header or null if the image does not have one.
		/// </summary>
		public virtual CorHeader? CorHeader => null;

		/// <summary>
		/// Creates a module reference wrapper that resolves this metadata with the supplied type-system options.
		/// </summary>
		/// <param name="options">Type-system materialization flags to apply when building <see cref="MetadataModule"/>.</param>
		/// <returns>An <see cref="IModuleReference"/> that resolves this metadata with <paramref name="options"/>.</returns>
		public IModuleReference WithOptions(TypeSystemOptions options)
		{
			return new MetadataFileWithOptions(this, options);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		private class MetadataFileWithOptions : IModuleReference
		{
			readonly MetadataFile peFile;
			readonly TypeSystemOptions options;

			public MetadataFileWithOptions(MetadataFile peFile, TypeSystemOptions options)
			{
				this.peFile = peFile;
				this.options = options;
			}

			IModule IModuleReference.Resolve(ITypeResolveContext context)
			{
				return new MetadataModule(context.Compilation, peFile, options);
			}
		}
	}

	/// <summary>
	/// Abstraction over PEMemoryBlock
	/// </summary>
	public readonly unsafe struct SectionData
	{
		/// <summary>
		/// Gets the native pointer to the start of the section payload.
		/// </summary>
		public byte* Pointer { get; }

		/// <summary>
		/// Gets the number of bytes available from <see cref="Pointer"/>.
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Initializes a section-data wrapper from a managed PE memory block.
		/// </summary>
		/// <param name="block">Section block provided by <see cref="PEReader"/>.</param>
		public SectionData(PEMemoryBlock block)
		{
			Pointer = block.Pointer;
			Length = block.Length;
		}

		/// <summary>
		/// Initializes a section-data wrapper from an unmanaged pointer and byte length.
		/// </summary>
		/// <param name="startPointer">Pointer to the beginning of the section payload.</param>
		/// <param name="length">Number of bytes available from <paramref name="startPointer"/>.</param>
		public SectionData(byte* startPointer, int length)
		{
			Pointer = startPointer;
			Length = length;
		}

		/// <summary>
		/// Creates a <see cref="BlobReader"/> spanning the entire section data.
		/// </summary>
		/// <returns>A reader over <see cref="Length"/> bytes starting at <see cref="Pointer"/>.</returns>
		public BlobReader GetReader()
		{
			return new BlobReader(Pointer, Length);
		}

		internal BlobReader GetReader(int offset, int size)
		{
			return new BlobReader(Pointer + offset, size);
		}
	}

	/// <summary>
	/// Represents one section entry in PE-style section tables used by <see cref="MetadataFile"/> implementations.
	/// </summary>
	public struct SectionHeader
	{
		/// <summary>
		/// Gets or sets the section name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Gets or sets the virtual size of the section.
		/// </summary>
		public uint VirtualSize;

		/// <summary>
		/// Gets or sets the section RVA.
		/// </summary>
		public uint VirtualAddress;

		/// <summary>
		/// Gets or sets the raw size of the section payload in the file.
		/// </summary>
		public uint RawDataSize;

		/// <summary>
		/// Gets or sets the file offset of the section payload.
		/// </summary>
		public uint RawDataPtr;
	}
}

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

#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Identifies how a manifest resource is stored and resolved.
	/// </summary>
	public enum ResourceType
	{
		/// <summary>
		/// The resource is stored in a separate file referenced by the module.
		/// </summary>
		Linked,
		/// <summary>
		/// The resource data is embedded directly into the containing module.
		/// </summary>
		Embedded,
		/// <summary>
		/// The resource is forwarded to another assembly via an assembly reference.
		/// </summary>
		AssemblyLinked,
	}

	/// <summary>
	/// Represents a logical manifest resource exposed by a PE file or synthesized in memory.
	/// </summary>
	public abstract class Resource
	{
		/// <summary>
		/// Gets the storage model used to locate the resource payload.
		/// </summary>
		public virtual ResourceType ResourceType => ResourceType.Embedded;
		/// <summary>
		/// Gets the raw manifest visibility and behavior flags for the resource.
		/// </summary>
		public virtual ManifestResourceAttributes Attributes => ManifestResourceAttributes.Public;
		/// <summary>
		/// Gets the resource name as recorded in the manifest.
		/// </summary>
		public abstract string Name { get; }
		/// <summary>
		/// Attempts to open the resource payload for reading.
		/// </summary>
		/// <returns>
		/// A readable stream for the resource contents, or <see langword="null"/> when the payload is unavailable
		/// from the current module representation.
		/// </returns>
		public abstract Stream? TryOpenStream();
		/// <summary>
		/// Attempts to determine the payload length without materializing the full resource content.
		/// </summary>
		/// <returns>
		/// The payload length in bytes, or <see langword="null"/> when the length cannot be determined.
		/// </returns>
		public abstract long? TryGetLength();
	}

	/// <summary>
	/// Represents an in-memory resource backed by an immutable byte buffer.
	/// </summary>
	public class ByteArrayResource : Resource
	{
		/// <inheritdoc />
		public override string Name { get; }
		byte[] data;

		/// <summary>
		/// Creates a resource wrapper over an existing byte array.
		/// </summary>
		/// <param name="name">Manifest name that should be reported for the resource.</param>
		/// <param name="data">Resource payload bytes.</param>
		public ByteArrayResource(string name, byte[] data)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.data = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <inheritdoc />
		public override Stream TryOpenStream()
		{
			return new MemoryStream(data);
		}

		/// <inheritdoc />
		public override long? TryGetLength()
		{
			return data.Length;
		}
	}

	sealed class MetadataResource : Resource
	{
		public MetadataFile Module { get; }
		public ManifestResourceHandle Handle { get; }
		public bool IsNil => Handle.IsNil;

		public MetadataResource(MetadataFile module, ManifestResourceHandle handle)
		{
			this.Module = module ?? throw new ArgumentNullException(nameof(module));
			this.Handle = handle;
		}

		public bool Equals(MetadataResource other)
		{
			return Module == other.Module && Handle == other.Handle;
		}

		public override bool Equals(object? obj)
		{
			if (obj is MetadataResource res)
				return Equals(res);
			return false;
		}

		public override int GetHashCode()
		{
			return unchecked(982451629 * Module.GetHashCode() + 982451653 * MetadataTokens.GetToken(Handle));
		}

		public override string Name => Module.Metadata.GetString(Module.Metadata.GetManifestResource(Handle).Name);

		public override ManifestResourceAttributes Attributes => Module.Metadata.GetManifestResource(Handle).Attributes;
		public bool HasFlag(ManifestResourceAttributes flag) => (Attributes & flag) == flag;
		public override ResourceType ResourceType => GetResourceType();

		ResourceType GetResourceType()
		{
			var resource = Module.Metadata.GetManifestResource(Handle);
			if (resource.Implementation.IsNil)
				return ResourceType.Embedded;
			if (resource.Implementation.Kind == HandleKind.AssemblyReference)
				return ResourceType.AssemblyLinked;
			return ResourceType.Linked;
		}

		unsafe bool TryReadResource(out byte* ptr, out long length)
		{
			ptr = null;
			length = 0;
			// embedded resources cannot be read from this binary.
			if (ResourceType != ResourceType.Embedded)
				return false;
			if (Module.CorHeader == null)
				return false;
			var resources = Module.CorHeader.ResourcesDirectory;
			// validate resources directory, GetSectionData throws on negative RVAs
			if (resources.RelativeVirtualAddress <= 0)
				return false;
			var sectionData = Module.GetSectionData(resources.RelativeVirtualAddress);
			// validate section length: we need at least 4 bytes to extract
			// the actual length of the resource blob.
			if (sectionData.Length < 4)
				return false;
			var offset = Module.Metadata.GetManifestResource(Handle).Offset;
			// validate resource offset
			if (offset < 0 || offset > sectionData.Length - 4)
				return false;
			ptr = sectionData.Pointer + offset;
			// get actual length of resource blob.
			length = ptr[0] + (ptr[1] << 8) + (ptr[2] << 16) + (ptr[3] << 24);
			return length >= 0 && length <= sectionData.Length;
		}

		public override unsafe Stream? TryOpenStream()
		{
			if (!TryReadResource(out var ptr, out var length))
				return null;
			return new ResourceMemoryStream(Module, ptr + sizeof(int), length);
		}

		public unsafe override long? TryGetLength()
		{
			if (!TryReadResource(out _, out var length))
				return null;
			return length;
		}
	}

	sealed unsafe class ResourceMemoryStream : UnmanagedMemoryStream
	{
#pragma warning disable IDE0052 // Remove unread private members
		readonly MetadataFile peReader;
#pragma warning restore IDE0052 // Remove unread private members

		public ResourceMemoryStream(MetadataFile peReader, byte* data, long length)
			: base(data, length, length, FileAccess.Read)
		{
			// Keep the PEReader alive while the stream in in use.
			this.peReader = peReader;
		}
	}
}

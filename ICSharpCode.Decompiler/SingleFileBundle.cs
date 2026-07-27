// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Class for dealing with .NET 5 single-file bundles.
	/// 
	/// Based on code from Microsoft.NET.HostModel.
	/// </summary>
	public static class SingleFileBundle
	{
		/// <summary>
		/// Check if the memory-mapped data is a single-file bundle
		/// </summary>
		/// <param name="view">View accessor over the candidate host file.</param>
		/// <param name="bundleHeaderOffset">
		/// Receives the offset of the bundle manifest header when the signature is found;
		/// otherwise <c>0</c>.
		/// </param>
		/// <returns><c>true</c> if the view contains a valid .NET single-file bundle signature; otherwise <c>false</c>.</returns>
		public static unsafe bool IsBundle(MemoryMappedViewAccessor view, out long bundleHeaderOffset)
		{
			var buffer = view.SafeMemoryMappedViewHandle;
			byte* ptr = null;
			buffer.AcquirePointer(ref ptr);
			try
			{
				return IsBundle(ptr, checked((long)buffer.ByteLength), out bundleHeaderOffset);
			}
			finally
			{
				buffer.ReleasePointer();
			}
		}

		/// <summary>
		/// Checks whether an unmanaged byte range contains a .NET single-file bundle signature.
		/// </summary>
		/// <param name="data">Pointer to the beginning of the candidate data region.</param>
		/// <param name="size">Length, in bytes, of the candidate data region.</param>
		/// <param name="bundleHeaderOffset">
		/// Receives the offset of the bundle manifest header when the signature is found;
		/// otherwise <c>0</c>.
		/// </param>
		/// <returns><c>true</c> if a valid bundle signature and header offset are found; otherwise <c>false</c>.</returns>
		public static unsafe bool IsBundle(byte* data, long size, out long bundleHeaderOffset)
		{
			ReadOnlySpan<byte> bundleSignature = new byte[] {
				// 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
				0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
				0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
				0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
				0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
			};

			byte* end = data + (size - bundleSignature.Length);
			for (byte* ptr = data; ptr < end; ptr++)
			{
				if (*ptr == 0x8b && bundleSignature.SequenceEqual(new ReadOnlySpan<byte>(ptr, bundleSignature.Length)))
				{
					// A genuine bundle stores the 8-byte header offset immediately before the
					// signature, so the signature never appears within the first sizeof(long)
					// bytes of the file. Without this guard, a crafted file with the signature at
					// offset 0..7 reads before the start of the (page-aligned, memory-mapped)
					// buffer, faulting on the preceding unmapped page.
					if (ptr - data >= sizeof(long))
					{
						bundleHeaderOffset = Unsafe.ReadUnaligned<long>(ptr - sizeof(long));
						if (bundleHeaderOffset > 0 && bundleHeaderOffset < size)
						{
							return true;
						}
					}
				}
			}

			bundleHeaderOffset = 0;
			return false;
		}

		/// <summary>
		/// Parsed metadata header for a .NET single-file bundle.
		/// </summary>
		public struct Header
		{
			/// <summary>Bundle manifest major version.</summary>
			public uint MajorVersion;
			/// <summary>Bundle manifest minor version.</summary>
			public uint MinorVersion;
			/// <summary>Number of entries stored in <see cref="Entries"/>.</summary>
			public int FileCount;
			/// <summary>Bundle identifier emitted by the bundler.</summary>
			public string BundleID;

			// Fields introduced with v2:
			/// <summary>Offset of the embedded <c>.deps.json</c> file.</summary>
			public long DepsJsonOffset;
			/// <summary>Size of the embedded <c>.deps.json</c> file.</summary>
			public long DepsJsonSize;
			/// <summary>Offset of the embedded <c>.runtimeconfig.json</c> file.</summary>
			public long RuntimeConfigJsonOffset;
			/// <summary>Size of the embedded <c>.runtimeconfig.json</c> file.</summary>
			public long RuntimeConfigJsonSize;
			/// <summary>Bundle flags introduced by newer manifest versions.</summary>
			public ulong Flags;

			/// <summary>Entries described by the bundle manifest.</summary>
			public ImmutableArray<Entry> Entries;
		}

		/// <summary>
		/// FileType: Identifies the type of file embedded into the bundle.
		///
		/// The bundler differentiates a few kinds of files via the manifest,
		/// with respect to the way in which they'll be used by the runtime.
		/// </summary>
		public enum FileType : byte
		{
			/// <summary>Type not determined.</summary>
			Unknown,           // Type not determined.
			/// <summary>Managed assembly (IL and ReadyToRun).</summary>
			Assembly,          // IL and R2R Assemblies
			/// <summary>Native binary payload.</summary>
			NativeBinary,      // NativeBinaries
			/// <summary><c>.deps.json</c> configuration payload.</summary>
			DepsJson,          // .deps.json configuration file
			/// <summary><c>.runtimeconfig.json</c> configuration payload.</summary>
			RuntimeConfigJson, // .runtimeconfig.json configuration file
			/// <summary>Debug symbols payload (for example PDB files).</summary>
			Symbols            // PDB Files
		};

		/// <summary>
		/// One file entry described in a bundle manifest.
		/// </summary>
		public struct Entry
		{
			/// <summary>Offset of the payload within the bundle file.</summary>
			public long Offset;
			/// <summary>Uncompressed payload size, in bytes.</summary>
			public long Size;
			/// <summary>Compressed payload size in the bundle, or <c>0</c> when the payload is stored uncompressed.</summary>
			public long CompressedSize; // 0 if not compressed, otherwise the compressed size in the bundle
			/// <summary>Type classification used by the host runtime.</summary>
			public FileType Type;
			/// <summary>Path of the embedded file, relative to the bundle source directory.</summary>
			public string RelativePath; // Path of an embedded file, relative to the Bundle source-directory.
		}

		/// <summary>
		/// Reads the manifest header from the memory mapping.
		/// </summary>
		/// <param name="view">View accessor over the bundle file.</param>
		/// <param name="bundleHeaderOffset">Absolute byte offset where the bundle header starts.</param>
		/// <returns>The parsed bundle manifest header.</returns>
		public static Header ReadManifest(MemoryMappedViewAccessor view, long bundleHeaderOffset)
		{
			using var stream = view.AsStream();
			stream.Seek(bundleHeaderOffset, SeekOrigin.Begin);
			return ReadManifest(stream);
		}

		/// <summary>
		/// Reads the manifest header from the stream.
		/// </summary>
		/// <param name="stream">Stream positioned at the beginning of a bundle manifest header.</param>
		/// <returns>The parsed bundle manifest header.</returns>
		/// <exception cref="InvalidDataException">Thrown when the manifest version is outside the supported range.</exception>
		public static Header ReadManifest(Stream stream)
		{
			var header = new Header();
			using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
			header.MajorVersion = reader.ReadUInt32();
			header.MinorVersion = reader.ReadUInt32();

			// Major versions 3, 4 and 5 were skipped to align bundle versioning with .NET versioning scheme
			if (header.MajorVersion < 1 || header.MajorVersion > 6)
			{
				throw new InvalidDataException($"Unsupported manifest version: {header.MajorVersion}.{header.MinorVersion}");
			}
			header.FileCount = reader.ReadInt32();
			// FileCount is used below to pre-size the entry array. Each entry occupies at least
			// one byte in the stream, so a count larger than the bytes that remain cannot be
			// honest; reject it instead of attempting a huge allocation for a crafted manifest.
			long remainingBytes = stream.Length - stream.Position;
			if (header.FileCount < 0 || header.FileCount > remainingBytes)
			{
				throw new InvalidDataException($"Invalid bundle manifest: FileCount {header.FileCount} exceeds available data.");
			}
			header.BundleID = reader.ReadString();
			if (header.MajorVersion >= 2)
			{
				header.DepsJsonOffset = reader.ReadInt64();
				header.DepsJsonSize = reader.ReadInt64();
				header.RuntimeConfigJsonOffset = reader.ReadInt64();
				header.RuntimeConfigJsonSize = reader.ReadInt64();
				header.Flags = reader.ReadUInt64();
			}
			var entries = ImmutableArray.CreateBuilder<Entry>(header.FileCount);
			for (int i = 0; i < header.FileCount; i++)
			{
				entries.Add(ReadEntry(reader, header.MajorVersion));
			}
			header.Entries = entries.MoveToImmutable();
			return header;
		}

		private static Entry ReadEntry(BinaryReader reader, uint bundleMajorVersion)
		{
			Entry entry;
			entry.Offset = reader.ReadInt64();
			entry.Size = reader.ReadInt64();
			entry.CompressedSize = bundleMajorVersion >= 6 ? reader.ReadInt64() : 0;
			entry.Type = (FileType)reader.ReadByte();
			entry.RelativePath = reader.ReadString();
			return entry;
		}
	}
}

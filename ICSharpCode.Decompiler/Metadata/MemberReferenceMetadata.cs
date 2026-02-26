// Copyright (c) 2023 James May
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
using System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.Metadata
{
#if !VSADDIN
	/// <summary>
	/// Convenience wrapper for <see cref="MemberReference"/> and <see cref="MemberReferenceHandle"/>.
	/// </summary>
	public sealed class MemberReferenceMetadata
	{
		readonly MemberReference entry;

		/// <summary>
		/// Gets the metadata reader that owns this member reference row.
		/// </summary>
		public MetadataReader Metadata { get; }

		/// <summary>
		/// Gets the handle that identifies the underlying <see cref="MemberReference"/> row.
		/// </summary>
		public MemberReferenceHandle Handle { get; }

		string? name;

		/// <summary>
		/// Gets the simple member name stored in the member reference row.
		/// </summary>
		/// <remarks>
		/// If metadata is malformed, a stable fallback string containing the row handle is returned.
		/// </remarks>
		public string Name {
			get {
				try
				{
					return name ??= Metadata.GetString(entry.Name);
				}
				catch (BadImageFormatException)
				{
					return name = $"MR:{Handle}";
				}
			}
		}

		/// <summary>
		/// Gets the metadata handle for the declaring type or module referenced by this member.
		/// </summary>
		public EntityHandle Parent => entry.Parent;

		/// <summary>
		/// Gets whether this reference points to a method or a field.
		/// </summary>
		public MemberReferenceKind MemberReferenceKind => entry.GetKind();

		/// <summary>
		/// Creates a metadata projection for a member reference row.
		/// </summary>
		/// <param name="metadata">Metadata reader containing the member reference table.</param>
		/// <param name="handle">Handle identifying the member reference row to project.</param>
		/// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public MemberReferenceMetadata(MetadataReader metadata, MemberReferenceHandle handle)
		{
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Handle = handle;
			entry = metadata.GetMemberReference(handle);
		}

		/// <summary>
		/// Returns the member name for debugger and tree display scenarios.
		/// </summary>
		public override string ToString()
			=> Name;
	}
#endif
}

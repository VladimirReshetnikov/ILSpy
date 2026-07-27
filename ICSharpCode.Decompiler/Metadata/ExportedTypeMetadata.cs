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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.Metadata
{
#if !VSADDIN
	/// <summary>
	/// Convenience wrapper for <see cref="ExportedType"/> and <see cref="ExportedTypeHandle"/>.
	/// </summary>
	public sealed class ExportedTypeMetadata
	{
		readonly ExportedType entry;

		/// <summary>
		/// Gets the metadata reader that owns this exported type row.
		/// </summary>
		public MetadataReader Metadata { get; }

		/// <summary>
		/// Gets the handle that identifies the underlying <see cref="ExportedType"/> row.
		/// </summary>
		public ExportedTypeHandle Handle { get; }

		string? name;
		/// <summary>
		/// Gets the simple type name stored in the exported type row.
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
					return name = $"ET:{Handle}";
				}
			}
		}

		string? @namespace;
		/// <summary>
		/// Gets the namespace stored in the exported type row.
		/// </summary>
		/// <remarks>
		/// If metadata is malformed, a stable fallback string containing the row handle is returned.
		/// </remarks>
		public string Namespace {
			get {
				try
				{
					return @namespace ??= Metadata.GetString(entry.Namespace);
				}
				catch (BadImageFormatException)
				{
					return @namespace = $"namespace(ET:{Handle})";
				}
			}
		}

		/// <summary>
		/// Gets the implementation target for the exported type (file, assembly reference, or enclosing exported type).
		/// </summary>
		public EntityHandle Implementation => entry.Implementation;
		/// <summary>
		/// Gets raw <see cref="TypeAttributes"/> flags for the exported type.
		/// </summary>
		public TypeAttributes Attributes => entry.Attributes;
		/// <summary>
		/// Gets whether this row represents a forwarded type rather than a type defined in this assembly.
		/// </summary>
		public bool IsForwarder => entry.IsForwarder;
		/// <summary>
		/// Gets the namespace definition referenced by this exported type row.
		/// </summary>
		public NamespaceDefinition NamespaceDefinition => Metadata.GetNamespaceDefinition(entry.NamespaceDefinition);

		ImmutableArray<ExportedTypeMetadata> exportedTypes;
		/// <summary>
		/// Gets exported types implemented by this exported type (nested exported types), ordered by namespace and name.
		/// </summary>
		public ImmutableArray<ExportedTypeMetadata> ExportedTypes {
			get {
				var value = exportedTypes;
				if (value.IsDefault)
				{
					value = Metadata.ExportedTypes
						.Select(r => new ExportedTypeMetadata(Metadata, r))
						.Where(r => r.Implementation == Handle)
						.OrderBy(r => r.Namespace)
						.ThenBy(r => r.Name)
						.ToImmutableArray();
					exportedTypes = value;
				}
				return value;
			}
		}

		/// <summary>
		/// Creates a metadata projection for an exported type row.
		/// </summary>
		/// <param name="metadata">Metadata reader containing the exported type table.</param>
		/// <param name="handle">Handle identifying the exported type row to project.</param>
		/// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public ExportedTypeMetadata(MetadataReader metadata, ExportedTypeHandle handle)
		{
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Handle = handle;
			entry = metadata.GetExportedType(handle);
		}

		/// <summary>
		/// Returns a display-friendly qualified name in <c>Namespace::Name</c> form.
		/// </summary>
		public override string ToString() => $"{Namespace}::{Name}";
	}
#endif
}

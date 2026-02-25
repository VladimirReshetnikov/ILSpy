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
	/// Provides cached access to a <see cref="ModuleReference"/> row and related metadata projected from the same module.
	/// </summary>
	public class ModuleReferenceMetadata /* : IModuleReference*/
	{
		readonly ModuleReference entry;

		/// <summary>
		/// Gets the metadata reader that owns <see cref="Handle"/>.
		/// </summary>
		public MetadataReader Metadata { get; }
		/// <summary>
		/// Gets the metadata handle for the module reference row.
		/// </summary>
		public ModuleReferenceHandle Handle { get; }

		string? name;
		/// <summary>
		/// Gets the referenced module name.
		/// </summary>
		/// <remarks>
		/// Invalid metadata names are converted into a stable fallback value containing the metadata token.
		/// </remarks>
		public string Name {
			get {
				if (name == null)
				{
					try
					{
						name = Metadata.GetString(entry.Name);
					}
					catch (BadImageFormatException)
					{
						name = $"AR:{Handle}";
					}
				}
				return name;
			}
		}

		ImmutableArray<CustomAttribute> attributes;
		/// <summary>
		/// Gets custom attributes declared on this module reference row.
		/// </summary>
		public ImmutableArray<CustomAttribute> Attributes {
			get {
				var value = attributes;
				if (value.IsDefault)
				{
					value = entry.GetCustomAttributes().Select(Metadata.GetCustomAttribute).ToImmutableArray();
					attributes = value;
				}
				return value;
			}
		}

		ImmutableArray<TypeReferenceMetadata> typeReferences;
		/// <summary>
		/// Gets type references that resolve through this module reference.
		/// </summary>
		public ImmutableArray<TypeReferenceMetadata> TypeReferences {
			get {
				var value = typeReferences;
				if (value.IsDefault)
				{
					value = Metadata.TypeReferences
						.Select(r => new TypeReferenceMetadata(Metadata, r))
						.Where(r => r.ResolutionScope == Handle)
						.OrderBy(r => r.Namespace)
						.ThenBy(r => r.Name)
						.ToImmutableArray();
					typeReferences = value;
				}
				return value;
			}
		}

		ImmutableArray<ExportedTypeMetadata> exportedTypes;
		/// <summary>
		/// Gets exported type entries whose implementation points at this module reference.
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
		/// Creates a metadata projection for a module reference row.
		/// </summary>
		/// <param name="metadata">Metadata reader containing the module reference table.</param>
		/// <param name="handle">Handle identifying the module reference row to project.</param>
		/// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public ModuleReferenceMetadata(MetadataReader metadata, ModuleReferenceHandle handle)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = metadata;
			Handle = handle;
			entry = metadata.GetModuleReference(handle);
		}

		/// <summary>
		/// Creates a metadata projection for a module reference row from a PE file.
		/// </summary>
		/// <param name="module">PE file that owns the metadata tables.</param>
		/// <param name="handle">Handle identifying the module reference row to project.</param>
		/// <exception cref="ArgumentNullException"><paramref name="module"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public ModuleReferenceMetadata(PEFile module, ModuleReferenceHandle handle)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = module.Metadata;
			Handle = handle;
			entry = Metadata.GetModuleReference(handle);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Name;
		}
	}
#endif
}

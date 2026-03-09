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
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Decodes metadata signatures into <see cref="FullTypeName"/> values for name-oriented analysis paths.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This decoder intentionally keeps only naming information. Shape details that are irrelevant for current callers
	/// (for example array rank, pointer/by-ref wrappers, and generic arguments) are collapsed to the underlying type name.
	/// The result is therefore suitable for pattern checks and synthesized-name filtering, but not for full semantic
	/// reconstruction of a signature.
	/// </para>
	/// <para>
	/// The implementation is used by metadata helper extensions that classify compiler-generated fields and similar
	/// artifacts based on canonical type-name prefixes.
	/// </para>
	/// </remarks>
	public sealed class FullTypeNameSignatureDecoder : ISignatureTypeProvider<FullTypeName, Unit>, ICustomAttributeTypeProvider<FullTypeName>
	{
		readonly MetadataReader metadata;

		/// <summary>
		/// Initializes a new decoder bound to a metadata reader.
		/// </summary>
		/// <param name="metadata">Metadata reader used when recursively decoding type specifications.</param>
		public FullTypeNameSignatureDecoder(MetadataReader metadata)
		{
			this.metadata = metadata;
		}

		/// <inheritdoc />
		public FullTypeName GetArrayType(FullTypeName elementType, ArrayShape shape)
		{
			return elementType;
		}

		/// <inheritdoc />
		public FullTypeName GetByReferenceType(FullTypeName elementType)
		{
			return elementType;
		}

		/// <inheritdoc />
		/// <remarks>
		/// Function pointer signatures are not representable as <see cref="FullTypeName"/> in this decoder and therefore
		/// return the default value.
		/// </remarks>
		public FullTypeName GetFunctionPointerType(MethodSignature<FullTypeName> signature)
		{
			return default;
		}

		/// <inheritdoc />
		public FullTypeName GetGenericInstantiation(FullTypeName genericType, ImmutableArray<FullTypeName> typeArguments)
		{
			return genericType;
		}

		/// <inheritdoc />
		/// <remarks>
		/// Generic parameters do not have a stable namespace-qualified type identity, so this decoder returns default.
		/// </remarks>
		public FullTypeName GetGenericMethodParameter(Unit genericContext, int index)
		{
			return default;
		}

		/// <inheritdoc />
		/// <remarks>
		/// Generic parameters do not have a stable namespace-qualified type identity, so this decoder returns default.
		/// </remarks>
		public FullTypeName GetGenericTypeParameter(Unit genericContext, int index)
		{
			return default;
		}

		/// <inheritdoc />
		public FullTypeName GetModifiedType(FullTypeName modifier, FullTypeName unmodifiedType, bool isRequired)
		{
			return unmodifiedType;
		}

		/// <inheritdoc />
		public FullTypeName GetPinnedType(FullTypeName elementType)
		{
			return elementType;
		}

		/// <inheritdoc />
		public FullTypeName GetPointerType(FullTypeName elementType)
		{
			return elementType;
		}

		/// <inheritdoc />
		public FullTypeName GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			var ktr = KnownTypeReference.Get(typeCode.ToKnownTypeCode());
			if (ktr == null)
				return default;
			return new TopLevelTypeName(ktr.Namespace, ktr.Name, ktr.TypeParameterCount);
		}

		/// <inheritdoc />
		public FullTypeName GetSystemType()
		{
			return new TopLevelTypeName("System", "Type");
		}

		/// <inheritdoc />
		public FullTypeName GetSZArrayType(FullTypeName elementType)
		{
			return elementType;
		}

		/// <inheritdoc />
		public FullTypeName GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			return handle.GetFullTypeName(reader);
		}

		/// <inheritdoc />
		public FullTypeName GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			return handle.GetFullTypeName(reader);
		}

		/// <inheritdoc />
		public FullTypeName GetTypeFromSerializedName(string name)
		{
			return new FullTypeName(name);
		}

		/// <inheritdoc />
		public FullTypeName GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
		{
			return reader.GetTypeSpecification(handle).DecodeSignature(new FullTypeNameSignatureDecoder(metadata), default);
		}

		/// <inheritdoc />
		/// <exception cref="NotImplementedException">This decoder does not currently map enum underlying types.</exception>
		public PrimitiveTypeCode GetUnderlyingEnumType(FullTypeName type)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool IsSystemType(FullTypeName type)
		{
			return type.IsKnownType(KnownTypeCode.Type);
		}
	}
}

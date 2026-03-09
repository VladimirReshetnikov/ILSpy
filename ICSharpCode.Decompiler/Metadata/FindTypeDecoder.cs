// Copyright (c) 2022 Siegfried Pammer
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
	/// Signature decoder that determines whether a decoded signature references a specific target type.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The decoder propagates a boolean through the signature tree. Leaf callbacks return whether the current token
	/// identifies the configured target type, while composite callbacks aggregate child results.
	/// </para>
	/// <para>
	/// Depending on the constructor used, identity checks are either direct metadata-handle comparisons (same-module
	/// decoding) or reference-resolution checks through <see cref="MetadataModule.ResolveType"/>.
	/// </para>
	/// </remarks>
	public class FindTypeDecoder : ISignatureTypeProvider<bool, Unit>
	{
		readonly MetadataFile declaringModule;
		readonly MetadataModule? currentModule;
		readonly TypeDefinitionHandle handle;
		readonly string? typeName;
		readonly string? namespaceName;
		readonly PrimitiveTypeCode primitiveType;

		/// <summary>
		/// Creates a decoder that matches a type definition handle in the same metadata module.
		/// </summary>
		/// <param name="handle">Type definition to search for.</param>
		/// <param name="declaringModule">Metadata module that owns <paramref name="handle"/>.</param>
		internal FindTypeDecoder(TypeDefinitionHandle handle, MetadataFile declaringModule)
		{
			this.handle = handle;
			this.declaringModule = declaringModule;
			this.primitiveType = 0;
			this.currentModule = null;
		}

		/// <summary>
		/// Creates a decoder that resolves type references from <paramref name="currentModule"/> to <paramref name="type"/>.
		/// </summary>
		/// <param name="currentModule">Module whose signatures will be decoded.</param>
		/// <param name="type">Target type to detect in decoded signatures.</param>
		/// <exception cref="InvalidOperationException"><paramref name="type"/> is not metadata-backed.</exception>
		public FindTypeDecoder(MetadataModule currentModule, ITypeDefinition type)
		{
			this.currentModule = currentModule;
			this.declaringModule = type.ParentModule?.MetadataFile ?? throw new InvalidOperationException("Cannot use MetadataModule without PEFile as context.");
			this.handle = (TypeDefinitionHandle)type.MetadataToken;
			this.primitiveType = type.KnownTypeCode == KnownTypeCode.None ? 0 : type.KnownTypeCode.ToPrimitiveTypeCode();
			this.typeName = type.MetadataName;
			this.namespaceName = type.Namespace;
		}

		/// <inheritdoc />
		public bool GetArrayType(bool elementType, ArrayShape shape) => elementType;
		/// <inheritdoc />
		public bool GetByReferenceType(bool elementType) => elementType;
		/// <inheritdoc />
		public bool GetFunctionPointerType(MethodSignature<bool> signature)
		{
			return AnyInMethodSignature(signature);
		}

		/// <summary>
		/// Checks whether a decoded method signature contains any match.
		/// </summary>
		/// <param name="signature">Method signature whose boolean-encoded types should be aggregated.</param>
		/// <returns>
		/// <see langword="true"/> when the return type or any parameter type matched; otherwise <see langword="false"/>.
		/// </returns>
		public static bool AnyInMethodSignature(MethodSignature<bool> signature)
		{
			if (signature.ReturnType)
				return true;
			foreach (bool type in signature.ParameterTypes)
			{
				if (type)
					return true;
			}
			return false;
		}

		/// <inheritdoc />
		public bool GetGenericInstantiation(bool genericType, ImmutableArray<bool> typeArguments)
		{
			if (genericType)
				return true;
			foreach (bool ta in typeArguments)
			{
				if (ta)
					return true;
			}
			return false;
		}

		/// <inheritdoc />
		public bool GetGenericMethodParameter(Unit genericContext, int index) => false;
		/// <inheritdoc />
		public bool GetGenericTypeParameter(Unit genericContext, int index) => false;
		/// <inheritdoc />
		public bool GetModifiedType(bool modifier, bool unmodifiedType, bool isRequired) => unmodifiedType || modifier;
		/// <inheritdoc />
		public bool GetPinnedType(bool elementType) => elementType;
		/// <inheritdoc />
		public bool GetPointerType(bool elementType) => elementType;

		/// <inheritdoc />
		public bool GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			return typeCode == primitiveType;
		}

		/// <inheritdoc />
		public bool GetSZArrayType(bool elementType) => elementType;

		/// <inheritdoc />
		/// <remarks>
		/// Metadata-reader identity is part of the comparison so equal row numbers from different modules do not match.
		/// </remarks>
		public bool GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			return this.handle == handle && reader == declaringModule.Metadata;
		}

		/// <inheritdoc />
		/// <remarks>
		/// A fast textual prefilter is applied before resolving the reference to avoid unnecessary resolution work.
		/// </remarks>
		public bool GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			if (currentModule == null || typeName == null || namespaceName == null)
				return false;

			var tr = reader.GetTypeReference(handle);
			if (!reader.StringComparer.Equals(tr.Name, typeName))
				return false;
			if (!((tr.Namespace.IsNil && namespaceName.Length == 0) || reader.StringComparer.Equals(tr.Namespace, namespaceName)))
				return false;

			var t = currentModule.ResolveType(handle, default);
			var td = t.GetDefinition();
			if (td == null)
				return false;

			return td.MetadataToken == this.handle && td.ParentModule?.MetadataFile == declaringModule;
		}

		/// <inheritdoc />
		public bool GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
		{
			return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
		}

		/// <summary>
		/// Resolves an arbitrary metadata entity handle as a potential type usage.
		/// </summary>
		/// <param name="reader">Metadata reader that owns <paramref name="handle"/>.</param>
		/// <param name="handle">Entity handle to inspect.</param>
		/// <param name="genericContext">Generic context for type-specification decoding.</param>
		/// <param name="rawTypeKind">Raw type kind propagated by metadata decoding APIs.</param>
		/// <returns>
		/// <see langword="true"/> when the entity can be interpreted as a type reference that resolves to the target type;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool GetTypeFromEntity(MetadataReader reader, EntityHandle handle, Unit genericContext = default, byte rawTypeKind = 0)
		{
			switch (handle.Kind)
			{
				case HandleKind.TypeReference:
					return GetTypeFromReference(reader, (TypeReferenceHandle)handle, rawTypeKind);
				case HandleKind.TypeDefinition:
					return GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, rawTypeKind);
				case HandleKind.TypeSpecification:
					return GetTypeFromSpecification(reader, genericContext, (TypeSpecificationHandle)handle, rawTypeKind);
				default:
					return false;
			}
		}
	}
}

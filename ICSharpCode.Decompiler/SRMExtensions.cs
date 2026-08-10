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

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler
{
	public static partial class SRMExtensions
	{
		/// <summary>
		/// Tests whether all bits from <paramref name="attribute"/> are set on a type definition.
		/// </summary>
		/// <param name="typeDefinition">The type definition whose attribute flags are examined.</param>
		/// <param name="attribute">The flag or flag combination to test.</param>
		/// <returns><see langword="true"/> when all requested bits are present; otherwise <see langword="false"/>.</returns>
		public static bool HasFlag(this TypeDefinition typeDefinition, TypeAttributes attribute)
			=> (typeDefinition.Attributes & attribute) == attribute;

		/// <summary>
		/// Tests whether all bits from <paramref name="attribute"/> are set on a method definition.
		/// </summary>
		/// <param name="methodDefinition">The method definition whose attribute flags are examined.</param>
		/// <param name="attribute">The flag or flag combination to test.</param>
		/// <returns><see langword="true"/> when all requested bits are present; otherwise <see langword="false"/>.</returns>
		public static bool HasFlag(this MethodDefinition methodDefinition, MethodAttributes attribute)
			=> (methodDefinition.Attributes & attribute) == attribute;

		/// <summary>
		/// Tests whether all bits from <paramref name="attribute"/> are set on a field definition.
		/// </summary>
		/// <param name="fieldDefinition">The field definition whose attribute flags are examined.</param>
		/// <param name="attribute">The flag or flag combination to test.</param>
		/// <returns><see langword="true"/> when all requested bits are present; otherwise <see langword="false"/>.</returns>
		public static bool HasFlag(this FieldDefinition fieldDefinition, FieldAttributes attribute)
			=> (fieldDefinition.Attributes & attribute) == attribute;

		/// <summary>
		/// Tests whether all bits from <paramref name="attribute"/> are set on a property definition.
		/// </summary>
		/// <param name="propertyDefinition">The property definition whose attribute flags are examined.</param>
		/// <param name="attribute">The flag or flag combination to test.</param>
		/// <returns><see langword="true"/> when all requested bits are present; otherwise <see langword="false"/>.</returns>
		public static bool HasFlag(this PropertyDefinition propertyDefinition, PropertyAttributes attribute)
			=> (propertyDefinition.Attributes & attribute) == attribute;

		/// <summary>
		/// Tests whether all bits from <paramref name="attribute"/> are set on an event definition.
		/// </summary>
		/// <param name="eventDefinition">The event definition whose attribute flags are examined.</param>
		/// <param name="attribute">The flag or flag combination to test.</param>
		/// <returns><see langword="true"/> when all requested bits are present; otherwise <see langword="false"/>.</returns>
		public static bool HasFlag(this EventDefinition eventDefinition, EventAttributes attribute)
			=> (eventDefinition.Attributes & attribute) == attribute;

		/// <summary>
		/// Determines whether a metadata handle kind denotes a type entity.
		/// </summary>
		/// <param name="kind">The handle kind to classify.</param>
		/// <returns><see langword="true"/> for type-definition, type-reference, or type-specification kinds.</returns>
		public static bool IsTypeKind(this HandleKind kind) =>
			kind == HandleKind.TypeDefinition || kind == HandleKind.TypeReference
			|| kind == HandleKind.TypeSpecification;
		/// <summary>
		/// Determines whether a metadata handle kind denotes a member entity.
		/// </summary>
		/// <param name="kind">The handle kind to classify.</param>
		/// <returns><see langword="true"/> for method, field, property, event, member-reference, or method-spec kinds.</returns>
		public static bool IsMemberKind(this HandleKind kind) =>
			kind == HandleKind.MethodDefinition || kind == HandleKind.PropertyDefinition
			|| kind == HandleKind.FieldDefinition || kind == HandleKind.EventDefinition
			|| kind == HandleKind.MemberReference || kind == HandleKind.MethodSpecification;
		/// <summary>
		/// Determines whether a handle can be treated as an entity handle.
		/// </summary>
		/// <remarks>
		/// The check mirrors SRM token ranges and also treats the nil handle as an entity handle,
		/// which simplifies callers that naturally propagate nil values.
		/// </remarks>
		/// <param name="handle">The metadata handle to classify.</param>
		/// <returns><see langword="true"/> when <paramref name="handle"/> can be interpreted as an entity handle.</returns>
		public static bool IsEntityHandle(this Handle handle) =>
			handle.IsNil || (byte)handle.Kind < 112;

		/// <summary>
		/// Determines whether the resolved type definition is a value type (excluding <see cref="KnownTypeCode.Enum"/> itself).
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the resolved type is a value type.</returns>
		public static bool IsValueType(this TypeDefinitionHandle handle, MetadataReader reader)
		{
			return reader.GetTypeDefinition(handle).IsValueType(reader);
		}

		/// <summary>
		/// Determines whether a type definition represents a value type.
		/// </summary>
		/// <remarks>
		/// The method returns <see langword="true"/> for enums because enums derive from <c>System.Enum</c>,
		/// and returns <see langword="false"/> for <c>System.Enum</c> itself.
		/// </remarks>
		/// <param name="typeDefinition">The type definition to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="typeDefinition"/>.</param>
		/// <returns><see langword="true"/> when <paramref name="typeDefinition"/> represents a value type.</returns>
		public static bool IsValueType(this TypeDefinition typeDefinition, MetadataReader reader)
		{
			EntityHandle baseType = typeDefinition.GetBaseTypeOrNil();
			if (baseType.IsNil)
				return false;
			if (baseType.IsKnownType(reader, KnownTypeCode.Enum))
				return true;
			if (!baseType.IsKnownType(reader, KnownTypeCode.ValueType))
				return false;
			var thisType = typeDefinition.GetFullTypeName(reader);
			return !thisType.IsKnownType(KnownTypeCode.Enum);
		}

		/// <summary>
		/// Determines whether the resolved type definition directly derives from <c>System.Enum</c>.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the type's direct base type is <c>System.Enum</c>.</returns>
		public static bool IsEnum(this TypeDefinitionHandle handle, MetadataReader reader)
		{
			return reader.GetTypeDefinition(handle).IsEnum(reader);
		}

		/// <summary>
		/// Determines whether a type definition directly derives from <c>System.Enum</c>.
		/// </summary>
		/// <param name="typeDefinition">The type definition to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="typeDefinition"/>.</param>
		/// <returns><see langword="true"/> when the type's direct base type is <c>System.Enum</c>.</returns>
		public static bool IsEnum(this TypeDefinition typeDefinition, MetadataReader reader)
		{
			EntityHandle baseType = typeDefinition.GetBaseTypeOrNil();
			if (baseType.IsNil)
				return false;
			return baseType.IsKnownType(reader, KnownTypeCode.Enum);
		}

		/// <summary>
		/// Determines whether the resolved type definition is an enum and returns its underlying primitive type.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <param name="underlyingType">On success, receives the enum's underlying primitive type code.</param>
		/// <returns>
		/// <see langword="true"/> when the type is an enum and the underlying field could be decoded; otherwise <see langword="false"/>.
		/// </returns>
		public static bool IsEnum(this TypeDefinitionHandle handle, MetadataReader reader,
			out PrimitiveTypeCode underlyingType)
		{
			return reader.GetTypeDefinition(handle).IsEnum(reader, out underlyingType);
		}

		/// <summary>
		/// Determines whether a type definition is an enum and extracts its underlying primitive type.
		/// </summary>
		/// <param name="typeDefinition">The type definition to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="typeDefinition"/>.</param>
		/// <param name="underlyingType">On success, receives the enum's non-static backing field type code.</param>
		/// <returns>
		/// <see langword="true"/> when the type derives from <c>System.Enum</c> and a valid backing field was found; otherwise <see langword="false"/>.
		/// </returns>
		public static bool IsEnum(this TypeDefinition typeDefinition, MetadataReader reader,
			out PrimitiveTypeCode underlyingType)
		{
			underlyingType = 0;
			EntityHandle baseType = typeDefinition.GetBaseTypeOrNil();
			if (baseType.IsNil)
				return false;
			if (!baseType.IsKnownType(reader, KnownTypeCode.Enum))
				return false;
			foreach (var handle in typeDefinition.GetFields())
			{
				var field = reader.GetFieldDefinition(handle);
				if ((field.Attributes & FieldAttributes.Static) != 0)
					continue;
				var blob = reader.GetBlobReader(field.Signature);
				if (blob.ReadSignatureHeader().Kind != SignatureKind.Field)
					return false;
				underlyingType = (PrimitiveTypeCode)blob.ReadByte();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Determines whether the resolved type definition derives from <c>System.MulticastDelegate</c>.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the type derives from <c>System.MulticastDelegate</c>.</returns>
		public static bool IsDelegate(this TypeDefinitionHandle handle, MetadataReader reader)
		{
			return reader.GetTypeDefinition(handle).IsDelegate(reader);
		}

		/// <summary>
		/// Determines whether a type definition derives from <c>System.MulticastDelegate</c>.
		/// </summary>
		/// <param name="typeDefinition">The type definition to inspect.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="typeDefinition"/>.</param>
		/// <returns><see langword="true"/> when the type derives from <c>System.MulticastDelegate</c>.</returns>
		public static bool IsDelegate(this TypeDefinition typeDefinition, MetadataReader reader)
		{
			var baseType = typeDefinition.GetBaseTypeOrNil();
			return !baseType.IsNil && baseType.IsKnownType(reader, KnownTypeCode.MulticastDelegate);
		}

		/// <summary>
		/// Determines whether a method definition is expected to have an IL body.
		/// </summary>
		/// <remarks>
		/// Abstract, P/Invoke, runtime/native/internal-call and RVA-less methods are treated as body-less.
		/// </remarks>
		/// <param name="methodDefinition">The method definition to inspect.</param>
		/// <returns><see langword="true"/> when the method should have decompilable IL.</returns>
		public static bool HasBody(this MethodDefinition methodDefinition)
		{
			const MethodAttributes noBodyAttrs = MethodAttributes.Abstract | MethodAttributes.PinvokeImpl;
			const MethodImplAttributes noBodyImplAttrs = MethodImplAttributes.InternalCall
				| MethodImplAttributes.Native | MethodImplAttributes.Unmanaged | MethodImplAttributes.Runtime;
			return (methodDefinition.Attributes & noBodyAttrs) == 0 &&
				(methodDefinition.ImplAttributes & noBodyImplAttrs) == 0 &&
				methodDefinition.RelativeVirtualAddress > 0;
		}

		/// <summary>
		/// Gets the length of the method body IL stream in bytes.
		/// </summary>
		/// <param name="body">The decoded method body block.</param>
		/// <returns>The byte length of the IL instruction stream.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
		public static int GetCodeSize(this MethodBodyBlock body)
		{
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			return body.GetILReader().Length;
		}

		/// <summary>
		/// Returns the first non-nil accessor from a property accessor set.
		/// </summary>
		/// <param name="accessors">The property accessor set to inspect.</param>
		/// <returns>The getter when present; otherwise the setter (which may still be nil).</returns>
		public static MethodDefinitionHandle GetAny(this PropertyAccessors accessors)
		{
			if (!accessors.Getter.IsNil)
				return accessors.Getter;
			return accessors.Setter;
		}

		/// <summary>
		/// Returns the first non-nil accessor from an event accessor set.
		/// </summary>
		/// <param name="accessors">The event accessor set to inspect.</param>
		/// <returns>The adder, remover, or raiser handle in that order.</returns>
		public static MethodDefinitionHandle GetAny(this EventAccessors accessors)
		{
			if (!accessors.Adder.IsNil)
				return accessors.Adder;
			if (!accessors.Remover.IsNil)
				return accessors.Remover;
			return accessors.Raiser;
		}

		/// <summary>
		/// Extracts the generic type handle from a type specification that encodes a generic instantiation.
		/// </summary>
		/// <remarks>
		/// The method performs a minimal blob scan and does not validate or decode generic arguments.
		/// It returns a nil handle when the signature shape does not match a generic instantiation.
		/// </remarks>
		/// <param name="ts">The type specification to inspect.</param>
		/// <param name="metadata">The metadata reader used to read signature data.</param>
		/// <returns>The generic type handle, or nil when the signature is not a generic instantiation.</returns>
		public static EntityHandle GetGenericType(this in TypeSpecification ts, MetadataReader metadata)
		{
			if (ts.Signature.IsNil)
				return default;
			// Do a quick scan using BlobReader
			var signature = metadata.GetBlobReader(ts.Signature);
			// When dealing with FSM implementations, we can safely assume that if it's a type spec,
			// it must be a generic type instance.
			if (signature.ReadByte() != (byte)SignatureTypeCode.GenericTypeInstance)
				return default;
			// Skip over the rawTypeKind: value type or class
			var rawTypeKind = signature.ReadCompressedInteger();
			if (rawTypeKind < 17 || rawTypeKind > 18)
				return default;
			// Only read the generic type, ignore the type arguments
			return signature.ReadTypeHandle();
		}

		/// <summary>
		/// Resolves the declaring type for a metadata entity handle.
		/// </summary>
		/// <param name="entity">The metadata entity whose declaring type is requested.</param>
		/// <param name="metadata">The metadata reader used to resolve referenced rows.</param>
		/// <returns>The declaring type handle, or nil when no declaring type exists.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="entity"/> is of a handle kind that is not supported by this helper.
		/// </exception>
		public static EntityHandle GetDeclaringType(this EntityHandle entity, MetadataReader metadata)
		{
			switch (entity.Kind)
			{
				case HandleKind.TypeDefinition:
					var td = metadata.GetTypeDefinition((TypeDefinitionHandle)entity);
					return td.GetDeclaringType();
				case HandleKind.TypeReference:
					var tr = metadata.GetTypeReference((TypeReferenceHandle)entity);
					return tr.GetDeclaringType();
				case HandleKind.TypeSpecification:
					var ts = metadata.GetTypeSpecification((TypeSpecificationHandle)entity);
					return ts.GetGenericType(metadata).GetDeclaringType(metadata);
				case HandleKind.FieldDefinition:
					var fd = metadata.GetFieldDefinition((FieldDefinitionHandle)entity);
					return fd.GetDeclaringType();
				case HandleKind.MethodDefinition:
					var md = metadata.GetMethodDefinition((MethodDefinitionHandle)entity);
					return md.GetDeclaringType();
				case HandleKind.MemberReference:
					var mr = metadata.GetMemberReference((MemberReferenceHandle)entity);
					return mr.Parent;
				case HandleKind.EventDefinition:
					var ed = metadata.GetEventDefinition((EventDefinitionHandle)entity);
					return metadata.GetMethodDefinition(ed.GetAccessors().GetAny()).GetDeclaringType();
				case HandleKind.PropertyDefinition:
					var pd = metadata.GetPropertyDefinition((PropertyDefinitionHandle)entity);
					return metadata.GetMethodDefinition(pd.GetAccessors().GetAny()).GetDeclaringType();
				case HandleKind.MethodSpecification:
					var ms = metadata.GetMethodSpecification((MethodSpecificationHandle)entity);
					return ms.Method.GetDeclaringType(metadata);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Gets the declaring type reference for a nested type reference.
		/// </summary>
		/// <param name="tr">The type reference to inspect.</param>
		/// <returns>
		/// The enclosing <see cref="TypeReferenceHandle"/> when <paramref name="tr"/> is nested; otherwise a nil handle.
		/// </returns>
		public static TypeReferenceHandle GetDeclaringType(this in TypeReference tr)
		{
			switch (tr.ResolutionScope.Kind)
			{
				case HandleKind.TypeReference:
					return (TypeReferenceHandle)tr.ResolutionScope;
				default:
					return default(TypeReferenceHandle);
			}
		}

		/// <summary>
		/// Resolves a metadata type handle to a <see cref="FullTypeName"/>.
		/// </summary>
		/// <param name="handle">The type-like metadata handle to decode.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>The decoded full type name.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="handle"/> is not a type-like handle.</exception>
		public static FullTypeName GetFullTypeName(this EntityHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			switch (handle.Kind)
			{
				case HandleKind.TypeDefinition:
					return ((TypeDefinitionHandle)handle).GetFullTypeName(reader);
				case HandleKind.TypeReference:
					return ((TypeReferenceHandle)handle).GetFullTypeName(reader);
				case HandleKind.TypeSpecification:
					return ((TypeSpecificationHandle)handle).GetFullTypeName(reader);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Determines whether a metadata handle refers to a specific well-known runtime type.
		/// </summary>
		/// <param name="handle">The metadata handle to test.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <param name="knownType">The target well-known type identifier.</param>
		/// <returns><see langword="true"/> when <paramref name="handle"/> matches <paramref name="knownType"/>.</returns>
		public static bool IsKnownType(this EntityHandle handle, MetadataReader reader,
			KnownTypeCode knownType)
		{
			return IsKnownType(handle, reader, KnownTypeReference.Get(knownType).TypeName);
		}

		internal static bool IsKnownType(this EntityHandle handle, MetadataReader reader,
			KnownAttribute knownType)
		{
			return IsKnownType(handle, reader, knownType.GetTypeName());
		}

		private static bool IsKnownType(EntityHandle handle, MetadataReader reader, TopLevelTypeName knownType)
		{
			if (handle.IsNil)
				return false;
			StringHandle nameHandle, namespaceHandle;
			try
			{
				switch (handle.Kind)
				{
					case HandleKind.TypeReference:
						var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
						// ignore exported and nested types
						if (tr.ResolutionScope.IsNil || tr.ResolutionScope.Kind == HandleKind.TypeReference)
							return false;
						nameHandle = tr.Name;
						namespaceHandle = tr.Namespace;
						break;
					case HandleKind.TypeDefinition:
						var td = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
						if (td.IsNested)
							return false;
						nameHandle = td.Name;
						namespaceHandle = td.Namespace;
						break;
					case HandleKind.TypeSpecification:
						var ts = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
						var blob = reader.GetBlobReader(ts.Signature);
						return SignatureIsKnownType(reader, knownType, ref blob);
					default:
						return false;
				}
			}
			catch (BadImageFormatException)
			{
				// ignore bad metadata when trying to resolve ResolutionScope et al.
				return false;
			}
			if (knownType.TypeParameterCount == 0)
			{
				if (!reader.StringComparer.Equals(nameHandle, knownType.Name))
					return false;
			}
			else
			{
				string name = reader.GetString(nameHandle);
				name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out int typeParameterCount);
				if (typeParameterCount != knownType.TypeParameterCount || name != knownType.Name)
					return false;
			}
			if (namespaceHandle.IsNil)
			{
				return knownType.Namespace.Length == 0;
			}
			else
			{
				return reader.StringComparer.Equals(namespaceHandle, knownType.Namespace);
			}
		}

		private static bool SignatureIsKnownType(MetadataReader reader, TopLevelTypeName knownType, ref BlobReader blob)
		{
			if (!blob.TryReadCompressedInteger(out int typeCode))
				return false;
			switch (typeCode)
			{
				case 0x1: // ELEMENT_TYPE_VOID
					return knownType.IsKnownType(KnownTypeCode.Void);
				case 0x2: // ELEMENT_TYPE_BOOLEAN 
					return knownType.IsKnownType(KnownTypeCode.Boolean);
				case 0x3: // ELEMENT_TYPE_CHAR 
					return knownType.IsKnownType(KnownTypeCode.Char);
				case 0x4: // ELEMENT_TYPE_I1 
					return knownType.IsKnownType(KnownTypeCode.SByte);
				case 0x5: // ELEMENT_TYPE_U1
					return knownType.IsKnownType(KnownTypeCode.Byte);
				case 0x6: // ELEMENT_TYPE_I2
					return knownType.IsKnownType(KnownTypeCode.Int16);
				case 0x7: // ELEMENT_TYPE_U2
					return knownType.IsKnownType(KnownTypeCode.UInt16);
				case 0x8: // ELEMENT_TYPE_I4
					return knownType.IsKnownType(KnownTypeCode.Int32);
				case 0x9: // ELEMENT_TYPE_U4
					return knownType.IsKnownType(KnownTypeCode.UInt32);
				case 0xA: // ELEMENT_TYPE_I8
					return knownType.IsKnownType(KnownTypeCode.Int64);
				case 0xB: // ELEMENT_TYPE_U8
					return knownType.IsKnownType(KnownTypeCode.UInt64);
				case 0xC: // ELEMENT_TYPE_R4
					return knownType.IsKnownType(KnownTypeCode.Single);
				case 0xD: // ELEMENT_TYPE_R8
					return knownType.IsKnownType(KnownTypeCode.Double);
				case 0xE: // ELEMENT_TYPE_STRING
					return knownType.IsKnownType(KnownTypeCode.String);
				case 0x16: // ELEMENT_TYPE_TYPEDBYREF
					return knownType.IsKnownType(KnownTypeCode.TypedReference);
				case 0x18: // ELEMENT_TYPE_I
					return knownType.IsKnownType(KnownTypeCode.IntPtr);
				case 0x19: // ELEMENT_TYPE_U
					return knownType.IsKnownType(KnownTypeCode.UIntPtr);
				case 0x1C: // ELEMENT_TYPE_OBJECT
					return knownType.IsKnownType(KnownTypeCode.Object);
				case 0xF: // ELEMENT_TYPE_PTR 
				case 0x10: // ELEMENT_TYPE_BYREF 
				case 0x45: // ELEMENT_TYPE_PINNED
				case 0x1D: // ELEMENT_TYPE_SZARRAY
				case 0x1B: // ELEMENT_TYPE_FNPTR 
				case 0x14: // ELEMENT_TYPE_ARRAY 
					return false;
				case 0x1F: // ELEMENT_TYPE_CMOD_REQD 
				case 0x20: // ELEMENT_TYPE_CMOD_OPT 
						   // modifier
					blob.ReadTypeHandle(); // skip modifier
					return SignatureIsKnownType(reader, knownType, ref blob);
				case 0x15: // ELEMENT_TYPE_GENERICINST 
						   // generic type
					return SignatureIsKnownType(reader, knownType, ref blob);
				case 0x13: // ELEMENT_TYPE_VAR
				case 0x1E: // ELEMENT_TYPE_MVAR 
						   // index
					return false;
				case 0x11: // ELEMENT_TYPE_VALUETYPE
				case 0x12: // ELEMENT_TYPE_CLASS
					return IsKnownType(blob.ReadTypeHandle(), reader, knownType);
				default:
					return false;
			}
		}

		/// <summary>
		/// Resolves a type specification handle to its decoded <see cref="FullTypeName"/>.
		/// </summary>
		/// <param name="handle">The type specification handle to decode.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>The decoded full type name.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public static FullTypeName GetFullTypeName(this TypeSpecificationHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			var ts = reader.GetTypeSpecification(handle);
			return ts.DecodeSignature(new Metadata.FullTypeNameSignatureDecoder(reader), default(Unit));
		}

		/// <summary>
		/// Resolves a type reference handle to a <see cref="FullTypeName"/>, including nested type chains.
		/// </summary>
		/// <remarks>
		/// Invalid metadata for individual string/scope lookups is tolerated where possible so callers can
		/// still get a stable synthetic name for diagnostics and decompilation fallback paths.
		/// </remarks>
		/// <param name="handle">The type reference handle to decode.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>The decoded full type name.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public static FullTypeName GetFullTypeName(this TypeReferenceHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			var tr = reader.GetTypeReference(handle);
			string name;
			try
			{
				name = reader.GetString(tr.Name);
			}
			catch (BadImageFormatException)
			{
				name = $"TR{reader.GetToken(handle):x8}";
			}
			name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(
				name, out var typeParameterCount);
			TypeReferenceHandle declaringTypeHandle;
			try
			{
				declaringTypeHandle = tr.GetDeclaringType();
			}
			catch (BadImageFormatException)
			{
				declaringTypeHandle = default;
			}
			if (declaringTypeHandle.IsNil)
			{
				string ns;
				try
				{
					ns = tr.Namespace.IsNil ? "" : reader.GetString(tr.Namespace);
				}
				catch (BadImageFormatException)
				{
					ns = "";
				}
				return new FullTypeName(new TopLevelTypeName(ns, name, typeParameterCount));
			}
			else
			{
				return declaringTypeHandle.GetFullTypeName(reader).NestedType(name, typeParameterCount);
			}
		}

		/// <summary>
		/// Resolves a type definition handle to its <see cref="FullTypeName"/>.
		/// </summary>
		/// <param name="handle">The type definition handle to decode.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>The decoded full type name.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="handle"/> is nil.</exception>
		public static FullTypeName GetFullTypeName(this TypeDefinitionHandle handle, MetadataReader reader)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			return reader.GetTypeDefinition(handle).GetFullTypeName(reader);
		}

		/// <summary>
		/// Resolves a type definition to its <see cref="FullTypeName"/>, preserving nested type hierarchy.
		/// </summary>
		/// <param name="td">The type definition row to decode.</param>
		/// <param name="reader">The metadata reader that owns <paramref name="td"/>.</param>
		/// <returns>The decoded full type name.</returns>
		public static FullTypeName GetFullTypeName(this TypeDefinition td, MetadataReader reader)
		{
			TypeDefinitionHandle declaringTypeHandle;
			string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(
				reader.GetString(td.Name), out var typeParameterCount);
			if ((declaringTypeHandle = td.GetDeclaringType()).IsNil)
			{
				string @namespace = td.Namespace.IsNil ? "" : reader.GetString(td.Namespace);
				return new FullTypeName(new TopLevelTypeName(@namespace, name, typeParameterCount));
			}
			else
			{
				return declaringTypeHandle.GetFullTypeName(reader).NestedType(name, typeParameterCount);
			}
		}

		/// <summary>
		/// Resolves an exported type row to its full type name.
		/// </summary>
		/// <remarks>
		/// Nested exported types are encoded through chained <see cref="HandleKind.ExportedType"/> implementations;
		/// this method walks that chain to reconstruct the nested name.
		/// </remarks>
		/// <param name="type">The exported type row to decode.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="type"/>.</param>
		/// <returns>The decoded full type name.</returns>
		public static FullTypeName GetFullTypeName(this ExportedType type, MetadataReader metadata)
		{
			string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(
				metadata.GetString(type.Name), out int typeParameterCount);
			if (type.Implementation.Kind == HandleKind.ExportedType)
			{
				var outerType = metadata.GetExportedType((ExportedTypeHandle)type.Implementation);
				return outerType.GetFullTypeName(metadata).NestedType(name, typeParameterCount);
			}
			else
			{
				string ns = type.Namespace.IsNil ? "" : metadata.GetString(type.Namespace);
				return new TopLevelTypeName(ns, name, typeParameterCount);
			}
		}

		static bool HasOnlyReadOnlyProperties(TypeDefinition type, MetadataReader metadata)
		{
			foreach (var handle in type.GetProperties())
			{
				if (!metadata.GetPropertyDefinition(handle).GetAccessors().Setter.IsNil)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determines whether a type definition matches the compiler's anonymous-type naming and attribute pattern.
		/// An anonymous type with a settable property cannot be written as a C# anonymous type, so its
		/// declaration must not be hidden.
		/// </summary>
		/// <param name="type">The type definition to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="type"/>.</param>
		/// <returns><see langword="true"/> when the type matches the anonymous-type heuristic.</returns>
		public static bool IsAnonymousType(this TypeDefinition type, MetadataReader metadata)
		{
			string name = metadata.GetString(type.Name);
			if (type.Namespace.IsNil && type.HasGeneratedName(metadata)
				&& (name.Contains("AnonType") || name.Contains("AnonymousType")))
			{
				return type.IsCompilerGenerated(metadata) && HasOnlyReadOnlyProperties(type, metadata);
			}
			return false;
		}

		#region HasGeneratedName

		/// <summary>
		/// Determines whether a metadata name string looks compiler-generated.
		/// </summary>
		/// <remarks>
		/// The heuristic mirrors common C# and VB name mangling prefixes and separators (for example <c>&lt;...&gt;</c> and <c>$</c>).
		/// </remarks>
		/// <param name="handle">The metadata string handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the decoded name resembles a compiler-generated identifier.</returns>
		public static bool IsGeneratedName(this StringHandle handle, MetadataReader metadata)
		{
			return !handle.IsNil && IsGeneratedName(metadata.GetString(handle));
		}

		/// <summary>
		/// Detects the mangled names compilers give to entities that have no user-written
		/// declaration. The C# compiler prefixes them with '&lt;', the VB compiler separates
		/// the parts with '$' (VB$AnonymousType_0, VB$StateMachine_1_Foo). Neither character
		/// is legal in a C# or VB identifier.
		/// Note that a name may legitimately contain '&lt;' without being generated: explicit
		/// implementations of generic interface members are named after the interface.
		/// </summary>
		internal static bool IsGeneratedName(string name)
		{
			return name.StartsWith("<", StringComparison.Ordinal) || name.Contains("$");
		}

		/// <summary>
		/// Determines whether a method definition has a compiler-generated-looking name.
		/// </summary>
		/// <param name="handle">The method definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the method name matches generated-name heuristics.</returns>
		public static bool HasGeneratedName(this MethodDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetMethodDefinition(handle).Name.IsGeneratedName(metadata);
		}

		/// <summary>
		/// Determines whether a type definition has a compiler-generated-looking name.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the type name matches generated-name heuristics.</returns>
		public static bool HasGeneratedName(this TypeDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetTypeDefinition(handle).Name.IsGeneratedName(metadata);
		}

		/// <summary>
		/// Determines whether a type definition has a compiler-generated-looking name.
		/// </summary>
		/// <param name="type">The type definition to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="type"/>.</param>
		/// <returns><see langword="true"/> when the type name matches generated-name heuristics.</returns>
		public static bool HasGeneratedName(this TypeDefinition type, MetadataReader metadata)
		{
			return type.Name.IsGeneratedName(metadata);
		}

		/// <summary>
		/// Determines whether a field definition has a compiler-generated-looking name.
		/// </summary>
		/// <param name="handle">The field definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the field name matches generated-name heuristics.</returns>
		public static bool HasGeneratedName(this FieldDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetFieldDefinition(handle).Name.IsGeneratedName(metadata);
		}

		#endregion

		#region IsCompilerGenerated

		/// <summary>
		/// Determines whether a method definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="handle">The method definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the method has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this MethodDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetMethodDefinition(handle).IsCompilerGenerated(metadata);
		}

		/// <summary>
		/// Determines whether a method is compiler-generated, or declared in a compiler-generated type.
		/// </summary>
		/// <param name="handle">The method definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>
		/// <see langword="true"/> when either the method itself or its declaring type is marked compiler-generated.
		/// </returns>
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this MethodDefinitionHandle handle,
			MetadataReader metadata)
		{
			MethodDefinition method = metadata.GetMethodDefinition(handle);
			if (method.IsCompilerGenerated(metadata))
				return true;
			TypeDefinitionHandle declaringTypeHandle = method.GetDeclaringType();
			if (!declaringTypeHandle.IsNil && declaringTypeHandle.IsCompilerGenerated(metadata))
				return true;
			return false;
		}

		/// <summary>
		/// Determines whether a type is compiler-generated, or nested in a compiler-generated type.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns>
		/// <see langword="true"/> when either the type itself or its declaring type is marked compiler-generated.
		/// </returns>
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this TypeDefinitionHandle handle,
			MetadataReader metadata)
		{
			TypeDefinition type = metadata.GetTypeDefinition(handle);
			if (type.IsCompilerGenerated(metadata))
				return true;
			TypeDefinitionHandle declaringTypeHandle = type.GetDeclaringType();
			if (!declaringTypeHandle.IsNil && declaringTypeHandle.IsCompilerGenerated(metadata))
				return true;
			return false;
		}

		/// <summary>
		/// Determines whether a method definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="method">The method definition to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="method"/>.</param>
		/// <returns><see langword="true"/> when the method has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this MethodDefinition method, MetadataReader metadata)
		{
			return method.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.CompilerGenerated);
		}

		/// <summary>
		/// Determines whether a field definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="handle">The field definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the field has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this FieldDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetFieldDefinition(handle).IsCompilerGenerated(metadata);
		}

		/// <summary>
		/// Determines whether a field definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="field">The field definition to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="field"/>.</param>
		/// <returns><see langword="true"/> when the field has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this FieldDefinition field, MetadataReader metadata)
		{
			return field.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.CompilerGenerated);
		}

		/// <summary>
		/// Determines whether a type definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="handle">The type definition handle to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="handle"/>.</param>
		/// <returns><see langword="true"/> when the type has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this TypeDefinitionHandle handle, MetadataReader metadata)
		{
			return metadata.GetTypeDefinition(handle).IsCompilerGenerated(metadata);
		}

		/// <summary>
		/// Determines whether a type definition is annotated with <c>CompilerGeneratedAttribute</c>.
		/// </summary>
		/// <param name="type">The type definition to inspect.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="type"/>.</param>
		/// <returns><see langword="true"/> when the type has <c>CompilerGeneratedAttribute</c>.</returns>
		public static bool IsCompilerGenerated(this TypeDefinition type, MetadataReader metadata)
		{
			return type.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.CompilerGenerated);
		}

		#endregion

		#region Attribute extensions
		/// <summary>
		/// Gets the attribute type referenced by a custom attribute constructor.
		/// </summary>
		/// <param name="attribute">The custom attribute to inspect.</param>
		/// <param name="reader">The metadata reader used to resolve constructor handles.</param>
		/// <returns>The metadata handle of the attribute type.</returns>
		public static EntityHandle GetAttributeType(this SRM.CustomAttribute attribute, MetadataReader reader)
		{
			switch (attribute.Constructor.Kind)
			{
				case HandleKind.MethodDefinition:
					var md = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
					return md.GetDeclaringType();
				case HandleKind.MemberReference:
					var mr = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
					return mr.Parent;
				default:
					throw new BadImageFormatException("Unexpected token kind for attribute constructor: "
						+ attribute.Constructor.Kind);
			}
		}

		/// <summary>
		/// Determines whether any attribute in the collection matches the specified known attribute kind.
		/// </summary>
		/// <param name="customAttributes">The attribute handles to scan.</param>
		/// <param name="metadata">The metadata reader used to resolve attribute rows.</param>
		/// <param name="type">The known attribute kind to match.</param>
		/// <returns><see langword="true"/> when at least one attribute in the collection matches <paramref name="type"/>.</returns>
		public static bool HasKnownAttribute(this CustomAttributeHandleCollection customAttributes,
			MetadataReader metadata, KnownAttribute type)
		{
			foreach (var handle in customAttributes)
			{
				var customAttribute = metadata.GetCustomAttribute(handle);
				if (customAttribute.IsKnownAttribute(metadata, type))
					return true;
			}
			return false;
		}

		internal static bool IsKnownAttribute(this SRM.CustomAttribute attr, MetadataReader metadata,
			KnownAttribute attrType)
		{
			return attr.GetAttributeType(metadata).IsKnownType(metadata, attrType);
		}

		/// <summary>
		/// Extracts the nullable-context state from a custom attribute list.
		/// </summary>
		/// <param name="customAttributes">The attribute handles to scan.</param>
		/// <param name="metadata">The metadata reader used to decode matching attribute payloads.</param>
		/// <returns>
		/// The decoded nullable context value when a valid <c>NullableContextAttribute</c> is present;
		/// otherwise <see langword="null"/>.
		/// </returns>
		public static Nullability? GetNullableContext(this CustomAttributeHandleCollection customAttributes,
			MetadataReader metadata)
		{
			foreach (var handle in customAttributes)
			{
				var customAttribute = metadata.GetCustomAttribute(handle);
				if (customAttribute.IsKnownAttribute(metadata, KnownAttribute.NullableContext))
				{
					// Decode 
					CustomAttributeValue<IType> value;
					try
					{
						value = customAttribute.DecodeValue(
							Metadata.MetadataExtensions.MinimalAttributeTypeProvider);
					}
					catch (BadImageFormatException)
					{
						continue;
					}
					catch (Metadata.EnumUnderlyingTypeResolveException)
					{
						continue;
					}
					if (value.FixedArguments.Length == 1 && value.FixedArguments[0].Value is byte b && b <= 2)
					{
						return (Nullability)b;
					}
				}
			}
			return null;
		}
		#endregion

		/// <summary>
		/// Returns a blob reader over the initial data bytes for an RVA-backed field.
		/// </summary>
		/// <param name="field">The field definition to inspect.</param>
		/// <param name="pefile">The owning metadata/PE file used to map RVA data.</param>
		/// <param name="typeSystem">The compilation used for field-signature size decoding.</param>
		/// <returns>
		/// A reader over the field's initialized bytes, or a default reader when the field has no RVA data.
		/// </returns>
		/// <exception cref="BadImageFormatException">
		/// The field data RVA cannot be mapped to a section, or the decoded field size is invalid for the section.
		/// </exception>
		public static unsafe BlobReader GetInitialValue(this FieldDefinition field, MetadataFile pefile,
			ICompilation typeSystem)
		{
			if (!field.HasFlag(FieldAttributes.HasFieldRVA))
				return default;
			int rva = field.GetRelativeVirtualAddress();
			if (rva == 0)
				return default;
			int size = field.DecodeSignature(new FieldValueSizeDecoder(typeSystem), default);
			var sectionData = pefile.GetSectionData(rva);
			if (sectionData.Length == 0 && size != 0)
				throw new BadImageFormatException($"Field data (rva=0x{rva:x}) could not be found"
					+ " in any section!");
			if (size < 0 || size > sectionData.Length)
				throw new BadImageFormatException($"Invalid size {size} for field data!");
			return sectionData.GetReader(0, size);
		}

		sealed class FieldValueSizeDecoder : ISignatureTypeProvider<int, GenericContext>
		{
			readonly MetadataModule module;
			readonly int pointerSize;

			public FieldValueSizeDecoder(ICompilation typeSystem = null)
			{
				this.module = (MetadataModule)typeSystem?.MainModule;
				if (module?.MetadataFile is not PEFile pefile)
					this.pointerSize = IntPtr.Size;
				else
					this.pointerSize = pefile.Reader.PEHeaders.PEHeader.Magic == PEMagic.PE32 ? 4 : 8;
			}

			public int GetArrayType(int elementType, ArrayShape shape) =>
				GetPrimitiveType(PrimitiveTypeCode.Object);
			public int GetSZArrayType(int elementType) => GetPrimitiveType(PrimitiveTypeCode.Object);
			public int GetByReferenceType(int elementType) => pointerSize;
			public int GetFunctionPointerType(MethodSignature<int> signature) => pointerSize;
			public int GetGenericInstantiation(int genericType, ImmutableArray<int> typeArguments)
				=> genericType;
			public int GetGenericMethodParameter(GenericContext genericContext, int index) => 0;
			public int GetGenericTypeParameter(GenericContext genericContext, int index) => 0;
			public int GetModifiedType(int modifier, int unmodifiedType, bool isRequired) => unmodifiedType;
			public int GetPinnedType(int elementType) => elementType;
			public int GetPointerType(int elementType) => pointerSize;

			public int GetPrimitiveType(PrimitiveTypeCode typeCode)
			{
				switch (typeCode)
				{
					case PrimitiveTypeCode.Boolean:
					case PrimitiveTypeCode.Byte:
					case PrimitiveTypeCode.SByte:
						return 1;
					case PrimitiveTypeCode.Char:
					case PrimitiveTypeCode.Int16:
					case PrimitiveTypeCode.UInt16:
						return 2;
					case PrimitiveTypeCode.Int32:
					case PrimitiveTypeCode.UInt32:
					case PrimitiveTypeCode.Single:
						return 4;
					case PrimitiveTypeCode.Int64:
					case PrimitiveTypeCode.UInt64:
					case PrimitiveTypeCode.Double:
						return 8;
					case PrimitiveTypeCode.IntPtr:
					case PrimitiveTypeCode.UIntPtr:
						return pointerSize;
					default:
						return 0;
				}
			}

			public int GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle,
				byte rawTypeKind)
			{
				var td = reader.GetTypeDefinition(handle);
				return td.GetLayout().Size;
			}

			public int GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle,
				byte rawTypeKind)
			{
				var typeDef = module?.ResolveType(handle, new GenericContext()).GetDefinition();
				if (typeDef == null || typeDef.MetadataToken.IsNil)
					return 0;
				reader = typeDef.ParentModule.MetadataFile.Metadata;
				var td = reader.GetTypeDefinition((TypeDefinitionHandle)typeDef.MetadataToken);
				return td.GetLayout().Size;
			}

			public int GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext,
				TypeSpecificationHandle handle, byte rawTypeKind)
			{
				return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
			}
		}

		/// <summary>
		/// Gets a type definition's base type handle, returning nil for malformed metadata.
		/// </summary>
		/// <param name="definition">The type definition whose base type is requested.</param>
		/// <returns>The base type handle, or nil when metadata decoding fails.</returns>
		public static EntityHandle GetBaseTypeOrNil(this TypeDefinition definition)
		{
			try
			{
				return definition.BaseType;
			}
			catch (BadImageFormatException)
			{
				return default;
			}
		}

		/// <summary>
		/// Converts a signature calling-convention value to ILAsm-compatible syntax text.
		/// </summary>
		/// <param name="callConv">The signature calling-convention value.</param>
		/// <returns>The ILAsm keyword sequence for <paramref name="callConv"/>.</returns>
		public static string ToILSyntax(this SignatureCallingConvention callConv)
		{
			return callConv switch {
				SignatureCallingConvention.Default => "default",
				SignatureCallingConvention.CDecl => "unmanaged cdecl",
				SignatureCallingConvention.StdCall => "unmanaged stdcall",
				SignatureCallingConvention.ThisCall => "unmanaged thiscall",
				SignatureCallingConvention.FastCall => "unmanaged fastcall",
				SignatureCallingConvention.VarArgs => "vararg",
				SignatureCallingConvention.Unmanaged => "unmanaged",
				_ => callConv.ToString().ToLowerInvariant()
			};
		}

		/// <summary>
		/// Opens an unmanaged stream view over the full memory-mapped accessor range.
		/// </summary>
		/// <param name="view">The memory-mapped accessor to expose as a stream.</param>
		/// <returns>An unmanaged stream spanning the whole mapped view.</returns>
		public static UnmanagedMemoryStream AsStream(this MemoryMappedViewAccessor view)
		{
			long size = checked((long)view.SafeMemoryMappedViewHandle.ByteLength);
			return new UnmanagedMemoryStream(view.SafeMemoryMappedViewHandle, 0, size);
		}
	}
}

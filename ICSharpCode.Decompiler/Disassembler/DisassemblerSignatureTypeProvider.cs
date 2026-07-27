// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Decodes metadata signatures into ILAsm-style textual type fragments.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="System.Reflection.Metadata"/> decoders call this provider while walking signature blobs.
	/// Instead of returning immediate text, each method returns an <see cref="Action{T}"/> that writes its
	/// fragment only when invoked with an <see cref="ILNameSyntax"/> mode. This allows callers to decode once
	/// and render with different naming modes (for example full names vs. signature syntax).
	/// </para>
	/// <para>
	/// The instance is stateful because every returned delegate writes to the <see cref="ITextOutput"/> supplied
	/// at construction time. Reuse is therefore scoped to one output sink at a time.
	/// </para>
	/// </remarks>
	public class DisassemblerSignatureTypeProvider : ISignatureTypeProvider<Action<ILNameSyntax>, MetadataGenericContext>
	{
		readonly MetadataFile module;
		readonly MetadataReader metadata;
		readonly ITextOutput output;

		/// <summary>
		/// Initializes a signature decoder that writes to a specific disassembler output sink.
		/// </summary>
		/// <param name="module">Metadata module used for type-parameter and entity-handle lookups.</param>
		/// <param name="output">Text sink that receives decoded signature fragments.</param>
		/// <exception cref="ArgumentNullException"><paramref name="module"/> or <paramref name="output"/> is <see langword="null"/>.</exception>
		public DisassemblerSignatureTypeProvider(MetadataFile module, ITextOutput output)
		{
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.metadata = module.Metadata;
		}

		/// <summary>
		/// Builds an IL writer for a multi-dimensional array signature including bounds when present.
		/// </summary>
		/// <param name="elementType">Writer for the element type.</param>
		/// <param name="shape">Rank and optional lower/upper bound information.</param>
		/// <returns>An action that writes the full array type fragment.</returns>
		public Action<ILNameSyntax> GetArrayType(Action<ILNameSyntax> elementType, ArrayShape shape)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('[');
				for (int i = 0; i < shape.Rank; i++)
				{
					if (i > 0)
						output.Write(", ");
					if (i < shape.LowerBounds.Length || i < shape.Sizes.Length)
					{
						int lower = 0;
						if (i < shape.LowerBounds.Length)
						{
							lower = shape.LowerBounds[i];
							output.Write(lower.ToString());
						}
						output.Write("...");
						if (i < shape.Sizes.Length)
							output.Write((lower + shape.Sizes[i] - 1).ToString());
					}
				}
				output.Write(']');
			};
		}

		/// <summary>
		/// Builds an IL writer for a managed by-reference type (<c>&amp;</c> suffix).
		/// </summary>
		/// <param name="elementType">Writer for the referenced element type.</param>
		/// <returns>An action that writes the by-reference type fragment.</returns>
		public Action<ILNameSyntax> GetByReferenceType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('&');
			};
		}

		/// <summary>
		/// Builds an IL writer for a function pointer signature.
		/// </summary>
		/// <param name="signature">Decoded function pointer signature header, return type, and parameter types.</param>
		/// <returns>An action that writes the full function pointer type fragment.</returns>
		public Action<ILNameSyntax> GetFunctionPointerType(MethodSignature<Action<ILNameSyntax>> signature)
		{
			return syntax => {
				output.Write("method ");
				signature.Header.WriteTo(output);
				signature.ReturnType(syntax);
				output.Write(" *(");
				for (int i = 0; i < signature.ParameterTypes.Length; i++)
				{
					if (i > 0)
						output.Write(", ");
					signature.ParameterTypes[i](syntax);
				}
				output.Write(')');
			};
		}

		/// <summary>
		/// Builds an IL writer for a constructed generic type.
		/// </summary>
		/// <param name="genericType">Writer for the generic type definition.</param>
		/// <param name="typeArguments">Writers for each instantiated type argument.</param>
		/// <returns>An action that writes the closed generic type fragment.</returns>
		public Action<ILNameSyntax> GetGenericInstantiation(Action<ILNameSyntax> genericType, ImmutableArray<Action<ILNameSyntax>> typeArguments)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				genericType(syntaxForElementTypes);
				output.Write('<');
				for (int i = 0; i < typeArguments.Length; i++)
				{
					if (i > 0)
						output.Write(", ");
					typeArguments[i](syntaxForElementTypes);
				}
				output.Write('>');
			};
		}

		/// <summary>
		/// Builds an IL writer for a method generic parameter reference (<c>!!n</c> or named form).
		/// </summary>
		/// <param name="genericContext">Context used to resolve method generic parameter handles.</param>
		/// <param name="index">Zero-based method type-parameter index from the signature blob.</param>
		/// <returns>An action that writes the method-generic parameter token.</returns>
		public Action<ILNameSyntax> GetGenericMethodParameter(MetadataGenericContext genericContext, int index)
		{
			return syntax => {
				output.Write("!!");
				WriteTypeParameter(genericContext.GetGenericMethodTypeParameterHandleOrNull(index), index, syntax);
			};
		}

		/// <summary>
		/// Builds an IL writer for a type generic parameter reference (<c>!n</c> or named form).
		/// </summary>
		/// <param name="genericContext">Context used to resolve type generic parameter handles.</param>
		/// <param name="index">Zero-based type-parameter index from the signature blob.</param>
		/// <returns>An action that writes the type-generic parameter token.</returns>
		public Action<ILNameSyntax> GetGenericTypeParameter(MetadataGenericContext genericContext, int index)
		{
			return syntax => {
				output.Write("!");
				WriteTypeParameter(genericContext.GetGenericTypeParameterHandleOrNull(index), index, syntax);
			};
		}

		/// <summary>
		/// Writes one generic parameter token using either positional or source-name notation.
		/// </summary>
		/// <param name="paramRef">Resolved generic parameter handle when available.</param>
		/// <param name="index">Positional generic parameter index from the signature blob.</param>
		/// <param name="syntax">Current rendering mode that decides whether names are allowed.</param>
		void WriteTypeParameter(GenericParameterHandle paramRef, int index, ILNameSyntax syntax)
		{
			if (paramRef.IsNil || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
				output.Write(index.ToString());
			else
			{
				var param = metadata.GetGenericParameter(paramRef);
				if (param.Name.IsNil)
					output.Write(param.Index.ToString());
				else
					output.Write(DisassemblerHelpers.Escape(metadata.GetString(param.Name)));
			}
		}

		/// <summary>
		/// Builds an IL writer for a required/optional custom modifier applied to a type.
		/// </summary>
		/// <param name="modifier">Writer for the modifier type.</param>
		/// <param name="unmodifiedType">Writer for the underlying type before modifiers are appended.</param>
		/// <param name="isRequired"><see langword="true"/> to emit <c>modreq</c>; <see langword="false"/> to emit <c>modopt</c>.</param>
		/// <returns>An action that writes the modified type fragment.</returns>
		public Action<ILNameSyntax> GetModifiedType(Action<ILNameSyntax> modifier, Action<ILNameSyntax> unmodifiedType, bool isRequired)
		{
			return syntax => {
				unmodifiedType(syntax);
				if (isRequired)
					output.Write(" modreq");
				else
					output.Write(" modopt");
				output.Write('(');
				modifier(ILNameSyntax.TypeName);
				output.Write(')');
			};
		}

		/// <summary>
		/// Builds an IL writer for a pinned local/argument type.
		/// </summary>
		/// <param name="elementType">Writer for the element type being pinned.</param>
		/// <returns>An action that writes the pinned type fragment.</returns>
		public Action<ILNameSyntax> GetPinnedType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write(" pinned");
			};
		}

		/// <summary>
		/// Builds an IL writer for an unmanaged pointer type (<c>*</c> suffix).
		/// </summary>
		/// <param name="elementType">Writer for the pointed-to element type.</param>
		/// <returns>An action that writes the pointer type fragment.</returns>
		public Action<ILNameSyntax> GetPointerType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('*');
			};
		}

		/// <summary>
		/// Builds an IL writer for a primitive CLI type keyword.
		/// </summary>
		/// <param name="typeCode">Primitive type discriminator produced by metadata signature decoding.</param>
		/// <returns>A writer that emits the corresponding IL keyword (for example <c>int32</c> or <c>native int</c>).</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="typeCode"/> is not a supported primitive value.</exception>
		public Action<ILNameSyntax> GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			switch (typeCode)
			{
				case PrimitiveTypeCode.SByte:
					return syntax => output.Write("int8");
				case PrimitiveTypeCode.Int16:
					return syntax => output.Write("int16");
				case PrimitiveTypeCode.Int32:
					return syntax => output.Write("int32");
				case PrimitiveTypeCode.Int64:
					return syntax => output.Write("int64");
				case PrimitiveTypeCode.Byte:
					return syntax => output.Write("uint8");
				case PrimitiveTypeCode.UInt16:
					return syntax => output.Write("uint16");
				case PrimitiveTypeCode.UInt32:
					return syntax => output.Write("uint32");
				case PrimitiveTypeCode.UInt64:
					return syntax => output.Write("uint64");
				case PrimitiveTypeCode.Single:
					return syntax => output.Write("float32");
				case PrimitiveTypeCode.Double:
					return syntax => output.Write("float64");
				case PrimitiveTypeCode.Void:
					return syntax => output.Write("void");
				case PrimitiveTypeCode.Boolean:
					return syntax => output.Write("bool");
				case PrimitiveTypeCode.String:
					return syntax => output.Write("string");
				case PrimitiveTypeCode.Char:
					return syntax => output.Write("char");
				case PrimitiveTypeCode.Object:
					return syntax => output.Write("object");
				case PrimitiveTypeCode.IntPtr:
					return syntax => output.Write("native int");
				case PrimitiveTypeCode.UIntPtr:
					return syntax => output.Write("native uint");
				case PrimitiveTypeCode.TypedReference:
					return syntax => output.Write("typedref");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Builds an IL writer for a single-dimensional zero-based array (<c>[]</c>).
		/// </summary>
		/// <param name="elementType">Writer for the array element type.</param>
		/// <returns>An action that writes the vector type fragment.</returns>
		public Action<ILNameSyntax> GetSZArrayType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('[');
				output.Write(']');
			};
		}

		/// <summary>
		/// Builds an IL writer for a type-definition reference in a signature.
		/// </summary>
		/// <param name="reader">Metadata reader provided by the decoder; unused because this instance already captures module metadata.</param>
		/// <param name="handle">Type definition token to format.</param>
		/// <param name="rawTypeKind">Raw signature kind byte (class/valuetype qualifier) from the blob.</param>
		/// <returns>A writer that emits optional <c>class</c>/<c>valuetype</c> prefix and the referenced type.</returns>
		/// <remarks>
		/// The returned action throws <see cref="BadImageFormatException"/> when it is invoked, if
		/// <paramref name="rawTypeKind"/> holds an unknown discriminator value. The call itself never throws.
		/// </remarks>
		public Action<ILNameSyntax> GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			return syntax => {
				switch (rawTypeKind)
				{
					case 0x00:
						break;
					case 0x11:
						output.Write("valuetype ");
						break;
					case 0x12:
						output.Write("class ");
						break;
					default:
						throw new BadImageFormatException($"Unexpected rawTypeKind: {rawTypeKind} (0x{rawTypeKind:x})");
				}
				((EntityHandle)handle).WriteTo(module, output, default);
			};
		}

		/// <summary>
		/// Builds an IL writer for a type-reference token in a signature.
		/// </summary>
		/// <param name="reader">Metadata reader provided by the decoder; unused because this instance already captures module metadata.</param>
		/// <param name="handle">Type reference token to format.</param>
		/// <param name="rawTypeKind">Raw signature kind byte (class/valuetype qualifier) from the blob.</param>
		/// <returns>A writer that emits optional <c>class</c>/<c>valuetype</c> prefix and the referenced type.</returns>
		/// <remarks>
		/// The returned action throws <see cref="BadImageFormatException"/> when it is invoked, if
		/// <paramref name="rawTypeKind"/> holds an unknown discriminator value. The call itself never throws.
		/// </remarks>
		public Action<ILNameSyntax> GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			return syntax => {
				switch (rawTypeKind)
				{
					case 0x00:
						break;
					case 0x11:
						output.Write("valuetype ");
						break;
					case 0x12:
						output.Write("class ");
						break;
					default:
						throw new BadImageFormatException($"Unexpected rawTypeKind: {rawTypeKind} (0x{rawTypeKind:x})");
				}
				((EntityHandle)handle).WriteTo(module, output, default);
			};
		}

		/// <summary>
		/// Decodes a type-specification signature and returns its deferred IL writer.
		/// </summary>
		/// <param name="reader">Metadata reader containing the type-specification blob.</param>
		/// <param name="genericContext">Generic context used to resolve <c>!n</c> and <c>!!n</c> placeholders.</param>
		/// <param name="handle">Type specification token to decode.</param>
		/// <param name="rawTypeKind">Raw kind byte passed by the metadata decoder; ignored for type specifications.</param>
		/// <returns>An action that writes the decoded type fragment when invoked.</returns>
		public Action<ILNameSyntax> GetTypeFromSpecification(MetadataReader reader, MetadataGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
		{
			return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
		}
	}
}

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

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Carries the metadata context required to resolve generic parameter names and handles for a declaring type and optional method.
	/// </summary>
	/// <remarks>
	/// This value type is used by metadata decoders that need stable generic-parameter lookup without materializing a full type-system symbol.
	/// If the stored metadata reader is unavailable, name lookup methods fall back to returning numeric indices.
	/// </remarks>
	public readonly struct MetadataGenericContext
	{
		readonly MetadataReader? metadata;
		readonly TypeDefinitionHandle declaringType;
		readonly MethodDefinitionHandle method;

		/// <summary>
		/// Creates a context for method-level and declaring-type generic parameter lookup using a module-backed metadata reader.
		/// </summary>
		/// <param name="method">The method definition that contributes method generic parameters.</param>
		/// <param name="module">The module that contains the method metadata.</param>
		public MetadataGenericContext(MethodDefinitionHandle method, MetadataFile module)
		{
			this.metadata = module.Metadata;
			this.method = method;
			this.declaringType = module.Metadata.GetMethodDefinition(method).GetDeclaringType();
		}

		/// <summary>
		/// Creates a context for method-level and declaring-type generic parameter lookup using an explicit metadata reader.
		/// </summary>
		/// <param name="method">The method definition that contributes method generic parameters.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="method"/>.</param>
		public MetadataGenericContext(MethodDefinitionHandle method, MetadataReader metadata)
		{
			this.metadata = metadata;
			this.method = method;
			this.declaringType = metadata.GetMethodDefinition(method).GetDeclaringType();
		}

		/// <summary>
		/// Creates a context for type-level generic parameter lookup using a module-backed metadata reader.
		/// </summary>
		/// <param name="declaringType">The declaring type that contributes type generic parameters.</param>
		/// <param name="module">The module that contains the type metadata.</param>
		public MetadataGenericContext(TypeDefinitionHandle declaringType, MetadataFile module)
		{
			this.metadata = module.Metadata;
			this.method = default;
			this.declaringType = declaringType;
		}

		/// <summary>
		/// Creates a context for type-level generic parameter lookup using an explicit metadata reader.
		/// </summary>
		/// <param name="declaringType">The declaring type that contributes type generic parameters.</param>
		/// <param name="metadata">The metadata reader that owns <paramref name="declaringType"/>.</param>
		public MetadataGenericContext(TypeDefinitionHandle declaringType, MetadataReader metadata)
		{
			this.metadata = metadata;
			this.method = default;
			this.declaringType = declaringType;
		}

		/// <summary>
		/// Gets the display name of the generic type parameter at the specified index.
		/// </summary>
		/// <param name="index">The zero-based generic parameter index.</param>
		/// <returns>
		/// The metadata-defined parameter name when available; otherwise the numeric <paramref name="index"/> converted to text.
		/// </returns>
		public string GetGenericTypeParameterName(int index)
		{
			GenericParameterHandle genericParameter = GetGenericTypeParameterHandleOrNull(index);
			if (genericParameter.IsNil || metadata == null)
				return index.ToString();
			return metadata.GetString(metadata.GetGenericParameter(genericParameter).Name);
		}

		/// <summary>
		/// Gets the display name of the generic method parameter at the specified index.
		/// </summary>
		/// <param name="index">The zero-based generic parameter index.</param>
		/// <returns>
		/// The metadata-defined parameter name when available; otherwise the numeric <paramref name="index"/> converted to text.
		/// </returns>
		public string GetGenericMethodTypeParameterName(int index)
		{
			GenericParameterHandle genericParameter = GetGenericMethodTypeParameterHandleOrNull(index);
			if (genericParameter.IsNil || metadata == null)
				return index.ToString();
			return metadata.GetString(metadata.GetGenericParameter(genericParameter).Name);
		}

		/// <summary>
		/// Gets the generic type parameter handle at the specified index.
		/// </summary>
		/// <param name="index">The zero-based generic parameter index.</param>
		/// <returns>
		/// A valid <see cref="GenericParameterHandle"/> when the index exists; otherwise the nil handle.
		/// </returns>
		public GenericParameterHandle GetGenericTypeParameterHandleOrNull(int index)
		{
			if (declaringType.IsNil || index < 0 || metadata == null)
				return MetadataTokens.GenericParameterHandle(0);
			var genericParameters = metadata.GetTypeDefinition(declaringType).GetGenericParameters();
			if (index >= genericParameters.Count)
				return MetadataTokens.GenericParameterHandle(0);
			return genericParameters[index];
		}

		/// <summary>
		/// Gets the generic method parameter handle at the specified index.
		/// </summary>
		/// <param name="index">The zero-based generic parameter index.</param>
		/// <returns>
		/// A valid <see cref="GenericParameterHandle"/> when the index exists; otherwise the nil handle.
		/// </returns>
		public GenericParameterHandle GetGenericMethodTypeParameterHandleOrNull(int index)
		{
			if (method.IsNil || index < 0 || metadata == null)
				return MetadataTokens.GenericParameterHandle(0);
			var genericParameters = metadata.GetMethodDefinition(method).GetGenericParameters();
			if (index >= genericParameters.Count)
				return MetadataTokens.GenericParameterHandle(0);
			return genericParameters[index];
		}
	}
}

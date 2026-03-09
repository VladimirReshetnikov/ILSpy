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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a parameter decoded from metadata for a method, property accessor, or indexer signature.
	/// </summary>
	/// <remarks>
	/// The implementation delays expensive metadata decoding until members are queried and translates low-level flags/custom
	/// attributes into the higher-level parameter contract used by language output and semantic analysis.
	/// </remarks>
	sealed class MetadataParameter : IParameter
	{
		readonly MetadataModule module;
		readonly ParameterHandle handle;
		readonly ParameterAttributes attributes;

		public IType Type { get; }
		public IParameterizedMember Owner { get; }

		// lazy-loaded:
		string name;
		// these can't be bool? as bool? is not thread-safe from torn reads
		byte constantValueInSignatureState;
		byte decimalConstantState;

		/// <summary>
		/// Initializes a metadata-backed parameter wrapper.
		/// </summary>
		/// <param name="module">Metadata module providing table and custom-attribute access.</param>
		/// <param name="owner">Member that declares this parameter.</param>
		/// <param name="type">Decoded parameter type from signature metadata.</param>
		/// <param name="handle">Handle to the underlying parameter row.</param>
		internal MetadataParameter(MetadataModule module, IParameterizedMember owner, IType type, ParameterHandle handle)
		{
			this.module = module;
			this.Owner = owner;
			this.Type = type;
			this.handle = handle;

			var param = module.metadata.GetParameter(handle);
			this.attributes = param.Attributes;
			if (!IsOptional)
				decimalConstantState = ThreeState.False; // only optional parameters can be constants
		}

		/// <summary>
		/// Gets the metadata token for the underlying parameter row.
		/// </summary>
		public EntityHandle MetadataToken => handle;

		#region Attributes
		public IEnumerable<IAttribute> GetAttributes()
		{
			var b = new AttributeListBuilder(module);
			var metadata = module.metadata;
			var parameter = metadata.GetParameter(handle);

			bool defaultValueAssignmentAllowed = this.IsDefaultValueAssignmentAllowed();

			if (IsOptional && !defaultValueAssignmentAllowed)
			{
				b.Add(KnownAttribute.Optional);
			}

			if (!IsDecimalConstant && HasConstantValueInSignature && !defaultValueAssignmentAllowed)
			{
				b.Add(KnownAttribute.DefaultParameterValue, KnownTypeCode.Object, GetConstantValue(throwOnInvalidMetadata: false));
			}

			if ((attributes & ParameterAttributes.In) == ParameterAttributes.In && ReferenceKind is not (ReferenceKind.In or ReferenceKind.RefReadOnly))
				b.Add(KnownAttribute.In);
			if ((attributes & ParameterAttributes.Out) == ParameterAttributes.Out && ReferenceKind != ReferenceKind.Out)
				b.Add(KnownAttribute.Out);
			b.Add(parameter.GetCustomAttributes(), SymbolKind.Parameter);
			b.AddMarshalInfo(parameter.GetMarshallingDescriptor());

			return b.Build();
		}
		#endregion

		const ParameterAttributes inOut = ParameterAttributes.In | ParameterAttributes.Out;

		/// <summary>
		/// Gets the effective reference kind inferred from byref signature shape and known compiler attributes.
		/// </summary>
		public ReferenceKind ReferenceKind => DetectRefKind();

		/// <summary>
		/// Gets whether the metadata optional flag is set for this parameter.
		/// </summary>
		public bool IsOptional => (attributes & ParameterAttributes.Optional) != 0;

		ReferenceKind DetectRefKind()
		{
			if (Type.Kind != TypeKind.ByReference)
				return ReferenceKind.None;
			if ((attributes & inOut) == ParameterAttributes.Out)
				return ReferenceKind.Out;
			if ((module.TypeSystemOptions & TypeSystemOptions.ReadOnlyStructsAndParameters) != 0)
			{
				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				if (parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.IsReadOnly))
					return ReferenceKind.In;
			}
			if ((module.TypeSystemOptions & TypeSystemOptions.RefReadOnlyParameters) != 0
				&& (attributes & inOut) == ParameterAttributes.In)
			{
				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				if (parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.RequiresLocation))
					return ReferenceKind.RefReadOnly;
			}
			return ReferenceKind.Ref;
		}

		/// <summary>
		/// Gets the decoded lifetime annotation (for example scoped-ref metadata) when supported by the active type-system options.
		/// </summary>
		public LifetimeAnnotation Lifetime {
			get {
				if ((module.TypeSystemOptions & TypeSystemOptions.ScopedRef) == 0)
				{
					return default;
				}

				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				if ((module.TypeSystemOptions & TypeSystemOptions.ParamsCollections) != 0
					&& parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.ParamCollection))
				{
					// params collections are implicitly scoped
					return default;
				}
				if (parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.ScopedRef))
				{
					return new LifetimeAnnotation { ScopedRef = true };
				}
				return default;
			}
		}

		/// <summary>
		/// Gets whether this parameter is treated as a params parameter (array form or params-collection attribute form).
		/// </summary>
		public bool IsParams {
			get {
				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				if (Type.Kind == TypeKind.Array)
				{
					return parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.ParamArray);
				}
				if (module.TypeSystemOptions.HasFlag(TypeSystemOptions.ParamsCollections))
				{
					return parameterDef.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.ParamCollection);
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the metadata name of the parameter.
		/// </summary>
		public string Name {
			get {
				string name = LazyInit.VolatileRead(ref this.name);
				if (name != null)
					return name;
				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				return LazyInit.GetOrSet(ref this.name, metadata.GetString(parameterDef.Name));
			}
		}

		bool IVariable.IsConst => false;

		/// <summary>
		/// Gets the default constant value encoded for this parameter, if any.
		/// </summary>
		/// <param name="throwOnInvalidMetadata">
		/// <see langword="true"/> to surface malformed metadata as <see cref="BadImageFormatException"/>;
		/// <see langword="false"/> to return <see langword="null"/> on metadata decoding failures.
		/// </param>
		/// <returns>The decoded constant value, or <see langword="null"/> when no constant is present or decoding is suppressed.</returns>
		public object GetConstantValue(bool throwOnInvalidMetadata)
		{
			try
			{
				var metadata = module.metadata;
				var parameterDef = metadata.GetParameter(handle);
				if (IsDecimalConstant)
					return DecimalConstantHelper.GetDecimalConstantValue(module, parameterDef.GetCustomAttributes());

				var constantHandle = parameterDef.GetDefaultValue();
				if (constantHandle.IsNil)
					return null;

				var constant = metadata.GetConstant(constantHandle);
				var blobReader = metadata.GetBlobReader(constant.Value);
				try
				{
					return blobReader.ReadConstant(constant.TypeCode);
				}
				catch (ArgumentOutOfRangeException)
				{
					throw new BadImageFormatException($"Constant with invalid typecode: {constant.TypeCode}");
				}
			}
			catch (BadImageFormatException) when (!throwOnInvalidMetadata)
			{
				return null;
			}
		}

		/// <summary>
		/// Gets whether the parameter declaration carries an embedded default constant in metadata.
		/// </summary>
		public bool HasConstantValueInSignature {
			get {
				if (constantValueInSignatureState == ThreeState.Unknown)
				{
					if (IsDecimalConstant)
					{
						constantValueInSignatureState = ThreeState.From(DecimalConstantHelper.AllowsDecimalConstants(module));
					}
					else
					{
						constantValueInSignatureState = ThreeState.From(!module.metadata.GetParameter(handle).GetDefaultValue().IsNil);
					}
				}
				return constantValueInSignatureState == ThreeState.True;
			}
		}

		bool IsDecimalConstant {
			get {
				if (decimalConstantState == ThreeState.Unknown)
				{
					var parameterDef = module.metadata.GetParameter(handle);
					decimalConstantState = ThreeState.From(DecimalConstantHelper.IsDecimalConstant(module, parameterDef.GetCustomAttributes()));
				}
				return decimalConstantState == ThreeState.True;
			}
		}

		SymbolKind ISymbol.SymbolKind => SymbolKind.Parameter;

		public override string ToString()
		{
			return $"{MetadataTokens.GetToken(handle):X8} {DefaultParameter.ToString(this)}";
		}
	}
}

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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{

	/// <summary>
	/// Metadata-backed implementation of <see cref="ITypeParameter"/>.
	/// </summary>
	/// <remarks>
	/// This type decodes ECMA-335 generic parameter tables lazily and maps optional custom-attribute based constraints
	/// (for example unmanaged and nullable metadata) into the normalized constraint model expected by the decompiler type system.
	/// </remarks>
	sealed class MetadataTypeParameter : AbstractTypeParameter
	{
		readonly MetadataModule module;
		readonly GenericParameterHandle handle;

		readonly GenericParameterAttributes attr;

		// lazy-loaded:
		IReadOnlyList<TypeConstraint> constraints;
		byte unmanagedConstraint = ThreeState.Unknown;
		const byte nullabilityNotYetLoaded = 255;
		byte nullabilityConstraint = nullabilityNotYetLoaded;

		/// <summary>
		/// Creates type parameters for a nested metadata owner, reusing inherited outer type parameters when applicable.
		/// </summary>
		/// <param name="module">Metadata module containing the generic parameter records.</param>
		/// <param name="copyFromOuter">Declaring type whose existing type parameters should be reused for outer slots.</param>
		/// <param name="owner">Concrete owner for newly materialized type-parameter slots.</param>
		/// <param name="handles">Generic parameter handles in metadata order.</param>
		/// <returns>An array aligned to <paramref name="handles"/> where outer slots may be reused and inner slots are decoded from metadata.</returns>
		public static ITypeParameter[] Create(MetadataModule module, ITypeDefinition copyFromOuter, IEntity owner, GenericParameterHandleCollection handles)
		{
			if (handles.Count == 0)
				return Empty<ITypeParameter>.Array;
			var outerTps = copyFromOuter.TypeParameters;
			var tps = new ITypeParameter[handles.Count];
			int i = 0;
			foreach (var handle in handles)
			{
				if (i < outerTps.Count)
					tps[i] = outerTps[i];
				else
					tps[i] = Create(module, owner, i, handle);
				i++;
			}
			return tps;
		}

		/// <summary>
		/// Creates metadata-backed type parameters for a single generic owner.
		/// </summary>
		/// <param name="module">Metadata module containing the generic parameter records.</param>
		/// <param name="owner">Generic type or method that declares the parameters.</param>
		/// <param name="handles">Generic parameter handles in metadata order.</param>
		/// <returns>An array of decoded <see cref="MetadataTypeParameter"/> instances.</returns>
		public static ITypeParameter[] Create(MetadataModule module, IEntity owner, GenericParameterHandleCollection handles)
		{
			if (handles.Count == 0)
				return Empty<ITypeParameter>.Array;
			var tps = new ITypeParameter[handles.Count];
			int i = 0;
			foreach (var handle in handles)
			{
				tps[i] = Create(module, owner, i, handle);
				i++;
			}
			return tps;
		}

		/// <summary>
		/// Decodes a single metadata generic-parameter row.
		/// </summary>
		/// <param name="module">Metadata module containing the generic parameter record.</param>
		/// <param name="owner">Type or method that owns the parameter.</param>
		/// <param name="index">Expected zero-based position used for consistency checks.</param>
		/// <param name="handle">Handle of the generic parameter row.</param>
		/// <returns>A metadata-backed type parameter.</returns>
		public static MetadataTypeParameter Create(MetadataModule module, IEntity owner, int index, GenericParameterHandle handle)
		{
			var metadata = module.metadata;
			var gp = metadata.GetGenericParameter(handle);
			Debug.Assert(gp.Index == index);
			return new MetadataTypeParameter(module, owner, index, module.GetString(gp.Name), handle, gp.Attributes);
		}

		private MetadataTypeParameter(MetadataModule module, IEntity owner, int index, string name,
			GenericParameterHandle handle, GenericParameterAttributes attr)
			: base(owner, index, name, GetVariance(attr))
		{
			this.module = module;
			this.handle = handle;
			this.attr = attr;
		}

		private static VarianceModifier GetVariance(GenericParameterAttributes attr)
		{
			switch (attr & GenericParameterAttributes.VarianceMask)
			{
				case GenericParameterAttributes.Contravariant:
					return VarianceModifier.Contravariant;
				case GenericParameterAttributes.Covariant:
					return VarianceModifier.Covariant;
				default:
					return VarianceModifier.Invariant;
			}
		}

		/// <summary>
		/// Gets the metadata token for the underlying generic parameter row.
		/// </summary>
		public GenericParameterHandle MetadataToken => handle;

		public override IEnumerable<IAttribute> GetAttributes()
		{
			var metadata = module.metadata;
			var gp = metadata.GetGenericParameter(handle);

			var attributes = gp.GetCustomAttributes();
			var b = new AttributeListBuilder(module, attributes.Count);
			b.Add(attributes, SymbolKind.TypeParameter);
			return b.Build();
		}

		public override bool HasDefaultConstructorConstraint => (attr & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
		public override bool HasReferenceTypeConstraint => (attr & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
		public override bool HasValueTypeConstraint => (attr & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
		public override bool AllowsRefLikeType => (attr & SRMExtensions.AllowByRefLike) != 0;

		/// <summary>
		/// Gets whether the parameter carries the unmanaged constraint marker recognized by the configured type-system feature set.
		/// </summary>
		public override bool HasUnmanagedConstraint {
			get {
				if (unmanagedConstraint == ThreeState.Unknown)
				{
					unmanagedConstraint = ThreeState.From(LoadUnmanagedConstraint());
				}
				return unmanagedConstraint == ThreeState.True;
			}
		}

		private bool LoadUnmanagedConstraint()
		{
			if ((module.TypeSystemOptions & TypeSystemOptions.UnmanagedConstraints) == 0)
				return false;
			var metadata = module.metadata;
			var gp = metadata.GetGenericParameter(handle);
			return gp.GetCustomAttributes().HasKnownAttribute(metadata, KnownAttribute.IsUnmanaged);
		}

		/// <summary>
		/// Gets the nullable annotation constraint resolved from explicit <c>NullableAttribute</c> payloads or nullable context fallback.
		/// </summary>
		public override Nullability NullabilityConstraint {
			get {
				if (nullabilityConstraint == nullabilityNotYetLoaded)
				{
					nullabilityConstraint = (byte)LoadNullabilityConstraint();
				}
				return (Nullability)nullabilityConstraint;
			}
		}

		Nullability LoadNullabilityConstraint()
		{
			if (!module.ShouldDecodeNullableAttributes(Owner))
				return Nullability.Oblivious;

			var metadata = module.metadata;
			var gp = metadata.GetGenericParameter(handle);

			foreach (var handle in gp.GetCustomAttributes())
			{
				var customAttribute = metadata.GetCustomAttribute(handle);
				if (customAttribute.IsKnownAttribute(metadata, KnownAttribute.Nullable))
				{
					var attrVal = customAttribute.DecodeValue(module.TypeProvider);
					if (attrVal.FixedArguments.Length == 1)
					{
						if (attrVal.FixedArguments[0].Value is byte b && b <= 2)
						{
							return (Nullability)b;
						}
					}
				}
			}
			if (Owner is MetadataMethod method)
			{
				return method.NullableContext;
			}
			else if (Owner is ITypeDefinition td)
			{
				return td.NullableContext;
			}
			else
			{
				return Nullability.Oblivious;
			}
		}

		/// <summary>
		/// Gets decoded type constraints with synthesized object/value-type anchors when metadata omits an explicit non-interface base.
		/// </summary>
		public override IReadOnlyList<TypeConstraint> TypeConstraints {
			get {
				var constraints = LazyInit.VolatileRead(ref this.constraints);
				if (constraints == null)
				{
					constraints = LazyInit.GetOrSet(ref this.constraints, DecodeConstraints());
				}
				return constraints;
			}
		}

		private IReadOnlyList<TypeConstraint> DecodeConstraints()
		{
			var metadata = module.metadata;
			var gp = metadata.GetGenericParameter(handle);
			Nullability nullableContext;
			if (Owner is ITypeDefinition typeDef)
			{
				nullableContext = typeDef.NullableContext;
			}
			else if (Owner is MetadataMethod method)
			{
				nullableContext = method.NullableContext;
			}
			else
			{
				nullableContext = Nullability.Oblivious;
			}

			var constraintHandleCollection = gp.GetConstraints();
			var result = new List<TypeConstraint>(constraintHandleCollection.Count + 1);
			bool hasNonInterfaceConstraint = false;
			foreach (var constraintHandle in constraintHandleCollection)
			{
				var constraint = metadata.GetGenericParameterConstraint(constraintHandle);
				var attrs = constraint.GetCustomAttributes();
				var ty = module.ResolveType(constraint.Type, new GenericContext(Owner), attrs, nullableContext);
				if (attrs.Count == 0)
				{
					result.Add(new TypeConstraint(ty));
				}
				else
				{
					AttributeListBuilder b = new AttributeListBuilder(module);
					b.Add(attrs, SymbolKind.Constraint);
					result.Add(new TypeConstraint(ty, b.Build()));
				}
				hasNonInterfaceConstraint |= (ty.Kind != TypeKind.Interface);
			}
			if (this.HasValueTypeConstraint)
			{
				result.Add(new TypeConstraint(Compilation.FindType(KnownTypeCode.ValueType)));
			}
			else if (!hasNonInterfaceConstraint)
			{
				result.Add(new TypeConstraint(Compilation.FindType(KnownTypeCode.Object)));
			}
			return result;
		}

		public override int GetHashCode()
		{
			return 0x51fc5b83 ^ module.MetadataFile.GetHashCode() ^ handle.GetHashCode();
		}

		public override bool Equals(IType other)
		{
			return other is MetadataTypeParameter tp && handle == tp.handle && module.MetadataFile == tp.module.MetadataFile;
		}

		public override string ToString()
		{
			return $"{MetadataTokens.GetToken(handle):X8} {ReflectionName}";
		}
	}
}
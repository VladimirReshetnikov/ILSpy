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
using System.Linq;
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

		/// <summary>
		/// The custom modifiers on this parameter's type that C# has no syntax for.
		/// </summary>
		public IReadOnlyList<string> ErasedModifiers { get; set; } = Empty<string>.Array;
		public IParameterizedMember Owner { get; }

		// lazy-loaded:
		string name;
		// these can't be bool? as bool? is not thread-safe from torn reads
		byte constantValueInSignatureState;
		byte decimalConstantState;
		// (ReferenceKind + 1), 0 while not yet computed; a byte for the same torn-read reason
		byte referenceKindState;

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

		/// <summary>
		/// Returns whether the signature default can be written as a DefaultParameterValue argument.
		/// Visual Basic stores an optional parameter's default as a null constant even where the
		/// parameter is a value type; C# requires the argument to be implicitly convertible to the
		/// parameter type (CS1908), so a null default is representable only for reference types and
		/// Nullable&lt;T&gt; - anywhere else (value types, unconstrained type parameters) it has no C#
		/// spelling and is dropped rather than emitted unusably.
		/// </summary>
		bool CanTypeDefaultValue(object constantValue)
		{
			if (constantValue != null)
				return true;
			var parameterType = UnwrapByReference(Type);
			return parameterType.IsReferenceType == true
				|| parameterType.IsKnownType(KnownTypeCode.NullableOfT);
		}

		static IType UnwrapByReference(IType type)
		{
			return type is ByReferenceType byReference ? byReference.ElementType : type;
		}

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
				object constantValue = GetConstantValue(throwOnInvalidMetadata: false);
				// The signature default is stored as the constant's element type (e.g. int 0 for an
				// enum or a small-integer parameter). C# requires a DefaultParameterValue argument's
				// type to match the parameter type (CS1908), so for those parameter types type the
				// synthesized argument as the parameter type - the value then renders as (MyEnum)0 /
				// (short)3 instead of a bare int literal. Other parameter types are not affected.
				var parameterType = UnwrapByReference(Type);
				if (constantValue != null && (parameterType.Kind == TypeKind.Enum || parameterType.IsCSharpSmallIntegerType()))
					b.Add(KnownAttribute.DefaultParameterValue, parameterType, constantValue);
				else if (CanTypeDefaultValue(constantValue))
					b.Add(KnownAttribute.DefaultParameterValue, KnownTypeCode.Object, constantValue);
			}

			if ((attributes & ParameterAttributes.In) == ParameterAttributes.In && ReferenceKind is not (ReferenceKind.In or ReferenceKind.RefReadOnly))
				b.Add(KnownAttribute.In);
			if ((attributes & ParameterAttributes.Out) == ParameterAttributes.Out && ReferenceKind != ReferenceKind.Out
				&& (Type.Kind != TypeKind.ByReference || (attributes & ParameterAttributes.In) == ParameterAttributes.In))
			{
				// A bare [Out] on a byref parameter is not valid C# (CS0662 requires [In] alongside it).
				// Surface the flag only where it compiles: on a by-value parameter, or on a byref parameter
				// that also carries [In] (rendered as '[In, Out] ref'). A byref parameter flagged [Out] only,
				// such as a Visual Basic ByRef override of a C# 'ref', drops the otherwise uncompilable flag.
				b.Add(KnownAttribute.Out);
			}
			b.Add(parameter.GetCustomAttributes(), SymbolKind.Parameter);
			b.AddMarshalInfo(parameter.GetMarshallingDescriptor());

			return b.Build();
		}
		#endregion

		const ParameterAttributes inOut = ParameterAttributes.In | ParameterAttributes.Out;

		/// <summary>
		/// Gets the effective reference kind inferred from byref signature shape and known compiler attributes.
		/// </summary>
		public ReferenceKind ReferenceKind {
			get {
				// Cached because DetectRefKind can walk the owner's base members and interfaces
				// (see ContractParameterIs), which is far too expensive to repeat on every read.
				// Racing threads compute the same value, so an unsynchronized overwrite is fine;
				// a reentrant read during the walk recomputes instead of seeing a torn state.
				byte state = referenceKindState;
				if (state != 0)
					return (ReferenceKind)(state - 1);
				ReferenceKind kind = DetectRefKind();
				referenceKindState = (byte)((byte)kind + 1);
				return kind;
			}
		}

		/// <summary>
		/// Gets whether the metadata optional flag is set for this parameter.
		/// </summary>
		public bool IsOptional => (attributes & ParameterAttributes.Optional) != 0;

		ReferenceKind DetectRefKind()
		{
			if (Type.Kind != TypeKind.ByReference)
				return ReferenceKind.None;
			if ((attributes & inOut) == ParameterAttributes.Out)
			{
				// A byref parameter flagged [Out] normally renders 'out'. The Visual Basic compiler,
				// however, also emits [Out] on a ByRef parameter that overrides or implements a C# 'ref'
				// parameter. Rendering it 'out' there breaks the override, because C# requires the override
				// to match the base member's ref-kind (CS0115). When the contract declares the parameter
				// 'ref', follow the contract. The [Out] flag itself is then dropped rather than written out,
				// because [Out] without [In] on a byref parameter is not valid C# either (see GetAttributes).
				// Only when the contracts AGREE on 'ref'. A type can reach several contracts that
				// disagree - an interface declaring the parameter 'ref' and a base class declaring
				// it 'out' - and then the metadata flag is the only statement about this member
				// itself, so it stands.
				var contracts = GetContractReferenceKinds();
				if (contracts == ContractKinds.Ref)
					return ReferenceKind.Ref;
				return ReferenceKind.Out;
			}
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
			// A Visual Basic ByRef parameter that implements or overrides an 'out' parameter carries no
			// ParameterAttributes.Out flag, because the VB compiler does not emit one. Without the flag it
			// would render as 'ref' and no longer match the overridden/implemented 'out' member, so the
			// override fails to bind (CS0115/CS0534/CS0539). Recover the direction from the contract.
			// Same rule in the other direction: recover 'out' from the contracts only where they
			// agree on it.
			if (GetContractReferenceKinds() == ContractKinds.Out)
				return ReferenceKind.Out;
			return ReferenceKind.Ref;
		}

		[Flags]
		enum ContractKinds
		{
			None = 0,
			Ref = 1,
			Out = 2,
		}

		/// <summary>
		/// Returns which reference kinds the corresponding parameter carries across every member that
		/// this parameter's owning method implements or overrides. 'out' and 'ref' share the same byref
		/// signature, so the contracts are what distinguish them when the implementation's own [Out]
		/// flag disagrees. Several contracts can apply at once and can contradict each other, so all of
		/// them are collected: only a unanimous answer is allowed to overrule the metadata flag.
		/// </summary>
		ContractKinds GetContractReferenceKinds()
		{
			if (Owner is not IMethod method || handle.IsNil)
				return ContractKinds.None;
			// Reading a contract parameter's ReferenceKind asks this same question of that parameter,
			// and metadata can name an interface that inherits from one inheriting it back. The base
			// type walk is finite, but the question would keep bouncing between the two members, so
			// stop as soon as it comes back around: an unanswerable contract constrains nothing.
			using var busyLock = BusyManager.Enter(this);
			if (!busyLock.Success)
				return ContractKinds.None;
			int parameterIndex = module.metadata.GetParameter(handle).SequenceNumber - 1;
			if (parameterIndex < 0)
				return ContractKinds.None;
			ContractKinds found = ContractKinds.None;
			ContractKinds KindOf(IMember contract)
			{
				if (contract is not IMethod contractMethod || parameterIndex >= contractMethod.Parameters.Count)
					return ContractKinds.None;
				return contractMethod.Parameters[parameterIndex].ReferenceKind switch {
					ReferenceKind.Ref => ContractKinds.Ref,
					ReferenceKind.Out => ContractKinds.Out,
					_ => ContractKinds.None
				};
			}
			// Direct interface-implementation links cover Visual Basic 'Implements' (renamed or not)
			// and C#-style explicit implementations; they name the implemented member outright, so
			// they are authoritative.
			foreach (var contract in method.ExplicitlyImplementedInterfaceMembers)
			{
				found |= KindOf(contract);
			}
			// GetBaseMembers additionally covers overrides and implicit same-signature interface
			// implementations. It matches by name and byref-insensitive signature alone, so each
			// candidate still has to be checked for actually constraining this method.
			foreach (var contract in InheritanceHelper.GetBaseMembers(method, includeImplementedInterfaces: true))
			{
				var kind = KindOf(contract);
				if (kind == ContractKinds.None)
					continue;
				if (contract.DeclaringType.Kind == TypeKind.Interface)
				{
					// A signature match alone does not make this method the implementation of the
					// interface member: the runtime maps the slot to a same-signature public method
					// only when no explicit implementation claims it. Without this check, a method
					// that merely shares the name and byref shape (ref and out compare equal in
					// signatures) would have its ref-kind flipped by an interface member that is
					// actually implemented by a sibling.
					if (method.Accessibility != Accessibility.Public)
						continue;
					if (IsExplicitlyImplementedInDeclaringType(contract))
						continue;
					found |= kind;
					continue;
				}
				// GetBaseMembers also returns a base class member that this method merely hides
				// (a 'new' member). Hiding does not require a matching ref-kind, so a hidden
				// base member is not a contract: only a genuine override or an interface member
				// constrains the parameter direction. Skip a hidden non-interface base member.
				if (!method.IsOverride)
					continue;
				found |= kind;
			}
			return found;
		}

		/// <summary>
		/// Returns true if any member of the owning method's declaring type explicitly implements
		/// the given interface member, i.e. the member's implementation slot is already taken and
		/// cannot belong to a merely signature-matching method.
		/// </summary>
		bool IsExplicitlyImplementedInDeclaringType(IMember interfaceMember)
		{
			var declaringType = Owner.DeclaringTypeDefinition;
			if (declaringType == null)
				return false;
			var interfaceMemberDefinition = interfaceMember.MemberDefinition;
			IEnumerable<IMethod> candidates = Owner.SymbolKind == SymbolKind.Accessor
				? declaringType.GetAccessors(options: GetMemberOptions.IgnoreInheritedMembers)
				: declaringType.GetMethods(options: GetMemberOptions.IgnoreInheritedMembers);
			foreach (var candidate in candidates)
			{
				if (!candidate.IsExplicitInterfaceImplementation)
					continue;
				foreach (var implemented in candidate.ExplicitlyImplementedInterfaceMembers)
				{
					if (interfaceMemberDefinition.Equals(implemented.MemberDefinition))
						return true;
				}
			}
			return false;
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

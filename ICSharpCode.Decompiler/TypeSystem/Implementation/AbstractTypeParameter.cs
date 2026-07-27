// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Globalization;
using System.Linq;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Provides shared behavior for <see cref="ITypeParameter"/> implementations that participate in member lookup and constraint evaluation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This base type centralizes the expensive derived views of generic constraints (effective base class and effective interface set)
	/// and protects those computations against cycles introduced by malformed metadata or self-referential constraints.
	/// </para>
	/// <para>
	/// Concrete implementations only supply raw metadata/project data (constraint flags, attributes, and explicit constraint list);
	/// <see cref="AbstractTypeParameter"/> then exposes the normalized type-system contract consumed by lookup and decompilation stages.
	/// </para>
	/// </remarks>
	public abstract class AbstractTypeParameter : ITypeParameter, ICompilationProvider
	{
		readonly ICompilation compilation;
		readonly SymbolKind ownerType;
		readonly IEntity owner;
		readonly int index;
		readonly string name;
		readonly VarianceModifier variance;

		/// <summary>
		/// Initializes a type parameter that is owned by an existing entity symbol.
		/// </summary>
		/// <param name="owner">Owning method or type.</param>
		/// <param name="index">Zero-based generic parameter index on <paramref name="owner"/>.</param>
		/// <param name="name">Declared name, or <see langword="null"/> to synthesize a metadata-style fallback (<c>!0</c>/<c>!!0</c>).</param>
		/// <param name="variance">Declared variance annotation for this parameter.</param>
		/// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
		protected AbstractTypeParameter(IEntity owner, int index, string name, VarianceModifier variance)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			this.owner = owner;
			this.compilation = owner.Compilation;
			this.ownerType = owner.SymbolKind;
			this.index = index;
			this.name = name ?? ((this.OwnerType == SymbolKind.Method ? "!!" : "!") + index.ToString(CultureInfo.InvariantCulture));
			this.variance = variance;
		}

		/// <summary>
		/// Initializes a type parameter from compilation context when a concrete owner symbol is not available.
		/// </summary>
		/// <param name="compilation">Compilation used for known-type lookups and symbol identity.</param>
		/// <param name="ownerType">Kind of symbol that conceptually owns this type parameter.</param>
		/// <param name="index">Zero-based generic parameter index in the owner scope.</param>
		/// <param name="name">Declared name, or <see langword="null"/> to synthesize a metadata-style fallback (<c>!0</c>/<c>!!0</c>).</param>
		/// <param name="variance">Declared variance annotation for this parameter.</param>
		/// <exception cref="ArgumentNullException"><paramref name="compilation"/> is <see langword="null"/>.</exception>
		protected AbstractTypeParameter(ICompilation compilation, SymbolKind ownerType, int index, string name, VarianceModifier variance)
		{
			if (compilation == null)
				throw new ArgumentNullException(nameof(compilation));
			this.compilation = compilation;
			this.ownerType = ownerType;
			this.index = index;
			this.name = name ?? ((this.OwnerType == SymbolKind.Method ? "!!" : "!") + index.ToString(CultureInfo.InvariantCulture));
			this.variance = variance;
		}

		SymbolKind ISymbol.SymbolKind {
			get { return SymbolKind.TypeParameter; }
		}

		/// <summary>
		/// Gets the symbol kind of the owner that declares this type parameter.
		/// </summary>
		public SymbolKind OwnerType {
			get { return ownerType; }
		}

		/// <summary>
		/// Gets the owning entity when this type parameter was created from a concrete declaration; otherwise <see langword="null"/>.
		/// </summary>
		public IEntity Owner {
			get { return owner; }
		}

		/// <summary>
		/// Gets the zero-based position of the type parameter in its declaring generic parameter list.
		/// </summary>
		public int Index {
			get { return index; }
		}

		public abstract IEnumerable<IAttribute> GetAttributes();

		/// <summary>
		/// Gets the variance modifier decoded from the declaration.
		/// </summary>
		public VarianceModifier Variance {
			get { return variance; }
		}

		/// <summary>
		/// Gets the compilation that provides known-type resolution for this symbol.
		/// </summary>
		public ICompilation Compilation {
			get { return compilation; }
		}

		volatile IType effectiveBaseClass;

		/// <summary>
		/// Gets the effective base class implied by the declared constraint set.
		/// </summary>
		/// <remarks>
		/// This value normalizes missing base constraints to <c>System.Object</c>, maps <c>struct</c>/<c>valuetype</c>
		/// constraints to <c>System.ValueType</c>, and follows transitive type-parameter constraints. During cycle detection
		/// failures, the property returns <see cref="SpecialType.UnknownType"/> and avoids caching that temporary error result.
		/// </remarks>
		public IType EffectiveBaseClass {
			get {
				if (effectiveBaseClass == null)
				{
					// protect against cyclic type parameters
					using (var busyLock = BusyManager.Enter(this))
					{
						if (!busyLock.Success)
							return SpecialType.UnknownType; // don't cache this error
						effectiveBaseClass = CalculateEffectiveBaseClass();
					}
				}
				return effectiveBaseClass;
			}
		}

		IType CalculateEffectiveBaseClass()
		{
			if (HasValueTypeConstraint)
				return this.Compilation.FindType(KnownTypeCode.ValueType);

			List<IType> classTypeConstraints = new List<IType>();
			foreach (IType constraint in this.DirectBaseTypes)
			{
				if (constraint.Kind == TypeKind.Class)
				{
					classTypeConstraints.Add(constraint);
				}
				else if (constraint.Kind == TypeKind.TypeParameter)
				{
					IType baseClass = ((ITypeParameter)constraint).EffectiveBaseClass;
					if (baseClass.Kind == TypeKind.Class)
						classTypeConstraints.Add(baseClass);
				}
			}
			if (classTypeConstraints.Count == 0)
				return this.Compilation.FindType(KnownTypeCode.Object);
			// Find the derived-most type in the resulting set:
			IType result = classTypeConstraints[0];
			for (int i = 1; i < classTypeConstraints.Count; i++)
			{
				if (classTypeConstraints[i].GetDefinition().IsDerivedFrom(result.GetDefinition()))
					result = classTypeConstraints[i];
			}
			return result;
		}

		IReadOnlyCollection<IType> effectiveInterfaceSet;

		/// <summary>
		/// Gets the transitive set of effective interface constraints.
		/// </summary>
		/// <remarks>
		/// The set includes directly constrained interfaces and interfaces inherited through constrained type parameters.
		/// If a cyclic dependency is encountered while evaluating constraints, an empty set is returned for that read and not cached.
		/// </remarks>
		public IReadOnlyCollection<IType> EffectiveInterfaceSet {
			get {
				var result = LazyInit.VolatileRead(ref effectiveInterfaceSet);
				if (result != null)
				{
					return result;
				}
				else
				{
					// protect against cyclic type parameters
					using (var busyLock = BusyManager.Enter(this))
					{
						if (!busyLock.Success)
							return EmptyList<IType>.Instance; // don't cache this error
						return LazyInit.GetOrSet(ref effectiveInterfaceSet, CalculateEffectiveInterfaceSet());
					}
				}
			}
		}

		IReadOnlyCollection<IType> CalculateEffectiveInterfaceSet()
		{
			HashSet<IType> result = new HashSet<IType>();
			foreach (IType constraint in this.DirectBaseTypes)
			{
				if (constraint.Kind == TypeKind.Interface)
				{
					result.Add(constraint);
				}
				else if (constraint.Kind == TypeKind.TypeParameter)
				{
					result.UnionWith(((ITypeParameter)constraint).EffectiveInterfaceSet);
				}
			}
			return result.ToArray();
		}

		/// <summary>
		/// Gets whether the parameter includes a default-constructor (<c>new()</c>) constraint.
		/// </summary>
		public abstract bool HasDefaultConstructorConstraint { get; }

		/// <summary>
		/// Gets whether the parameter includes a reference-type (<c>class</c>) constraint.
		/// </summary>
		public abstract bool HasReferenceTypeConstraint { get; }

		/// <summary>
		/// Gets whether the parameter includes a non-nullable value-type (<c>struct</c>) constraint.
		/// </summary>
		public abstract bool HasValueTypeConstraint { get; }

		/// <summary>
		/// Gets whether the parameter includes an unmanaged constraint.
		/// </summary>
		public abstract bool HasUnmanagedConstraint { get; }

		/// <summary>
		/// Gets whether the parameter permits byref-like arguments via the runtime-specific allow-byref-like metadata marker.
		/// </summary>
		public abstract bool AllowsRefLikeType { get; }

		/// <summary>
		/// Gets the nullable-reference-type constraint flavor associated with this parameter.
		/// </summary>
		public abstract Nullability NullabilityConstraint { get; }

		/// <summary>
		/// Gets the static type kind represented by every type-parameter symbol.
		/// </summary>
		public TypeKind Kind {
			get { return TypeKind.TypeParameter; }
		}

		/// <summary>
		/// Gets whether this type parameter is known to be a reference type.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> when constraints guarantee reference-type semantics, <see langword="false"/> when constraints
		/// guarantee value-type semantics, or <see langword="null"/> when neither can be proven.
		/// </returns>
		public bool? IsReferenceType {
			get {
				if (this.HasValueTypeConstraint)
					return false;
				if (this.HasReferenceTypeConstraint)
					return true;

				// A type parameter is known to be a reference type if it has the reference type constraint
				// or its effective base class is not object or System.ValueType.
				IType effectiveBaseClass = this.EffectiveBaseClass;
				if (effectiveBaseClass.Kind == TypeKind.Class || effectiveBaseClass.Kind == TypeKind.Delegate)
				{
					ITypeDefinition effectiveBaseClassDef = effectiveBaseClass.GetDefinition();
					if (effectiveBaseClassDef != null)
					{
						switch (effectiveBaseClassDef.KnownTypeCode)
						{
							case KnownTypeCode.Object:
							case KnownTypeCode.ValueType:
							case KnownTypeCode.Enum:
								return null;
						}
					}
					return true;
				}
				else if (effectiveBaseClass.Kind == TypeKind.Struct || effectiveBaseClass.Kind == TypeKind.Enum)
				{
					return false;
				}
				return null;
			}
		}

		bool IType.IsByRefLike => false;
		Nullability IType.Nullability => Nullability.Oblivious;

		/// <summary>
		/// Applies a nullable annotation wrapper to this type parameter.
		/// </summary>
		/// <param name="nullability">Target nullability annotation.</param>
		/// <returns>
		/// This instance when <paramref name="nullability"/> is <see cref="Nullability.Oblivious"/>;
		/// otherwise a <see cref="NullabilityAnnotatedTypeParameter"/> wrapping this symbol.
		/// </returns>
		public IType ChangeNullability(Nullability nullability)
		{
			if (nullability == Nullability.Oblivious)
				return this;
			else
				return new NullabilityAnnotatedTypeParameter(this, nullability);
		}

		IType IType.DeclaringType {
			get { return null; }
		}

		int IType.TypeParameterCount {
			get { return 0; }
		}

		IReadOnlyList<ITypeParameter> IType.TypeParameters {
			get { return EmptyList<ITypeParameter>.Instance; }
		}

		IReadOnlyList<IType> IType.TypeArguments {
			get { return EmptyList<IType>.Instance; }
		}

		/// <summary>
		/// Gets direct base constraints projected as types.
		/// </summary>
		public IEnumerable<IType> DirectBaseTypes {
			get { return TypeConstraints.Select(t => t.Type); }
		}

		public abstract IReadOnlyList<TypeConstraint> TypeConstraints { get; }

		/// <summary>
		/// Gets the raw declared name of the type parameter.
		/// </summary>
		public string Name {
			get { return name; }
		}

		string INamedElement.Namespace {
			get { return string.Empty; }
		}

		string INamedElement.FullName {
			get { return name; }
		}

		/// <summary>
		/// Gets the reflection-style placeholder name (<c>`0</c> or <c>``0</c>) used in signature rendering.
		/// </summary>
		public string ReflectionName {
			get {
				return (this.OwnerType == SymbolKind.Method ? "``" : "`") + index.ToString(CultureInfo.InvariantCulture);
			}
		}

		ITypeDefinition IType.GetDefinition()
		{
			return null;
		}

		ITypeDefinitionOrUnknown IType.GetDefinitionOrUnknown()
		{
			return null;
		}

		/// <summary>
		/// Dispatches to <see cref="TypeVisitor.VisitTypeParameter"/>.
		/// </summary>
		/// <param name="visitor">Visitor receiving this symbol.</param>
		/// <returns>The value returned by <paramref name="visitor"/>.</returns>
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeParameter(this);
		}

		/// <summary>
		/// Returns this instance because type parameters do not contain nested child types.
		/// </summary>
		/// <param name="visitor">Unused visitor argument for interface compatibility.</param>
		/// <returns>The current instance.</returns>
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}

		IEnumerable<IType> IType.GetNestedTypes(Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}

		IEnumerable<IType> IType.GetNestedTypes(IReadOnlyList<IType> typeArguments, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return EmptyList<IType>.Instance;
		}

		/// <summary>
		/// Gets constructors visible on the effective base type and from constructor constraints.
		/// </summary>
		/// <param name="filter">Optional predicate used to filter resulting methods.</param>
		/// <param name="options">Lookup options controlling inherited-member traversal.</param>
		/// <returns>
		/// A single synthesized default constructor when <see cref="HasDefaultConstructorConstraint"/> or
		/// <see cref="HasValueTypeConstraint"/> is present and inherited members are ignored; otherwise constructors resolved
		/// from the effective base type hierarchy.
		/// </returns>
		public IEnumerable<IMethod> GetConstructors(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
			{
				if (this.HasDefaultConstructorConstraint || this.HasValueTypeConstraint)
				{
					var dummyCtor = FakeMethod.CreateDummyConstructor(compilation, this);
					if (filter == null || filter(dummyCtor))
					{
						return new[] { dummyCtor };
					}
				}
				return EmptyList<IMethod>.Instance;
			}
			else
			{
				return GetMembersHelper.GetConstructors(this, filter, options);
			}
		}

		/// <summary>
		/// Gets instance methods available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IMethod> GetMethods(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets instance methods available through this type parameter's effective constraints using explicit type arguments.
		/// </summary>
		public IEnumerable<IMethod> GetMethods(IReadOnlyList<IType> typeArguments, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetMethods(this, typeArguments, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets instance properties available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IProperty> GetProperties(Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IProperty>.Instance;
			else
				return GetMembersHelper.GetProperties(this, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets instance fields available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IField> GetFields(Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IField>.Instance;
			else
				return GetMembersHelper.GetFields(this, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets instance events available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IEvent> GetEvents(Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IEvent>.Instance;
			else
				return GetMembersHelper.GetEvents(this, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets instance members available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IMember> GetMembers(Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMember>.Instance;
			else
				return GetMembersHelper.GetMembers(this, FilterNonStatic(filter), options);
		}

		/// <summary>
		/// Gets property/event accessor methods available through this type parameter's effective constraints.
		/// </summary>
		public IEnumerable<IMethod> GetAccessors(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return GetMembersHelper.GetAccessors(this, FilterNonStatic(filter), options);
		}

		TypeParameterSubstitution IType.GetSubstitution()
		{
			return TypeParameterSubstitution.Identity;
		}

		static Predicate<T> FilterNonStatic<T>(Predicate<T> filter) where T : class, IMember
		{
			return member => (!member.IsStatic || member.SymbolKind == SymbolKind.Operator || member.IsVirtual)
				&& (filter == null || filter(member));
		}

		public sealed override bool Equals(object obj)
		{
			return Equals(obj as IType);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Compares type parameters by reference identity unless overridden by concrete implementations.
		/// </summary>
		/// <param name="other">Type to compare with this instance.</param>
		/// <returns><see langword="true"/> when both references identify the same type-parameter object.</returns>
		public virtual bool Equals(IType other)
		{
			return this == other; // use reference equality for type parameters
		}

		public override string ToString()
		{
			return this.ReflectionName;
		}
	}
}

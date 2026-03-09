using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Decorates an <see cref="IType"/> with an explicit C# nullable reference annotation state.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This wrapper exists for types that are represented by reference-like symbols in metadata and therefore
	/// can carry nullable context information even when the underlying symbol identity is unchanged.
	/// </para>
	/// <para>
	/// Arrays are intentionally excluded from this decorator pattern in ILSpy: <see cref="ArrayType"/> stores
	/// nullability directly because element and array nullability must both be represented and transformed together.
	/// </para>
	/// </remarks>
	public class NullabilityAnnotatedType : DecoratedType, IType
	{
		readonly Nullability nullability;

		/// <summary>
		/// Creates a nullability-annotated wrapper around an oblivious base type.
		/// </summary>
		/// <param name="type">The type being annotated.</param>
		/// <param name="nullability">The explicit nullability state to project onto <paramref name="type"/>.</param>
		/// <remarks>
		/// The constructor is internal because ILSpy only inserts this wrapper in locations where consumers are known
		/// to handle decorated types correctly. Many code paths still rely on direct casts to concrete type
		/// implementations and would misbehave if wrappers were introduced indiscriminately.
		/// </remarks>
		internal NullabilityAnnotatedType(IType type, Nullability nullability)
			: base(type)
		{
			Debug.Assert(type.Nullability == Nullability.Oblivious);
			Debug.Assert(nullability != Nullability.Oblivious);
			// Due to IType -> concrete type casts all over the type system, we can insert
			// the NullabilityAnnotatedType wrapper only in some limited places.
			Debug.Assert(type is ITypeDefinition { IsReferenceType: not false }
				|| type.Kind == TypeKind.Dynamic
				|| type.Kind == TypeKind.Unknown
				|| (type is ITypeParameter && this is ITypeParameter));
			this.nullability = nullability;
		}

		/// <summary>
		/// Gets the nullability value introduced by this wrapper.
		/// </summary>
		public Nullability Nullability => nullability;

		/// <summary>
		/// Gets the undecorated type that this instance wraps.
		/// </summary>
		/// <remarks>
		/// This is the canonical escape hatch for normalization visitors that intentionally strip nullable annotations,
		/// such as signature-comparison paths.
		/// </remarks>
		public IType TypeWithoutAnnotation => baseType;

		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitNullabilityAnnotatedType(this);
		}

		public override bool Equals(IType other)
		{
			return other is NullabilityAnnotatedType nat
				&& nat.nullability == nullability
				&& nat.baseType.Equals(baseType);
		}

		public override IType ChangeNullability(Nullability nullability)
		{
			if (nullability == this.nullability)
				return this;
			else
				return baseType.ChangeNullability(nullability);
		}

		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType newBase = baseType.AcceptVisitor(visitor);
			if (newBase != baseType)
			{
				if (newBase.Nullability == Nullability.Nullable)
				{
					// `T!` with substitution T=`U?` becomes `U?`
					// This happens during type substitution for generic methods.
					return newBase;
				}
				if (newBase.Kind == TypeKind.TypeParameter || newBase.IsReferenceType == true)
				{
					return newBase.ChangeNullability(nullability);
				}
				else
				{
					// `T!` with substitution T=`int` becomes `int`, not `int!`
					return newBase;
				}
			}
			else
			{
				return this;
			}
		}

		/// <summary>
		/// Returns a debug-oriented textual form with C# nullable suffixes.
		/// </summary>
		/// <returns>
		/// <list type="bullet">
		/// <item><description><c>&lt;base&gt;?</c> when <see cref="Nullability"/> is <see cref="TypeSystem.Nullability.Nullable"/>.</description></item>
		/// <item><description><c>&lt;base&gt;!</c> when <see cref="Nullability"/> is <see cref="TypeSystem.Nullability.NotNullable"/>.</description></item>
		/// <item><description><c>&lt;base&gt;~</c> for the fallback oblivious representation used by diagnostics.</description></item>
		/// </list>
		/// </returns>
		public override string ToString()
		{
			switch (nullability)
			{
				case Nullability.Nullable:
					return $"{baseType.ToString()}?";
				case Nullability.NotNullable:
					return $"{baseType.ToString()}!";
				default:
					Debug.Assert(nullability == Nullability.Oblivious);
					return $"{baseType.ToString()}~";
			}
		}
	}

	/// <summary>
	/// Nullability-aware wrapper for an <see cref="ITypeParameter"/>.
	/// </summary>
	/// <remarks>
	/// Type parameters expose additional contract members (<see cref="ITypeParameter.Owner"/>, constraints,
	/// variance) that must remain reachable after decoration. This type keeps those members forwarded to the
	/// original parameter while still participating in visitor-based nullability transformations.
	/// </remarks>
	public sealed class NullabilityAnnotatedTypeParameter : NullabilityAnnotatedType, ITypeParameter
	{
		readonly new ITypeParameter baseType;

		/// <summary>
		/// Creates a nullability wrapper for a type parameter symbol.
		/// </summary>
		/// <param name="type">The original type parameter being annotated.</param>
		/// <param name="nullability">The explicit nullable state for the wrapped type parameter.</param>
		internal NullabilityAnnotatedTypeParameter(ITypeParameter type, Nullability nullability)
			: base(type, nullability)
		{
			this.baseType = type;
		}

		/// <summary>
		/// Gets the original type parameter before nullability decoration.
		/// </summary>
		public ITypeParameter OriginalTypeParameter => baseType;

		SymbolKind ITypeParameter.OwnerType => baseType.OwnerType;
		IEntity ITypeParameter.Owner => baseType.Owner;
		int ITypeParameter.Index => baseType.Index;
		string ITypeParameter.Name => baseType.Name;
		string ISymbol.Name => baseType.Name;
		VarianceModifier ITypeParameter.Variance => baseType.Variance;
		IType ITypeParameter.EffectiveBaseClass => baseType.EffectiveBaseClass;
		IReadOnlyCollection<IType> ITypeParameter.EffectiveInterfaceSet => baseType.EffectiveInterfaceSet;
		bool ITypeParameter.HasDefaultConstructorConstraint => baseType.HasDefaultConstructorConstraint;
		bool ITypeParameter.HasReferenceTypeConstraint => baseType.HasReferenceTypeConstraint;
		bool ITypeParameter.HasValueTypeConstraint => baseType.HasValueTypeConstraint;
		bool ITypeParameter.HasUnmanagedConstraint => baseType.HasUnmanagedConstraint;
		bool ITypeParameter.AllowsRefLikeType => baseType.AllowsRefLikeType;
		Nullability ITypeParameter.NullabilityConstraint => baseType.NullabilityConstraint;
		IReadOnlyList<TypeConstraint> ITypeParameter.TypeConstraints => baseType.TypeConstraints;
		SymbolKind ISymbol.SymbolKind => SymbolKind.TypeParameter;
		IEnumerable<IAttribute> ITypeParameter.GetAttributes() => baseType.GetAttributes();
	}
}

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

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Default mutable implementation of <see cref="ITypeParameter"/> used by metadata-backed and synthetic symbols.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type captures constraint flags and explicit type-constraint list exactly once during construction,
	/// and then exposes the normalized view expected by <see cref="AbstractTypeParameter"/>.
	/// </para>
	/// <para>
	/// Normalization intentionally injects <c>System.Object</c> or <c>System.ValueType</c> when needed so that
	/// effective-base-class calculations can rely on an explicit anchor even if metadata omits that base in the
	/// declared constraint list.
	/// </para>
	/// </remarks>
	public class DefaultTypeParameter : AbstractTypeParameter
	{
		readonly bool hasValueTypeConstraint;
		readonly bool hasReferenceTypeConstraint;
		readonly bool hasDefaultConstructorConstraint;
		readonly Nullability nullabilityConstraint;
		readonly IReadOnlyList<IAttribute> attributes;

		/// <summary>
		/// Initializes a type parameter that is owned by an existing symbol.
		/// </summary>
		/// <param name="owner">Owning method or type definition.</param>
		/// <param name="index">Zero-based type-parameter index in the owning symbol.</param>
		/// <param name="name">Optional display name. If <see langword="null"/>, a metadata-style fallback name is generated.</param>
		/// <param name="variance">Declared variance for interface/delegate type parameters.</param>
		/// <param name="attributes">Custom attributes applied to the type parameter declaration.</param>
		/// <param name="hasValueTypeConstraint">Whether the parameter has a <c>struct</c>/<c>valuetype</c> constraint.</param>
		/// <param name="hasReferenceTypeConstraint">Whether the parameter has a <c>class</c> constraint.</param>
		/// <param name="hasDefaultConstructorConstraint">Whether the parameter has a <c>new()</c> constraint.</param>
		/// <param name="constraints">Explicit type constraints (base type/interfaces/type parameters).</param>
		/// <param name="nullabilityConstraint">Nullable-reference-type constraint flavor decoded from metadata.</param>
		public DefaultTypeParameter(
			IEntity owner,
			int index, string name = null,
			VarianceModifier variance = VarianceModifier.Invariant,
			IReadOnlyList<IAttribute> attributes = null,
			bool hasValueTypeConstraint = false, bool hasReferenceTypeConstraint = false, bool hasDefaultConstructorConstraint = false,
			IReadOnlyList<IType> constraints = null, Nullability nullabilityConstraint = Nullability.Oblivious)
			: base(owner, index, name, variance)
		{
			this.hasValueTypeConstraint = hasValueTypeConstraint;
			this.hasReferenceTypeConstraint = hasReferenceTypeConstraint;
			this.hasDefaultConstructorConstraint = hasDefaultConstructorConstraint;
			this.nullabilityConstraint = nullabilityConstraint;
			this.TypeConstraints = MakeConstraints(constraints);
			this.attributes = attributes ?? EmptyList<IAttribute>.Instance;
		}

		/// <summary>
		/// Initializes a type parameter from compilation context without requiring a concrete owning symbol instance.
		/// </summary>
		/// <param name="compilation">Compilation that provides known-type lookups and symbol identity.</param>
		/// <param name="ownerType">Kind of owner that declares this type parameter.</param>
		/// <param name="index">Zero-based type-parameter index in the owner scope.</param>
		/// <param name="name">Optional display name. If <see langword="null"/>, a metadata-style fallback name is generated.</param>
		/// <param name="variance">Declared variance for interface/delegate type parameters.</param>
		/// <param name="attributes">Custom attributes applied to the type parameter declaration.</param>
		/// <param name="hasValueTypeConstraint">Whether the parameter has a <c>struct</c>/<c>valuetype</c> constraint.</param>
		/// <param name="hasReferenceTypeConstraint">Whether the parameter has a <c>class</c> constraint.</param>
		/// <param name="hasDefaultConstructorConstraint">Whether the parameter has a <c>new()</c> constraint.</param>
		/// <param name="constraints">Explicit type constraints (base type/interfaces/type parameters).</param>
		/// <param name="nullabilityConstraint">Nullable-reference-type constraint flavor decoded from metadata.</param>
		public DefaultTypeParameter(
			ICompilation compilation, SymbolKind ownerType,
			int index, string name = null,
			VarianceModifier variance = VarianceModifier.Invariant,
			IReadOnlyList<IAttribute> attributes = null,
			bool hasValueTypeConstraint = false, bool hasReferenceTypeConstraint = false, bool hasDefaultConstructorConstraint = false,
			IReadOnlyList<IType> constraints = null, Nullability nullabilityConstraint = Nullability.Oblivious)
			: base(compilation, ownerType, index, name, variance)
		{
			this.hasValueTypeConstraint = hasValueTypeConstraint;
			this.hasReferenceTypeConstraint = hasReferenceTypeConstraint;
			this.hasDefaultConstructorConstraint = hasDefaultConstructorConstraint;
			this.nullabilityConstraint = nullabilityConstraint;
			this.TypeConstraints = MakeConstraints(constraints);
			this.attributes = attributes ?? EmptyList<IAttribute>.Instance;
		}

		/// <inheritdoc/>
		public override IEnumerable<IAttribute> GetAttributes() => attributes;

		public override bool HasValueTypeConstraint => hasValueTypeConstraint;
		public override bool HasReferenceTypeConstraint => hasReferenceTypeConstraint;
		public override bool HasDefaultConstructorConstraint => hasDefaultConstructorConstraint;
		public override bool HasUnmanagedConstraint => false;
		public override bool AllowsRefLikeType => false;
		public override Nullability NullabilityConstraint => nullabilityConstraint;

		/// <inheritdoc/>
		public override IReadOnlyList<TypeConstraint> TypeConstraints { get; }

		IReadOnlyList<TypeConstraint> MakeConstraints(IReadOnlyList<IType> constraints)
		{
			var result = new List<TypeConstraint>();
			bool hasNonInterfaceConstraint = false;
			if (constraints != null)
			{
				foreach (IType c in constraints)
				{
					result.Add(new TypeConstraint(c));
					if (c.Kind != TypeKind.Interface)
						hasNonInterfaceConstraint = true;
				}
			}
			// Do not add the 'System.Object' constraint if there is another constraint with a base class.
			if (this.HasValueTypeConstraint || !hasNonInterfaceConstraint)
			{
				result.Add(new TypeConstraint(this.Compilation.FindType(this.HasValueTypeConstraint ? KnownTypeCode.ValueType : KnownTypeCode.Object)));
			}
			return result;
		}
	}
}

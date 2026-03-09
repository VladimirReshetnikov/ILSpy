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

using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents an array type.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The instance stores only array shape information (element type, rank, and optional nullability annotation).
	/// It delegates member surface queries to <see cref="KnownTypeCode.Array"/> so callers observe the members that
	/// are logically available on arrays in the current compilation.
	/// </para>
	/// <para>
	/// For single-dimensional zero-based arrays of non-pointer element types, <see cref="DirectBaseTypes"/> also
	/// exposes generic list-style interfaces such as <see cref="KnownTypeCode.IListOfT"/> and
	/// <see cref="KnownTypeCode.IReadOnlyListOfT"/> when those definitions are available in the target framework.
	/// </para>
	/// </remarks>
	public sealed class ArrayType : TypeWithElementType, ICompilationProvider
	{
		readonly int dimensions;
		readonly ICompilation compilation;
		readonly Nullability nullability;

		/// <summary>
		/// Initializes an array type with the specified element type and rank.
		/// </summary>
		/// <param name="compilation">Compilation used to look up framework base/interface types and inherited members.</param>
		/// <param name="elementType">Element type stored in each array slot.</param>
		/// <param name="dimensions">Array rank. A value of <c>1</c> represents a vector (<c>T[]</c>).</param>
		/// <param name="nullability">Nullability annotation carried by the array type itself.</param>
		/// <exception cref="ArgumentNullException"><paramref name="compilation"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="dimensions"/> is less than or equal to zero.</exception>
		/// <exception cref="InvalidOperationException">
		/// <paramref name="elementType"/> belongs to a different compilation than <paramref name="compilation"/>.
		/// </exception>
		public ArrayType(ICompilation compilation, IType elementType, int dimensions = 1, Nullability nullability = Nullability.Oblivious) : base(elementType)
		{
			if (compilation == null)
				throw new ArgumentNullException(nameof(compilation));
			if (dimensions <= 0)
				throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "dimensions must be positive");
			this.compilation = compilation;
			this.dimensions = dimensions;
			this.nullability = nullability;

			ICompilationProvider p = elementType as ICompilationProvider;
			if (p != null && p.Compilation != compilation)
				throw new InvalidOperationException("Cannot create an array type using a different compilation from the element type.");
		}

		public override TypeKind Kind {
			get { return TypeKind.Array; }
		}

		/// <summary>
		/// Gets the owning compilation used for framework-type lookup and member projection.
		/// </summary>
		public ICompilation Compilation {
			get { return compilation; }
		}

		/// <summary>
		/// Gets the array rank.
		/// </summary>
		/// <value>
		/// The number of dimensions in the array shape. This value is always positive.
		/// </value>
		public int Dimensions {
			get { return dimensions; }
		}

		public override Nullability Nullability => nullability;

		public override IType ChangeNullability(Nullability nullability)
		{
			if (nullability == this.nullability)
				return this;
			else
				return new ArrayType(compilation, elementType, dimensions, nullability);
		}

		/// <summary>
		/// Gets the CLR-style suffix that represents the array shape.
		/// </summary>
		/// <value>
		/// <c>[]</c> for vectors, <c>[,]</c> for rank-2 arrays, and generally <c>[</c> followed by
		/// <c>Dimensions - 1</c> commas and then <c>]</c>.
		/// </value>
		public override string NameSuffix {
			get {
				return "[" + new string(',', dimensions - 1) + "]";
			}
		}

		public override bool? IsReferenceType {
			get { return true; }
		}

		public override int GetHashCode()
		{
			return unchecked(elementType.GetHashCode() * 71681 + dimensions);
		}

		public override bool Equals(IType other)
		{
			ArrayType a = other as ArrayType;
			return a != null && elementType.Equals(a.elementType) && a.dimensions == dimensions && a.nullability == nullability;
		}

		public override string ToString()
		{
			switch (nullability)
			{
				case Nullability.Nullable:
					return elementType.ToString() + NameSuffix + "?";
				case Nullability.NotNullable:
					return elementType.ToString() + NameSuffix + "!";
				default:
					return elementType.ToString() + NameSuffix;
			}
		}

		/// <summary>
		/// Gets the immediate runtime base types implied by this array shape.
		/// </summary>
		/// <value>
		/// A sequence containing <see cref="KnownTypeCode.Array"/> when available, plus generic list-style interfaces
		/// for single-dimensional non-pointer arrays when those framework definitions exist in the compilation.
		/// </value>
		public override IEnumerable<IType> DirectBaseTypes {
			get {
				List<IType> baseTypes = new List<IType>();
				IType t = compilation.FindType(KnownTypeCode.Array);
				if (t.Kind != TypeKind.Unknown)
					baseTypes.Add(t);
				if (dimensions == 1 && elementType.Kind != TypeKind.Pointer)
				{
					// single-dimensional arrays implement IList<T>
					ITypeDefinition def = compilation.FindType(KnownTypeCode.IListOfT) as ITypeDefinition;
					if (def != null)
						baseTypes.Add(new ParameterizedType(def, new[] { elementType }));
					// And in .NET 4.5 they also implement IReadOnlyList<T>
					def = compilation.FindType(KnownTypeCode.IReadOnlyListOfT) as ITypeDefinition;
					if (def != null)
						baseTypes.Add(new ParameterizedType(def, new[] { elementType }));
				}
				return baseTypes;
			}
		}

		/// <summary>
		/// Resolves method members visible on this array type.
		/// </summary>
		/// <remarks>
		/// Unless <see cref="GetMemberOptions.IgnoreInheritedMembers"/> is set, this forwards to
		/// <see cref="KnownTypeCode.Array"/> and returns that type's methods.
		/// </remarks>
		public override IEnumerable<IMethod> GetMethods(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return compilation.FindType(KnownTypeCode.Array).GetMethods(filter, options);
		}

		/// <summary>
		/// Resolves generic method members visible on this array type.
		/// </summary>
		public override IEnumerable<IMethod> GetMethods(IReadOnlyList<IType> typeArguments, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return compilation.FindType(KnownTypeCode.Array).GetMethods(typeArguments, filter, options);
		}

		/// <summary>
		/// Resolves accessor methods visible on this array type.
		/// </summary>
		public override IEnumerable<IMethod> GetAccessors(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IMethod>.Instance;
			else
				return compilation.FindType(KnownTypeCode.Array).GetAccessors(filter, options);
		}

		/// <summary>
		/// Resolves properties visible on this array type.
		/// </summary>
		public override IEnumerable<IProperty> GetProperties(Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
				return EmptyList<IProperty>.Instance;
			else
				return compilation.FindType(KnownTypeCode.Array).GetProperties(filter, options);
		}

		// NestedTypes, Events, Fields: System.Array doesn't have any; so we can use the AbstractType default implementation
		// that simply returns an empty list

		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitArrayType(this);
		}

		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new ArrayType(compilation, e, dimensions, nullability);
		}
	}

	[Serializable]
	/// <summary>
	/// Represents an unresolved reference to an array type.
	/// </summary>
	/// <remarks>
	/// Use this type while decoding metadata signatures before an <see cref="ITypeResolveContext"/> is available.
	/// <see cref="Resolve"/> materializes the corresponding <see cref="ArrayType"/>.
	/// </remarks>
	public sealed class ArrayTypeReference : ITypeReference, ISupportsInterning
	{
		readonly ITypeReference elementType;
		readonly int dimensions;

		/// <summary>
		/// Initializes an unresolved array reference.
		/// </summary>
		/// <param name="elementType">Unresolved element type reference.</param>
		/// <param name="dimensions">Array rank. Must be greater than zero.</param>
		/// <exception cref="ArgumentNullException"><paramref name="elementType"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="dimensions"/> is less than or equal to zero.</exception>
		public ArrayTypeReference(ITypeReference elementType, int dimensions = 1)
		{
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (dimensions <= 0)
				throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "dimensions must be positive");
			this.elementType = elementType;
			this.dimensions = dimensions;
		}

		/// <summary>
		/// Gets the unresolved element type reference.
		/// </summary>
		public ITypeReference ElementType {
			get { return elementType; }
		}

		/// <summary>
		/// Gets the unresolved array rank.
		/// </summary>
		public int Dimensions {
			get { return dimensions; }
		}

		/// <summary>
		/// Resolves this reference into a concrete <see cref="ArrayType"/> in <paramref name="context"/>.
		/// </summary>
		/// <param name="context">Type resolution context that provides compilation and symbol lookup.</param>
		/// <returns>An array type built from the resolved element type and the stored rank.</returns>
		public IType Resolve(ITypeResolveContext context)
		{
			return new ArrayType(context.Compilation, elementType.Resolve(context), dimensions);
		}

		public override string ToString()
		{
			return elementType.ToString() + "[" + new string(',', dimensions - 1) + "]";
		}

		int ISupportsInterning.GetHashCodeForInterning()
		{
			return elementType.GetHashCode() ^ dimensions;
		}

		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ArrayTypeReference o = other as ArrayTypeReference;
			return o != null && elementType == o.elementType && dimensions == o.dimensions;
		}
	}
}

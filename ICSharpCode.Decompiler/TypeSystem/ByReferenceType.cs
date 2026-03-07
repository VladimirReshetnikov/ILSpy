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

using ICSharpCode.Decompiler.TypeSystem.Implementation;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a managed by-reference type (<c>T&amp;</c> in metadata, <c>ref T</c> in C# signatures).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Instances of this type model managed references used for by-ref parameters, locals, returns, and
	/// address-producing IL instructions. They are treated as ref-like in ILSpy's type system and therefore
	/// report <see cref="IType.IsByRefLike"/> as <see langword="true"/>.
	/// </para>
	/// <para>
	/// Unlike ordinary reference types, managed references do not have independent object identity; consequently
	/// <see cref="IType.IsReferenceType"/> is intentionally <see langword="null"/>.
	/// </para>
	/// </remarks>
	public sealed class ByReferenceType : TypeWithElementType
	{
		/// <summary>
		/// Creates a managed by-reference wrapper for <paramref name="elementType"/>.
		/// </summary>
		/// <param name="elementType">The referenced element type.</param>
		public ByReferenceType(IType elementType) : base(elementType)
		{
		}

		public override TypeKind Kind {
			get { return TypeKind.ByReference; }
		}

		public override string NameSuffix {
			get {
				return "&";
			}
		}

		public override bool? IsReferenceType {
			get { return null; }
		}

		public override bool IsByRefLike => true;

		public override int GetHashCode()
		{
			return elementType.GetHashCode() ^ 91725813;
		}

		public override bool Equals(IType other)
		{
			ByReferenceType a = other as ByReferenceType;
			return a != null && elementType.Equals(a.elementType);
		}

		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitByReferenceType(this);
		}

		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new ByReferenceType(e);
		}
	}

	[Serializable]
	/// <summary>
	/// Represents an unresolved type reference for a managed by-reference type.
	/// </summary>
	/// <remarks>
	/// This type preserves unresolved metadata shape and constructs the concrete <see cref="ByReferenceType"/>
	/// only when <see cref="Resolve"/> is invoked against a resolution context.
	/// </remarks>
	public sealed class ByReferenceTypeReference : ITypeReference, ISupportsInterning
	{
		readonly ITypeReference elementType;

		/// <summary>
		/// Initializes a by-reference type reference for the specified unresolved element type.
		/// </summary>
		/// <param name="elementType">The unresolved element type that the managed reference points to.</param>
		/// <exception cref="ArgumentNullException"><paramref name="elementType"/> is <see langword="null"/>.</exception>
		public ByReferenceTypeReference(ITypeReference elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			this.elementType = elementType;
		}

		/// <summary>
		/// Gets the unresolved element type that this reference wraps.
		/// </summary>
		public ITypeReference ElementType {
			get { return elementType; }
		}

		/// <summary>
		/// Resolves this reference to a concrete <see cref="ByReferenceType"/> in the supplied type-system context.
		/// </summary>
		/// <param name="context">The context used to resolve <see cref="ElementType"/>.</param>
		/// <returns>A managed by-reference type whose element type is resolved in <paramref name="context"/>.</returns>
		public IType Resolve(ITypeResolveContext context)
		{
			return new ByReferenceType(elementType.Resolve(context));
		}

		public override string ToString()
		{
			return elementType.ToString() + "&";
		}

		int ISupportsInterning.GetHashCodeForInterning()
		{
			return elementType.GetHashCode() ^ 91725814;
		}

		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ByReferenceTypeReference brt = other as ByReferenceTypeReference;
			return brt != null && this.elementType == brt.elementType;
		}
	}
}

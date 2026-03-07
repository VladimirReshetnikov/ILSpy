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
	/// Represents an unmanaged pointer type (<c>T*</c>).
	/// </summary>
	/// <remarks>
	/// Pointer types are used for unsafe IL/C# operations and are distinct from managed references
	/// represented by <see cref="ByReferenceType"/>. As with other pointer-like forms, the runtime category is
	/// neither a normal reference type nor a plain value type, so <see cref="IType.IsReferenceType"/> is
	/// reported as <see langword="null"/>.
	/// </remarks>
	public sealed class PointerType : TypeWithElementType
	{
		/// <summary>
		/// Creates an unmanaged pointer type for <paramref name="elementType"/>.
		/// </summary>
		/// <param name="elementType">The pointed-to element type.</param>
		public PointerType(IType elementType) : base(elementType)
		{
		}

		public override TypeKind Kind {
			get { return TypeKind.Pointer; }
		}

		public override string NameSuffix {
			get {
				return "*";
			}
		}

		public override bool? IsReferenceType {
			get { return null; }
		}

		public override int GetHashCode()
		{
			return elementType.GetHashCode() ^ 91725811;
		}

		public override bool Equals(IType other)
		{
			PointerType a = other as PointerType;
			return a != null && elementType.Equals(a.elementType);
		}

		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitPointerType(this);
		}

		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new PointerType(e);
		}
	}

	[Serializable]
	/// <summary>
	/// Represents an unresolved type reference for an unmanaged pointer type.
	/// </summary>
	public sealed class PointerTypeReference : ITypeReference, ISupportsInterning
	{
		readonly ITypeReference elementType;

		/// <summary>
		/// Initializes a pointer type reference for the specified unresolved element type.
		/// </summary>
		/// <param name="elementType">The unresolved element type that the pointer points to.</param>
		/// <exception cref="ArgumentNullException"><paramref name="elementType"/> is <see langword="null"/>.</exception>
		public PointerTypeReference(ITypeReference elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			this.elementType = elementType;
		}

		/// <summary>
		/// Gets the unresolved pointed-to element type.
		/// </summary>
		public ITypeReference ElementType {
			get { return elementType; }
		}

		/// <summary>
		/// Resolves this reference to a concrete <see cref="PointerType"/>.
		/// </summary>
		/// <param name="context">The context used to resolve <see cref="ElementType"/>.</param>
		/// <returns>An unmanaged pointer type whose element type is resolved in <paramref name="context"/>.</returns>
		public IType Resolve(ITypeResolveContext context)
		{
			return new PointerType(elementType.Resolve(context));
		}

		public override string ToString()
		{
			return elementType.ToString() + "*";
		}

		int ISupportsInterning.GetHashCodeForInterning()
		{
			return elementType.GetHashCode() ^ 91725812;
		}

		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			PointerTypeReference o = other as PointerTypeReference;
			return o != null && this.elementType == o.elementType;
		}
	}
}

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
using System.Diagnostics;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem.Implementation;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a constructed generic type where all type parameters are bound to concrete type arguments.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This wrapper preserves the generic definition identity (<see cref="GenericType"/>) and overlays a stable
	/// type-argument vector. Member enumeration APIs project definition members through this substitution so callers
	/// see signatures as they appear on the constructed receiver.
	/// </para>
	/// <para>
	/// Use <see cref="GetMemberOptions.ReturnMemberDefinitions"/> on member queries when you need original unspecialized
	/// symbols instead of substituted wrappers.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// IType listDefinition = compilation.FindType(new FullTypeName("System.Collections.Generic.List`1"));
	/// IType constructed = new ParameterizedType(listDefinition, new[] { compilation.FindType(KnownTypeCode.String) });
	///
	/// // Returns methods specialized for List&lt;string&gt;.
	/// var methods = constructed.GetMethods();
	/// </code>
	/// </example>
	[Serializable]
	public sealed class ParameterizedType : IType
	{
		readonly IType genericType;
		readonly IType[] typeArguments;

		/// <summary>
		/// Initializes a constructed generic type.
		/// </summary>
		/// <param name="genericType">Generic type definition or open generic type to construct.</param>
		/// <param name="typeArguments">Type arguments that bind the generic type parameters.</param>
		/// <exception cref="ArgumentNullException"><paramref name="genericType"/> or <paramref name="typeArguments"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">The argument sequence is empty, or its length does not match <paramref name="genericType"/>'s arity.</exception>
		/// <exception cref="InvalidOperationException">Any argument belongs to a different compilation than <paramref name="genericType"/>.</exception>
		public ParameterizedType(IType genericType, IEnumerable<IType> typeArguments)
		{
			if (genericType == null)
				throw new ArgumentNullException(nameof(genericType));
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			this.genericType = genericType;
			this.typeArguments = typeArguments.ToArray(); // copy input array to ensure it isn't modified
			if (this.typeArguments.Length == 0)
				throw new ArgumentException("Cannot use ParameterizedType with 0 type arguments.");
			if (genericType.TypeParameterCount != this.typeArguments.Length)
				throw new ArgumentException("Number of type arguments must match the type definition's number of type parameters");
			ICompilationProvider gp = genericType as ICompilationProvider;
			for (int i = 0; i < this.typeArguments.Length; i++)
			{
				if (this.typeArguments[i] == null)
					throw new ArgumentNullException("typeArguments[" + i + "]");
				ICompilationProvider p = this.typeArguments[i] as ICompilationProvider;
				if (p != null && gp != null && p.Compilation != gp.Compilation)
					throw new InvalidOperationException("Cannot parameterize a type with type arguments from a different compilation.");
			}
		}

		/// <summary>
		/// Fast internal version of the constructor. (no safety checks)
		/// Keeps the array that was passed and assumes it won't be modified.
		/// </summary>
		internal ParameterizedType(IType genericType, params IType[] typeArguments)
		{
			Debug.Assert(genericType.TypeParameterCount == typeArguments.Length);
			this.genericType = genericType;
			this.typeArguments = typeArguments;
		}

		public TypeKind Kind {
			get { return genericType.Kind; }
		}

		public IType GenericType {
			get { return genericType; }
		}

		public bool? IsReferenceType => genericType.IsReferenceType;
		public bool IsByRefLike => genericType.IsByRefLike;
		public Nullability Nullability => genericType.Nullability;

		public IType ChangeNullability(Nullability nullability)
		{
			IType newGenericType = genericType.ChangeNullability(nullability);
			if (newGenericType == genericType)
				return this;
			else
				return new ParameterizedType(newGenericType, typeArguments);
		}

		public IType DeclaringType {
			get {
				IType declaringType = genericType.DeclaringType;
				if (declaringType != null && declaringType.TypeParameterCount > 0
					&& declaringType.TypeParameterCount <= genericType.TypeParameterCount)
				{
					IType[] newTypeArgs = new IType[declaringType.TypeParameterCount];
					Array.Copy(this.typeArguments, 0, newTypeArgs, 0, newTypeArgs.Length);
					return new ParameterizedType(declaringType, newTypeArgs);
				}
				return declaringType;
			}
		}

		public int TypeParameterCount {
			get { return typeArguments.Length; }
		}

		public string FullName {
			get { return genericType.FullName; }
		}

		public string Name {
			get { return genericType.Name; }
		}

		public string Namespace {
			get { return genericType.Namespace; }
		}

		public string ReflectionName {
			get {
				StringBuilder b = new StringBuilder(genericType.ReflectionName);
				b.Append('[');
				for (int i = 0; i < typeArguments.Length; i++)
				{
					if (i > 0)
						b.Append(',');
					b.Append('[');
					b.Append(typeArguments[i].ReflectionName);
					b.Append(']');
				}
				b.Append(']');
				return b.ToString();
			}
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder(genericType.ToString());
			b.Append('[');
			for (int i = 0; i < typeArguments.Length; i++)
			{
				if (i > 0)
					b.Append(',');
				b.Append('[');
				b.Append(typeArguments[i].ToString());
				b.Append(']');
			}
			b.Append(']');
			return b.ToString();
		}

		public IReadOnlyList<IType> TypeArguments => typeArguments;

		/// <summary>
		/// Gets the type argument at the specified generic parameter position.
		/// </summary>
		/// <param name="index">Zero-based generic parameter index.</param>
		/// <returns>The bound type argument at <paramref name="index"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the valid range.</exception>
		public IType GetTypeArgument(int index)
		{
			return typeArguments[index];
		}

		public IReadOnlyList<ITypeParameter> TypeParameters => genericType.TypeParameters;

		/// <summary>
		/// Gets the type definition behind <see cref="GenericType"/>.
		/// </summary>
		/// <returns>
		/// The generic type definition, or <see langword="null"/> when the generic type is unresolved (for
		/// example an <see cref="Implementation.UnknownType"/>). Use <see cref="GetDefinitionOrUnknown"/> to
		/// get a non-null result in that case.
		/// </returns>
		public ITypeDefinition GetDefinition()
		{
			return genericType.GetDefinition();
		}

		public ITypeDefinitionOrUnknown GetDefinitionOrUnknown()
		{
			return genericType.GetDefinitionOrUnknown();
		}

		/// <summary>
		/// Creates a substitution that maps the declaring type's generic parameters to this instance's type arguments.
		/// </summary>
		/// <returns>A substitution usable for specializing member/type signatures against this constructed type.</returns>
		public TypeParameterSubstitution GetSubstitution()
		{
			return new TypeParameterSubstitution(typeArguments, null);
		}

		/// <summary>
		/// Creates a substitution that combines this type's class-parameter bindings with method type arguments.
		/// </summary>
		/// <param name="methodTypeArguments">Type arguments to bind method generic parameters.</param>
		/// <returns>A composed substitution for class and method generic parameters.</returns>
		public TypeParameterSubstitution GetSubstitution(IReadOnlyList<IType> methodTypeArguments)
		{
			return new TypeParameterSubstitution(typeArguments, methodTypeArguments);
		}

		public IEnumerable<IType> DirectBaseTypes {
			get {
				var substitution = GetSubstitution();
				return genericType.DirectBaseTypes.Select(t => t.AcceptVisitor(substitution));
			}
		}

		public IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetNestedTypes(filter, options);
			else
				return GetMembersHelper.GetNestedTypes(this, filter, options);
		}

		public IEnumerable<IType> GetNestedTypes(IReadOnlyList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetNestedTypes(typeArguments, filter, options);
			else
				return GetMembersHelper.GetNestedTypes(this, typeArguments, filter, options);
		}

		public IEnumerable<IMethod> GetConstructors(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetConstructors(filter, options);
			else
				return GetMembersHelper.GetConstructors(this, filter, options);
		}

		public IEnumerable<IMethod> GetMethods(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetMethods(filter, options);
			else
				return GetMembersHelper.GetMethods(this, filter, options);
		}

		public IEnumerable<IMethod> GetMethods(IReadOnlyList<IType> typeArguments, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetMethods(typeArguments, filter, options);
			else
				return GetMembersHelper.GetMethods(this, typeArguments, filter, options);
		}

		public IEnumerable<IProperty> GetProperties(Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetProperties(filter, options);
			else
				return GetMembersHelper.GetProperties(this, filter, options);
		}

		public IEnumerable<IField> GetFields(Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetFields(filter, options);
			else
				return GetMembersHelper.GetFields(this, filter, options);
		}

		public IEnumerable<IEvent> GetEvents(Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetEvents(filter, options);
			else
				return GetMembersHelper.GetEvents(this, filter, options);
		}

		public IEnumerable<IMember> GetMembers(Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetMembers(filter, options);
			else
				return GetMembersHelper.GetMembers(this, filter, options);
		}

		public IEnumerable<IMethod> GetAccessors(Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
				return genericType.GetAccessors(filter, options);
			else
				return GetMembersHelper.GetAccessors(this, filter, options);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IType);
		}

		public bool Equals(IType other)
		{
			if (this == other)
				return true;
			ParameterizedType c = other as ParameterizedType;
			if (c == null || !genericType.Equals(c.genericType) || typeArguments.Length != c.typeArguments.Length)
				return false;
			for (int i = 0; i < typeArguments.Length; i++)
			{
				if (!typeArguments[i].Equals(c.typeArguments[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = genericType.GetHashCode();
			unchecked
			{
				foreach (var ta in typeArguments)
				{
					hashCode *= 1000000007;
					hashCode += 1000000009 * ta.GetHashCode();
				}
			}
			return hashCode;
		}

		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitParameterizedType(this);
		}

		public IType VisitChildren(TypeVisitor visitor)
		{
			IType g = genericType.AcceptVisitor(visitor);
			// Keep ta == null as long as no elements changed, allocate the array only if necessary.
			IType[] ta = (g != genericType) ? new IType[typeArguments.Length] : null;
			for (int i = 0; i < typeArguments.Length; i++)
			{
				IType r = typeArguments[i].AcceptVisitor(visitor);
				if (r == null)
					throw new NullReferenceException("TypeVisitor.Visit-method returned null");
				if (ta == null && r != typeArguments[i])
				{
					// we found a difference, so we need to allocate the array
					ta = new IType[typeArguments.Length];
					for (int j = 0; j < i; j++)
					{
						ta[j] = typeArguments[j];
					}
				}
				if (ta != null)
					ta[i] = r;
			}
			if (ta == null)
				return this;
			else
				return new ParameterizedType(g, ta);
		}
	}

	/// <summary>
	/// Represents an unresolved reference to a constructed generic type.
	/// </summary>
	/// <remarks>
	/// The reference resolves both the generic type and each argument in the supplied <see cref="ITypeResolveContext"/>,
	/// then materializes a <see cref="ParameterizedType"/> instance.
	/// </remarks>
	[Serializable]
	public sealed class ParameterizedTypeReference : ITypeReference, ISupportsInterning
	{
		readonly ITypeReference genericType;
		readonly ITypeReference[] typeArguments;

		/// <summary>
		/// Initializes a new constructed-type reference.
		/// </summary>
		/// <param name="genericType">Reference to the generic type being constructed.</param>
		/// <param name="typeArguments">References for the type arguments.</param>
		/// <exception cref="ArgumentNullException"><paramref name="genericType"/>, <paramref name="typeArguments"/>, or any element is <see langword="null"/>.</exception>
		public ParameterizedTypeReference(ITypeReference genericType, IEnumerable<ITypeReference> typeArguments)
		{
			if (genericType == null)
				throw new ArgumentNullException(nameof(genericType));
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			this.genericType = genericType;
			this.typeArguments = typeArguments.ToArray();
			for (int i = 0; i < this.typeArguments.Length; i++)
			{
				if (this.typeArguments[i] == null)
					throw new ArgumentNullException("typeArguments[" + i + "]");
			}
		}

		public ITypeReference GenericType {
			get { return genericType; }
		}

		public IReadOnlyList<ITypeReference> TypeArguments {
			get {
				return typeArguments;
			}
		}

		/// <summary>
		/// Resolves this constructed-type reference in the supplied context.
		/// </summary>
		/// <param name="context">Resolution context that supplies type definitions and substitutions.</param>
		/// <returns>
		/// A resolved constructed type. If the resolved generic type has arity zero, the generic type itself is returned.
		/// Missing trailing arguments are substituted with <see cref="SpecialType.UnknownType"/>.
		/// </returns>
		public IType Resolve(ITypeResolveContext context)
		{
			IType baseType = genericType.Resolve(context);
			int tpc = baseType.TypeParameterCount;
			if (tpc == 0)
				return baseType;
			IType[] resolvedTypes = new IType[tpc];
			for (int i = 0; i < resolvedTypes.Length; i++)
			{
				if (i < typeArguments.Length)
					resolvedTypes[i] = typeArguments[i].Resolve(context);
				else
					resolvedTypes[i] = SpecialType.UnknownType;
			}
			return new ParameterizedType(baseType, resolvedTypes);
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder(genericType.ToString());
			b.Append('[');
			for (int i = 0; i < typeArguments.Length; i++)
			{
				if (i > 0)
					b.Append(',');
				b.Append('[');
				b.Append(typeArguments[i].ToString());
				b.Append(']');
			}
			b.Append(']');
			return b.ToString();
		}

		int ISupportsInterning.GetHashCodeForInterning()
		{
			int hashCode = genericType.GetHashCode();
			unchecked
			{
				foreach (ITypeReference t in typeArguments)
				{
					hashCode *= 27;
					hashCode += t.GetHashCode();
				}
			}
			return hashCode;
		}

		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ParameterizedTypeReference o = other as ParameterizedTypeReference;
			if (o != null && genericType == o.genericType && typeArguments.Length == o.typeArguments.Length)
			{
				for (int i = 0; i < typeArguments.Length; i++)
				{
					if (typeArguments[i] != o.typeArguments[i])
						return false;
				}
				return true;
			}
			return false;
		}
	}
}

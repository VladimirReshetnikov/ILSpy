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
using System.Globalization;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Deferred reference to a generic type parameter token (<c>!n</c> or <c>!!n</c>).
	/// </summary>
	/// <remarks>
	/// Resolution depends on <see cref="ITypeResolveContext.CurrentTypeDefinition"/> and
	/// <see cref="ITypeResolveContext.CurrentMember"/>. If the requested index is unavailable, the implementation
	/// returns a <see cref="DummyTypeParameter"/> placeholder instead of throwing.
	/// </remarks>
	[Serializable]
	public sealed class TypeParameterReference : ITypeReference
	{
		static readonly TypeParameterReference[] classTypeParameterReferences = new TypeParameterReference[8];
		static readonly TypeParameterReference[] methodTypeParameterReferences = new TypeParameterReference[8];

		/// <summary>
		/// Creates a type parameter reference.
		/// For common type parameter references, this method may return a shared instance.
		/// </summary>
		public static TypeParameterReference Create(SymbolKind ownerType, int index)
		{
			if (index >= 0 && index < 8 && (ownerType == SymbolKind.TypeDefinition || ownerType == SymbolKind.Method))
			{
				TypeParameterReference[] arr = (ownerType == SymbolKind.TypeDefinition) ? classTypeParameterReferences : methodTypeParameterReferences;
				TypeParameterReference result = LazyInit.VolatileRead(ref arr[index]);
				if (result == null)
				{
					result = LazyInit.GetOrSet(ref arr[index], new TypeParameterReference(ownerType, index));
				}
				return result;
			}
			else
			{
				return new TypeParameterReference(ownerType, index);
			}
		}

		readonly SymbolKind ownerType;
		readonly int index;

		/// <summary>
		/// Gets the zero-based generic-parameter index encoded by this reference.
		/// </summary>
		public int Index {
			get {
				return index;
			}
		}

		/// <summary>
		/// Initializes a reference to a generic parameter on a type or method owner.
		/// </summary>
		/// <param name="ownerType">Owner kind that determines whether <c>!n</c> or <c>!!n</c> semantics are used.</param>
		/// <param name="index">Zero-based generic-parameter index.</param>
		public TypeParameterReference(SymbolKind ownerType, int index)
		{
			this.ownerType = ownerType;
			this.index = index;
		}

		/// <summary>
		/// Resolves this generic-parameter reference in the specified context.
		/// </summary>
		/// <param name="context">Context supplying current type and method generic parameter lists.</param>
		/// <returns>
		/// The matching <see cref="ITypeParameter"/> when available; otherwise a fallback placeholder or
		/// <see cref="SpecialType.UnknownType"/> for unsupported owner kinds.
		/// </returns>
		public IType Resolve(ITypeResolveContext context)
		{
			if (ownerType == SymbolKind.Method)
			{
				IMethod method = context.CurrentMember as IMethod;
				if (method != null && index < method.TypeParameters.Count)
				{
					return method.TypeParameters[index];
				}
				return DummyTypeParameter.GetMethodTypeParameter(index);
			}
			else if (ownerType == SymbolKind.TypeDefinition)
			{
				ITypeDefinition typeDef = context.CurrentTypeDefinition;
				if (typeDef != null && index < typeDef.TypeParameters.Count)
				{
					return typeDef.TypeParameters[index];
				}
				return DummyTypeParameter.GetClassTypeParameter(index);
			}
			else
			{
				return SpecialType.UnknownType;
			}
		}

		public override string ToString()
		{
			if (ownerType == SymbolKind.Method)
				return "!!" + index.ToString(CultureInfo.InvariantCulture);
			else
				return "!" + index.ToString(CultureInfo.InvariantCulture);
		}
	}
}

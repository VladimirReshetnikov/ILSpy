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
using System.Linq;

#nullable enable

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Provides member- and attribute-level traversal helpers over type inheritance chains.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The helpers in this class operate on <see cref="IMember.MemberDefinition"/> and reconstruct inheritance relationships
	/// by matching member signatures in base types. The implementation intentionally does not rely on metadata-level override maps
	/// alone, because the type system also serves synthesized and specialized members created during decompilation.
	/// </para>
	/// <para>
	/// Most methods return members specialized back into the substitution context of the input symbol, which allows callers to
	/// reason about inherited members using the same generic arguments that were visible on the original lookup path.
	/// </para>
	/// </remarks>
	public static class InheritanceHelper
	{
		// TODO: maybe these should be extension methods?
		// or even part of the interface itself? (would allow for easy caching)

		#region GetBaseMember
		/// <summary>
		/// Gets the closest inherited member that matches the signature of <paramref name="member"/>.
		/// </summary>
		/// <param name="member">The member whose immediate inherited declaration should be resolved.</param>
		/// <returns>
		/// The first matching base member when one exists; otherwise <see langword="null"/>.
		/// Interface implementations are not considered.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
		public static IMember? GetBaseMember(IMember member)
		{
			return GetBaseMembers(member, false).FirstOrDefault();
		}

		/// <summary>
		/// Enumerates inherited members whose signatures match <paramref name="member"/>.
		/// </summary>
		/// <param name="member">The member whose inheritance chain should be searched.</param>
		/// <param name="includeImplementedInterfaces">
		/// <see langword="true"/> to include interface members (including explicit interface implementation roots);
		/// <see langword="false"/> to inspect only non-interface base types.
		/// </param>
		/// <returns>
		/// Base members with matching signatures, ordered from the closest base declaration to the furthest one.
		/// </returns>
		/// <remarks>
		/// The returned members are specialized with the substitution of <paramref name="member"/>, so generic type arguments remain
		/// aligned with the original call site.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the returned sequence is first enumerated and <paramref name="member"/> is <see langword="null"/>.
		/// </exception>
		public static IEnumerable<IMember> GetBaseMembers(IMember member, bool includeImplementedInterfaces)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			if (includeImplementedInterfaces)
			{
				if (member.IsExplicitInterfaceImplementation && member.ExplicitlyImplementedInterfaceMembers.Count() == 1)
				{
					// C#-style explicit interface implementation
					member = member.ExplicitlyImplementedInterfaceMembers.First();
					yield return member;
				}
			}

			// Remove generic specialization
			var substitution = member.Substitution;
			member = member.MemberDefinition;

			if (member.DeclaringTypeDefinition == null)
			{
				// For global methods, return empty list. (prevent SharpDevelop UDC crash 4524)
				yield break;
			}

			IEnumerable<IType> allBaseTypes;
			if (includeImplementedInterfaces)
			{
				allBaseTypes = member.DeclaringTypeDefinition.GetAllBaseTypes();
			}
			else
			{
				allBaseTypes = member.DeclaringTypeDefinition.GetNonInterfaceBaseTypes();
			}
			foreach (IType baseType in allBaseTypes.Reverse())
			{
				if (baseType == member.DeclaringTypeDefinition)
					continue;

				IEnumerable<IMember> baseMembers;
				if (member.SymbolKind == SymbolKind.Accessor)
				{
					baseMembers = baseType.GetAccessors(m => m.Name == member.Name && m.Accessibility > Accessibility.Private, GetMemberOptions.IgnoreInheritedMembers);
				}
				else
				{
					baseMembers = baseType.GetMembers(m => m.Name == member.Name && m.Accessibility > Accessibility.Private, GetMemberOptions.IgnoreInheritedMembers);
				}
				foreach (IMember baseMember in baseMembers)
				{
					System.Diagnostics.Debug.Assert(baseMember.Accessibility != Accessibility.Private);
					if (SignatureComparer.Ordinal.Equals(member, baseMember))
					{
						yield return baseMember.Specialize(substitution);
					}
				}
			}
		}
		#endregion

		#region GetDerivedMember
		/// <summary>
		/// Finds the declaration in <paramref name="derivedType"/> that corresponds to <paramref name="baseMember"/>.
		/// </summary>
		/// <param name="baseMember">The inherited member to match.</param>
		/// <param name="derivedType">The candidate derived type that might introduce an override or matching declaration.</param>
		/// <returns>
		/// The member declared on <paramref name="derivedType"/> that maps to <paramref name="baseMember"/>, or
		/// <see langword="null"/> when no such member exists.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="baseMember"/> or <paramref name="derivedType"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="baseMember"/> and <paramref name="derivedType"/> belong to different compilations.
		/// </exception>
		public static IMember? GetDerivedMember(IMember baseMember, ITypeDefinition derivedType)
		{
			if (baseMember == null)
				throw new ArgumentNullException(nameof(baseMember));
			if (derivedType == null)
				throw new ArgumentNullException(nameof(derivedType));

			if (baseMember.Compilation != derivedType.Compilation)
				throw new ArgumentException("baseMember and derivedType must be from the same compilation");

			baseMember = baseMember.MemberDefinition;
			bool includeInterfaces = baseMember.DeclaringTypeDefinition?.Kind == TypeKind.Interface;
			if (baseMember is IMethod method)
			{
				foreach (IMethod derivedMethod in derivedType.Methods)
				{
					if (derivedMethod.Name == method.Name && derivedMethod.Parameters.Count == method.Parameters.Count)
					{
						if (derivedMethod.TypeParameters.Count == method.TypeParameters.Count)
						{
							// The method could override the base method:
							if (GetBaseMembers(derivedMethod, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
								return derivedMethod;
						}
					}
				}
			}
			if (baseMember is IProperty property)
			{
				foreach (IProperty derivedProperty in derivedType.Properties)
				{
					if (derivedProperty.Name == property.Name && derivedProperty.Parameters.Count == property.Parameters.Count)
					{
						// The property could override the base property:
						if (GetBaseMembers(derivedProperty, includeInterfaces).Any(m => m.MemberDefinition == baseMember))
							return derivedProperty;
					}
				}
			}
			if (baseMember is IEvent)
			{
				foreach (IEvent derivedEvent in derivedType.Events)
				{
					if (derivedEvent.Name == baseMember.Name)
						return derivedEvent;
				}
			}
			if (baseMember is IField)
			{
				foreach (IField derivedField in derivedType.Fields)
				{
					if (derivedField.Name == baseMember.Name)
						return derivedField;
				}
			}
			return null;
		}
		#endregion

		#region Attributes
		internal static IEnumerable<IAttribute> GetAttributes(ITypeDefinition typeDef)
		{
			foreach (var baseType in typeDef.GetNonInterfaceBaseTypes().Reverse())
			{
				ITypeDefinition? baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef == null)
					continue;
				foreach (var attr in baseTypeDef.GetAttributes())
				{
					yield return attr;
				}
			}
		}

		internal static IAttribute? GetAttribute(ITypeDefinition typeDef, KnownAttribute attributeType)
		{
			foreach (var baseType in typeDef.GetNonInterfaceBaseTypes().Reverse())
			{
				ITypeDefinition? baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef == null)
					continue;
				var attr = baseTypeDef.GetAttribute(attributeType);
				if (attr != null)
					return attr;
			}
			return null;
		}

		internal static IEnumerable<IAttribute> GetAttributes(IMember member)
		{
			HashSet<IMember> visitedMembers = new HashSet<IMember>();
			while (true)
			{
				member = member.MemberDefinition; // it's sufficient to look at the definitions
				if (!visitedMembers.Add(member))
				{
					// abort if we seem to be in an infinite loop (cyclic inheritance)
					break;
				}
				foreach (var attr in member.GetAttributes())
				{
					yield return attr;
				}
				if (!member.IsOverride)
					break;
				var baseMember = GetBaseMember(member);
				if (baseMember == null)
					break;
				member = baseMember;
			}
		}

		internal static IAttribute? GetAttribute(IMember member, KnownAttribute attributeType)
		{
			HashSet<IMember> visitedMembers = new HashSet<IMember>();
			while (true)
			{
				member = member.MemberDefinition; // it's sufficient to look at the definitions
				if (!visitedMembers.Add(member))
				{
					// abort if we seem to be in an infinite loop (cyclic inheritance)
					break;
				}
				var attr = member.GetAttribute(attributeType);
				if (attr != null)
					return attr;
				if (!member.IsOverride)
					break;
				var baseMember = GetBaseMember(member);
				if (baseMember == null)
					break;
				member = baseMember;
			}
			return null;
		}
		#endregion
	}
}

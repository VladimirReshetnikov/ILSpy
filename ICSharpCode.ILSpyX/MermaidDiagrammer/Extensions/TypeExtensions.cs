// Copyright (c) 2024 Holger Schmidt
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyX.MermaidDiagrammer.Extensions
{
	internal static class TypeExtensions
	{
		/// <summary>
		/// Determines whether the type is the canonical <see cref="object"/> type.
		/// </summary>
		/// <param name="t">Type to inspect.</param>
		/// <returns><see langword="true"/> when <paramref name="t"/> is <see cref="KnownTypeCode.Object"/>.</returns>
		internal static bool IsObject(this IType t) => t.IsKnownType(KnownTypeCode.Object);

		/// <summary>
		/// Determines whether the type is an interface definition or interface instantiation.
		/// </summary>
		/// <param name="t">Type to inspect.</param>
		/// <returns><see langword="true"/> if <paramref name="t"/> has <see cref="TypeKind.Interface"/> kind.</returns>
		internal static bool IsInterface(this IType t) => t.Kind == TypeKind.Interface;

		/// <summary>
		/// Tries to unwrap <see cref="KnownTypeCode.NullableOfT"/> and returns its single type argument.
		/// </summary>
		/// <param name="type">Type that may represent <c>Nullable&lt;T&gt;</c>.</param>
		/// <param name="typeArg">When successful, receives the wrapped value type argument.</param>
		/// <returns><see langword="true"/> if <paramref name="type"/> is a nullable value type wrapper.</returns>
		internal static bool TryGetNullableType(this IType type, [MaybeNullWhen(false)] out IType typeArg)
		{
			bool isNullable = type.IsKnownType(KnownTypeCode.NullableOfT);
			typeArg = isNullable ? type.TypeArguments.Single() : null;
			return isNullable;
		}
	}

	internal static class MemberInfoExtensions
	{
		/// <summary>Groups the <paramref name="members"/> into a dictionary
		/// with <see cref="IMember.DeclaringType"/> keys.</summary>
		/// <param name="members">Members to group by their declaring type.</param>
		internal static Dictionary<IType, T[]> GroupByDeclaringType<T>(this IEnumerable<T> members) where T : IMember
			=> members.GroupByDeclaringType(m => m);

		/// <summary>Groups the <paramref name="objectsWithMembers"/> into a dictionary
		/// with <see cref="IMember.DeclaringType"/> keys using <paramref name="getMember"/>.</summary>
		/// <param name="objectsWithMembers">Objects whose associated members should be grouped.</param>
		/// <param name="getMember">Accessor that extracts the member used for grouping.</param>
		internal static Dictionary<IType, T[]> GroupByDeclaringType<T>(this IEnumerable<T> objectsWithMembers, Func<T, IMember> getMember)
			=> objectsWithMembers.GroupBy(m => getMember(m).DeclaringType).ToDictionary(g => g.Key, g => g.ToArray());
	}

	internal static class DictionaryExtensions
	{
		/// <summary>Returns the <paramref name="dictionary"/>s value for the specified <paramref name="key"/>
		/// if available and otherwise the default for <typeparamref name="Tout"/>.</summary>
		/// <param name="dictionary">Dictionary to query.</param>
		/// <param name="key">Lookup key.</param>
		internal static Tout? GetValue<T, Tout>(this IDictionary<T, Tout> dictionary, T key)
			=> dictionary.TryGetValue(key, out Tout? value) ? value : default;
	}
}

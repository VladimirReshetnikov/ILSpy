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
using System.Text;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents the metadata identity of a type definition, including nested-type segments.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> <see cref="FullTypeName"/> models only definition names. It does not encode assembly identity,
	/// generic type arguments, array/pointer/byref modifiers, or other constructed-type information.
	/// </para>
	/// <para>
	/// <b>Name forms.</b> <see cref="ReflectionName"/> uses <c>+</c> between nested segments and backtick arity suffixes;
	/// <see cref="FullName"/> uses <c>.</c> separators and omits arity suffixes.
	/// </para>
	/// <para>
	/// <b>Usage.</b> Use this type when traversing metadata handles or type-definition trees. For top-level-only keys,
	/// use <see cref="TopLevelTypeName"/>.
	/// </para>
	/// </remarks>
	[Serializable]
	public readonly struct FullTypeName : IEquatable<FullTypeName>
	{
		[Serializable]
		readonly struct NestedTypeName
		{
			public readonly string Name;
			public readonly int AdditionalTypeParameterCount;

			public NestedTypeName(string name, int additionalTypeParameterCount)
			{
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				this.Name = name;
				this.AdditionalTypeParameterCount = additionalTypeParameterCount;
			}
		}

		readonly TopLevelTypeName topLevelType;
		readonly NestedTypeName[] nestedTypes;

		FullTypeName(TopLevelTypeName topLevelTypeName, NestedTypeName[] nestedTypes)
		{
			this.topLevelType = topLevelTypeName;
			this.nestedTypes = nestedTypes;
		}

		/// <summary>
		/// Constructs a FullTypeName representing the given top-level type.
		/// </summary>
		/// <remarks>
		/// FullTypeName has an implicit conversion operator from TopLevelTypeName,
		/// so you can simply write:
		/// <c>FullTypeName f = new TopLevelTypeName(...);</c>
		/// </remarks>
		public FullTypeName(TopLevelTypeName topLevelTypeName)
		{
			this.topLevelType = topLevelTypeName;
			this.nestedTypes = null;
		}

		/// <summary>
		/// Constructs a FullTypeName by parsing the given reflection name.
		/// Note that FullTypeName can only represent type definition names. If the reflection name
		/// might refer to a parameterized type or array etc., use
		/// <see cref="ReflectionHelper.ParseReflectionName(string, ITypeResolveContext)"/> instead.
		/// </summary>
		/// <remarks>
		/// Expected syntax: <c>NamespaceName '.' TopLevelTypeName ['`'#] { '+' NestedTypeName ['`'#] }</c>
		/// where # are type parameter counts
		/// </remarks>
		public FullTypeName(string reflectionName)
		{
			int pos = reflectionName.IndexOf('+');
			if (pos < 0)
			{
				// top-level type
				this.topLevelType = new TopLevelTypeName(reflectionName);
				this.nestedTypes = null;
			}
			else
			{
				// nested type
				string[] parts = reflectionName.Split('+');
				this.topLevelType = new TopLevelTypeName(parts[0]);
				this.nestedTypes = new NestedTypeName[parts.Length - 1];
				for (int i = 0; i < nestedTypes.Length; i++)
				{
					int tpc;
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(parts[i + 1], out tpc);
					nestedTypes[i] = new NestedTypeName(name, tpc);
				}
			}
		}

		/// <summary>
		/// Gets the top-level type name.
		/// </summary>
		public TopLevelTypeName TopLevelTypeName {
			get { return topLevelType; }
		}

		/// <summary>
		/// Gets a value indicating whether at least one nested-type segment is present.
		/// </summary>
		/// <value><see langword="true"/> when this name refers to a nested definition; otherwise <see langword="false"/>.</value>
		public bool IsNested {
			get {
				return nestedTypes != null;
			}
		}

		/// <summary>
		/// Gets the nesting level.
		/// </summary>
		public int NestingLevel {
			get {
				return nestedTypes != null ? nestedTypes.Length : 0;
			}
		}

		/// <summary>
		/// Gets the name of the type.
		/// For nested types, this is the name of the innermost type.
		/// </summary>
		public string Name {
			get {
				if (nestedTypes != null)
					return nestedTypes[nestedTypes.Length - 1].Name;
				else
					return topLevelType.Name;
			}
		}

		/// <summary>
		/// Gets the reflection-style metadata name.
		/// </summary>
		/// <value>Namespace-qualified top-level name plus nested segments separated by <c>+</c>, including backtick arity suffixes.</value>
		public string ReflectionName {
			get {
				if (nestedTypes == null)
					return topLevelType.ReflectionName;
				StringBuilder b = new StringBuilder(topLevelType.ReflectionName);
				foreach (NestedTypeName nt in nestedTypes)
				{
					b.Append('+');
					b.Append(nt.Name);
					if (nt.AdditionalTypeParameterCount > 0)
					{
						b.Append('`');
						b.Append(nt.AdditionalTypeParameterCount);
					}
				}
				return b.ToString();
			}
		}

		/// <summary>
		/// Gets the dotted full name without generic arity suffixes.
		/// </summary>
		/// <value>The namespace-qualified type name using <c>.</c> for nested segments.</value>
		public string FullName {
			get {
				if (nestedTypes == null)
					return topLevelType.Namespace + "." + topLevelType.Name;
				StringBuilder b = new StringBuilder(topLevelType.Namespace);
				b.Append('.');
				b.Append(topLevelType.Name);
				foreach (NestedTypeName nt in nestedTypes)
				{
					b.Append('.');
					b.Append(nt.Name);
				}
				return b.ToString();
			}
		}

		/// <summary>
		/// Gets the total type parameter count.
		/// </summary>
		public int TypeParameterCount {
			get {
				int tpc = topLevelType.TypeParameterCount;
				if (nestedTypes != null)
				{
					foreach (var nt in nestedTypes)
					{
						tpc += nt.AdditionalTypeParameterCount;
					}
				}
				return tpc;
			}
		}

		/// <summary>
		/// Gets the simple metadata name of the nested segment at <paramref name="nestingLevel"/>.
		/// </summary>
		/// <param name="nestingLevel">Zero-based nesting level, where <c>0</c> is the first nested type under the top-level definition.</param>
		/// <returns>The simple name of the requested nested segment.</returns>
		/// <exception cref="InvalidOperationException">This instance represents a top-level type and has no nested segments.</exception>
		public string GetNestedTypeName(int nestingLevel)
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			return nestedTypes[nestingLevel].Name;
		}

		/// <summary>
		/// Gets the generic arity introduced by the nested segment at <paramref name="nestingLevel"/>.
		/// </summary>
		/// <param name="nestingLevel">Zero-based nesting level, where <c>0</c> is the first nested type under the top-level definition.</param>
		/// <returns>The number of additional type parameters declared by the selected nested type.</returns>
		/// <exception cref="InvalidOperationException">This instance represents a top-level type and has no nested segments.</exception>
		public int GetNestedTypeAdditionalTypeParameterCount(int nestingLevel)
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			return nestedTypes[nestingLevel].AdditionalTypeParameterCount;
		}

		/// <summary>
		/// Gets the declaring type name.
		/// </summary>
		/// <exception cref="InvalidOperationException">This is a top-level type name.</exception>
		/// <example><c>new FullTypeName("NS.A+B+C").GetDeclaringType()</c> will return <c>new FullTypeName("NS.A+B")</c></example>
		public FullTypeName GetDeclaringType()
		{
			if (nestedTypes == null)
				throw new InvalidOperationException();
			if (nestedTypes.Length == 1)
				return topLevelType;
			NestedTypeName[] outerNestedTypeNames = new NestedTypeName[nestedTypes.Length - 1];
			Array.Copy(nestedTypes, 0, outerNestedTypeNames, 0, outerNestedTypeNames.Length);
			return new FullTypeName(topLevelType, outerNestedTypeNames);
		}

		/// <summary>
		/// Returns a new name that appends a nested type segment to this instance.
		/// </summary>
		/// <param name="name">Simple metadata name of the nested type segment to append.</param>
		/// <param name="additionalTypeParameterCount">Generic arity introduced by the appended nested segment.</param>
		/// <returns>A new <see cref="FullTypeName"/> containing the additional nested segment.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
		/// <example><c>new FullTypeName("NS.A+B").NestedType("C", 1)</c> will return <c>new FullTypeName("NS.A+B+C`1")</c></example>
		public FullTypeName NestedType(string name, int additionalTypeParameterCount)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			var newNestedType = new NestedTypeName(name, additionalTypeParameterCount);
			if (nestedTypes == null)
				return new FullTypeName(topLevelType, new[] { newNestedType });
			NestedTypeName[] newNestedTypeNames = new NestedTypeName[nestedTypes.Length + 1];
			nestedTypes.CopyTo(newNestedTypeNames, 0);
			newNestedTypeNames[newNestedTypeNames.Length - 1] = newNestedType;
			return new FullTypeName(topLevelType, newNestedTypeNames);
		}

		/// <summary>
		/// Converts a top-level type name into a non-nested <see cref="FullTypeName"/>.
		/// </summary>
		/// <param name="topLevelTypeName">The top-level type identity to convert.</param>
		/// <returns>A <see cref="FullTypeName"/> with no nested segments.</returns>
		public static implicit operator FullTypeName(TopLevelTypeName topLevelTypeName)
		{
			return new FullTypeName(topLevelTypeName);
		}

		public override string ToString()
		{
			return this.ReflectionName;
		}

		#region Equals and GetHashCode implementation
		public override bool Equals(object obj)
		{
			return obj is FullTypeName && Equals((FullTypeName)obj);
		}

		public bool Equals(FullTypeName other)
		{
			return FullTypeNameComparer.Ordinal.Equals(this, other);
		}

		public override int GetHashCode()
		{
			return FullTypeNameComparer.Ordinal.GetHashCode(this);
		}

		public static bool operator ==(FullTypeName left, FullTypeName right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FullTypeName left, FullTypeName right)
		{
			return !left.Equals(right);
		}
		#endregion
	}

	/// <summary>
	/// Compares <see cref="FullTypeName"/> values using a configurable string comparison policy.
	/// </summary>
	/// <remarks>
	/// Comparison includes top-level namespace/name/arity and every nested segment name/arity pair.
	/// </remarks>
	[Serializable]
	public sealed class FullTypeNameComparer : IEqualityComparer<FullTypeName>
	{
		/// <summary>
		/// Case-sensitive comparer using ordinal string comparison for all name segments.
		/// </summary>
		public static readonly FullTypeNameComparer Ordinal = new FullTypeNameComparer(StringComparer.Ordinal);

		/// <summary>
		/// Case-insensitive comparer using ordinal-ignore-case semantics for all name segments.
		/// </summary>
		public static readonly FullTypeNameComparer OrdinalIgnoreCase = new FullTypeNameComparer(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the comparer used for namespace/type segment equality and hash code generation.
		/// </summary>
		public readonly StringComparer NameComparer;

		/// <summary>
		/// Initializes a comparer with the supplied segment comparison policy.
		/// </summary>
		/// <param name="nameComparer">Comparer applied to namespace, top-level name, and nested type names.</param>
		public FullTypeNameComparer(StringComparer nameComparer)
		{
			this.NameComparer = nameComparer;
		}

		public bool Equals(FullTypeName x, FullTypeName y)
		{
			if (x.NestingLevel != y.NestingLevel)
				return false;
			TopLevelTypeName topX = x.TopLevelTypeName;
			TopLevelTypeName topY = y.TopLevelTypeName;
			if (topX.TypeParameterCount == topY.TypeParameterCount
				&& NameComparer.Equals(topX.Name, topY.Name)
				&& NameComparer.Equals(topX.Namespace, topY.Namespace))
			{
				for (int i = 0; i < x.NestingLevel; i++)
				{
					if (x.GetNestedTypeAdditionalTypeParameterCount(i) != y.GetNestedTypeAdditionalTypeParameterCount(i))
						return false;
					if (!NameComparer.Equals(x.GetNestedTypeName(i), y.GetNestedTypeName(i)))
						return false;
				}
				return true;
			}
			return false;
		}

		public int GetHashCode(FullTypeName obj)
		{
			TopLevelTypeName top = obj.TopLevelTypeName;
			int hash = NameComparer.GetHashCode(top.Name) ^ NameComparer.GetHashCode(top.Namespace) ^ top.TypeParameterCount;
			unchecked
			{
				for (int i = 0; i < obj.NestingLevel; i++)
				{
					hash *= 31;
					hash += NameComparer.GetHashCode(obj.Name) ^ obj.TypeParameterCount;
				}
			}
			return hash;
		}
	}
}

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
	/// Represents the metadata identity of a non-nested type definition.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> Instances store exactly three metadata components: namespace, simple type name,
	/// and generic arity. The value does not encode nested-type segments, type arguments, pointer/array modifiers,
	/// or assembly identity.
	/// </para>
	/// <para>
	/// <b>Usage.</b> This value type is used as a stable dictionary key when indexing top-level definitions
	/// (for example in metadata and compilation caches). For nested definitions, use <see cref="FullTypeName"/>.
	/// </para>
	/// <para>
	/// <b>Comparison.</b> The built-in equality operators and <see cref="Equals(TopLevelTypeName)"/> use
	/// ordinal case-sensitive string comparison. Use <see cref="TopLevelTypeNameComparer.OrdinalIgnoreCase"/>
	/// when a case-insensitive policy is required.
	/// </para>
	/// </remarks>
	[Serializable]
	public readonly struct TopLevelTypeName : IEquatable<TopLevelTypeName>
	{
		readonly string namespaceName;
		readonly string name;
		readonly int typeParameterCount;

		/// <summary>
		/// Initializes a top-level type name from explicit namespace, type name, and arity components.
		/// </summary>
		/// <param name="namespaceName">Namespace containing the type. Use an empty string for the global namespace.</param>
		/// <param name="name">Metadata type name without namespace and without backtick-arity suffix.</param>
		/// <param name="typeParameterCount">Generic arity encoded in metadata for the type definition.</param>
		/// <exception cref="ArgumentNullException"><paramref name="namespaceName"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
		public TopLevelTypeName(string namespaceName, string name, int typeParameterCount = 0)
		{
			if (namespaceName == null)
				throw new ArgumentNullException(nameof(namespaceName));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			this.namespaceName = namespaceName;
			this.name = name;
			this.typeParameterCount = typeParameterCount;
		}

		/// <summary>
		/// Initializes a top-level type name by parsing a reflection-style metadata name.
		/// </summary>
		/// <param name="reflectionName">Type name in the format <c>Namespace.TypeName</c> with optional <c>`arity</c> suffix.</param>
		/// <remarks>
		/// The parser uses the last <c>.</c> separator to split namespace and type-name segments.
		/// Generic arity is read from a trailing backtick suffix (for example <c>List`1</c>).
		/// </remarks>
		public TopLevelTypeName(string reflectionName)
		{
			int pos = reflectionName.LastIndexOf('.');
			if (pos < 0)
			{
				namespaceName = string.Empty;
				name = reflectionName;
			}
			else
			{
				namespaceName = reflectionName.Substring(0, pos);
				name = reflectionName.Substring(pos + 1);
			}
			name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out typeParameterCount);
		}

		/// <summary>
		/// Gets the containing namespace.
		/// </summary>
		/// <value>The namespace portion of the metadata name, or an empty string for the global namespace.</value>
		public string Namespace {
			get { return namespaceName; }
		}

		/// <summary>
		/// Gets the metadata type name without namespace qualification.
		/// </summary>
		/// <value>The simple metadata name for the type definition without arity suffix.</value>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the declared generic arity for the top-level type.
		/// </summary>
		/// <value>The number of generic type parameters declared by the type definition.</value>
		public int TypeParameterCount {
			get { return typeParameterCount; }
		}

		/// <summary>
		/// Gets the reflection-style metadata name including namespace and generic arity suffix.
		/// </summary>
		/// <value>
		/// A string in reflection-name form (for example <c>System.Collections.Generic.List`1</c>).
		/// </value>
		public string ReflectionName {
			get {
				StringBuilder b = new StringBuilder();
				if (!string.IsNullOrEmpty(namespaceName))
				{
					b.Append(namespaceName);
					b.Append('.');
				}
				b.Append(name);
				if (typeParameterCount > 0)
				{
					b.Append('`');
					b.Append(typeParameterCount);
				}
				return b.ToString();
			}
		}

		/// <summary>
		/// Returns <see cref="ReflectionName"/>.
		/// </summary>
		/// <returns>The reflection-style metadata representation of this instance.</returns>
		public override string ToString()
		{
			return this.ReflectionName;
		}

		public override bool Equals(object obj)
		{
			return (obj is TopLevelTypeName) && Equals((TopLevelTypeName)obj);
		}

		/// <summary>
		/// Determines whether this instance and <paramref name="other"/> represent the same top-level metadata type.
		/// </summary>
		/// <param name="other">The candidate value to compare against.</param>
		/// <returns>
		/// <see langword="true"/> when namespace, simple name, and generic arity match exactly using ordinal semantics;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool Equals(TopLevelTypeName other)
		{
			return this.namespaceName == other.namespaceName && this.name == other.name && this.typeParameterCount == other.typeParameterCount;
		}

		/// <summary>
		/// Returns a hash code derived from namespace, simple name, and arity using ordinal string hashing.
		/// </summary>
		/// <returns>A hash code suitable for hash-based collections that use default equality semantics.</returns>
		public override int GetHashCode()
		{
			return (name != null ? name.GetHashCode() : 0) ^ (namespaceName != null ? namespaceName.GetHashCode() : 0) ^ typeParameterCount;
		}

		public static bool operator ==(TopLevelTypeName lhs, TopLevelTypeName rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(TopLevelTypeName lhs, TopLevelTypeName rhs)
		{
			return !lhs.Equals(rhs);
		}
	}

	/// <summary>
	/// Compares <see cref="TopLevelTypeName"/> values using a configurable string comparison policy.
	/// </summary>
	/// <remarks>
	/// Use this comparer when dictionary/set lookup policy must differ from the struct's default ordinal
	/// case-sensitive equality implementation.
	/// </remarks>
	[Serializable]
	public sealed class TopLevelTypeNameComparer : IEqualityComparer<TopLevelTypeName>
	{
		/// <summary>
		/// Case-sensitive comparer that uses ordinal string semantics for namespace and type name segments.
		/// </summary>
		public static readonly TopLevelTypeNameComparer Ordinal = new TopLevelTypeNameComparer(StringComparer.Ordinal);

		/// <summary>
		/// Case-insensitive comparer that uses ordinal-ignore-case semantics for namespace and type name segments.
		/// </summary>
		public static readonly TopLevelTypeNameComparer OrdinalIgnoreCase = new TopLevelTypeNameComparer(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the string comparer used for namespace/name comparisons and hash code generation.
		/// </summary>
		public readonly StringComparer NameComparer;

		/// <summary>
		/// Initializes a comparer with the provided string comparison policy.
		/// </summary>
		/// <param name="nameComparer">String comparer applied to both namespace and type name.</param>
		public TopLevelTypeNameComparer(StringComparer nameComparer)
		{
			this.NameComparer = nameComparer;
		}

		/// <summary>
		/// Determines whether two top-level type names are equal under this comparer policy.
		/// </summary>
		/// <param name="x">The first type name.</param>
		/// <param name="y">The second type name.</param>
		/// <returns>
		/// <see langword="true"/> when arity matches and both namespace/name segments are equal under <see cref="NameComparer"/>;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public bool Equals(TopLevelTypeName x, TopLevelTypeName y)
		{
			return x.TypeParameterCount == y.TypeParameterCount
				&& NameComparer.Equals(x.Name, y.Name)
				&& NameComparer.Equals(x.Namespace, y.Namespace);
		}

		/// <summary>
		/// Computes a hash code using <see cref="NameComparer"/> and the type arity.
		/// </summary>
		/// <param name="obj">The type name to hash.</param>
		/// <returns>A hash code consistent with <see cref="Equals(TopLevelTypeName, TopLevelTypeName)"/>.</returns>
		public int GetHashCode(TopLevelTypeName obj)
		{
			return NameComparer.GetHashCode(obj.Name) ^ NameComparer.GetHashCode(obj.Namespace) ^ obj.TypeParameterCount;
		}
	}
}

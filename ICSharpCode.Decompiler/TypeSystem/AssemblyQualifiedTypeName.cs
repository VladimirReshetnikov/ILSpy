// Copyright (c) 2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a type identity composed of a <see cref="FullTypeName"/> and an optional assembly name.
	/// </summary>
	/// <remarks>
	/// The textual form follows reflection-style assembly-qualified names:
	/// <c>Namespace.Type, AssemblyName</c>. If <see cref="AssemblyName"/> is empty, only the type name is emitted.
	/// </remarks>
	public struct AssemblyQualifiedTypeName : IEquatable<AssemblyQualifiedTypeName>
	{
		/// <summary>
		/// The simple assembly identity portion associated with <see cref="TypeName"/>.
		/// </summary>
		/// <remarks>
		/// This is typically a simple assembly name (for example <c>mscorlib</c>), not a full display name.
		/// </remarks>
		public readonly string AssemblyName;

		/// <summary>
		/// The full metadata type name portion of this qualified name.
		/// </summary>
		public readonly FullTypeName TypeName;

		/// <summary>
		/// Initializes a type/assembly pair from explicit components.
		/// </summary>
		/// <param name="typeName">The full type name portion.</param>
		/// <param name="assemblyName">The assembly name portion, or <see langword="null"/>/<see cref="string.Empty"/> for an unqualified name.</param>
		public AssemblyQualifiedTypeName(FullTypeName typeName, string assemblyName)
		{
			this.AssemblyName = assemblyName;
			this.TypeName = typeName;
		}

		/// <summary>
		/// Initializes a type/assembly pair from a resolved type definition.
		/// </summary>
		/// <param name="typeDefinition">Type definition whose full name and parent module assembly name are copied.</param>
		public AssemblyQualifiedTypeName(ITypeDefinition typeDefinition)
		{
			this.AssemblyName = typeDefinition.ParentModule.AssemblyName;
			this.TypeName = typeDefinition.FullTypeName;
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(AssemblyName))
				return TypeName.ToString();
			else
				return TypeName.ToString() + ", " + AssemblyName;
		}

		/// <summary>
		/// Determines whether this instance and <paramref name="obj"/> represent the same type/assembly pair.
		/// </summary>
		/// <param name="obj">Object to compare against.</param>
		/// <returns><see langword="true"/> when <paramref name="obj"/> is an equal <see cref="AssemblyQualifiedTypeName"/> value.</returns>
		public override bool Equals(object obj)
		{
			return (obj is AssemblyQualifiedTypeName) && Equals((AssemblyQualifiedTypeName)obj);
		}

		/// <summary>
		/// Determines whether this instance and <paramref name="other"/> represent the same type/assembly pair.
		/// </summary>
		/// <param name="other">Value to compare against.</param>
		/// <returns><see langword="true"/> when both <see cref="TypeName"/> and <see cref="AssemblyName"/> are equal.</returns>
		public bool Equals(AssemblyQualifiedTypeName other)
		{
			return this.AssemblyName == other.AssemblyName && this.TypeName == other.TypeName;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked
			{
				if (AssemblyName != null)
					hashCode += 1000000007 * AssemblyName.GetHashCode();
				hashCode += TypeName.GetHashCode();
			}
			return hashCode;
		}

		/// <summary>
		/// Compares two qualified names for value equality.
		/// </summary>
		/// <param name="lhs">Left-hand value.</param>
		/// <param name="rhs">Right-hand value.</param>
		/// <returns><see langword="true"/> when both operands have equal type and assembly components.</returns>
		public static bool operator ==(AssemblyQualifiedTypeName lhs, AssemblyQualifiedTypeName rhs)
		{
			return lhs.Equals(rhs);
		}

		/// <summary>
		/// Compares two qualified names for value inequality.
		/// </summary>
		/// <param name="lhs">Left-hand value.</param>
		/// <param name="rhs">Right-hand value.</param>
		/// <returns><see langword="true"/> when either the type name or assembly name differs.</returns>
		public static bool operator !=(AssemblyQualifiedTypeName lhs, AssemblyQualifiedTypeName rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
}

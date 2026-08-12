// Copyright (c) 2015 Siegfried Pammer
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
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Extension helpers for identifying compiler-generated symbols and retrieving metadata-backed documentation.
	/// </summary>
	/// <remarks>
	/// These helpers are used across decompiler transforms and output shaping to decide whether synthesized artifacts
	/// should be hidden, rewritten, or rendered using higher-level language constructs.
	/// </remarks>
	public static class NRExtensions
	{
		/// <summary>
		/// Determines whether an entity is marked with <see cref="KnownAttribute.CompilerGenerated"/>.
		/// </summary>
		/// <param name="entity">Entity to inspect.</param>
		/// <returns><see langword="true"/> when the compiler-generated attribute is present; otherwise <see langword="false"/>.</returns>
		public static bool IsCompilerGenerated(this IEntity entity)
		{
			if (entity != null)
			{
				return entity.HasAttribute(KnownAttribute.CompilerGenerated);
			}
			return false;
		}

		/// <summary>
		/// Determines whether an entity is compiler-generated itself or declared inside a compiler-generated type.
		/// </summary>
		/// <param name="entity">Entity to inspect.</param>
		/// <returns><see langword="true"/> if the entity or any declaring type is compiler-generated.</returns>
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IEntity entity)
		{
			if (entity == null)
				return false;
			if (entity.IsCompilerGenerated())
				return true;
			return IsCompilerGeneratedOrIsInCompilerGeneratedClass(entity.DeclaringTypeDefinition);
		}

		/// <summary>
		/// Checks whether a member name follows compiler-generated naming conventions.
		/// </summary>
		/// <param name="member">Member to inspect.</param>
		/// <returns><see langword="true"/> if the name starts with <c>&lt;</c>; otherwise <see langword="false"/>.</returns>
		public static bool HasGeneratedName(this IMember member)
		{
			return member.Name.StartsWith("<", StringComparison.Ordinal);
		}

		/// <summary>
		/// Checks whether a type name follows compiler-generated naming conventions.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <returns>
		/// <see langword="true"/> if the type name contains <c>&lt;</c>. Unlike the <see cref="IMember"/> overload,
		/// the character need not be a prefix.
		/// </returns>
		public static bool HasGeneratedName(this IType type)
		{
			return SRMExtensions.IsGeneratedName(type.Name);
		}

		/// <summary>
		/// A C# anonymous type is immutable and compares all of its members, so only an anonymous
		/// type with no settable property can be written as one. The C# compiler emits nothing
		/// else, but VB's properties are settable unless declared 'Key', and only 'Key' members
		/// take part in Equals/GetHashCode: those types keep their own declaration.
		/// </summary>
		static bool HasOnlyReadOnlyProperties(ITypeDefinition type)
		{
			foreach (var property in type.Properties)
			{
				if (property.CanSet)
					return false;
			}
			return true;
		}

		/// <summary>
		/// An anonymous type that keeps its own declaration because it cannot be written as a C#
		/// anonymous type: a VB anonymous type with at least one settable, non-'Key' property.
		/// </summary>
		public static bool IsAnonymousTypeDeclaredAsNamedType(this ITypeDefinition type)
		{
			return type != null
				&& string.IsNullOrEmpty(type.Namespace)
				&& type.HasGeneratedName()
				&& (type.Name.Contains("AnonType") || type.Name.Contains("AnonymousType"))
				&& type.IsCompilerGenerated()
				&& !HasOnlyReadOnlyProperties(type);
		}

		/// <summary>
		/// Determines whether a type matches the anonymous-type pattern used by C# and VB compilers.
		/// </summary>
		/// <param name="type">Type to inspect.</param>
		/// <returns><see langword="true"/> if the type appears to be an anonymous compiler-generated type.</returns>
		public static bool IsAnonymousType(this IType type)
		{
			if (type == null)
				return false;
			if (string.IsNullOrEmpty(type.Namespace) && type.HasGeneratedName()
				&& (type.Name.Contains("AnonType") || type.Name.Contains("AnonymousType")))
			{
				ITypeDefinition td = type.GetDefinition();
				return td != null && td.IsCompilerGenerated() && HasOnlyReadOnlyProperties(td);
			}
			return false;
		}

		/// <summary>
		/// Determines whether a type graph contains any anonymous type occurrence.
		/// </summary>
		/// <param name="type">Root type to traverse.</param>
		/// <returns><see langword="true"/> if an anonymous type is found anywhere in the visited structure.</returns>
		public static bool ContainsAnonymousType(this IType type)
		{
			var visitor = new ContainsAnonTypeVisitor();
			type.AcceptVisitor(visitor);
			return visitor.ContainsAnonType;
		}

		class ContainsAnonTypeVisitor : TypeVisitor
		{
			/// <summary>
			/// Gets whether any visited type matched the anonymous-type predicate.
			/// </summary>
			public bool ContainsAnonType;

			public override IType VisitOtherType(IType type)
			{
				if (IsAnonymousType(type))
					ContainsAnonType = true;
				return base.VisitOtherType(type);
			}

			public override IType VisitTypeDefinition(ITypeDefinition type)
			{
				if (IsAnonymousType(type))
					ContainsAnonType = true;
				return base.VisitTypeDefinition(type);
			}
		}

		/// <summary>
		/// Loads XML documentation text for an entity from the owning module's documentation provider.
		/// </summary>
		/// <param name="entity">Entity whose documentation should be retrieved.</param>
		/// <returns>
		/// Raw XML documentation content when available; otherwise <see langword="null"/>.
		/// </returns>
		internal static string GetDocumentation(this IEntity entity)
		{
			var docProvider = XmlDocLoader.LoadDocumentation(entity.ParentModule.MetadataFile);
			if (docProvider == null)
				return null;
			return docProvider.GetDocumentation(entity);
		}

		/// <summary>
		/// Reads raw metadata <see cref="System.Reflection.TypeAttributes"/> for a type definition.
		/// </summary>
		/// <param name="type">Type definition to inspect.</param>
		/// <returns>
		/// Metadata attributes from the declaring module when available; otherwise <c>0</c>.
		/// </returns>
		internal static System.Reflection.TypeAttributes GetMetadataAttributes(this ITypeDefinition type)
		{
			var metadata = type.ParentModule.MetadataFile?.Metadata;
			if (metadata == null || type.MetadataToken.IsNil)
				return 0;
			return metadata.GetTypeDefinition((TypeDefinitionHandle)type.MetadataToken).Attributes;
		}
	}
}

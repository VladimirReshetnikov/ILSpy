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
		/// <returns><see langword="true"/> if the type name is mangled in a compiler-generated style.</returns>
		public static bool HasGeneratedName(this IType type)
		{
			return type.Name.StartsWith("<", StringComparison.Ordinal) || type.Name.Contains("<");
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
				return td != null && td.IsCompilerGenerated();
			}
			return type.IsVBAnonymousType();
		}

		/// <summary>
		/// The VB compiler emits anonymous types as real named generic types called
		/// "VB$AnonymousType_N`K" in the global namespace, unlike the C# compiler which uses
		/// the "&lt;&gt;f__AnonymousType" naming convention recognized by <see cref="IsAnonymousType(IType)"/>.
		/// The shape is fixed: a compiler-generated generic type whose every type parameter is
		/// surfaced by exactly one gettable instance property of that type parameter's type, so the
		/// type can be reconstructed as a C# anonymous type "new { Name0 = ..., Name1 = ... }".
		/// </summary>
		public static bool IsVBAnonymousType(this IType type)
		{
			if (type == null)
				return false;
			if (!string.IsNullOrEmpty(type.Namespace))
				return false;
			if (!type.Name.StartsWith("VB$AnonymousType_", StringComparison.Ordinal))
				return false;
			ITypeDefinition td = type.GetDefinition();
			if (td == null || !td.IsCompilerGenerated())
				return false;
			int typeParameterCount = td.TypeParameterCount;
			if (typeParameterCount == 0)
				return false;
			// Each type parameter must be surfaced by exactly one gettable instance property of
			// that type parameter's type; the property carries the anonymous member's name.
			var coveredTypeParameters = new bool[typeParameterCount];
			int propertyCount = 0;
			foreach (IProperty property in td.Properties)
			{
				propertyCount++;
				if (property.IsStatic || !property.CanGet)
					return false;
				if (property.ReturnType is not ITypeParameter tp || tp.OwnerType != SymbolKind.TypeDefinition)
					return false;
				int index = tp.Index;
				if (index < 0 || index >= typeParameterCount || coveredTypeParameters[index])
					return false;
				coveredTypeParameters[index] = true;
			}
			return propertyCount == typeParameterCount;
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

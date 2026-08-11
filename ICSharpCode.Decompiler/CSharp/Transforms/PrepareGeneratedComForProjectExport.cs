// Copyright (c) 2026 Vladimir Reshetnikov
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

#nullable enable

using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Makes materialized COM source-generator output safe to export as a project.
	/// </summary>
	/// <remarks>This transform is only enabled when exporting a full assembly as a project.</remarks>
	class PrepareGeneratedComForProjectExport : IAstTransform
	{
		const string ComExposedClassAttribute = "System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute";
		const string GeneratedComClassAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute";
		const string GeneratedComInterfaceAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute";
		const string IUnknownDerivedAttribute = "System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute";

		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var declaration in rootNode.DescendantsAndSelf.OfType<TypeDeclaration>())
			{
				if (declaration.GetSymbol() is not ITypeDefinition type)
					continue;

				string? triggerAttribute = type.Kind switch {
					TypeKind.Interface when HasAttribute(type, GeneratedComInterfaceAttribute, typeParameterCount: 0)
						&& HasAttribute(type, IUnknownDerivedAttribute, typeParameterCount: 2)
						=> GeneratedComInterfaceAttribute,
					TypeKind.Class when HasAttribute(type, GeneratedComClassAttribute, typeParameterCount: 0)
						&& HasAttribute(type, ComExposedClassAttribute, typeParameterCount: 1)
						=> GeneratedComClassAttribute,
					_ => null
				};
				if (triggerAttribute == null || !RemoveAttribute(declaration, triggerAttribute))
					continue;

				if (context.Settings.CommentOutUnrepresentableMetadata)
				{
					declaration.AddLeadingTrivia(new Comment(
						$" {GetAttributeName(triggerAttribute)} attribute left out because its generated implementation is already present"));
				}
			}
		}

		static bool HasAttribute(IEntity entity, string fullName, int typeParameterCount)
		{
			return entity.GetAttributes().Any(attribute => attribute.AttributeType.FullName == fullName
				&& attribute.AttributeType.TypeParameterCount == typeParameterCount);
		}

		static bool RemoveAttribute(TypeDeclaration declaration, string fullName)
		{
			bool removed = false;
			foreach (var section in declaration.Attributes.ToArray())
			{
				foreach (var attribute in section.Attributes.ToArray())
				{
					var type = attribute.Type.Annotation<TypeResolveResult>()?.Type;
					if (type?.FullName != fullName)
						continue;
					attribute.Remove();
					removed = true;
				}
				if (section.Attributes.Count == 0)
					section.Remove();
			}
			return removed;
		}

		static string GetAttributeName(string fullName)
		{
			int namespaceSeparator = fullName.LastIndexOf('.');
			return fullName.Substring(namespaceSeparator + 1, fullName.Length - namespaceSeparator - "Attribute".Length - 1);
		}
	}
}

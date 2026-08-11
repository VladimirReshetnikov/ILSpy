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
	/// Makes embedded No-PIA COM event interfaces legal to emit as project source.
	/// </summary>
	/// <remarks>This transform is only enabled when exporting a full assembly as a project.</remarks>
	class PrepareEmbeddedInteropTypesForProjectExport : IAstTransform
	{
		const string CompilerGeneratedAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
		const string ComEventInterfaceAttribute = "System.Runtime.InteropServices.ComEventInterfaceAttribute";
		const string ComImportAttribute = "System.Runtime.InteropServices.ComImportAttribute";
		const string GuidAttribute = "System.Runtime.InteropServices.GuidAttribute";
		const string TypeIdentifierAttribute = "System.Runtime.InteropServices.TypeIdentifierAttribute";

		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var declaration in rootNode.DescendantsAndSelf.OfType<TypeDeclaration>())
			{
				if (declaration.GetSymbol() is not ITypeDefinition type
					|| !IsEmbeddedComEventInterfaceWithoutGuid(type)
					|| !RemoveAttribute(declaration, ComImportAttribute))
					continue;

				if (context.Settings.CommentOutUnrepresentableMetadata)
				{
					declaration.AddLeadingTrivia(new Comment(
						" ComImport attribute left out because this embedded COM event interface has no Guid"));
				}
			}
		}

		static bool IsEmbeddedComEventInterfaceWithoutGuid(ITypeDefinition type)
		{
			if (type.Kind != TypeKind.Interface
				|| !HasAttribute(type, ComImportAttribute)
				|| !HasAttribute(type, CompilerGeneratedAttribute)
				|| !HasComEventInterfaceAttribute(type)
				|| HasAttribute(type, GuidAttribute))
				return false;

			return type.GetAttributes().Any(attribute => attribute.AttributeType.FullName == TypeIdentifierAttribute
				&& !attribute.HasDecodeErrors
				&& attribute.FixedArguments.Length == 2
				&& attribute.FixedArguments[0].Value is string
				&& attribute.FixedArguments[1].Value is string);
		}

		static bool HasComEventInterfaceAttribute(IEntity entity)
		{
			return entity.GetAttributes().Any(attribute => attribute.AttributeType.FullName == ComEventInterfaceAttribute
				&& !attribute.HasDecodeErrors
				&& attribute.FixedArguments.Length == 2);
		}

		static bool HasAttribute(IEntity entity, string fullName)
		{
			return entity.GetAttributes().Any(attribute => attribute.AttributeType.FullName == fullName);
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
	}
}

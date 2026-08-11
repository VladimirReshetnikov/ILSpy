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

using System;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Makes the output of System.Text.RegularExpressions.Generator safe to export as a project.
	/// </summary>
	/// <remarks>This transform is only enabled when exporting a full assembly as a project.</remarks>
	class PrepareGeneratedRegexForProjectExport : IAstTransform
	{
		const string GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
		const string GeneratedRegexAttribute = "System.Text.RegularExpressions.GeneratedRegexAttribute";
		const string RegexGeneratorName = "System.Text.RegularExpressions.Generator";

		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var declaration in rootNode.DescendantsAndSelf.OfType<EntityDeclaration>())
			{
				if (declaration.GetSymbol() is not IEntity entity || !IsRegexGeneratorOutput(entity))
					continue;

				if (declaration is TypeDeclaration && declaration.HasModifier(Modifiers.File))
				{
					// The generator puts its file-local helpers and the partial implementations that
					// reference them in one generated file. Project export instead writes the completed
					// user type to its own file, so 'file' would hide the helper from its caller. The
					// metadata name is unique before it is unmangled; restoring metadata accessibility
					// keeps the source name usable across the generated project.
					declaration.Modifiers &= ~Modifiers.File;
					declaration.Modifiers |= TypeSystemAstBuilder.ModifierFromAccessibility(
						entity.Accessibility, context.Settings.IntroducePrivateProtectedAccessibility);
				}
				else if (RemoveGeneratedRegexAttributes(declaration)
					&& context.Settings.CommentOutUnrepresentableMetadata)
				{
					// Replaying this attribute on the already-generated concrete member asks the source
					// generator to implement it again. The generator rejects that shape with SYSLIB1043.
					declaration.AddLeadingTrivia(new Comment(
						" GeneratedRegex attribute left out because its generated implementation is already present"));
				}
			}
		}

		static bool IsRegexGeneratorOutput(IEntity entity)
		{
			foreach (var attribute in entity.GetAttributes())
			{
				if (attribute.AttributeType.FullName == GeneratedCodeAttribute
					&& !attribute.HasDecodeErrors
					&& attribute.FixedArguments.Length >= 1
					&& attribute.FixedArguments[0].Value is string generatorName
					&& string.Equals(generatorName, RegexGeneratorName, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		static bool RemoveGeneratedRegexAttributes(EntityDeclaration declaration)
		{
			bool removed = false;
			foreach (var section in declaration.Attributes.ToArray())
			{
				foreach (var attribute in section.Attributes.ToArray())
				{
					var type = attribute.Type.Annotation<TypeResolveResult>()?.Type;
					if (type?.FullName != GeneratedRegexAttribute)
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

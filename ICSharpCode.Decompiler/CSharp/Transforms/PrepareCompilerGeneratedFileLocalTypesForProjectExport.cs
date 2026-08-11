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
	/// Makes compiler-generated file-local helper types usable after exporting a whole project.
	/// </summary>
	/// <remarks>This transform is only enabled when exporting a full assembly as a project.</remarks>
	class PrepareCompilerGeneratedFileLocalTypesForProjectExport : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var declaration in rootNode.DescendantsAndSelf.OfType<TypeDeclaration>())
			{
				if (!declaration.HasModifier(Modifiers.File)
					|| declaration.GetSymbol() is not ITypeDefinition type
					|| (!type.IsCompilerGenerated() && !ComesFromPrivateImplementationDetailsFile(type)))
				{
					continue;
				}

				// Source generators and compiler features may put a file-local helper beside the
				// partial user type that calls it. Whole-project export writes those top-level types
				// to different files, where the original 'file' accessibility would hide the helper.
				// The mangled metadata name is unique, so restoring metadata accessibility makes the
				// explicit cross-file reference legal without risking a source-name collision.
				declaration.Modifiers &= ~Modifiers.File;
				declaration.Modifiers |= TypeSystemAstBuilder.ModifierFromAccessibility(
					type.Accessibility, context.Settings.IntroducePrivateProtectedAccessibility);
			}
		}

		static bool ComesFromPrivateImplementationDetailsFile(ITypeDefinition type)
		{
			return FileLocalTypeName.TryParse(type.MetadataName, out _, out string? fileHash)
				&& fileHash.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal);
		}
	}
}

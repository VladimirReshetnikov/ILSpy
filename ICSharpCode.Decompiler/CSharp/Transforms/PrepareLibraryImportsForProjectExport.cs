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
	/// Prevents LibraryImportGenerator from replaying implementations that are already present
	/// when exporting an assembly as a project.
	/// </summary>
	/// <remarks>This transform is only enabled when exporting a full assembly as a project.</remarks>
	class PrepareLibraryImportsForProjectExport : IAstTransform
	{
		const string DllImportAttribute = "System.Runtime.InteropServices.DllImportAttribute";
		const string GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
		const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
		const string LibraryImportGeneratorName = "Microsoft.Interop.LibraryImportGenerator";

		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var declaration in rootNode.DescendantsAndSelf.OfType<MethodDeclaration>())
			{
				if (!HasLibraryImportAttribute(declaration)
					|| declaration.GetSymbol() is not IMethod method
					|| !HasMaterializedImplementation(method))
					continue;

				if (RemoveLibraryImportAttributes(declaration)
					&& context.Settings.CommentOutUnrepresentableMetadata)
				{
					declaration.AddLeadingTrivia(new Comment(
						" LibraryImport attribute left out because its implementation is already present"));
				}
			}
		}

		static bool HasLibraryImportAttribute(MethodDeclaration declaration)
		{
			foreach (var section in declaration.Attributes)
			{
				foreach (var attribute in section.Attributes)
				{
					var type = attribute.Type.Annotation<TypeResolveResult>()?.Type;
					if (type?.FullName == LibraryImportAttribute)
						return true;
				}
			}
			return false;
		}

		static bool HasMaterializedImplementation(IMethod method)
		{
			bool isLibraryImportGeneratorOutput = false;
			foreach (var attribute in method.GetAttributes())
			{
				if (attribute.AttributeType.FullName == DllImportAttribute)
					return true;

				if (attribute.AttributeType.FullName == GeneratedCodeAttribute
					&& !attribute.HasDecodeErrors
					&& attribute.FixedArguments.Length >= 1
					&& attribute.FixedArguments[0].Value is string generatorName
					&& string.Equals(generatorName, LibraryImportGeneratorName, StringComparison.Ordinal))
				{
					isLibraryImportGeneratorOutput = true;
				}
			}
			return method.HasBody && isLibraryImportGeneratorOutput;
		}

		static bool RemoveLibraryImportAttributes(MethodDeclaration declaration)
		{
			bool removed = false;
			foreach (var section in declaration.Attributes.ToArray())
			{
				foreach (var attribute in section.Attributes.ToArray())
				{
					var type = attribute.Type.Annotation<TypeResolveResult>()?.Type;
					if (type?.FullName != LibraryImportAttribute)
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

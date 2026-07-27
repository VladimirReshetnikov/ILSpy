// Copyright (c) 2018 Siegfried Pammer
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

using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyX.Analyzers
{
	/// <summary>
	/// Shared low-level helper methods used by multiple analyzer implementations.
	/// </summary>
	internal static class AnalyzerHelpers
	{
		/// <summary>
		/// Performs a cheap metadata-level check to determine whether a handle might reference a target method.
		/// </summary>
		/// <param name="member">Candidate metadata handle read from IL operand data.</param>
		/// <param name="module">Module that owns <paramref name="member"/>.</param>
		/// <param name="analyzedMethod">Method currently being searched for.</param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="member"/> could resolve to <paramref name="analyzedMethod"/>;
		/// otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// This method intentionally favors speed over precision so analyzers can skip expensive resolution for
		/// clearly unrelated operands.
		/// </remarks>
		public static bool IsPossibleReferenceTo(EntityHandle member, MetadataFile module, IMethod analyzedMethod)
		{
			if (member.IsNil)
				return false;
			MetadataReader metadata = module.Metadata;
			switch (member.Kind)
			{
				case HandleKind.MethodDefinition:
					return member == analyzedMethod.MetadataToken
						&& module == analyzedMethod.ParentModule?.MetadataFile;
				case HandleKind.MemberReference:
					var mr = metadata.GetMemberReference((MemberReferenceHandle)member);
					if (mr.GetKind() != MemberReferenceKind.Method)
						return false;
					return metadata.StringComparer.Equals(mr.Name, analyzedMethod.Name);
				case HandleKind.MethodSpecification:
					var ms = metadata.GetMethodSpecification((MethodSpecificationHandle)member);
					return IsPossibleReferenceTo(ms.Method, module, analyzedMethod);
				default:
					return false;
			}
		}

		/// <summary>
		/// Resolves the symbol that owns a custom attribute row.
		/// </summary>
		/// <param name="ts">Type system used to resolve metadata handles to symbols.</param>
		/// <param name="customAttribute">Custom attribute row whose owner should be returned.</param>
		/// <returns>
		/// The owning symbol for the attribute, or <see langword="null"/> when the owner kind cannot be mapped.
		/// </returns>
		public static ISymbol? GetParentEntity(DecompilerTypeSystem ts, CustomAttribute customAttribute)
		{
			var metadata = ts.MainModule.MetadataFile.Metadata;
			switch (customAttribute.Parent.Kind)
			{
				case HandleKind.MethodDefinition:
					IMethod parent = (IMethod)ts.MainModule.ResolveEntity(customAttribute.Parent);
					return parent?.AccessorOwner ?? parent;
				case HandleKind.FieldDefinition:
				case HandleKind.PropertyDefinition:
				case HandleKind.EventDefinition:
				case HandleKind.TypeDefinition:
					return ts.MainModule.ResolveEntity(customAttribute.Parent);
				case HandleKind.AssemblyDefinition:
				case HandleKind.ModuleDefinition:
					return ts.MainModule;
				case HandleKind.GenericParameterConstraint:
					var gpc = metadata.GetGenericParameterConstraint((GenericParameterConstraintHandle)customAttribute.Parent);
					var gp = metadata.GetGenericParameter(gpc.Parameter);
					return ts.MainModule.ResolveEntity(gp.Parent);
				case HandleKind.GenericParameter:
					gp = metadata.GetGenericParameter((GenericParameterHandle)customAttribute.Parent);
					return ts.MainModule.ResolveEntity(gp.Parent);
				default:
					return null;
			}
		}
	}
}

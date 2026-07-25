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

using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Drops repeated applications of an attribute whose AttributeUsage does not allow multiple.
	///
	/// Metadata can carry them - a Visual Basic Module ends up with two StandardModuleAttribute rows,
	/// for instance - but C# has no syntax for the second application and rejects one (CS0579). The
	/// first application is the one kept; where the repeats differ in their arguments there is
	/// nothing better to do, since none of them can be written alongside another.
	///
	/// This runs at the end of the pipeline rather than while the attributes are built, because the
	/// decompiler removes particular applications of its own along the way: an async method carries
	/// the compiler's DebuggerStepThrough as well as any the source declared, and it is the first of
	/// the two that gets dropped.
	/// </summary>
	class RemoveRepeatedAttributes : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var node in rootNode.DescendantsAndSelf)
			{
				RemoveRepeatsOn(node);
			}
		}

		static void RemoveRepeatsOn(AstNode owner)
		{
			var sections = owner.Children.OfType<AttributeSection>().ToArray();
			if (sections.Length == 0)
				return;
			HashSet<(string?, string)>? seen = null;
			foreach (var section in sections)
			{
				foreach (var attribute in section.Attributes.ToArray())
				{
					var attributeType = attribute.Type.GetResolveResult().Type;
					if (AllowsMultipleApplications(attributeType))
						continue;
					seen ??= new HashSet<(string?, string)>();
					if (!seen.Add((section.AttributeTarget, attributeType.ReflectionName)))
						attribute.Remove();
				}
				if (section.Attributes.Count == 0)
					section.Remove();
			}
		}

		static bool AllowsMultipleApplications(IType attributeType)
		{
			var definition = attributeType.GetDefinition();
			if (definition == null)
				return true; // an unresolved attribute says nothing; keep every application
			foreach (var usage in definition.GetAttributes())
			{
				if (usage.AttributeType.FullName != "System.AttributeUsageAttribute")
					continue;
				foreach (var argument in usage.NamedArguments)
				{
					if (argument.Name == "AllowMultiple")
						return argument.Value is true;
				}
				break;
			}
			return false; // AttributeUsage defaults AllowMultiple to false, as does its absence
		}
	}
}

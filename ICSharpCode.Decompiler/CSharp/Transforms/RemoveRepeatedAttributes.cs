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
				RemoveRepeatsOn(node, context.Settings.CommentOutUnrepresentableMetadata);
			}
		}

		static void RemoveRepeatsOn(AstNode owner, bool commentOutDropped)
		{
			var sections = owner.Children.OfType<AttributeSection>().ToArray();
			if (sections.Length == 0)
				return;
			HashSet<(string?, string)>? seen = null;
			var dropped = new List<string>();
			foreach (var section in sections)
			{
				foreach (var attribute in section.Attributes.ToArray())
				{
					var attributeType = attribute.Type.GetResolveResult().Type;
					if (AllowsMultipleApplications(attributeType))
						continue;
					seen ??= new HashSet<(string?, string)>();
					if (!seen.Add((section.AttributeTarget, attributeType.ReflectionName)))
					{
						// The repeat is real metadata that no C# declaration can carry. Dropping it
						// without a word would make the output look like the assembly holds one
						// application where it holds several.
						dropped.Add(DescribeAttribute(section.AttributeTarget, attribute));
						attribute.Remove();
					}
				}
				if (section.Attributes.Count == 0)
					section.Remove();
			}
			if (commentOutDropped)
			{
				foreach (var description in dropped)
				{
					owner.AddLeadingTrivia(new Comment(" repeated attribute: " + description));
				}
			}
		}

		/// <summary>
		/// Renders a dropped attribute the way it would have been written. A section without a
		/// target reports it as the empty string rather than null, so both stand for "no target".
		/// </summary>
		static string DescribeAttribute(string? target, Syntax.Attribute attribute)
		{
			var text = attribute.ToString();
			return string.IsNullOrEmpty(target) ? "[" + text + "]" : "[" + target + ": " + text + "]";
		}

		/// <summary>
		/// Returns whether the attribute may be applied more than once to the same declaration.
		/// AttributeUsage is itself an inherited attribute, so an attribute class that declares none
		/// is governed by the nearest base class that does - which is why the base chain is walked
		/// from the attribute type outwards rather than only its own attributes being read.
		/// </summary>
		static bool AllowsMultipleApplications(IType attributeType)
		{
			// An unresolved attribute says nothing about itself, so keep every application.
			if (attributeType.GetDefinition() == null)
				return true;
			// GetNonInterfaceBaseTypes lists base types before derived ones, so reversing it puts the
			// attribute type first and walks outwards to Attribute/object.
			foreach (var type in attributeType.GetNonInterfaceBaseTypes().Reverse())
			{
				foreach (var usage in type.GetDefinition()?.GetAttributes() ?? Enumerable.Empty<IAttribute>())
				{
					if (usage.AttributeType.FullName != "System.AttributeUsageAttribute")
						continue;
					foreach (var argument in usage.NamedArguments)
					{
						if (argument.Name == "AllowMultiple")
							return argument.Value is true;
					}
					return false; // an AttributeUsage without AllowMultiple leaves it at its false default
				}
			}
			return false; // no AttributeUsage in the whole chain, which also means AllowMultiple is false
		}
	}
}

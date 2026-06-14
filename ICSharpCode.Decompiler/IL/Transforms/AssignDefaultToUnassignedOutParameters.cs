// Copyright (c) 2026 Daniel Grunwald
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

using System.Collections.Generic;

using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Prepends <c>outParam = default;</c> at the start of a method for every 'out' parameter
	/// whose value is not definitely written on every path that returns from the method.
	///
	/// C# requires an 'out' parameter to be definitely assigned before the method returns, but the
	/// metadata for a parameter that overrides or implements an 'out' contract does not guarantee
	/// this: a body produced by a language that does not enforce definite assignment (e.g. Visual
	/// Basic) can return without writing the value. Rendering such a body verbatim would not compile
	/// (CS0177), so a leading default assignment is injected to restore definite assignment without
	/// changing observable behavior on the paths that do write the value.
	///
	/// A C# 'out' parameter is unaffected: the C# compiler already proved it definitely assigned, so
	/// the analysis never flags it and no assignment is added.
	/// </summary>
	public class AssignDefaultToUnassignedOutParameters : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			if (function.Kind != ILFunctionKind.TopLevelFunction)
				return;
			if (function.Method == null)
				return;

			List<ILVariable> outParameters = null;
			foreach (var v in function.Variables)
			{
				if (v.Kind != VariableKind.Parameter || v.Index is not int index || index < 0)
					continue;
				if (index >= function.Method.Parameters.Count)
					continue;
				if (function.Method.Parameters[index].ReferenceKind != ReferenceKind.Out)
					continue;
				if (v.Type is not ByReferenceType)
					continue;
				(outParameters ??= new List<ILVariable>()).Add(v);
			}
			if (outParameters == null)
				return;

			var visitor = new OutParameterAssignmentVisitor(function, outParameters, context.CancellationToken);
			function.AcceptVisitor(visitor);

			var entryPoint = ((BlockContainer)function.Body).EntryPoint;
			int inserted = 0;
			foreach (var outParameter in outParameters)
			{
				if (!visitor.IsPotentiallyUnwrittenOnReturn(outParameter))
					continue;
				context.Step($"Default-initialize unassigned out parameter {outParameter.Name}", function);
				IType elementType = ((ByReferenceType)outParameter.Type).ElementType;
				var assignment = new StObj(new LdLoc(outParameter), new DefaultValue(elementType), elementType);
				entryPoint.Instructions.Insert(inserted++, assignment);
			}
		}
	}
}

// Copyright (c) 2025 ICSharpCode
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

using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
	/// <summary>
	/// Tuple element names are carried by <c>TupleElementNamesAttribute</c> on fields, parameters
	/// and return types, but never on local-variable signatures. A local declared with a tuple type
	/// therefore loses its element names, even though the value assigned to it (e.g. the result of a
	/// method whose return type names the elements) carries them. Accessing such an element by name
	/// then fails to compile (CS1061), because the rendered local type is unnamed while a use site
	/// refers to the element by name.
	///
	/// This transform recovers the names: for a local whose tuple type lacks element names, it copies
	/// the names from the values stored into the local, provided every named store agrees and the
	/// underlying (name-erased) types match. The recovered names make explicit method type arguments
	/// and query range variables derived from the local consistent with the by-name member accesses.
	/// </summary>
	class IntroduceTupleElementNamesOnLocals : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			if (!context.Settings.TupleTypes)
				return;
			foreach (var nestedFunction in function.Descendants.OfType<ILFunction>())
			{
				Dictionary<int, IType> variableTypeMapping = new();
				foreach (var variable in nestedFunction.Variables)
				{
					if (variable.Kind != VariableKind.Local &&
						variable.Kind != VariableKind.StackSlot &&
						variable.Kind != VariableKind.ForeachLocal &&
						variable.Kind != VariableKind.UsingLocal)
					{
						continue;
					}
					if (!ContainsUnnamedTupleElement(variable.Type))
						continue;
					IType? merged = null;
					bool conflict = false;
					foreach (var store in variable.StoreInstructions)
					{
						if (store is not StLoc stloc)
							continue;
						var sourceType = stloc.Value.InferType(context.TypeSystem);
						if (!ContainsNamedTupleElement(sourceType))
							continue;
						var candidate = TupleType.MergeTupleElementNames(merged ?? variable.Type, sourceType);
						if (candidate == null)
						{
							conflict = true;
							break;
						}
						merged = candidate;
					}
					if (conflict || merged == null || merged.Equals(variable.Type))
						continue;
					variable.Type = merged;
					if (variable.Kind == VariableKind.Local && variable.Index.HasValue)
						variableTypeMapping[variable.Index.Value] = merged;
				}
				foreach (var variable in nestedFunction.Variables)
				{
					if (variable.Kind == VariableKind.Local && variable.Index.HasValue
						&& variableTypeMapping.TryGetValue(variable.Index.Value, out var type))
					{
						variable.Type = type;
					}
				}
			}
		}

		static bool ContainsUnnamedTupleElement(IType type)
		{
			switch (type)
			{
				case TupleType tuple:
					if (tuple.ElementNames.Any(n => n == null))
						return true;
					return tuple.ElementTypes.Any(ContainsUnnamedTupleElement);
				case ParameterizedType pt:
					return pt.TypeArguments.Any(ContainsUnnamedTupleElement);
				case ArrayType array:
					return ContainsUnnamedTupleElement(array.ElementType);
				case ByReferenceType byRef:
					return ContainsUnnamedTupleElement(byRef.ElementType);
				default:
					return false;
			}
		}

		static bool ContainsNamedTupleElement(IType type)
		{
			switch (type)
			{
				case TupleType tuple:
					if (tuple.ElementNames.Any(n => n != null))
						return true;
					return tuple.ElementTypes.Any(ContainsNamedTupleElement);
				case ParameterizedType pt:
					return pt.TypeArguments.Any(ContainsNamedTupleElement);
				case ArrayType array:
					return ContainsNamedTupleElement(array.ElementType);
				case ByReferenceType byRef:
					return ContainsNamedTupleElement(byRef.ElementType);
				default:
					return false;
			}
		}
	}
}

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
using System.Collections.Immutable;
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
						var candidate = MergeTupleElementNames(merged ?? variable.Type, sourceType);
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

		/// <summary>
		/// Returns a copy of <paramref name="target"/> whose tuple element names are taken from
		/// <paramref name="source"/> wherever target lacks a name and source provides one. The element
		/// types of target are preserved (so its nullability/dynamic stays intact); only names are added.
		/// Returns null if the two types disagree on a name that is already present in both, which would
		/// make the recovered type inconsistent.
		/// </summary>
		static IType? MergeTupleElementNames(IType target, IType source)
		{
			switch (target)
			{
				case TupleType targetTuple when source is TupleType sourceTuple
					&& targetTuple.ElementTypes.Length == sourceTuple.ElementTypes.Length:
				{
					var newElementTypes = ImmutableArray.CreateBuilder<IType>(targetTuple.ElementTypes.Length);
					var newElementNames = ImmutableArray.CreateBuilder<string>(targetTuple.ElementTypes.Length);
					bool changed = false;
					for (int i = 0; i < targetTuple.ElementTypes.Length; i++)
					{
						var mergedElement = MergeTupleElementNames(targetTuple.ElementTypes[i], sourceTuple.ElementTypes[i]);
						if (mergedElement == null)
							return null;
						if (!mergedElement.Equals(targetTuple.ElementTypes[i]))
							changed = true;
						newElementTypes.Add(mergedElement);

						string? targetName = targetTuple.ElementNames[i];
						string? sourceName = sourceTuple.ElementNames[i];
						if (targetName == null && sourceName != null)
						{
							newElementNames.Add(sourceName);
							changed = true;
						}
						else if (targetName != null && sourceName != null && targetName != sourceName)
						{
							return null;
						}
						else
						{
							newElementNames.Add(targetName!);
						}
					}
					if (!changed)
						return target;
					return new TupleType(
						targetTuple.Compilation,
						newElementTypes.MoveToImmutable(),
						newElementNames.MoveToImmutable(),
						targetTuple.GetDefinition()?.ParentModule);
				}
				case ParameterizedType targetPt when source is ParameterizedType sourcePt
					&& targetPt.TypeParameterCount == sourcePt.TypeParameterCount
					&& targetPt.GenericType.Equals(sourcePt.GenericType):
				{
					var newArguments = new IType[targetPt.TypeArguments.Count];
					bool changed = false;
					for (int i = 0; i < targetPt.TypeArguments.Count; i++)
					{
						var mergedArgument = MergeTupleElementNames(targetPt.TypeArguments[i], sourcePt.TypeArguments[i]);
						if (mergedArgument == null)
							return null;
						if (!mergedArgument.Equals(targetPt.TypeArguments[i]))
							changed = true;
						newArguments[i] = mergedArgument;
					}
					if (!changed)
						return target;
					return new ParameterizedType(targetPt.GenericType, newArguments);
				}
				case ArrayType targetArray when source is ArrayType sourceArray
					&& targetArray.Dimensions == sourceArray.Dimensions:
				{
					var mergedElement = MergeTupleElementNames(targetArray.ElementType, sourceArray.ElementType);
					if (mergedElement == null)
						return null;
					if (mergedElement.Equals(targetArray.ElementType))
						return target;
					return new ArrayType(targetArray.Compilation, mergedElement, targetArray.Dimensions, targetArray.Nullability);
				}
				case ByReferenceType targetByRef when source is ByReferenceType sourceByRef:
				{
					var mergedElement = MergeTupleElementNames(targetByRef.ElementType, sourceByRef.ElementType);
					if (mergedElement == null)
						return null;
					if (mergedElement.Equals(targetByRef.ElementType))
						return target;
					return new ByReferenceType(mergedElement);
				}
				default:
					return target;
			}
		}
	}
}

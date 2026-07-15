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
using ICSharpCode.Decompiler.TypeSystem.Implementation;

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
	/// underlying (name-erased) types match. It also uses names on generated delegate-method input
	/// parameters when the local is the source of a generic extension-method call. The recovered names
	/// make explicit method type arguments and query range variables derived from the local consistent
	/// with the by-name member accesses.
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
					IType merged = variable.Type;
					bool foundEvidence = false;
					bool foundConsumerEvidence = false;
					bool conflict = false;
					foreach (var store in variable.StoreInstructions)
					{
						if (store is not StLoc stloc)
							continue;
						var sourceType = stloc.Value.InferType(context.TypeSystem);
						if (!ContainsNamedTupleElement(sourceType))
							continue;
						var candidate = TupleType.MergeTupleElementNames(merged, sourceType);
						if (candidate == null)
						{
							conflict = true;
							break;
						}
						merged = candidate;
						foundEvidence = true;
					}
					if (!conflict)
					{
						foreach (var sourceType in GetConsumerTupleNameCandidates(variable))
						{
							var candidate = TupleType.MergeTupleElementNames(merged, sourceType);
							if (candidate == null)
							{
								conflict = true;
								break;
							}
							merged = candidate;
							foundEvidence = true;
							foundConsumerEvidence = true;
						}
					}
					if (conflict || !foundEvidence || merged.Equals(variable.Type))
						continue;
					variable.Type = merged;
					if (foundConsumerEvidence)
						PropagateTupleNamesToSourceCalls(variable);
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

		static void PropagateTupleNamesToSourceCalls(ILVariable variable)
		{
			foreach (var load in variable.LoadInstructions.ToArray())
			{
				ILInstruction source = load;
				IType sourceType = variable.Type;
				while (true)
				{
					if (source.Parent is Box box && source.ChildIndex == 0
						&& NormalizeTypeVisitor.TypeErasure.EquivalentTypes(box.Type, sourceType))
					{
						source = box;
					}
					if (source.Parent is not CallInstruction call || source.ChildIndex != 0
						|| call.Method.MemberDefinition is not IMethod methodDefinition
						|| !call.Method.IsStatic || !methodDefinition.IsExtensionMethod
						|| methodDefinition.Parameters.Count == 0
						|| call.Method.TypeArguments.Count != methodDefinition.TypeParameters.Count)
					{
						break;
					}

					var sourceTypeParameters = GetMethodTypeParameterIndices(methodDefinition.Parameters[0].Type);
					if (sourceTypeParameters.Count == 0)
						break;
					var typeArguments = call.Method.TypeArguments.ToArray();
					bool changed = false;
					foreach (int typeParameterIndex in sourceTypeParameters)
					{
						var namedType = FindUniqueNamedEquivalentType(sourceType, typeArguments[typeParameterIndex]);
						if (namedType == null || namedType.Equals(typeArguments[typeParameterIndex]))
							continue;
						typeArguments[typeParameterIndex] = namedType;
						changed = true;
					}
					if (changed)
					{
						var substitution = new TypeParameterSubstitution(call.Method.Substitution.ClassTypeArguments, typeArguments);
						var newMethod = methodDefinition.Specialize(substitution);
						var newCall = CallInstruction.Create(call.OpCode, newMethod);
						newCall.ConstrainedTo = call.ConstrainedTo;
						newCall.ILStackWasEmpty = call.ILStackWasEmpty;
						newCall.IsTail = call.IsTail;
						newCall.AddILRange(call);
						newCall.Arguments.ReplaceList(call.Arguments);
						call.ReplaceWith(newCall);
						call = newCall;
					}

					var returnTypeParameters = GetMethodTypeParameterIndices(methodDefinition.ReturnType);
					if (!returnTypeParameters.Overlaps(sourceTypeParameters))
						break;
					source = call;
					sourceType = call.Method.ReturnType;
				}
			}
		}

		static IEnumerable<IType> GetConsumerTupleNameCandidates(ILVariable variable)
		{
			foreach (var load in variable.LoadInstructions)
			{
				ILInstruction source = load;
				IType sourceType = variable.Type;
				while (true)
				{
					// Boxing is the only representation-only wrapper admitted here. In particular,
					// do not walk through casts or arbitrary expressions that could change the source.
					if (source.Parent is Box box && source.ChildIndex == 0
						&& NormalizeTypeVisitor.TypeErasure.EquivalentTypes(box.Type, sourceType))
					{
						source = box;
					}
					if (source.Parent is not CallInstruction call || source.ChildIndex != 0)
						break;
					if (!call.Method.IsStatic || call.Method.MemberDefinition is not IMethod methodDefinition
						|| !methodDefinition.IsExtensionMethod || methodDefinition.Parameters.Count == 0
						|| call.Method.TypeArguments.Count != methodDefinition.TypeParameters.Count)
					{
						break;
					}

					var sourceTypeParameters = GetMethodTypeParameterIndices(methodDefinition.Parameters[0].Type);
					if (sourceTypeParameters.Count == 0)
						break;

					// Work from the already-selected overload's generic definition. Only delegate
					// inputs tied to the first (extension source) parameter are evidence; delegate
					// return types are deliberately ignored so projections cannot name their source.
					for (int argumentIndex = 1; argumentIndex < call.Arguments.Count; argumentIndex++)
					{
						if (call.Arguments[argumentIndex] is not ILFunction { Kind: ILFunctionKind.Delegate } lambda
							|| argumentIndex >= methodDefinition.Parameters.Count)
						{
							continue;
						}
						var invokeMethod = methodDefinition.Parameters[argumentIndex].Type.GetDelegateInvokeMethod();
						if (invokeMethod == null || invokeMethod.Parameters.Count != lambda.Parameters.Count)
							continue;
						for (int parameterIndex = 0; parameterIndex < lambda.Parameters.Count; parameterIndex++)
						{
							var inputTypeParameters = GetMethodTypeParameterIndices(invokeMethod.Parameters[parameterIndex].Type);
							if (inputTypeParameters.Count != 1)
								continue;
							int typeParameterIndex = inputTypeParameters.Single();
							if (!sourceTypeParameters.Contains(typeParameterIndex))
								continue;

							var namedParameterType = lambda.Parameters[parameterIndex].Type;
							var actualTypeArgument = call.Method.TypeArguments[typeParameterIndex];
							if (!ContainsNamedTupleElement(namedParameterType)
								|| !NormalizeTypeVisitor.TypeErasure.EquivalentTypes(actualTypeArgument, namedParameterType))
							{
								continue;
							}
							var candidate = ReplaceUniqueEquivalentType(variable.Type, actualTypeArgument, namedParameterType);
							if (candidate != null)
								yield return candidate;
						}
					}

					var returnTypeParameters = GetMethodTypeParameterIndices(methodDefinition.ReturnType);
					// Continue along a fluent chain only while the source type parameter itself flows
					// into the result. Select-like projections stop the search at this call.
					if (!returnTypeParameters.Overlaps(sourceTypeParameters))
						break;
					source = call;
					sourceType = call.Method.ReturnType;
				}
			}
		}

		internal static void IntroduceTupleElementNamesFromConsumers(ILVariable variable)
		{
			if (!ContainsUnnamedTupleElement(variable.Type))
				return;
			IType merged = variable.Type;
			bool foundEvidence = false;
			foreach (var sourceType in GetConsumerTupleNameCandidates(variable))
			{
				var candidate = TupleType.MergeTupleElementNames(merged, sourceType);
				if (candidate == null)
					return;
				merged = candidate;
				foundEvidence = true;
			}
			if (!foundEvidence || merged.Equals(variable.Type))
				return;
			variable.Type = merged;
			PropagateTupleNamesToSourceCalls(variable);
		}

		static HashSet<int> GetMethodTypeParameterIndices(IType type)
		{
			var visitor = new MethodTypeParameterCollector();
			type.AcceptVisitor(visitor);
			return visitor.Indices;
		}

		static IType? ReplaceUniqueEquivalentType(IType type, IType oldType, IType newType)
		{
			var visitor = new UniqueTypeReplacementVisitor(oldType, newType);
			var candidate = type.AcceptVisitor(visitor);
			return visitor.ReplacementCount == 1 ? candidate : null;
		}

		static IType? FindUniqueNamedEquivalentType(IType type, IType unnamedType)
		{
			var visitor = new UniqueNamedEquivalentTypeVisitor(unnamedType);
			type.AcceptVisitor(visitor);
			return visitor.MatchCount == 1 ? visitor.Match : null;
		}

		sealed class MethodTypeParameterCollector : TypeVisitor
		{
			public HashSet<int> Indices { get; } = new();

			public override IType VisitTypeParameter(ITypeParameter type)
			{
				if (type.OwnerType == SymbolKind.Method)
					Indices.Add(type.Index);
				return type;
			}
		}

		sealed class UniqueTypeReplacementVisitor : TypeVisitor
		{
			readonly IType oldType;
			readonly IType newType;

			public int ReplacementCount { get; private set; }

			public UniqueTypeReplacementVisitor(IType oldType, IType newType)
			{
				this.oldType = oldType;
				this.newType = newType;
			}

			IType Visit(IType type, System.Func<IType> visitChildren)
			{
				if (NormalizeTypeVisitor.TypeErasure.EquivalentTypes(type, oldType))
				{
					ReplacementCount++;
					return newType;
				}
				return visitChildren();
			}

			public override IType VisitTypeDefinition(ITypeDefinition type) => Visit(type, () => base.VisitTypeDefinition(type));
			public override IType VisitTypeParameter(ITypeParameter type) => Visit(type, () => base.VisitTypeParameter(type));
			public override IType VisitParameterizedType(ParameterizedType type) => Visit(type, () => base.VisitParameterizedType(type));
			public override IType VisitArrayType(ArrayType type) => Visit(type, () => base.VisitArrayType(type));
			public override IType VisitPointerType(PointerType type) => Visit(type, () => base.VisitPointerType(type));
			public override IType VisitByReferenceType(ByReferenceType type) => Visit(type, () => base.VisitByReferenceType(type));
			public override IType VisitTupleType(TupleType type) => Visit(type, () => base.VisitTupleType(type));
			public override IType VisitOtherType(IType type) => Visit(type, () => base.VisitOtherType(type));
			public override IType VisitModReq(ModifiedType type) => Visit(type, () => base.VisitModReq(type));
			public override IType VisitModOpt(ModifiedType type) => Visit(type, () => base.VisitModOpt(type));
			public override IType VisitNullabilityAnnotatedType(NullabilityAnnotatedType type) => Visit(type, () => base.VisitNullabilityAnnotatedType(type));
			public override IType VisitFunctionPointerType(FunctionPointerType type) => Visit(type, () => base.VisitFunctionPointerType(type));
		}

		sealed class UniqueNamedEquivalentTypeVisitor : TypeVisitor
		{
			readonly IType unnamedType;

			public int MatchCount { get; private set; }
			public IType? Match { get; private set; }

			public UniqueNamedEquivalentTypeVisitor(IType unnamedType)
			{
				this.unnamedType = unnamedType;
			}

			IType Visit(IType type, System.Func<IType> visitChildren)
			{
				if (ContainsNamedTupleElement(type)
					&& NormalizeTypeVisitor.TypeErasure.EquivalentTypes(type, unnamedType))
				{
					MatchCount++;
					Match = type;
					return type;
				}
				return visitChildren();
			}

			public override IType VisitTypeDefinition(ITypeDefinition type) => Visit(type, () => base.VisitTypeDefinition(type));
			public override IType VisitTypeParameter(ITypeParameter type) => Visit(type, () => base.VisitTypeParameter(type));
			public override IType VisitParameterizedType(ParameterizedType type) => Visit(type, () => base.VisitParameterizedType(type));
			public override IType VisitArrayType(ArrayType type) => Visit(type, () => base.VisitArrayType(type));
			public override IType VisitPointerType(PointerType type) => Visit(type, () => base.VisitPointerType(type));
			public override IType VisitByReferenceType(ByReferenceType type) => Visit(type, () => base.VisitByReferenceType(type));
			public override IType VisitTupleType(TupleType type) => Visit(type, () => base.VisitTupleType(type));
			public override IType VisitOtherType(IType type) => Visit(type, () => base.VisitOtherType(type));
			public override IType VisitModReq(ModifiedType type) => Visit(type, () => base.VisitModReq(type));
			public override IType VisitModOpt(ModifiedType type) => Visit(type, () => base.VisitModOpt(type));
			public override IType VisitNullabilityAnnotatedType(NullabilityAnnotatedType type) => Visit(type, () => base.VisitNullabilityAnnotatedType(type));
			public override IType VisitFunctionPointerType(FunctionPointerType type) => Visit(type, () => base.VisitFunctionPointerType(type));
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

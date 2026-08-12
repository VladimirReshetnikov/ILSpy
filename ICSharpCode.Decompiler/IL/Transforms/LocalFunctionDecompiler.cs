// Copyright (c) 2019 Siegfried Pammer
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Decompiler step for C# 7.0 local functions
	/// </summary>
	public class LocalFunctionDecompiler : IILTransform
	{
		ILTransformContext context;
		ITypeResolveContext resolveContext;

		struct LocalFunctionInfo
		{
			public List<ILInstruction> UseSites;
			public IMethod Method;
			public ILFunction Definition;
			public HashSet<ILVariable> CapturedClosureVariables;
			public HashSet<ILInstruction> UnresolvedClosureArguments;
			/// <summary>
			/// Used to store all synthesized call-site arguments grouped by the parameter index.
			/// We use a dictionary instead of a simple array, because -1 is used for the this parameter
			/// and there might be many non-synthesized arguments in between.
			/// </summary>
			public Dictionary<int, List<ILInstruction>> LocalFunctionArguments;
		}

		/// <summary>
		/// The transform works like this:
		/// 
		/// <para>
		/// local functions can either be used in method calls, i.e., call and callvirt instructions,
		/// or can be used as part of the "delegate construction" pattern, i.e.,
		/// <c>newobj Delegate(&lt;target-expression&gt;, ldftn &lt;method&gt;)</c>.
		/// </para>
		/// As local functions can be declared practically anywhere, we have to take a look at
		/// all use-sites and infer the declaration location from that. Use-sites can be call,
		/// callvirt and ldftn instructions.
		/// After all use-sites are collected we construct the ILAst of the local function
		/// and add it to the parent function.
		/// Then all use-sites of the local-function are transformed to a call to the 
		/// <c>LocalFunctionMethod</c> or a ldftn of the <c>LocalFunctionMethod</c>.
		/// In a next step we handle all nested local functions.
		/// After all local functions are transformed, we move all local functions that capture
		/// any variables to their respective declaration scope.
		/// </summary>
		public void Run(ILFunction function, ILTransformContext context)
		{
			if (!context.Settings.LocalFunctions)
				return;
			// Disable the transform if we are decompiling a display-class or local function method:
			// This happens if a local function or display class is selected in the ILSpy tree view.
			if (IsLocalFunctionMethod(function.Method, context) || IsLocalFunctionDisplayClass(
					function.Method.ParentModule.MetadataFile,
					(TypeDefinitionHandle)function.Method.DeclaringTypeDefinition.MetadataToken,
					context)
				)
			{
				return;
			}

			this.context = context;
			this.resolveContext = new SimpleTypeResolveContext(function.Method);
			var localFunctions = new Dictionary<MethodDefinitionHandle, LocalFunctionInfo>();
			// Find all local functions declared inside this method, including nested local functions or local functions declared in lambdas.
			FindUseSites(function, context, localFunctions);
			ReplaceReferencesToDisplayClassThis(localFunctions.Values);
			DetermineCaptureAndDeclarationScopes(localFunctions.Values);
			PropagateClosureParameterArguments(localFunctions);
			TransformUseSites(localFunctions.Values);
		}

		private void ReplaceReferencesToDisplayClassThis(Dictionary<MethodDefinitionHandle, LocalFunctionInfo>.ValueCollection localFunctions)
		{
			foreach (var info in localFunctions)
			{
				var localFunction = info.Definition;
				if (localFunction.Method.IsStatic)
					continue;
				var thisVar = localFunction.Variables.SingleOrDefault(VariableKindExtensions.IsThis);
				if (thisVar == null)
					continue;
				var compatibleArgument = FindCompatibleArgument(
					info,
					info.UseSites.OfType<CallInstruction>().Select(u => u.Arguments[0]).ToArray(),
					ignoreStructure: true
				);
				if (compatibleArgument == null)
				{
					if (TransformDisplayClassUsage.IsPotentialClosure(context, thisVar.Type.GetDefinition()))
					{
						// The instance receiver is itself a compiler closure, but no use-site reveals the
						// storage that owns it. Record opaque provenance so later widening fails closed.
						info.UnresolvedClosureArguments.Add(localFunction);
					}
					continue;
				}
				context.Step($"Replace 'this' with {compatibleArgument}", localFunction);
				localFunction.AcceptVisitor(new DelegateConstruction.ReplaceDelegateTargetVisitor(compatibleArgument, thisVar));
				DetermineCaptureAndDeclarationScope(info, -1, compatibleArgument);
			}
		}

		private void DetermineCaptureAndDeclarationScopes(Dictionary<MethodDefinitionHandle, LocalFunctionInfo>.ValueCollection localFunctions)
		{
			var resolvedLocalFunctions = localFunctions.Where(info => info.Definition != null).ToArray();
			foreach (var info in localFunctions)
			{
				if (info.Definition == null)
				{
					context.Function.Warnings.Add($"Could not decode local function '{info.Method}'");
				}
			}

			// Resolve every compiler-generated closure argument before deciding whether any function can
			// be widened. A parent local function and all local functions declared within it move as one
			// subtree, so parent-before-child discovery order must not hide a descendant's external scope.
			foreach (var info in resolvedLocalFunctions)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				foreach (var useSite in info.UseSites)
				{
					if (!useSite.IsDescendantOf(info.Definition))
					{
						DetermineCaptureAndDeclarationScope(info, useSite);
					}
				}
			}
			if (context.Function.Method.IsConstructor)
			{
				// A closure-free local function in a constructor gets its initial declaration scope from its
				// first use site. Seed all such scopes before any relocation so a logically nested decoded
				// function participates in its parent's moving-subtree safety checks even though all decoded
				// local functions are still attached as siblings in the ILAst at this phase.
				bool changed;
				do
				{
					changed = false;
					foreach (var info in resolvedLocalFunctions.Where(info => info.Definition.DeclarationScope == null))
					{
						foreach (var useSite in info.UseSites.Where(useSite => !useSite.IsDescendantOf(info.Definition)))
						{
							var scope = FindDeclarationScopeAtUseSite(info.Definition, useSite);
							if (scope != null)
							{
								info.Definition.DeclarationScope = scope;
								changed = true;
								break;
							}
						}
					}
				} while (changed);
			}

			foreach (var info in resolvedLocalFunctions)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				context.StepStartGroup($"Determine and move to declaration scope of " + info.Definition.Name, info.Definition);
				try
				{
					var localFunction = info.Definition;

					foreach (var useSite in info.UseSites)
					{
						// A recursive reference is always in scope. Until declaration scopes are applied,
						// the local-function ILAst is attached directly to the root function, so treating
						// its own body as an ordinary use-site would falsely widen the declaration.
						if (useSite.IsDescendantOf(localFunction))
							continue;
						var useSiteScope = FindDeclarationScopeAtUseSite(localFunction, useSite);
						// Outside a constructor, only step in where the declaration does not already cover
						// the use-site. Moving a function that every caller can see would churn the output
						// of methods that decompile correctly today.
						bool useSiteOutOfScope = useSiteScope != null && localFunction.DeclarationScope != null
							&& !useSiteScope.IsDescendantOf(localFunction.DeclarationScope);
						if (context.Function.Method.IsConstructor || useSiteOutOfScope)
						{
							var scope = useSiteScope;
							if (localFunction.DeclarationScope == null)
							{
								localFunction.DeclarationScope = scope;
							}
							else if (scope != null)
							{
								var widenedScope = FindCommonAncestorInstruction<BlockContainer>(scope, localFunction.DeclarationScope)
									?? (BlockContainer)context.Function.Body;
								if (!TryGetMovingLocalFunctions(info, resolvedLocalFunctions, out var movingFunctions))
								{
									continue;
								}
								bool canWidenByKind = GetDeclaringFunction(localFunction) == context.Function
									|| CapturesAtMostThis(localFunction)
									|| ClosureLivesIn(localFunction, context.Function);
								if (!canWidenByKind
									|| !MovingSubtreeCapturesOnlyFrom(movingFunctions, context.Function)
									|| !CapturedClosuresRemainInScope(movingFunctions, resolvedLocalFunctions, widenedScope))
								{
									continue;
								}
								// Broaden the declaration scope to cover this use-site. A function that
								// captures at most the enclosing 'this' has no display-class struct parameter,
								// so closure analysis never anchors it to a particular scope; its scope comes
								// only from FindClosestContainer at the first use-site, which lands it inside
								// whichever lambda or local function happens to call it first. Sibling
								// use-sites in other lambdas would then see only an undefined member reference
								// (or an unresolvable name), so the scope must be widened to their common
								// ancestor in the enclosing body. For a function that captures locals via a
								// display class, broadening is limited to while the scope still lives directly
								// in the enclosing body: once closure analysis has placed it inside a nested
								// local function (e.g. its closure arrives via a forwarded by-ref display-class
								// struct), pulling the scope up to a common ancestor with a use-site would move
								// it out of that function and leave the captured display-class fields without a
								// declaration.
								localFunction.DeclarationScope = widenedScope;
							}
						}
					}

					if (localFunction.DeclarationScope == null)
					{
						localFunction.DeclarationScope = (BlockContainer)context.Function.Body;
					}

					// A switch block-container is rendered directly as a C# switch statement, and the
					// statement builder only emits local-function declarations for containers that turn
					// into block scopes (the function body and loop bodies). A local function whose
					// inferred declaration scope is a switch container would therefore be silently
					// dropped, leaving its call-sites referencing an undefined name. This happens when
					// all use-sites share a switch container as their closest common ancestor (e.g.
					// calls spread across several case arms). Lift the declaration to the nearest
					// enclosing non-switch container, which still encloses every use-site.
					while (localFunction.DeclarationScope.Kind == ContainerKind.Switch)
					{
						var enclosing = BlockContainer.FindClosestContainer(localFunction.DeclarationScope.Parent);
						localFunction.DeclarationScope = enclosing ?? (BlockContainer)context.Function.Body;
						if (enclosing == null)
							break;
					}

					ILFunction declaringFunction = GetDeclaringFunction(localFunction);
					if (declaringFunction != context.Function)
					{
						context.Step($"Move {localFunction.Name} from {context.Function.Name} to {declaringFunction.Name}", localFunction);
						context.Function.LocalFunctions.Remove(localFunction);
						declaringFunction.LocalFunctions.Add(localFunction);
					}

					if (TryValidateSkipCount(info, out int skipCount) && skipCount != localFunction.ReducedMethod.NumberOfCompilerGeneratedTypeParameters)
					{
						Debug.Assert(false);
						context.Function.Warnings.Add($"Could not decode local function '{info.Method}'");
						if (declaringFunction != context.Function)
						{
							declaringFunction.LocalFunctions.Remove(localFunction);
						}
					}
				}
				finally
				{
					context.StepEndGroup(keepIfEmpty: true);
				}
			}
		}

		private void TransformUseSites(Dictionary<MethodDefinitionHandle, LocalFunctionInfo>.ValueCollection localFunctions)
		{
			foreach (var info in localFunctions)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				if (info.Definition == null)
					continue;
				context.StepStartGroup($"TransformUseSites of " + info.Definition.Name, info.Definition);
				try
				{
					foreach (var useSite in info.UseSites)
					{
						context.Step($"Transform use-site at IL_{useSite.StartILOffset:x4}", useSite);
						switch (useSite)
						{
							case NewObj newObj:
								TransformToLocalFunctionReference(info.Definition, newObj);
								break;
							case CallInstruction call:
								var callReplacement = TransformToLocalFunctionInvocation(info.Definition.ReducedMethod, call);
								context.EndStep(callReplacement);
								break;
							case LdFtn fnptr:
								var specializeMethod = info.Definition.ReducedMethod
									.Specialize(fnptr.Method.Substitution);
								var replacement = new LdFtn(specializeMethod).WithILRange(fnptr);
								fnptr.ReplaceWith(replacement);
								context.EndStep(replacement);
								break;
							default:
								throw new NotSupportedException();
						}
					}
				}
				finally
				{
					context.StepEndGroup();
				}
			}
		}

		private void PropagateClosureParameterArguments(Dictionary<MethodDefinitionHandle, LocalFunctionInfo> localFunctions)
		{
			foreach (var localFunction in context.Function.Descendants.OfType<ILFunction>())
			{
				if (localFunction.Kind != ILFunctionKind.LocalFunction)
					continue;
				context.CancellationToken.ThrowIfCancellationRequested();
				var token = (MethodDefinitionHandle)localFunction.Method.MetadataToken;
				var info = localFunctions[token];

				foreach (var useSite in info.UseSites)
				{
					switch (useSite)
					{
						case NewObj newObj:
							AddAsArgument(-1, newObj.Arguments[0]);
							break;
						case CallInstruction call:
							int firstArgumentIndex;
							if (info.Method.IsStatic)
							{
								firstArgumentIndex = 0;
							}
							else
							{
								firstArgumentIndex = 1;
								AddAsArgument(-1, call.Arguments[0]);
							}
							for (int i = call.Arguments.Count - 1; i >= firstArgumentIndex; i--)
							{
								AddAsArgument(i - firstArgumentIndex, call.Arguments[i]);
							}
							break;
						case LdFtn _:
							// &LocalFunction is only possible, if the local function is declared static,
							// this means that the local function can be declared in the top-level scope.
							// Thus, there are no closure parameters that need propagation.
							break;
						default:
							throw new NotSupportedException();
					}
				}

				context.StepStartGroup($"PropagateClosureParameterArguments of " + info.Definition.Name, info.Definition);
				try
				{
					foreach (var (index, arguments) in info.LocalFunctionArguments)
					{
						var targetVariable = info.Definition.Variables.SingleOrDefault(p => p.Kind == VariableKind.Parameter && p.Index == index);
						if (targetVariable == null)
							continue;
						var compatibleArgument = FindCompatibleArgument(info, arguments);
						if (compatibleArgument == null)
							continue;
						context.Step($"Replace '{targetVariable}' with '{compatibleArgument}'", info.Definition);
						info.Definition.AcceptVisitor(new DelegateConstruction.ReplaceDelegateTargetVisitor(compatibleArgument, targetVariable));
					}
				}
				finally
				{
					context.StepEndGroup(keepIfEmpty: true);
				}

				void AddAsArgument(int index, ILInstruction argument)
				{
					switch (argument)
					{
						case LdLoc _:
						case LdLoca _:
						case LdFlda _:
						case LdObj _:
							if (index >= 0 && !IsClosureParameter(info.Method.Parameters[index], resolveContext))
								return;
							break;
						default:
							if (index >= 0 && IsClosureParameter(info.Method.Parameters[index], resolveContext))
								info.Definition.Warnings.Add("Could not transform parameter " + index + ": unsupported argument pattern");
							return;
					}

					if (!info.LocalFunctionArguments.TryGetValue(index, out var arguments))
					{
						arguments = new List<ILInstruction>();
						info.LocalFunctionArguments.Add(index, arguments);
					}
					arguments.Add(argument);
				}
			}
		}

		private ILInstruction FindCompatibleArgument(LocalFunctionInfo info, IList<ILInstruction> arguments, bool ignoreStructure = false)
		{
			foreach (var arg in arguments)
			{
				if (arg is IInstructionWithVariableOperand ld2 && (ignoreStructure || info.Definition.IsDescendantOf(ld2.Variable.Function)))
					return arg;
				var v = ResolveAncestorScopeReference(arg);
				if (v != null)
					return new LdLoc(v);
			}
			return null;
		}

		private ILVariable ResolveAncestorScopeReference(ILInstruction inst)
		{
			if (!inst.MatchLdFld(out var target, out var field))
				return null;
			if (field.Type.Kind != TypeKind.Class)
				return null;
			if (!(TransformDisplayClassUsage.IsPotentialClosure(context, field.Type.GetDefinition()) || context.Function.Method.DeclaringType.Equals(field.Type)))
				return null;
			// The referenced closure may be the method-level display class, which is a variable of the
			// top-level function itself rather than of a nested lambda or local function; Descendants
			// includes the node itself, so the search covers context.Function too. An exact instantiation
			// match is preferred; the fallback matches the closure type definition, because IsClosure may
			// report the unbound generic display class (e.g. <>c__DisplayClass0_0`1) while the
			// parent-pointer field is typed with its closed instantiation (<>c__DisplayClass0_0<T>) -
			// that mismatch otherwise leaves a local function's 'this' unresolved, leaking it as a cast
			// of 'this' to the display-class type.
			return FindClosureVariable(varType => varType.Equals(field.Type))
				?? FindClosureVariable(varType => varType.GetDefinition() is { } d && d.Equals(field.Type.GetDefinition()));

			ILVariable FindClosureVariable(Func<IType, bool> typeMatches)
			{
				foreach (var f in context.Function.Descendants.OfType<ILFunction>())
				{
					foreach (var v in f.Variables)
					{
						if (TransformDisplayClassUsage.IsClosure(context, v, out var varType, out _) && typeMatches(varType))
							return v;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Determines the declaration scope implied by a single use site of a local function.
		/// Usually this is the closest enclosing block container. However, if the use site is
		/// located inside another local function whose type parameters the target local function
		/// does not inherit, the target cannot be declared there: the C# compiler adds a copy of
		/// every type parameter of all enclosing generic functions to a nested local function, so
		/// a local function lacking these copies must have been declared further out. In that case
		/// the scope is hoisted to the declaration scope of the enclosing local function.
		/// Returns null, if no valid scope could be determined (e.g., because the declaration
		/// scope of the enclosing local function is not yet known).
		/// </summary>
		private BlockContainer FindDeclarationScopeAtUseSite(ILFunction localFunction, ILInstruction useSite)
		{
			var scope = BlockContainer.FindClosestContainer(useSite);
			var visitedFunctions = new HashSet<ILFunction>();
			while (scope != null)
			{
				var enclosingFunction = scope.Ancestors.OfType<ILFunction>().FirstOrDefault();
				if (enclosingFunction == null || enclosingFunction == context.Function
					|| enclosingFunction.Kind != ILFunctionKind.LocalFunction)
				{
					break;
				}
				if (!visitedFunctions.Add(enclosingFunction))
				{
					// Cyclic declaration scopes: give up on this use site.
					return null;
				}
				// Number of type parameters localFunction would inherit, if it were declared
				// inside enclosingFunction; mirrors the computation in TryValidateSkipCount.
				int inheritedTypeParametersCount = enclosingFunction.Method.DeclaringType.TypeParameterCount
					- localFunction.Method.DeclaringType.TypeParameterCount
					+ enclosingFunction.Method.TypeParameters.Count;
				if (inheritedTypeParametersCount <= localFunction.ReducedMethod.NumberOfCompilerGeneratedTypeParameters)
					break;
				scope = enclosingFunction.DeclarationScope;
			}
			return scope;
		}

		private ILFunction GetDeclaringFunction(ILFunction localFunction)
		{
			if (localFunction.DeclarationScope == null)
				return null;
			ILInstruction inst = localFunction.DeclarationScope;
			while (inst != null)
			{
				if (inst is ILFunction declaringFunction)
					return declaringFunction;
				inst = inst.Parent;
			}
			return null;
		}

		// A local function that captures at most the enclosing 'this' has no closure (display-class
		// struct) parameter, so closure analysis never assigns it a declaration scope; its only scope
		// is the one FindClosestContainer pins to the first use-site. Broadening that scope to a
		// use-site's common ancestor is always safe and never overrides a deeper placement chosen to
		// reach a forwarded display class. A static function captures nothing; an instance function
		// captures 'this', which -- unless 'this' is itself a display class -- is available throughout
		// the constructor body, so both can be declared at the common ancestor.
		/// <summary>
		/// Returns whether every display class the function captures is built by <paramref name="owner"/>
		/// itself. Widening the declaration scope is safe then: the widened scope still lies inside the
		/// function that declares those display classes, so their fields stay in scope. It is only
		/// climbing out of the owning function that would strand them.
		/// </summary>
		private bool ClosureLivesIn(ILFunction localFunction, ILFunction owner)
		{
			bool sawClosure = false;
			foreach (var variable in owner.Variables)
			{
				if (variable.Kind is not (VariableKind.DisplayClassLocal or VariableKind.Local))
					continue;
				if (variable.CaptureScope == null)
					continue;
				if (variable.CaptureScope.Ancestors.OfType<ILFunction>().FirstOrDefault() != owner)
					return false;
				sawClosure = true;
			}
			return sawClosure && localFunction.DeclarationScope?.Ancestors.OfType<ILFunction>()
				.Any(f => f == owner) == true;
		}

		/// <summary>
		/// Returns whether the local-function subtree captures variables only from the moving subtree
		/// itself or from <paramref name="owner"/>. Nested lambdas and local functions move together
		/// with their containing local function, so variables they own are internal to the move; any
		/// other owner is an external scope that widening must not cross.
		/// </summary>
		private bool MovingSubtreeCapturesOnlyFrom(IReadOnlyCollection<ILFunction> movingLocalFunctions, ILFunction owner)
		{
			var movingFunctions = movingLocalFunctions.SelectMany(function => function.Descendants.OfType<ILFunction>())
				.Concat(movingLocalFunctions).ToHashSet();
			foreach (var instruction in movingLocalFunctions.SelectMany(function =>
				function.Descendants.OfType<IInstructionWithVariableOperand>()))
			{
				var variableOwner = instruction.Variable.Function;
				if (variableOwner != owner && !movingFunctions.Contains(variableOwner))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Computes the decoded local functions that would move together with <paramref name="rootInfo"/>
		/// from their inferred declaration scopes. At this transform phase decoded local functions are
		/// still attached as siblings in the ILAst, so current parent pointers alone are insufficient.
		/// Returns false when a function called from that logical subtree still has unresolved closure
		/// provenance and no declaration scope with which to classify the move.
		/// </summary>
		private bool TryGetMovingLocalFunctions(LocalFunctionInfo rootInfo,
			IReadOnlyCollection<LocalFunctionInfo> allInfos, out HashSet<ILFunction> movingFunctions)
		{
			var result = new HashSet<ILFunction> { rootInfo.Definition };
			bool changed;
			do
			{
				changed = false;
				foreach (var info in allInfos)
				{
					if (result.Contains(info.Definition))
						continue;
					bool usedFromMovingSubtree = info.UseSites.Any(useSite => useSite.Ancestors.OfType<ILFunction>()
						.Any(result.Contains));
					if (usedFromMovingSubtree && info.Definition.DeclarationScope == null
						&& info.UnresolvedClosureArguments.Count > 0)
					{
						movingFunctions = result;
						return false;
					}
					if (info.Definition.DeclarationScope?.Ancestors.OfType<ILFunction>().Any(result.Contains) == true)
					{
						result.Add(info.Definition);
						changed = true;
					}
				}
			} while (changed);

			movingFunctions = result;
			return true;
		}

		private bool CapturedClosuresRemainInScope(IReadOnlyCollection<ILFunction> movingFunctions,
			IReadOnlyCollection<LocalFunctionInfo> allInfos, BlockContainer proposedScope)
		{
			// An unrelated root-owned display class must not authorize widening a function whose own
			// closure, or a moving descendant's closure, is built in a nested lexical scope. Known
			// unresolved closure arguments and recorded variables without a capture scope fail closed.
			return allInfos.Where(info => movingFunctions.Contains(info.Definition)).All(info =>
				info.UnresolvedClosureArguments.Count == 0
				&& info.CapturedClosureVariables.All(variable => variable.CaptureScope != null
					&& (proposedScope == variable.CaptureScope || proposedScope.IsDescendantOf(variable.CaptureScope))));
		}

		private bool CapturesAtMostThis(ILFunction localFunction)
		{
			foreach (var parameter in localFunction.Method.Parameters)
			{
				if (IsClosureParameter(parameter, resolveContext))
					return false;
			}
			// An instance local function declared inside a compiler-generated display class receives
			// that display class as its 'this' and thereby captures the locals it holds; such a
			// function is anchored to the scope that builds the display class and must not be broadened.
			if (!localFunction.Method.IsStatic
				&& localFunction.Method.DeclaringTypeDefinition is { } declaringType
				&& declaringType.IsCompilerGenerated())
			{
				return false;
			}
			return true;
		}

		bool TryValidateSkipCount(LocalFunctionInfo info, out int skipCount)
		{
			skipCount = 0;
			var localFunction = info.Definition;
			if (localFunction.Method.TypeParameters.Count == 0)
				return true;
			var parentMethod = ((ILFunction)localFunction.Parent).Method;
			var method = localFunction.Method;
			skipCount = parentMethod.DeclaringType.TypeParameterCount - method.DeclaringType.TypeParameterCount;

			if (skipCount > 0)
				return false;
			skipCount += parentMethod.TypeParameters.Count;
			if (skipCount < 0 || skipCount > method.TypeArguments.Count)
				return false;

			if (skipCount > 0)
			{
#if DEBUG
				foreach (var useSite in info.UseSites)
				{
					var callerMethod = useSite.Ancestors.OfType<ILFunction>().First().Method;
					callerMethod = callerMethod.ReducedFrom ?? callerMethod;
					IMethod m;
					switch (useSite)
					{
						case NewObj newObj:
							m = ((LdFtn)newObj.Arguments[1]).Method;
							break;
						case CallInstruction call:
							m = call.Method;
							break;
						case LdFtn fnptr:
							m = fnptr.Method;
							break;
						default:
							throw new NotSupportedException();
					}
					var totalSkipCount = skipCount + m.DeclaringType.TypeParameterCount;
					var methodSkippedArgs = m.DeclaringType.TypeArguments.Concat(m.TypeArguments).Take(totalSkipCount);
					Debug.Assert(methodSkippedArgs.SequenceEqual(callerMethod.DeclaringType.TypeArguments.Concat(callerMethod.TypeArguments).Take(totalSkipCount)));
					Debug.Assert(methodSkippedArgs.All(p => p.Kind == TypeKind.TypeParameter));
					Debug.Assert(methodSkippedArgs.Select(p => p.Name).SequenceEqual(m.DeclaringType.TypeParameters.Concat(m.TypeParameters).Take(totalSkipCount).Select(p => p.Name)));
				}
#endif
			}
			return true;
		}

		void FindUseSites(ILFunction function, ILTransformContext context, Dictionary<MethodDefinitionHandle, LocalFunctionInfo> localFunctions)
		{
			foreach (var inst in function.Body.Descendants)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				if (inst is CallInstruction call && !call.Method.IsLocalFunction && IsLocalFunctionMethod(call.Method, context))
				{
					HandleUseSite(call.Method, call);
				}
				else if (inst is LdFtn ldftn && !ldftn.Method.IsLocalFunction && IsLocalFunctionMethod(ldftn.Method, context))
				{
					if (ldftn.Parent is NewObj newObj && DelegateConstruction.MatchDelegateConstruction(newObj, out _, out _, out _))
						HandleUseSite(ldftn.Method, newObj);
					else
						HandleUseSite(ldftn.Method, ldftn);
				}
			}

			void HandleUseSite(IMethod targetMethod, ILInstruction inst)
			{
				if (!localFunctions.TryGetValue((MethodDefinitionHandle)targetMethod.MetadataToken, out var info))
				{
					context.StepStartGroup($"Read local function '{targetMethod.Name}'", inst);
					info = new LocalFunctionInfo() {
						UseSites = new List<ILInstruction>() { inst },
						LocalFunctionArguments = new Dictionary<int, List<ILInstruction>>(),
						CapturedClosureVariables = new HashSet<ILVariable>(),
						UnresolvedClosureArguments = new HashSet<ILInstruction>(),
						Method = (IMethod)targetMethod.MemberDefinition,
					};
					var rootFunction = context.Function;
					int skipCount = GetSkipCount(rootFunction, targetMethod);
					info.Definition = ReadLocalFunctionDefinition(rootFunction, targetMethod, skipCount);
					localFunctions.Add((MethodDefinitionHandle)targetMethod.MetadataToken, info);
					if (info.Definition != null)
					{
						FindUseSites(info.Definition, context, localFunctions);
					}
					context.StepEndGroup();
				}
				else
				{
					info.UseSites.Add(inst);
				}
			}
		}

		ILFunction ReadLocalFunctionDefinition(ILFunction rootFunction, IMethod targetMethod, int skipCount)
		{
			var methodDefinition = context.PEFile.Metadata.GetMethodDefinition((MethodDefinitionHandle)targetMethod.MetadataToken);
			var genericContext = GenericContextFromTypeArguments(targetMethod, skipCount);
			if (genericContext == null)
				return null;
			ILFunction function;
			bool hasBody = methodDefinition.HasBody();
			if (!hasBody)
			{
				function = new ILFunction(targetMethod, 0,
					new TypeSystem.GenericContext(genericContext?.ClassTypeParameters, genericContext?.MethodTypeParameters),
					new Nop(), ILFunctionKind.LocalFunction);
			}
			else
			{
				var ilReader = context.CreateILReader();
				var body = context.PEFile.GetMethodBody(methodDefinition.RelativeVirtualAddress);

				function = ilReader.ReadIL((MethodDefinitionHandle)targetMethod.MetadataToken, body,
					genericContext.GetValueOrDefault(), ILFunctionKind.LocalFunction,
					context.CancellationToken);
			}
			// Embed the local function into the parent function's ILAst, so that "Show steps" can show
			// how the local function body is being transformed.
			rootFunction.LocalFunctions.Add(function);
			if (hasBody)
			{
				function.DeclarationScope = (BlockContainer)rootFunction.Body;
				function.CheckInvariant(ILPhase.Normal);
				var nestedContext = new ILTransformContext(context, function);
				function.RunTransforms(CSharpDecompiler.GetILTransforms().TakeWhile(t => !(t is LocalFunctionDecompiler)), nestedContext);
				function.DeclarationScope = null;
			}
			function.ReducedMethod = ReduceToLocalFunction(function.Method, skipCount);
			return function;
		}

		int GetSkipCount(ILFunction rootFunction, IMethod targetMethod)
		{
			targetMethod = (IMethod)targetMethod.MemberDefinition;
			var skipCount = rootFunction.Method.DeclaringType.TypeParameters.Count + rootFunction.Method.TypeParameters.Count - targetMethod.DeclaringType.TypeParameters.Count;
			if (skipCount < 0)
			{
				skipCount = 0;
			}
			if (targetMethod.TypeParameters.Count > 0)
			{
				var lastParams = targetMethod.Parameters.Where(p => IsClosureParameter(p, this.resolveContext)).SelectMany(p => p.Type.UnwrapByRef().TypeArguments)
					.Select(pt => (int?)targetMethod.TypeParameters.IndexOf(pt)).DefaultIfEmpty().Max();
				if (lastParams != null && lastParams.GetValueOrDefault() + 1 > skipCount)
					skipCount = lastParams.GetValueOrDefault() + 1;
			}
			return skipCount;
		}

		static TypeSystem.GenericContext? GenericContextFromTypeArguments(IMethod targetMethod, int skipCount)
		{
			if (skipCount < 0 || skipCount > targetMethod.TypeParameters.Count)
			{
				Debug.Assert(false);
				return null;
			}
			int total = targetMethod.DeclaringType.TypeParameters.Count + skipCount;
			if (total == 0)
				return default(TypeSystem.GenericContext);

			var classTypeParameters = new List<ITypeParameter>(targetMethod.DeclaringType.TypeParameters);
			var methodTypeParameters = new List<ITypeParameter>(targetMethod.TypeParameters);
			var skippedTypeArguments = targetMethod.DeclaringType.TypeArguments.Concat(targetMethod.TypeArguments).Take(total);
			int idx = 0;
			foreach (var skippedTA in skippedTypeArguments)
			{
				int curIdx;
				List<ITypeParameter> curParameters;
				IReadOnlyList<IType> curArgs;
				if (idx < classTypeParameters.Count)
				{
					curIdx = idx;
					curParameters = classTypeParameters;
					curArgs = targetMethod.DeclaringType.TypeArguments;
				}
				else
				{
					curIdx = idx - classTypeParameters.Count;
					curParameters = methodTypeParameters;
					curArgs = targetMethod.TypeArguments;
				}
				if (curArgs[curIdx].Kind != TypeKind.TypeParameter)
					break;
				curParameters[curIdx] = (ITypeParameter)skippedTA;
				idx++;
			}
			if (idx != total)
			{
				Debug.Assert(false);
				return null;
			}

			return new TypeSystem.GenericContext(classTypeParameters, methodTypeParameters);
		}

		static T FindCommonAncestorInstruction<T>(ILInstruction a, ILInstruction b)
			where T : ILInstruction
		{
			var ancestorsOfB = new HashSet<T>(b.Ancestors.OfType<T>());
			return a.Ancestors.OfType<T>().FirstOrDefault(ancestorsOfB.Contains);
		}

		internal static bool IsClosureParameter(IParameter parameter, ITypeResolveContext context)
		{
			return IsClosureParameter(parameter, context.CurrentTypeDefinition);
		}

		internal static bool IsClosureParameter(IParameter parameter, ITypeDefinition currentTypeDefinition)
		{
			if (parameter.Type is not ByReferenceType brt)
				return false;
			var type = brt.ElementType.GetDefinition();
			return type != null
				&& type.Kind == TypeKind.Struct
				&& TransformDisplayClassUsage.IsPotentialClosure(currentTypeDefinition, type);
		}

		LocalFunctionMethod ReduceToLocalFunction(IMethod method, int typeParametersToRemove)
		{
			int parametersToRemove = 0;
			for (int i = method.Parameters.Count - 1; i >= 0; i--)
			{
				if (!IsClosureParameter(method.Parameters[i], resolveContext))
					break;
				parametersToRemove++;
			}
			return new LocalFunctionMethod(method, method.Name, CanBeStaticLocalFunction(), parametersToRemove, typeParametersToRemove);

			bool CanBeStaticLocalFunction()
			{
				if (!context.Settings.StaticLocalFunctions)
					return false;
				// Cannot be static because there are closure parameters that will be removed
				if (parametersToRemove > 0)
					return false;
				// no closure parameters, but static:
				// we can safely assume, this local function can be declared static
				if (method.IsStatic)
					return true;
				// the local function is used in conjunction with a lambda, which means,
				// it is defined inside the display-class type
				var declaringType = method.DeclaringTypeDefinition;
				if (!declaringType.IsCompilerGenerated())
					return false;
				// if there are no instance fields, we can make it a static local function
				return !declaringType.GetFields(f => !f.IsStatic).Any();
			}
		}

		static void TransformToLocalFunctionReference(ILFunction function, CallInstruction useSite)
		{
			ILInstruction target = useSite.Arguments[0];
			target.ReplaceWith(new LdNull().WithILRange(target));
			if (target is IInstructionWithVariableOperand withVar && withVar.Variable.Kind == VariableKind.Local)
			{
				withVar.Variable.Kind = VariableKind.DisplayClassLocal;
			}
			var fnptr = (IInstructionWithMethodOperand)useSite.Arguments[1];
			var specializeMethod = function.ReducedMethod.Specialize(fnptr.Method.Substitution);
			var replacement = new LdFtn(specializeMethod).WithILRange((ILInstruction)fnptr);
			useSite.Arguments[1].ReplaceWith(replacement);
		}

		Call TransformToLocalFunctionInvocation(LocalFunctionMethod reducedMethod, CallInstruction useSite)
		{
			var specializeMethod = reducedMethod.Specialize(useSite.Method.Substitution);
			bool wasInstanceCall = !useSite.Method.IsStatic;
			var replacement = new Call(specializeMethod);
			int firstArgumentIndex = wasInstanceCall ? 1 : 0;
			int argumentCount = useSite.Arguments.Count;
			int reducedArgumentCount = argumentCount - (reducedMethod.NumberOfCompilerGeneratedParameters + firstArgumentIndex);
			replacement.Arguments.AddRange(useSite.Arguments.Skip(firstArgumentIndex).Take(reducedArgumentCount));
			// copy flags
			replacement.ConstrainedTo = useSite.ConstrainedTo;
			replacement.ILStackWasEmpty = useSite.ILStackWasEmpty;
			replacement.IsTail = useSite.IsTail;
			// copy IL ranges
			replacement.AddILRange(useSite);
			if (wasInstanceCall)
			{
				replacement.AddILRange(useSite.Arguments[0]);
				if (useSite.Arguments[0].MatchLdLocRef(out var variable) && variable.Kind == VariableKind.NamedArgument)
				{
					// remove the store instruction of the simple load, if it is a named argument.
					var storeInst = (ILInstruction)variable.StoreInstructions[0];
					((Block)storeInst.Parent).Instructions.RemoveAt(storeInst.ChildIndex);
				}
			}
			for (int i = 0; i < reducedMethod.NumberOfCompilerGeneratedParameters; i++)
			{
				replacement.AddILRange(useSite.Arguments[argumentCount - i - 1]);
			}
			useSite.ReplaceWith(replacement);
			return replacement;
		}

		void DetermineCaptureAndDeclarationScope(LocalFunctionInfo info, ILInstruction useSite)
		{
			switch (useSite)
			{
				case CallInstruction call:
					if (DelegateConstruction.MatchDelegateConstruction(useSite, out _, out _, out _))
					{
						// if this is a delegate construction, skip the use-site, because the capture scope
						// was already determined when analyzing "this".
						break;
					}
					int firstArgumentIndex = info.Definition.Method.IsStatic ? 0 : 1;
					for (int i = call.Arguments.Count - 1; i >= firstArgumentIndex; i--)
					{
						if (!DetermineCaptureAndDeclarationScope(info, i - firstArgumentIndex, call.Arguments[i]))
							break;
					}
					if (firstArgumentIndex > 0)
					{
						DetermineCaptureAndDeclarationScope(info, -1, call.Arguments[0]);
					}
					break;
				case LdFtn _:
					// &LocalFunction is only possible, if the local function is declared static,
					// this means that the local function can be declared in the top-level scope.
					// leave info.DeclarationScope null/unassigned.
					break;
				default:
					throw new NotSupportedException();
			}
		}

		bool DetermineCaptureAndDeclarationScope(LocalFunctionInfo info, int parameterIndex, ILInstruction arg)
		{
			ILFunction function = info.Definition;
			ILVariable closureVar;
			if (parameterIndex >= 0)
			{
				if (!(parameterIndex < function.Method.Parameters.Count
					&& IsClosureParameter(function.Method.Parameters[parameterIndex], resolveContext)))
				{
					return false;
				}
			}
			if (!(arg.MatchLdLoc(out closureVar) || arg.MatchLdLoca(out closureVar)))
			{
				closureVar = ResolveAncestorScopeReference(arg);
				if (closureVar == null)
				{
					info.UnresolvedClosureArguments.Add(arg);
					return false;
				}
			}
			if (closureVar.Kind == VariableKind.NamedArgument)
			{
				info.UnresolvedClosureArguments.Add(arg);
				return false;
			}
			if (closureVar.IsThis()
				&& !TransformDisplayClassUsage.IsPotentialClosure(context, closureVar.Type.GetDefinition()))
			{
				// An ordinary enclosing 'this' is available throughout the owner function. It is not a
				// compiler closure whose lexical origin must be recovered from an initializer.
				return false;
			}
			if (closureVar.Kind == VariableKind.Parameter)
			{
				// The closure arrives only as a by-ref display-struct parameter of an enclosing
				// local function, which forwards its own closure into this one. Such a parameter
				// has no initializer of its own; follow it to the struct local it was forwarded
				// from so the capture scope is taken from the function that actually owns the
				// display class. Without this, the function defaults to the top-level body and
				// the SROA'd display-class local ends up out of scope at its field accesses.
				var forwardedLocal = ResolveForwardedClosureParameter(closureVar);
				if (forwardedLocal == null)
				{
					info.UnresolvedClosureArguments.Add(arg);
					return false;
				}
				closureVar = forwardedLocal;
			}
			var initializer = GetClosureInitializer(closureVar);
			if (initializer == null)
			{
				info.UnresolvedClosureArguments.Add(arg);
				return false;
			}
			info.UnresolvedClosureArguments.Remove(arg);
			// determine the capture scope of closureVar and the declaration scope of the function 
			var additionalScope = BlockContainer.FindClosestContainer(initializer);
			if (closureVar.CaptureScope == null)
				closureVar.CaptureScope = additionalScope;
			else
			{
				BlockContainer combinedScope = FindCommonAncestorInstruction<BlockContainer>(closureVar.CaptureScope, additionalScope);
				Debug.Assert(combinedScope != null);
				closureVar.CaptureScope = combinedScope;
			}
			if (closureVar.Kind == VariableKind.Local)
			{
				closureVar.Kind = VariableKind.DisplayClassLocal;
			}
			info.CapturedClosureVariables.Add(closureVar);
			if (function.DeclarationScope == null)
			{
				function.DeclarationScope = closureVar.CaptureScope;
			}
			else if (IsInNestedLocalFunction(function.DeclarationScope, closureVar.CaptureScope.Ancestors.OfType<ILFunction>().First()))
			{
				// The existing declaration scope already lies inside a local function nested within
				// the new capture scope's function. That deeper scope is required to see all captured
				// variables, so keep it.
			}
			else if (IsInNestedLocalFunction(closureVar.CaptureScope, function.DeclarationScope.Ancestors.OfType<ILFunction>().First()))
			{
				// The new capture scope lies inside a local function nested within the existing
				// declaration scope's function. This happens when a closure arrives only via a by-ref
				// display-class struct forwarded from an enclosing local function: the function must be
				// declared inside that enclosing function to reach the struct, so adopt the deeper
				// capture scope instead of climbing to a common ancestor.
				function.DeclarationScope = closureVar.CaptureScope;
			}
			else
			{
				var common = FindCommonAncestorInstruction<BlockContainer>(function.DeclarationScope, closureVar.CaptureScope);
				// A local function must be declared where all of its captured closures are visible.
				// When the capture scopes are nested (e.g. a closure created inside a lambda vs. one
				// created in the enclosing method), the deeper scope is the only valid declaration site:
				// it can still see the outer scope's variables, but not vice versa. Fall back to the
				// common ancestor only when the scopes are in unrelated branches.
				if (common != closureVar.CaptureScope)
					function.DeclarationScope = (common == function.DeclarationScope) ? closureVar.CaptureScope : common;
			}
			return true;

			ILInstruction GetClosureInitializer(ILVariable variable)
			{
				var type = variable.Type.UnwrapByRef().GetDefinition();
				if (type == null)
					return null;
				if (variable.Kind == VariableKind.Parameter)
					return null;
				if (type.Kind == TypeKind.Struct)
					return Block.GetContainingStatement(variable.AddressInstructions.OrderBy(i => i.StartILOffset).First());
				else
					return (StLoc)variable.StoreInstructions[0];
			}
		}

		/// <summary>
		/// A nested local function may receive its closure only through a by-ref display-struct
		/// parameter of an enclosing local function, which forwards its own closure into it.
		/// Such a parameter has no initializer, so the originating display-class local cannot be
		/// found from it directly. This walks back to that local: the display-class struct local,
		/// living in an enclosing local function, whose capture scope was already determined from
		/// its own direct use-sites. The result lets the nested function inherit that capture scope
		/// instead of defaulting to the top-level body (which would leave the SROA'd local out of
		/// scope at the nested function's field accesses).
		///
		/// Locals owned by the top-level function or another local function qualify. A local owned by a
		/// lambda does not: the nested function is already nested inside the forwarding function through
		/// the lambda's own scope handling, and rerouting it here would hoist it out of that scope. Returns
		/// null when no such local exists or the origin is ambiguous, so callers fail closed.
		/// </summary>
		ILVariable ResolveForwardedClosureParameter(ILVariable parameter)
		{
			var structType = parameter.Type.UnwrapByRef().GetDefinition();
			if (structType == null || structType.Kind != TypeKind.Struct)
				return null;
			ILVariable result = null;
			foreach (var f in context.Function.Descendants.OfType<ILFunction>())
			{
				if (f != context.Function && f.Kind != ILFunctionKind.LocalFunction)
					continue;
				foreach (var v in f.Variables)
				{
					if (v.Kind == VariableKind.Parameter)
						continue;
					if (v.Type.UnwrapByRef().GetDefinition() != structType)
						continue;
					if (result != null && result != v)
					{
						// Ambiguous: more than one originating local shares the struct type.
						// Bail rather than pick the wrong capture scope.
						return null;
					}
					result = v;
				}
			}
			return result;
		}

		bool IsInNestedLocalFunction(BlockContainer declarationScope, ILFunction function)
		{
			return TreeTraversal.PreOrder(function, f => f.LocalFunctions).Any(f => declarationScope.IsDescendantOf(f.Body));
		}

		internal static bool IsLocalFunctionReference(NewObj inst, ILTransformContext context)
		{
			if (inst == null || inst.Arguments.Count != 2 || inst.Method.DeclaringType.Kind != TypeKind.Delegate)
				return false;
			var opCode = inst.Arguments[1].OpCode;

			return opCode == OpCode.LdFtn
				&& IsLocalFunctionMethod(((IInstructionWithMethodOperand)inst.Arguments[1]).Method, context);
		}

		public static bool IsLocalFunctionMethod(IMethod method, ILTransformContext context)
		{
			if (method.MetadataToken.IsNil)
				return false;
			return IsLocalFunctionMethod(method.ParentModule.MetadataFile, (MethodDefinitionHandle)method.MetadataToken, context);
		}

		public static bool IsLocalFunctionMethod(MetadataFile module, MethodDefinitionHandle methodHandle, ILTransformContext context = null)
		{
			if (context != null && context.PEFile != module)
				return false;

			var metadata = module.Metadata;
			var method = metadata.GetMethodDefinition(methodHandle);
			var declaringType = method.GetDeclaringType();

			if ((method.Attributes & MethodAttributes.Assembly) == 0 || !(method.IsCompilerGenerated(metadata) || declaringType.IsCompilerGenerated(metadata)))
				return false;

			if (!ParseLocalFunctionName(metadata.GetString(method.Name), out _, out _))
				return false;

			return true;
		}

		public static bool LocalFunctionNeedsAccessibilityChange(MetadataFile module, MethodDefinitionHandle methodHandle)
		{
			if (!IsLocalFunctionMethod(module, methodHandle))
				return false;

			var metadata = module.Metadata;
			var method = metadata.GetMethodDefinition(methodHandle);

			FindRefStructParameters visitor = new FindRefStructParameters();
			method.DecodeSignature(visitor, default);

			foreach (var h in visitor.RefStructTypes)
			{
				var td = metadata.GetTypeDefinition(h);
				if (td.IsCompilerGenerated(metadata) && td.IsValueType(metadata))
					return true;
			}

			return false;
		}

		public static bool IsLocalFunctionDisplayClass(MetadataFile module, TypeDefinitionHandle typeHandle, ILTransformContext context = null)
		{
			if (context != null && context.PEFile != module)
				return false;

			var metadata = module.Metadata;
			var type = metadata.GetTypeDefinition(typeHandle);

			if ((type.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NestedPrivate)
				return false;
			if (!type.HasGeneratedName(metadata))
				return false;

			var declaringTypeHandle = type.GetDeclaringType();
			var declaringType = metadata.GetTypeDefinition(declaringTypeHandle);

			foreach (var method in declaringType.GetMethods())
			{
				if (!IsLocalFunctionMethod(module, method, context))
					continue;
				var md = metadata.GetMethodDefinition(method);
				if (md.DecodeSignature(new FindTypeDecoder(typeHandle, module), default).ParameterTypes.Any())
					return true;
			}

			return false;
		}

		/// <summary>
		/// Newer Roslyn versions use the format "&lt;callerName&gt;g__functionName|x_y"
		/// Older versions use "&lt;callerName&gt;g__functionNamex_y"
		/// </summary>
		static readonly Regex functionNameRegex = new Regex(@"^<(.*)>g__([^\|]*)\|{0,1}\d+(_\d+)?$", RegexOptions.Compiled);

		internal static bool ParseLocalFunctionName(string name, out string callerName, out string functionName)
		{
			callerName = null;
			functionName = null;
			if (string.IsNullOrWhiteSpace(name))
				return false;
			var match = functionNameRegex.Match(name);
			callerName = match.Groups[1].Value;
			functionName = match.Groups[2].Value;
			return match.Success;
		}

		class FindRefStructParameters : ISignatureTypeProvider<TypeDefinitionHandle, Unit>
		{
			public readonly List<TypeDefinitionHandle> RefStructTypes = new List<TypeDefinitionHandle>();

			public TypeDefinitionHandle GetArrayType(TypeDefinitionHandle elementType, ArrayShape shape) => default;
			public TypeDefinitionHandle GetFunctionPointerType(MethodSignature<TypeDefinitionHandle> signature) => default;
			public TypeDefinitionHandle GetGenericInstantiation(TypeDefinitionHandle genericType, ImmutableArray<TypeDefinitionHandle> typeArguments) => default;
			public TypeDefinitionHandle GetGenericMethodParameter(Unit genericContext, int index) => default;
			public TypeDefinitionHandle GetGenericTypeParameter(Unit genericContext, int index) => default;
			public TypeDefinitionHandle GetModifiedType(TypeDefinitionHandle modifier, TypeDefinitionHandle unmodifiedType, bool isRequired) => default;
			public TypeDefinitionHandle GetPinnedType(TypeDefinitionHandle elementType) => default;
			public TypeDefinitionHandle GetPointerType(TypeDefinitionHandle elementType) => default;
			public TypeDefinitionHandle GetPrimitiveType(PrimitiveTypeCode typeCode) => default;
			public TypeDefinitionHandle GetSZArrayType(TypeDefinitionHandle elementType) => default;

			public TypeDefinitionHandle GetByReferenceType(TypeDefinitionHandle elementType)
			{
				if (!elementType.IsNil)
					RefStructTypes.Add(elementType);
				return elementType;
			}

			public TypeDefinitionHandle GetTypeFromSpecification(MetadataReader reader, Unit genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => default;
			public TypeDefinitionHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle;
			public TypeDefinitionHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => default;
		}
	}
}

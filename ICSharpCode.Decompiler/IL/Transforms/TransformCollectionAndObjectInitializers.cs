// Copyright (c) 2017 Siegfried Pammer
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Transforms collection and object initialization patterns.
	/// </summary>
	public class TransformCollectionAndObjectInitializers : IStatementTransform
	{
		void IStatementTransform.Run(Block block, int pos, StatementTransformContext context)
		{
			if (!context.Settings.ObjectOrCollectionInitializers)
				return;
			ILInstruction inst = block.Instructions[pos];
			// Match stloc(v, newobj)
			if (!inst.MatchStLoc(out var v, out var initInst) || v.Kind != VariableKind.Local && v.Kind != VariableKind.StackSlot)
				return;
			IType instType;
			var blockKind = BlockKind.CollectionInitializer;
			var insertionPos = initInst.ChildIndex;
			var siblings = initInst.Parent!.Children;
			IMethod currentMethod = context.Function.Method!;
			// A non-record value-type 'with' is only recovered when at least one of the
			// folded members is an init-only setter (which cannot be assigned outside an
			// initializer). Without that guarantee a plain struct copy followed by member
			// assignments is legitimate separate statements and must be left untouched.
			bool requireInitOnlyForValueTypeWith = false;
			// The record clone call of a 'with' expression, if that is what starts this initializer.
			CallInstruction? recordCloneCall = null;
			// we allow a castclass instruction to wrap the init instruction:
			// this is needed, for example, for inherited record types used on .NET runtimes (e.g., .NET 4.x),
			// where covariant return types are not supported.
			if (initInst.MatchCastClass(out var arg, out var targetType))
			{
				initInst = arg;
			}
			// A 'with' whose receiver statically has a generic type parameter type (constrained to
			// a record class) is lowered with generic conversions around the clone call:
			// unbox.any T (callvirt <Clone>$ (box T (receiver))). Because the type parameter is
			// reference-constrained, both conversions are no-op reference conversions, so the
			// with-expression (whose C# type is the type parameter itself) can be recovered by
			// looking through them.
			else if (initInst.MatchUnboxAny(out arg, out var unboxedType)
				&& unboxedType is ITypeParameter && unboxedType.IsReferenceType == true
				&& arg is CallInstruction unboxedCloneCall
				&& IsRecordCloneMethodCall(unboxedCloneCall))
			{
				targetType = unboxedType;
				initInst = unboxedCloneCall;
			}
			switch (initInst)
			{
				case NewObj newObjInst:
					if (newObjInst.ILStackWasEmpty && v.Kind == VariableKind.Local
						&& !TypeContainsInitOnlyOrRequiredMembers(newObjInst.Method.DeclaringTypeDefinition, context.Settings.RequiredMembers)
						&& !currentMethod.IsConstructor
						&& !currentMethod.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
					{
						// on statement level (no other expressions on IL stack),
						// prefer to keep local variables (but not stack slots),
						// unless we are in a constructor (where inlining object initializers might be critical
						// for the base ctor call) or a compiler-generated delegate method, which might be used in a query expression.
						return;
					}
					// Do not try to transform delegate construction.
					// DelegateConstruction transform cannot deal with this.
					if (DelegateConstruction.MatchDelegateConstruction(newObjInst, out _, out _, out _)
						|| TransformDisplayClassUsage.IsPotentialClosure(context, newObjInst))
						return;
					// Cannot build a collection/object initializer attached to an AnonymousTypeCreateExpression
					// anon = new { A = 5 } { 3,4,5 } is invalid syntax.
					if (newObjInst.Method.DeclaringType.ContainsAnonymousType())
						return;
					// Tuples cannot have initializers
					if (TupleTransform.MatchTupleConstruction(newObjInst, out _))
						return;
					instType = newObjInst.Method.DeclaringType;
					break;
				case DefaultValue defaultVal:
					instType = defaultVal.Type;
					break;
				case Call c when c.Method.FullNameIs("System.Activator", "CreateInstance") && c.Method.TypeArguments.Count == 1:
					if (!context.Settings.UseObjectCreationOfGenericTypeParameter)
					{
						return;
					}
					instType = c.Method.TypeArguments[0];
					blockKind = BlockKind.ObjectInitializer;
					break;
				case CallInstruction ci when context.Settings.WithExpressions && IsRecordCloneMethodCall(ci):
					instType = ci.Method.DeclaringType;
					blockKind = BlockKind.WithInitializer;
					recordCloneCall = ci;
					initInst = ci.Arguments.Single();
					// For a receiver of generic type parameter type, the clone call's receiver is
					// boxed to the record class the parameter is constrained to; the box is a no-op
					// reference conversion, so the receiver expression is the unboxed value itself.
					if (initInst is Box { Type: ITypeParameter { IsReferenceType: true } } box)
					{
						initInst = box.Argument;
					}
					break;
				default:
					var typeDef = v.Type.GetDefinition();
					if (context.Settings.WithExpressions && typeDef?.IsReferenceType == false && typeDef.IsRecord)
					{
						instType = v.Type;
						blockKind = BlockKind.WithInitializer;
						break;
					}
					// A C# 11 'with' on a non-record value type compiles to a plain copy of the
					// source value into a local, followed by init-only setter calls on that local.
					// Recover it into a 'with' expression so the init-only members are not assigned
					// outside an initializer (CS8852).
					if (context.Settings.WithExpressions && typeDef?.IsReferenceType == false
						&& (initInst is LdLoc || initInst is LdObj)
						&& TypeContainsInitOnlyOrRequiredMembers(typeDef, includeRequiredMembers: false))
					{
						instType = v.Type;
						blockKind = BlockKind.WithInitializer;
						requireInitOnlyForValueTypeWith = true;
						break;
					}
					return;
			}
			if (targetType != null)
			{
				instType = targetType;
			}
			// Copy-propagate stack slot holding an 'ldloca' of the variable
			if (pos < block.Instructions.Count && block.Instructions[pos + 1] is StLoc { Variable: { Kind: VariableKind.StackSlot, IsSingleDefinition: true }, Value: LdLoca ldLoca } stLocStack && ldLoca.Variable == v)
			{
				CopyPropagation.Propagate(stLocStack, context);
			}
			if (recordCloneCall != null && v.IsSingleDefinition)
			{
				// C# evaluates 'src with { M = value }' by cloning src first and computing the values
				// afterwards. When computing a value takes statements of its own, the clone result has
				// to survive those statements on the IL stack, which surfaces as a second variable
				// aliasing the clone. Propagate the alias away, so all member assignments name the
				// clone variable itself.
				if (TryPropagateCloneAlias(block, pos, v, instType, context))
				{
					// Propagation removes a statement and re-runs inlining, so restart from scratch.
					context.RequestRerun(pos);
					return;
				}
				// Those same value-computing statements sit between the clone call and the member
				// assignments, where the scan below cannot cross them. Move the clone call, together
				// with the member assignments it belongs to, down in front of the last one. That puts
				// the with-initializer back together and orders the decompiled statements the way C#
				// requires them: the computations first, the with-expression after them.
				if (TryPlanDetachedWithInitializer(block, pos, v, instType, recordCloneCall, context, out var itemPositions))
				{
					context.Step("Move record clone call down to its with-initializer", inst);
					int landingPos = itemPositions[itemPositions.Count - 1] - itemPositions.Count;
					var moved = new List<ILInstruction>(itemPositions.Count + 1) { inst };
					foreach (int itemPos in itemPositions)
						moved.Add(block.Instructions[itemPos]);
					for (int i = itemPositions.Count - 1; i >= 0; i--)
						block.Instructions.RemoveAt(itemPositions[i]);
					block.Instructions.RemoveAt(pos);
					for (int i = 0; i < moved.Count; i++)
						block.Instructions.Insert(landingPos + i, moved[i]);
					// The initializer items are contiguous at the new position, so re-running the
					// statement transforms there folds them into a with-expression.
					context.RequestRerun(landingPos);
					context.EndStep(inst);
					return;
				}
			}
			// The same shape for an ordinary object initializer: the scan below only follows a
			// contiguous run, so a member whose value took statements to compute ends the
			// initializer and takes every later member with it. Where the type has init-only or
			// required members that is not a matter of style, so move the allocation down to where
			// the members are contiguous, exactly as the with-expression case does.
			if (initInst is NewObj newObjForInitializer && v.IsSingleDefinition
				&& TypeContainsInitOnlyOrRequiredMembers(newObjForInitializer.Method.DeclaringTypeDefinition, context.Settings.RequiredMembers))
			{
				// The allocation lands in a stack slot and is copied into a local partway through
				// the member assignments, so the members before and after that copy name different
				// variables and the scan below stops at the copy. Unify them first: both are single
				// definitions holding the same reference, so replacing one by the other is a
				// rename. Unlike the clone case this does not require every use to be a setter
				// target - the local is typically returned as well, and a rename is safe either way.
				//
				// Only worth doing when a member assignment is actually stranded behind an
				// intervening statement. Propagating whenever an alias exists would rename the
				// local in every ordinary debug build, where the alias IS the variable the source
				// declared and the scan handles the members perfectly well without help.
				if (HasSetterSeparatedFromAlias(block, pos, v, instType, context)
					&& TryPropagateCloneAlias(block, pos, v, instType, context, requireSetterTargetsOnly: false))
				{
					context.RequestRerun(pos);
					return;
				}
			}
			if (initInst is NewObj newObjForInitializer2 && v.IsSingleDefinition
				&& TryPlanDetachedObjectInitializer(block, pos, v, instType, newObjForInitializer2, context, out var objectItemPositions))
			{
				context.Step("Move object creation down to its initializer", inst);
				int landingPos = objectItemPositions[objectItemPositions.Count - 1] - objectItemPositions.Count;
				var moved = new List<ILInstruction>(objectItemPositions.Count + 1) { inst };
				foreach (int itemPos in objectItemPositions)
					moved.Add(block.Instructions[itemPos]);
				for (int i = objectItemPositions.Count - 1; i >= 0; i--)
					block.Instructions.RemoveAt(objectItemPositions[i]);
				block.Instructions.RemoveAt(pos);
				for (int i = 0; i < moved.Count; i++)
					block.Instructions.Insert(landingPos + i, moved[i]);
				context.RequestRerun(landingPos);
				context.EndStep(inst);
				return;
			}
			int initializerItemsCount = 0;
			bool initializerContainsInitOnlyItems = false;
			possibleIndexVariables.Clear();
			currentPath.Clear();
			isCollection = false;
			pathStack.Clear();
			pathStack.Push(new HashSet<AccessPathElement>());
			// Detect initializer type by scanning the following statements
			// each must be a callvirt with ldloc v as first argument
			// if the method is a setter we're dealing with an object initializer
			// if the method is named Add and has at least 2 arguments we're dealing with a collection/dictionary initializer
			while (pos + initializerItemsCount + 1 < block.Instructions.Count
				&& IsPartOfInitializer(block.Instructions, pos + initializerItemsCount + 1, v, instType, ref blockKind, ref initializerContainsInitOnlyItems, context))
			{
				initializerItemsCount++;
			}
			// Do not convert the statements into an initializer if there's an incompatible usage of the initializer variable
			// directly after the possible initializer.
			if (!initializerContainsInitOnlyItems && IsMethodCallOnVariable(block.Instructions[pos + initializerItemsCount + 1], v))
				return;
			// Calculate the correct number of statements inside the initializer:
			// All index variables that were used in the initializer have Index set to -1.
			// We fetch the first unused variable from the list and remove all instructions after its
			// first usage (i.e. the init store) from the initializer.
			var index = possibleIndexVariables.Where(info => info.Value.Index > -1).Min(info => (int?)info.Value.Index);
			if (index != null)
			{
				initializerItemsCount = index.Value - pos - 1;
			}
			// The initializer would be empty, there's nothing to do here.
			if (initializerItemsCount <= 0)
				return;
			// A non-record value-type copy is only folded into a 'with' expression when it
			// actually sets an init-only member; otherwise it is an ordinary mutable-struct
			// copy that must remain a sequence of separate assignments.
			if (requireInitOnlyForValueTypeWith && !initializerContainsInitOnlyItems)
				return;
			context.Step("CollectionOrObjectInitializer", inst);
			// Create a new block and final slot (initializer target variable)
			var initializerBlock = new Block(blockKind);
			ILVariable finalSlot = context.Function.RegisterVariable(VariableKind.InitializerTarget, instType);
			initializerBlock.FinalInstruction = new LdLoc(finalSlot);
			initializerBlock.Instructions.Add(new StLoc(finalSlot, initInst));
			// Move all instructions to the initializer block.
			for (int i = 1; i <= initializerItemsCount; i++)
			{
				switch (block.Instructions[i + pos])
				{
					case CallInstruction call:
						if (!(call is CallVirt || call is Call))
							continue;
						var newCall = call;
						var newTarget = newCall.Arguments[0];
						foreach (var load in newTarget.Descendants.OfType<IInstructionWithVariableOperand>())
							if ((load is LdLoc || load is LdLoca) && load.Variable == v)
								load.Variable = finalSlot;
						initializerBlock.Instructions.Add(newCall);
						break;
					case StObj stObj:
						var newStObj = stObj;
						foreach (var load in newStObj.Target.Descendants.OfType<IInstructionWithVariableOperand>())
							if ((load is LdLoc || load is LdLoca) && load.Variable == v)
								load.Variable = finalSlot;
						initializerBlock.Instructions.Add(newStObj);
						break;
					case StLoc stLoc:
						var newStLoc = stLoc;
						initializerBlock.Instructions.Add(newStLoc);
						break;
				}
			}
			block.Instructions.RemoveRange(pos + 1, initializerItemsCount);
			siblings[insertionPos] = initializerBlock;
			ILInlining.InlineIfPossible(block, pos, context);
			context.EndStep(initializerBlock);
		}

		/// <summary>
		/// Replaces a variable that merely aliases the record clone stored in <paramref name="v"/>
		/// with <paramref name="v"/> itself, provided the alias is used for nothing but member
		/// assignments. Returns true if such an alias was found and removed.
		/// </summary>
		/// <summary>
		/// Gets whether the object stored in <paramref name="v"/> is copied into another variable
		/// which is then assigned a member only after some unrelated statement. That is the shape
		/// where unifying the two variables buys something: the member is stranded behind the
		/// intervening statement and would otherwise fall out of the initializer.
		/// </summary>
		static bool HasSetterSeparatedFromAlias(Block block, int pos, ILVariable v, IType instType, StatementTransformContext context)
		{
			ILVariable? alias = null;
			bool sawInterveningStatement = false;
			for (int i = pos + 1; i < block.Instructions.Count; i++)
			{
				ILInstruction current = block.Instructions[i];
				if (alias == null)
				{
					if (current is StLoc { Variable: { IsSingleDefinition: true, AddressCount: 0, LoadCount: > 0 } candidate } aliasStore
						&& aliasStore.Value.MatchLdLoc(v))
					{
						alias = candidate;
						continue;
					}
					// Before the copy, only the object's own member assignments may intervene;
					// anything else means this is not one initializer being split.
					var (kindBefore, pathBefore, _, targetBefore, _) = AccessPathElement.GetAccessPath(current, instType, context.Settings, context.CSharpResolver);
					if (kindBefore != AccessPathKind.Setter || targetBefore != v || pathBefore.Count != 1)
						return false;
					continue;
				}
				var (kind, path, _, target, _) = AccessPathElement.GetAccessPath(current, instType, context.Settings, context.CSharpResolver);
				if (kind == AccessPathKind.Setter && target == alias && path.Count == 1)
				{
					if (sawInterveningStatement)
						return true;
					continue;
				}
				if (current.Descendants.OfType<IInstructionWithVariableOperand>().Any(load => load.Variable == alias))
					return false;
				sawInterveningStatement = true;
			}
			return false;
		}

		static bool TryPropagateCloneAlias(Block block, int pos, ILVariable v, IType instType, StatementTransformContext context,
			bool requireSetterTargetsOnly = true)
		{
			for (int i = pos + 1; i < block.Instructions.Count; i++)
			{
				ILInstruction current = block.Instructions[i];
				if (current is StLoc { Variable: { IsSingleDefinition: true, AddressCount: 0, LoadCount: > 0 } alias } aliasStore
					&& aliasStore.Value.MatchLdLoc(v)
					&& (!requireSetterTargetsOnly || alias.LoadInstructions.All(IsPropertySetterTarget)))
				{
					CopyPropagation.Propagate(aliasStore, context);
					return true;
				}
				// Only look past the member assignments of the clone itself; anything else means the
				// alias, if any, belongs to unrelated code.
				var (kind, path, _, targetVariable, _) = AccessPathElement.GetAccessPath(current, instType, context.Settings, context.CSharpResolver);
				if (kind != AccessPathKind.Setter || targetVariable != v || path.Count != 1)
					return false;
			}
			return false;

			static bool IsPropertySetterTarget(LdLoc load)
			{
				return load.Parent is CallInstruction { Method: { IsAccessor: true, IsStatic: false } method } call
					&& method.AccessorKind == System.Reflection.MethodSemanticsAttributes.Setter
					&& call.Arguments[0] == load;
			}
		}

		/// <summary>
		/// Scans the statements after <paramref name="pos"/> for the member assignments belonging to
		/// the record clone stored in <paramref name="v"/>. Returns true if statements computing
		/// initializer values are interleaved with them and the clone call may be moved down past
		/// those statements, in which case <paramref name="itemPositions"/> holds the indices of the
		/// member assignments that have to move along with it.
		/// </summary>
		static bool TryPlanDetachedWithInitializer(Block block, int pos, ILVariable v, IType instType,
			CallInstruction cloneCall, StatementTransformContext context, [NotNullWhen(true)] out List<int>? itemPositions)
		{
			itemPositions = null;
			if (!IsSideEffectFreeRecordCopy(cloneCall.Method.DeclaringTypeDefinition))
				return false;
			// The instruction producing the object being cloned moves down as well, so it must be
			// unaffected by the statements it moves past.
			ILInstruction source = cloneCall.Arguments[0];
			if (!SemanticHelper.IsPure(source.Flags))
				return false;
			return TryPlanDetachedInitializer(block, pos, v, instType, source, context, out itemPositions);
		}

		/// <summary>
		/// The same plan for an object initializer: the allocation stored in <paramref name="v"/>
		/// moves down to where its member assignments are contiguous, so that members whose values
		/// took statements to compute still land inside the initializer.
		/// </summary>
		/// <remarks>
		/// Only attempted for a type carrying init-only or required members, where the detached form
		/// is unwritable rather than merely ugly: an init-only member assigned outside an
		/// initializer is CS8852, and a required member left out of one is CS9035, which no later
		/// statement can satisfy.
		///
		/// Unlike the clone call above, the allocation cannot be shown harmless - the constructor
		/// usually belongs to another assembly, whose method bodies are never loaded - so what is
		/// checked instead is that nothing user-written travels with it (the constructor arguments
		/// are pure) and that nothing it passes can observe it. The constructor running later is
		/// accepted on the same ground the with-case accepts a mutating copy: the alternative does
		/// not compile at all.
		/// </remarks>
		static bool TryPlanDetachedObjectInitializer(Block block, int pos, ILVariable v, IType instType,
			NewObj newObjInst, StatementTransformContext context, [NotNullWhen(true)] out List<int>? itemPositions)
		{
			itemPositions = null;
			if (!TypeContainsInitOnlyOrRequiredMembers(newObjInst.Method.DeclaringTypeDefinition, context.Settings.RequiredMembers))
				return false;
			foreach (var argument in newObjInst.Arguments)
			{
				if (!SemanticHelper.IsPure(argument.Flags))
					return false;
			}
			return TryPlanDetachedInitializer(block, pos, v, instType, source: null, context, out itemPositions);
		}

		/// <summary>
		/// Shared body of the two planners above. <paramref name="source"/> is the instruction that
		/// travels down with the initializer target and must be reorderable with everything it
		/// passes; null where the travelling instruction is the allocation itself, which is impure
		/// by construction and so could never satisfy that test (see the remarks above).
		/// </summary>
		static bool TryPlanDetachedInitializer(Block block, int pos, ILVariable v, IType instType,
			ILInstruction? source, StatementTransformContext context, [NotNullWhen(true)] out List<int>? itemPositions)
		{
			itemPositions = null;
			var items = new List<int>();
			var itemValues = new List<ILInstruction>();
			var fillersBeforeItem = new List<int>();
			int fillerCount = 0;
			for (int i = pos + 1; i < block.Instructions.Count; i++)
			{
				ILInstruction current = block.Instructions[i];
				var (kind, path, values, targetVariable, _) = AccessPathElement.GetAccessPath(current, instType, context.Settings, context.CSharpResolver);
				if (kind == AccessPathKind.Setter && targetVariable == v && path.Count == 1 && values?.Count == 1)
				{
					items.Add(i);
					itemValues.Add(values[0]);
					fillersBeforeItem.Add(fillerCount);
					continue;
				}
				// A statement mentioning the clone variable would observe the clone, or worse, depend
				// on it having happened already.
				if (current.Descendants.OfType<IInstructionWithVariableOperand>().Any(load => load.Variable == v))
					break;
				// Moving the clone call down means its field reads happen after this statement
				// instead of before it. The clone is a compiler-generated memberwise copy (see
				// IsSideEffectFreeRecordCopy above), so only a statement that writes state the
				// clone reads could observe the difference; the fillers here are the compiler's
				// own initializer-value computations, whose calls (enumerators, getters) do not
				// do that in compiler-generated code. A hand-written callee that mutates the
				// source's fields would diverge - accepted, because the detached form otherwise
				// assigns an init-only member outside any initializer and cannot compile
				// (CS8852). The receiver expression itself must still be reorderable, so writes
				// to the receiver local are caught.
				if (source != null && !SemanticHelper.MayReorder(source, current))
					break;
				// The values of the member assignments found so far move past this statement, too.
				if (itemValues.Any(value => !SemanticHelper.MayReorder(value, current)))
					break;
				fillerCount++;
			}
			// Every member assignment but the last one moves down past the value-computing statements
			// that follow it, taking its own value and its setter along, so both must be free of
			// interfering side effects. Give up on the assignments beyond the first one that is not.
			int keep = items.Count;
			for (int i = 0; i + 1 < keep; i++)
			{
				if (!SemanticHelper.IsPure(itemValues[i].Flags)
					|| block.Instructions[items[i]] is not CallInstruction { Method: var setter }
					|| !setter.IsCompilerGenerated())
				{
					keep = i + 1;
					break;
				}
			}
			items.RemoveRange(keep, items.Count - keep);
			// Nothing was skipped: the regular scan already handles this shape.
			if (items.Count == 0 || fillersBeforeItem[items.Count - 1] == 0)
				return false;
			if (!AreAllUsesStatementsInBlockAfter(v, block, pos))
				return false;
			itemPositions = items;
			return true;
		}

		/// <summary>
		/// Gets whether every use of the record clone stored in <paramref name="v"/> is a statement of
		/// <paramref name="block"/> following <paramref name="pos"/>. Only then is it guaranteed that
		/// no code observes the clone before the position it is moved to - not even when one of the
		/// statements it is moved past throws.
		/// </summary>
		static bool AreAllUsesStatementsInBlockAfter(ILVariable v, Block block, int pos)
		{
			foreach (var use in v.LoadInstructions.Concat<ILInstruction>(v.AddressInstructions))
			{
				ILInstruction? statement = use;
				while (statement != null && statement.Parent != block)
					statement = statement.Parent;
				if (statement == null || statement.ChildIndex <= pos)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Gets whether cloning an instance of the given record type runs no user code.
		/// A compiler-generated copy constructor performs a plain memberwise copy and chains to the
		/// base record's copy constructor, so the clone is unobservable apart from the allocation.
		/// A hand-written copy constructor may do anything and pins the clone to its original place.
		/// </summary>
		/// <remarks>
		/// '&lt;Clone&gt;$' is virtual, so a record derived from <paramref name="recordType"/> could
		/// still contribute a hand-written copy constructor. That case is not detected here.
		/// </remarks>
		static bool IsSideEffectFreeRecordCopy(ITypeDefinition? recordType)
		{
			// Metadata can hold a base chain that never reaches object: 'class C<X> : C<C<X>>'
			// resolves back to the same definition. Track what has already been walked rather than
			// trusting the chain to end, and treat a cycle as unproven.
			HashSet<ITypeDefinition>? visited = null;
			while (recordType != null && recordType.IsRecord)
			{
				visited ??= new HashSet<ITypeDefinition>();
				if (!visited.Add(recordType))
					return false;
				IMethod? copyConstructor = null;
				foreach (var ctor in recordType.GetConstructors())
				{
					if (ctor.Parameters.Count == 1 && ctor.Parameters[0].Type.GetDefinition() == recordType)
					{
						copyConstructor = ctor;
						break;
					}
				}
				if (copyConstructor == null || !copyConstructor.IsCompilerGenerated())
					return false;
				recordType = recordType.DirectBaseTypes.FirstOrDefault(t => t.Kind == TypeKind.Class)?.GetDefinition();
			}
			return true;
		}

		private static bool TypeContainsInitOnlyOrRequiredMembers(ITypeDefinition? typeDefinition, bool includeRequiredMembers)
		{
			if (typeDefinition == null)
				return false;
			foreach (var property in typeDefinition.Properties)
			{
				if (property.Setter?.IsInitOnly ?? false)
				{
					return true;
				}
				if (includeRequiredMembers && property.HasAttribute(KnownAttribute.Required, inherit: false))
				{
					return true;
				}
			}
			if (includeRequiredMembers)
			{
				foreach (var field in typeDefinition.Fields)
				{
					if (field.HasAttribute(KnownAttribute.Required, inherit: false))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal static bool IsRecordCloneMethodCall(CallInstruction ci)
		{
			if (ci.Method.DeclaringTypeDefinition?.IsRecord != true)
				return false;
			if (ci.Method.Name != "<Clone>$")
				return false;
			if (ci.Arguments.Count != 1)
				return false;

			return true;
		}

		bool IsMethodCallOnVariable(ILInstruction inst, ILVariable variable)
		{
			if (inst.MatchLdLocRef(variable))
				return true;
			if (inst is CallInstruction call && call.Arguments.Count > 0 && !call.Method.IsStatic)
				return IsMethodCallOnVariable(call.Arguments[0], variable);
			if (inst.MatchLdFld(out var target, out _) || inst.MatchStFld(out target, out _, out _) || inst.MatchLdFlda(out target, out _))
				return IsMethodCallOnVariable(target, variable);
			return false;
		}

		readonly Dictionary<ILVariable, (int Index, ILInstruction Value)> possibleIndexVariables = new Dictionary<ILVariable, (int Index, ILInstruction Value)>();
		readonly List<AccessPathElement> currentPath = new List<AccessPathElement>();
		bool isCollection;
		readonly Stack<HashSet<AccessPathElement>> pathStack = new Stack<HashSet<AccessPathElement>>();

		bool IsPartOfInitializer(InstructionCollection<ILInstruction> instructions, int pos, ILVariable target, IType rootType, ref BlockKind blockKind, ref bool initializerContainsInitOnlyItems, StatementTransformContext context)
		{
			// Include any stores to local variables that are single-assigned and do not reference the initializer-variable
			// in the list of possible index variables.
			// Index variables are used to implement dictionary initializers.
			if (instructions[pos] is StLoc stloc && stloc.Variable.Kind == VariableKind.Local && stloc.Variable.IsSingleDefinition)
			{
				if (!context.Settings.DictionaryInitializers)
					return false;
				if (stloc.Value.Descendants.OfType<IInstructionWithVariableOperand>().Any(ld => ld.Variable == target && (ld is LdLoc || ld is LdLoca)))
					return false;
				possibleIndexVariables.Add(stloc.Variable, (stloc.ChildIndex, stloc.Value));
				return true;
			}
			(var kind, var newPath, var values, var targetVariable, var usedIndices) = AccessPathElement.GetAccessPath(instructions[pos], rootType, context.Settings, context.CSharpResolver);
			if (kind == AccessPathKind.Invalid || target != targetVariable)
				return false;
			// A with-initializer only permits direct member assignments. Nested object or
			// collection initializer syntax ("Member = { ... }") is not valid inside a
			// with-expression, even though it is valid in an object initializer.
			if (blockKind == BlockKind.WithInitializer && newPath.Count != 1)
				return false;
			// Treat last element separately:
			// Can either be an Add method call or property setter.
			var lastElement = newPath.Last();
			newPath.RemoveLast();
			// Compare new path with current path:
			int minLen = Math.Min(currentPath.Count, newPath.Count);
			int firstDifferenceIndex = 0;
			while (firstDifferenceIndex < minLen && newPath[firstDifferenceIndex] == currentPath[firstDifferenceIndex])
				firstDifferenceIndex++;
			while (currentPath.Count > firstDifferenceIndex)
			{
				isCollection = false;
				currentPath.RemoveAt(currentPath.Count - 1);
				pathStack.Pop();
			}
			while (currentPath.Count < newPath.Count)
			{
				AccessPathElement newElement = newPath[currentPath.Count];
				currentPath.Add(newElement);
				if (isCollection || !pathStack.Peek().Add(newElement))
					return false;
				pathStack.Push(new HashSet<AccessPathElement>());
			}
			switch (kind)
			{
				case AccessPathKind.Adder:
					isCollection = true;
					if (pathStack.Peek().Count != 0)
						return false;
					MarkUsedIndices();
					return true;
				case AccessPathKind.Setter:
					if (isCollection || !pathStack.Peek().Add(lastElement))
						return false;
					if (values?.Count != 1 || !IsValidObjectInitializerTarget(currentPath))
						return false;
					if (blockKind != BlockKind.ObjectInitializer && blockKind != BlockKind.WithInitializer)
						blockKind = BlockKind.ObjectInitializer;
					// A required member, like an init-only setter, must be set inside the object
					// initializer. Treat it the same so the "incompatible usage" bail-out below does not
					// abandon the initializer and emit the assignment as a standalone statement (which
					// would not satisfy the required-member rule). Gated on the setting, because when
					// 'required' is not emitted the loose assignment is legal and folding is optional.
					initializerContainsInitOnlyItems |= lastElement.Member is IProperty { Setter.IsInitOnly: true }
						|| (context.Settings.RequiredMembers
							&& lastElement.Member.HasAttribute(KnownAttribute.Required, inherit: false));
					MarkUsedIndices();
					return true;
				default:
					return false;
			}

			void MarkUsedIndices()
			{
				foreach (var index in usedIndices)
				{
					if (possibleIndexVariables.TryGetValue(index, out var item))
					{
						possibleIndexVariables[index] = (-1, item.Value);
					}
				}
			}
		}

		bool IsValidObjectInitializerTarget(List<AccessPathElement> path)
		{
			if (path.Count == 0)
				return true;
			var element = path.Last();
			var previous = path.SkipLast(1).LastOrDefault();
			if (element.Member is not IProperty p)
				return true;
			if (!p.IsIndexer)
				return true;
			if (previous != default)
			{
				return NormalizeTypeVisitor.IgnoreNullabilityAndTuples
					.EquivalentTypes(previous.Member.ReturnType, element.Member.DeclaringType);
			}
			return false;
		}
	}

	public enum AccessPathKind
	{
		Invalid,
		Setter,
		Adder
	}

	public struct AccessPathElement : IEquatable<AccessPathElement>
	{
		public AccessPathElement(OpCode opCode, IMember member, ILInstruction[]? indices = null)
		{
			this.OpCode = opCode;
			this.Member = member;
			this.Indices = indices;
		}

		public readonly OpCode OpCode;
		public readonly IMember Member;
		public readonly ILInstruction[]? Indices;

		public override string ToString() => $"[{Member}, {Indices}]";

		public static (AccessPathKind Kind, List<AccessPathElement> Path, List<ILInstruction>? Values, ILVariable? Target, List<ILVariable> UsedIndexVariables) GetAccessPath(
			ILInstruction instruction, IType rootType, DecompilerSettings? settings = null,
			CSharpResolver? resolver = null)
		{
			List<AccessPathElement> path = new List<AccessPathElement>();
			ILVariable? target = null;
			AccessPathKind kind = AccessPathKind.Invalid;
			List<ILInstruction>? values = null;
			IMethod method;
			ILInstruction? inst = instruction;
			List<ILVariable> usedIndexVariables = new();
			while (inst != null)
			{
				switch (inst)
				{
					case CallInstruction call:
						if (!(call is CallVirt || call is Call))
							goto default;
						method = call.Method;
						if (resolver != null && !IsMethodApplicable(method, call.Arguments, rootType, resolver, settings))
							goto default;
						inst = call.Arguments[0];
						if (inst is LdObjIfRef ldObjIfRef)
						{
							inst = ldObjIfRef.Target;
						}
						// Setters of a with-expression whose receiver has a generic type parameter
						// type are called on the receiver boxed to the record class the parameter is
						// constrained to. That box is a no-op reference conversion, so the write goes
						// through to the object the variable refers to.
						if (inst is Box { Type: ITypeParameter { IsReferenceType: true } } box)
						{
							inst = box.Argument;
						}
						if (method.IsAccessor)
						{
							if (method.AccessorOwner is IProperty property &&
								!CanBeUsedInInitializer(property, resolver, kind))
							{
								goto default;
							}

							var isGetter = method.AccessorKind == System.Reflection.MethodSemanticsAttributes.Getter;
							var indices = call.Arguments.Skip(1).Take(call.Arguments.Count - (isGetter ? 1 : 2)).ToArray();
							if (indices.Length > 0 && settings?.DictionaryInitializers == false)
								goto default;
							// Mark all index variables as used
							foreach (var index in indices.OfType<IInstructionWithVariableOperand>())
							{
								usedIndexVariables.Add(index.Variable);
							}
							path.Insert(0, new AccessPathElement(call.OpCode, method.AccessorOwner, indices));
						}
						else
						{
							path.Insert(0, new AccessPathElement(call.OpCode, method));
						}
						if (values == null)
						{
							if (method.IsAccessor)
							{
								kind = AccessPathKind.Setter;
								values = new List<ILInstruction> { call.Arguments.Last() };
							}
							else
							{
								kind = AccessPathKind.Adder;
								values = new List<ILInstruction>(call.Arguments.Skip(1));
								if (values.Count == 0)
									goto default;
							}
						}
						break;
					case LdObj ldobj:
					{
						if (ldobj.Target is LdFlda ldflda && (kind != AccessPathKind.Setter || !ldflda.Field.IsReadOnly))
						{
							path.Insert(0, new AccessPathElement(ldobj.OpCode, ldflda.Field));
							inst = ldflda.Target;
							break;
						}
						goto default;
					}
					case LdObjIfRef ldobj:
					{
						if (ldobj.Target is LdFlda ldflda && (kind != AccessPathKind.Setter || !ldflda.Field.IsReadOnly))
						{
							path.Insert(0, new AccessPathElement(ldobj.OpCode, ldflda.Field));
							inst = ldflda.Target;
							break;
						}
						if (ldobj.Target is LdLoca ldloca)
						{
							target = ldloca.Variable;
							inst = null;
							break;
						}
						goto default;
					}
					case StObj stobj:
					{
						if (stobj.Target is LdFlda ldflda)
						{
							path.Insert(0, new AccessPathElement(stobj.OpCode, ldflda.Field));
							inst = ldflda.Target;
							if (values == null)
							{
								values = new List<ILInstruction>(new[] { stobj.Value });
								kind = AccessPathKind.Setter;
							}
							break;
						}
						goto default;
					}
					case LdLoc ldloc:
						target = ldloc.Variable;
						inst = null;
						break;
					case LdLoca ldloca:
						target = ldloca.Variable;
						inst = null;
						break;
					case LdFlda ldflda:
						path.Insert(0, new AccessPathElement(ldflda.OpCode, ldflda.Field));
						inst = ldflda.Target;
						break;
					default:
						kind = AccessPathKind.Invalid;
						inst = null;
						break;
				}
			}
			if (kind != AccessPathKind.Invalid && values != null && values.SelectMany(v => v.Descendants).OfType<IInstructionWithVariableOperand>().Any(ld => ld.Variable == target && (ld is LdLoc || ld is LdLoca)))
				kind = AccessPathKind.Invalid;
			return (kind, path, values, target, usedIndexVariables);
		}

		private static bool CanBeUsedInInitializer(IProperty property, ITypeResolveContext? resolveContext, AccessPathKind kind)
		{
			if (property.CanSet && (property.Accessibility == property.Setter.Accessibility || IsAccessorAccessible(property.Setter, resolveContext)))
				return true;
			return kind != AccessPathKind.Setter;
		}

		private static bool IsAccessorAccessible(IMethod setter, ITypeResolveContext? resolveContext)
		{
			if (resolveContext == null)
				return true;
			var lookup = new MemberLookup(resolveContext.CurrentTypeDefinition, resolveContext.CurrentModule);
			return lookup.IsAccessible(setter, allowProtectedAccess: setter.DeclaringTypeDefinition == resolveContext.CurrentTypeDefinition);
		}

		static bool IsMethodApplicable(IMethod method, IReadOnlyList<ILInstruction> arguments, IType rootType, CSharpResolver resolver, DecompilerSettings? settings)
		{
			if (method.IsStatic && !method.IsExtensionMethod)
				return false;
			if (method.AccessorOwner is IProperty)
				return true;
			if (!"Add".Equals(method.Name, StringComparison.Ordinal) || arguments.Count == 0)
				return false;
			if (method.IsExtensionMethod)
			{
				if (settings?.ExtensionMethodsInCollectionInitializers == false)
					return false;
				if (!resolver.CanTransformToExtensionMethodCall(method, ignoreTypeArguments: true))
					return false;
			}
			var targetType = GetReturnTypeFromInstruction(arguments[0]) ?? rootType;
			if (targetType == null)
				return false;
			if (!targetType.GetAllBaseTypes().Any(i => i.IsKnownType(KnownTypeCode.IEnumerable) || i.IsKnownType(KnownTypeCode.IEnumerableOfT)))
				return false;
			return CanInferTypeArgumentsFromParameters(method);

			bool CanInferTypeArgumentsFromParameters(IMethod method)
			{
				if (method.TypeParameters.Count == 0)
					return true;
				// always use unspecialized member, otherwise type inference fails
				method = (IMethod)method.MemberDefinition;
				new TypeInference(resolver.Compilation)
					.InferTypeArguments(
						method.TypeParameters,
						// TODO : this is not entirely correct... we need argument type information to resolve Add methods properly
						method.Parameters.SelectReadOnlyArray(p => new ResolveResult(p.Type)),
						method.Parameters.SelectReadOnlyArray(p => p.Type),
						out bool success
					);
				return success;
			}
		}

		static IType? GetReturnTypeFromInstruction(ILInstruction instruction)
		{
			switch (instruction)
			{
				case CallInstruction call:
					if (!(call is CallVirt || call is Call))
						goto default;
					return call.Method.ReturnType;
				case LdObj ldobj:
					if (ldobj.Target is LdFlda ldflda)
						return ldflda.Field.ReturnType;
					goto default;
				case StObj stobj:
					if (stobj.Target is LdFlda ldflda2)
						return ldflda2.Field.ReturnType;
					goto default;
				default:
					return null;
			}
		}

		public override bool Equals(object? obj)
		{
			if (obj is AccessPathElement)
				return Equals((AccessPathElement)obj);
			return false;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked
			{
				if (Member != null)
					hashCode += 1000000007 * Member.GetHashCode();
			}
			return hashCode;
		}

		public bool Equals(AccessPathElement other)
		{
			return (other.Member == this.Member
				|| this.Member.Equals(other.Member))
				&& (other.Indices == this.Indices
				|| (other.Indices != null && this.Indices != null && this.Indices.SequenceEqual(other.Indices, ILInstructionMatchComparer.Instance)));
		}

		public static bool operator ==(AccessPathElement lhs, AccessPathElement rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(AccessPathElement lhs, AccessPathElement rhs)
		{
			return !(lhs == rhs);
		}
	}

	class ILInstructionMatchComparer : IEqualityComparer<ILInstruction>
	{
		public static readonly ILInstructionMatchComparer Instance = new ILInstructionMatchComparer();

		public bool Equals(ILInstruction? x, ILInstruction? y)
		{
			if (x == y)
				return true;
			if (x == null || y == null)
				return false;
			return SemanticHelper.IsPure(x.Flags)
				&& SemanticHelper.IsPure(y.Flags)
				&& x.Match(y).Success;
		}

		public int GetHashCode(ILInstruction obj)
		{
			return obj.GetHashCode();
		}
	}
}

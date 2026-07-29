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
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL.ControlFlow;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Transforms closure fields to local variables.
	/// 
	/// This is a post-processing step of <see cref="DelegateConstruction"/>, <see cref="LocalFunctionDecompiler"/>
	/// and <see cref="TransformExpressionTrees"/>.
	/// 
	/// In general we can perform SROA (scalar replacement of aggregates) on any variable that
	/// satisfies the following conditions:
	/// 1) It is initialized by an empty/default constructor call.
	/// 2) The variable is never passed to another method.
	/// 3) The variable is never the target of an invocation.
	/// 
	/// Note that 2) and 3) apply because declarations and uses of lambdas and local functions
	/// are already transformed by the time this transform is applied.
	/// </summary>
	public class TransformDisplayClassUsage : ILVisitor, IILTransform
	{
		class VariableToDeclare
		{
			private readonly DisplayClass container;
			private readonly IField field;
			private ILVariable declaredVariable;

			public string Name => @field.Name;

			public bool CanPropagate { get; private set; }
			public bool UsesInitialValue { get; set; }

			public HashSet<ILInstruction> Initializers { get; } = new HashSet<ILInstruction>();

			public VariableToDeclare(DisplayClass container, IField field, ILVariable declaredVariable = null)
			{
				this.container = container;
				this.field = field;
				this.declaredVariable = declaredVariable;

				Debug.Assert(declaredVariable == null || declaredVariable.StateMachineField == field);
			}

			public void Propagate(ILVariable variable)
			{
				this.declaredVariable = variable;
				this.CanPropagate = variable != null;
			}

			public ILVariable GetOrDeclare()
			{
				if (declaredVariable != null)
					return declaredVariable;
				declaredVariable = container.Variable.Function.RegisterVariable(VariableKind.Local, field.Type, field.Name);
				declaredVariable.InitialValueIsInitialized = true;
				declaredVariable.UsesInitialValue = UsesInitialValue;
				declaredVariable.CaptureScope = container.CaptureScope;
				return declaredVariable;
			}
		}

		[DebuggerDisplay("[DisplayClass {Variable} : {Type}]")]
		class DisplayClass
		{
			public readonly ILVariable Variable;
			public readonly ITypeDefinition Type;
			public readonly Dictionary<IField, VariableToDeclare> VariablesToDeclare;
			public BlockContainer CaptureScope;
			public ILInstruction Initializer;

			public DisplayClass(ILVariable variable, ITypeDefinition type)
			{
				Variable = variable;
				Type = type;
				VariablesToDeclare = new Dictionary<IField, VariableToDeclare>();
			}
		}

		ILTransformContext context;
		ITypeResolveContext decompilationContext;
		readonly Dictionary<ILVariable, DisplayClass> displayClasses = new Dictionary<ILVariable, DisplayClass>();
		readonly Dictionary<ILVariable, ILVariable> displayClassCopyMap = new Dictionary<ILVariable, ILVariable>();

		void IILTransform.Run(ILFunction function, ILTransformContext context)
		{
			if (this.context != null)
				throw new InvalidOperationException("Reentrancy in " + nameof(TransformDisplayClassUsage));
			try
			{
				this.context = context;
				this.decompilationContext = new SimpleTypeResolveContext(context.Function.Method);
				AnalyzeFunction(function);
				Transform(function);
			}
			finally
			{
				ClearState();
			}
		}

		void ClearState()
		{
			displayClasses.Clear();
			displayClassCopyMap.Clear();
			this.decompilationContext = null;
			this.context = null;
		}

		/// <summary>
		/// VB closures are initialized with a copy constructor <c>c = new Closure(prev)</c> that copies
		/// the captured fields from a previous instance (the parent scope, or the previous loop
		/// iteration), whereas SROA expects a parameterless constructor and a single store. When the
		/// copied values are dead - every instance field is reassigned before the closure instance is
		/// read or captured - the copy is unobservable. In that case we neutralize the copy-constructor
		/// argument to <c>null</c> and drop the dead default store, turning the closure into a single,
		/// empty-initialized display class that the normal SROA path dissolves.
		/// </summary>
		internal static void NormalizeVisualBasicClosures(ILFunction function, ILTransformContext context)
		{
			foreach (var newObj in function.Descendants.OfType<NewObj>().ToArray())
			{
				if (newObj.Arguments.Count != 1)
					continue;
				if (!(newObj.Parent is StLoc stloc) || stloc.Value != newObj)
					continue;
				// self-referential copy-constructor init: stloc c (newobj Closure(ldloc c))
				if (!newObj.Arguments[0].MatchLdLoc(out var argVariable) || argVariable != stloc.Variable)
					continue;
				var closureType = newObj.Method.DeclaringTypeDefinition;
				if (closureType == null || newObj.Method.Parameters.Count != 1
					|| !IsPotentialClosure(context, closureType))
				{
					continue;
				}
				if (!(stloc.Parent is Block block))
					continue;
				if (!IsVisualBasicCopyDead(stloc.Variable, block, stloc.ChildIndex, closureType))
					continue;
				context.Step($"Normalize VB closure initializer for {stloc.Variable.Name}", stloc);
				newObj.Arguments[0].ReplaceWith(new LdNull());
				// The self-referential argument was the only reader of the variable's initial/loop-carried
				// value; once it is null the closure is freshly constructed each time, so when the copy-
				// constructor store ends up as the variable's only store, the initial value is no longer
				// used (it would otherwise count as a second definition and block SROA). If any other
				// store survives, definite assignment may still flow through it, so the initial value
				// stays marked as used.
				if (RemoveDeadDefaultStores(stloc.Variable, stloc))
					stloc.Variable.UsesInitialValue = false;
			}
		}

		/// <summary>
		/// Returns true if every instance field copied by the copy constructor is reassigned before the
		/// closure instance is read or captured, making the copied values unobservable. The scan is
		/// limited to the initializing block; anything more complex is treated as a live copy (no rewrite).
		/// </summary>
		static bool IsVisualBasicCopyDead(ILVariable variable, Block block, int initIndex, ITypeDefinition closureType)
		{
			var unwritten = new HashSet<IField>(closureType.Fields
				.Where(f => !f.IsStatic)
				.Select(f => (IField)f.MemberDefinition));
			for (int i = initIndex + 1; i < block.Instructions.Count; i++)
			{
				var stmt = block.Instructions[i];
				// a field store `c.f = value`: the value is evaluated first, then f becomes written
				if (stmt.MatchStFld(out var storeTarget, out var storeField, out var storeValue)
					&& storeTarget.MatchLdLoc(variable))
				{
					if (!FieldReadsAreWritten(storeValue, variable, unwritten))
						return false;
					// The value is evaluated while the field is still unwritten. If it captures the
					// closure instance itself (rather than merely reading its fields), every copied
					// value becomes observable through the captured reference from this point on, so
					// the copy is only dead if all fields have already been reassigned.
					if (LoadsVariableInstance(storeValue, variable))
						return unwritten.Count == 0;
					unwritten.Remove((IField)storeField.MemberDefinition);
					continue;
				}
				// otherwise the statement must not read an unwritten field, and if it captures the
				// closure instance, all fields must be written by now.
				if (!FieldReadsAreWritten(stmt, variable, unwritten))
					return false;
				if (LoadsVariableInstance(stmt, variable))
					return unwritten.Count == 0;
			}
			return unwritten.Count == 0;
		}

		/// <summary>
		/// Returns false if <paramref name="inst"/> reads a field of <paramref name="variable"/> that is
		/// still in <paramref name="unwritten"/> (i.e. only holds the copied value).
		/// </summary>
		static bool FieldReadsAreWritten(ILInstruction inst, ILVariable variable, HashSet<IField> unwritten)
		{
			foreach (var node in inst.Descendants)
			{
				if (node.MatchLdFld(out var ldTarget, out var ldField)
					&& ldTarget.MatchLdLoc(variable)
					&& unwritten.Contains((IField)ldField.MemberDefinition))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns true if <paramref name="inst"/> loads the closure instance itself (capture/escape),
		/// as opposed to only using it as the target of a field access.
		/// </summary>
		static bool LoadsVariableInstance(ILInstruction inst, ILVariable variable)
		{
			foreach (var node in inst.Descendants.OfType<IInstructionWithVariableOperand>())
			{
				if (node.Variable != variable)
					continue;
				// a load used as the target of a field access is not a capture of the instance
				if (node is LdLoc ldloc && ldloc.Parent is LdFlda)
					continue;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes <c>variable = null</c> / <c>variable = default</c> stores that are trivially dead:
		/// only those in the same block as <paramref name="keepStore"/>, before it, and with no use
		/// of the variable in between, so the kept store overwrites them on every path. A default
		/// store anywhere else may be what keeps a conditionally constructed closure definitely
		/// assigned, so it survives. Returns true if <paramref name="keepStore"/> is the variable's
		/// only remaining store.
		/// </summary>
		static bool RemoveDeadDefaultStores(ILVariable variable, StLoc keepStore)
		{
			foreach (var store in variable.StoreInstructions.OfType<StLoc>().ToArray())
			{
				if (store == keepStore)
					continue;
				if ((store.Value is LdNull || store.Value is DefaultValue)
					&& store.Parent is Block block && block == keepStore.Parent
					&& store.ChildIndex < keepStore.ChildIndex
					&& !VariableIsUsedBetween(variable, block, store.ChildIndex + 1, keepStore.ChildIndex))
				{
					block.Instructions.RemoveAt(store.ChildIndex);
				}
			}
			return variable.StoreInstructions.All(store => store == keepStore);
		}

		static bool VariableIsUsedBetween(ILVariable variable, Block block, int startIndex, int endIndex)
		{
			for (int i = startIndex; i < endIndex; i++)
			{
				foreach (var node in block.Instructions[i].Descendants.OfType<IInstructionWithVariableOperand>())
				{
					if (node.Variable == variable)
						return true;
				}
			}
			return false;
		}

		void AnalyzeFunction(ILFunction function)
		{
			void VisitFunction(ILFunction f)
			{
				foreach (var v in f.Variables.ToArray())
				{
					var result = AnalyzeVariable(v);
					if (result == null || displayClasses.ContainsKey(result.Variable))
						continue;
					context.Step($"Detected display-class {result.Variable}", result.Initializer ?? f.Body);
					displayClasses.Add(result.Variable, result);
				}
			}

			void VisitChildren(ILInstruction inst)
			{
				foreach (var child in inst.Children)
				{
					Visit(child);
				}
			}

			void Visit(ILInstruction inst)
			{
				switch (inst)
				{
					case ILFunction f:
						VisitFunction(f);
						VisitChildren(inst);
						break;
					default:
						VisitChildren(inst);
						break;
				}
			}

			Visit(function);

			foreach (var (v, displayClass) in displayClasses.ToArray())
			{
				if (!ValidateDisplayClassUses(v, displayClass))
					displayClasses.Remove(v);
			}

			foreach (var displayClass in displayClasses.Values)
			{
				// handle uninitialized fields
				foreach (var f in displayClass.Type.Fields)
				{
					if (displayClass.VariablesToDeclare.ContainsKey(f))
						continue;
					var variable = AddVariable(displayClass, null, f);
					variable.UsesInitialValue = true;
					displayClass.VariablesToDeclare[(IField)f.MemberDefinition] = variable;
				}

				foreach (var (field, v) in displayClass.VariablesToDeclare)
				{
					if (v.CanPropagate)
					{
						var variableToPropagate = v.GetOrDeclare();
						// A field that aliases "this" but is assigned more than once is seeded
						// from "this" and later overwritten (e.g. "dc.f = this; dc.f = dc.f.Parent;").
						// Propagating it would turn the reassignments into stores to the read-only
						// "this" parameter. Declare a fresh local seeded from "this" instead.
						// Other parameters and locals are assignable, so a second store is harmless.
						if (variableToPropagate.IsThis() && CountDisplayClassFieldStores(function, displayClass, field) > 1)
						{
							v.Propagate(null);
							continue;
						}
						if (variableToPropagate.Kind != VariableKind.Parameter && !displayClasses.ContainsKey(variableToPropagate))
							v.Propagate(null);
					}
				}
			}
		}

		/// <summary>
		/// Counts how many StObj instructions in <paramref name="function"/> write
		/// <paramref name="field"/> of the given display class. The store target is resolved
		/// through the field-propagation chain, so writes reached via a nested display class'
		/// pointer to its parent (e.g. "ldflda field(ldfld parentPtr(ldloc innerDisplayClass))")
		/// are attributed to the display class that owns the field.
		/// </summary>
		int CountDisplayClassFieldStores(ILFunction function, DisplayClass displayClass, IField field)
		{
			var fieldDefinition = (IField)field.MemberDefinition;
			int count = 0;
			foreach (var stobj in function.Descendants.OfType<StObj>())
			{
				if (stobj.Target is LdFlda ldflda
					&& ldflda.Field.MemberDefinition == fieldDefinition
					&& ResolveStoreTargetDisplayClass(ldflda.Target) == displayClass)
				{
					count++;
				}
			}
			return count;
		}

		/// <summary>
		/// Resolves the instruction that loads a display-class instance (the target of an
		/// "ldflda field(...)") to the <see cref="DisplayClass"/> it refers to, following
		/// propagatable fields that point from a nested display class to an enclosing one.
		/// Returns null if the target does not resolve to a known display class.
		/// </summary>
		DisplayClass ResolveStoreTargetDisplayClass(ILInstruction target)
		{
			DisplayClass currentDisplayClass = null;
			foreach (var item in target.Descendants)
			{
				if (IsDisplayClassLoad(item, out var v))
				{
					if (!displayClasses.TryGetValue(v, out currentDisplayClass))
						return null;
				}
				if (currentDisplayClass == null)
					return null;
				if (item is LdFlda ldf && currentDisplayClass.VariablesToDeclare.TryGetValue((IField)ldf.Field.MemberDefinition, out var vd))
				{
					if (!vd.CanPropagate || !displayClasses.TryGetValue(vd.GetOrDeclare(), out var nested))
						return null;
					currentDisplayClass = nested;
				}
			}
			return currentDisplayClass;
		}

		bool ValidateDisplayClassUses(ILVariable v, DisplayClass displayClass)
		{
			foreach (var ldloc in v.LoadInstructions)
			{
				if (!ValidateUse(displayClass, ldloc))
					return false;
			}
			foreach (var ldloca in v.AddressInstructions)
			{
				if (!ValidateUse(displayClass, ldloca))
					return false;
			}
			return true;

			bool ValidateUse(DisplayClass container, ILInstruction use)
			{
				IField field;
				switch (use.Parent)
				{
					case LdFlda ldflda when ldflda.MatchLdFlda(out var target, out field) && target == use:
						var keyField = (IField)field.MemberDefinition;
						if (!container.VariablesToDeclare.TryGetValue(keyField, out VariableToDeclare variable) || variable == null)
						{
							variable = AddVariable(container, null, field);
						}
						container.VariablesToDeclare[keyField] = variable;
						return true;
					case StObj stobj when stobj.MatchStObj(out var target, out ILInstruction value, out _) && value == use:
						if (target.MatchLdFlda(out var load, out field) && load.MatchLdLocRef(out var otherVariable) && displayClasses.TryGetValue(otherVariable, out var otherDisplayClass))
						{
							if (otherDisplayClass.VariablesToDeclare.TryGetValue((IField)field.MemberDefinition, out var declaredVar))
								return declaredVar.CanPropagate;
						}
						return false;
					case StLoc stloc when stloc.Variable.IsSingleDefinition && stloc.Value == use:
						displayClassCopyMap[stloc.Variable] = v;
						return ValidateDisplayClassUses(stloc.Variable, displayClass);
					default:
						return false;
				}
			}
		}

		private DisplayClass AnalyzeVariable(ILVariable v)
		{
			switch (v.Kind)
			{
				case VariableKind.Parameter:
					if (context.Settings.YieldReturn && v.Function.StateMachineCompiledWithMono && v.IsThis())
						return HandleMonoStateMachine(v.Function, v);
					return null;
				case VariableKind.StackSlot:
				case VariableKind.Local:
				case VariableKind.DisplayClassLocal:
					return DetectDisplayClass(v);
				case VariableKind.InitializerTarget:
					return DetectDisplayClassInitializer(v);
				default:
					return null;
			}
		}

		DisplayClass DetectDisplayClass(ILVariable v)
		{
			ITypeDefinition definition;
			if (v.Kind != VariableKind.StackSlot)
			{
				definition = v.Type.GetDefinition();
			}
			else if (v.StoreInstructions.Count > 0 && v.StoreInstructions[0] is StLoc stloc)
			{
				definition = stloc.Value.InferType(context.TypeSystem).GetDefinition();
			}
			else
			{
				definition = null;
			}
			if (!ValidateDisplayClassDefinition(definition))
				return null;
			DisplayClass result;
			switch (definition.Kind)
			{
				case TypeKind.Class:
					if (!v.IsSingleDefinition)
						return null;
					if (!(v.StoreInstructions.SingleOrDefault() is StLoc stloc))
						return null;
					if (stloc.Value is NewObj newObj && ValidateConstructor(context, newObj.Method)
						&& CopiesNothingObservable(newObj, stloc.Variable))
					{
						result = new DisplayClass(v, definition) {
							CaptureScope = v.CaptureScope,
							Initializer = stloc
						};
					}
					else
					{
						return null;
					}

					HandleInitBlock(stloc.Parent as Block, stloc.ChildIndex + 1, result, result.Variable);
					break;
				case TypeKind.Struct:
					if (v.StoreInstructions.Count != 0)
						return null;
					Debug.Assert(v.StoreInstructions.Count == 0);
					result = new DisplayClass(v, definition) { CaptureScope = v.CaptureScope };
					HandleInitBlock(FindDisplayStructInitBlock(v), 0, result, result.Variable);
					break;
				default:
					return null;
			}

			if (IsMonoNestedCaptureScope(definition))
			{
				result.CaptureScope = null;
			}
			return result;
		}

		void HandleInitBlock(Block initBlock, int startIndex, DisplayClass result, ILVariable targetVariable)
		{
			if (initBlock == null)
				return;
			for (int i = startIndex; i < initBlock.Instructions.Count; i++)
			{
				var init = initBlock.Instructions[i];
				if (!init.MatchStFld(out var target, out var field, out _))
					break;
				if (!target.MatchLdLocRef(targetVariable))
					break;
				if (result.VariablesToDeclare.ContainsKey((IField)field.MemberDefinition))
					break;
				var variable = AddVariable(result, (StObj)init, field);
				result.VariablesToDeclare[(IField)field.MemberDefinition] = variable;
			}
		}

		private Block FindDisplayStructInitBlock(ILVariable v)
		{
			var root = v.Function.Body;
			return Visit(root)?.Ancestors.OfType<Block>().FirstOrDefault();

			// Try to find a common ancestor of all uses of the variable v.
			ILInstruction Visit(ILInstruction inst)
			{
				switch (inst)
				{
					case LdLoc l when l.Variable == v:
						return l;
					case StLoc s when s.Variable == v:
						return s;
					case LdLoca la when la.Variable == v:
						return la;
					default:
						return VisitChildren(inst);
				}
			}

			ILInstruction VisitChildren(ILInstruction inst)
			{
				// Visit all children of the instruction
				ILInstruction result = null;
				foreach (var child in inst.Children)
				{
					var newResult = Visit(child);
					// As soon as there is a second use of v in this sub-tree,
					// we can skip all other children and just return this node.
					if (result == null)
					{
						result = newResult;
					}
					else if (newResult != null)
					{
						return inst;
					}
				}
				// returns null, if v is not used in this sub-tree;
				// returns a descendant of inst, if it is the only use of v in this sub-tree.
				return result;
			}
		}

		DisplayClass DetectDisplayClassInitializer(ILVariable v)
		{
			if (v.StoreInstructions.Count != 1 || !(v.StoreInstructions[0] is StLoc store && store.Parent is Block initializerBlock && initializerBlock.Kind == BlockKind.ObjectInitializer))
				return null;
			if (!(store.Value is NewObj newObj))
				return null;
			var definition = newObj.Method.DeclaringType.GetDefinition();
			if (!ValidateDisplayClassDefinition(definition))
				return null;
			if (!ValidateConstructor(context, newObj.Method) || !CopiesNothingObservable(newObj, v))
				return null;
			if (!initializerBlock.Parent.MatchStLoc(out var referenceVariable))
				return null;
			if (!referenceVariable.IsSingleDefinition)
				return null;
			if (!(referenceVariable.StoreInstructions.SingleOrDefault() is StLoc))
				return null;
			var result = new DisplayClass(referenceVariable, definition) {
				CaptureScope = referenceVariable.CaptureScope,
				Initializer = initializerBlock.Parent
			};
			HandleInitBlock(initializerBlock, 1, result, v);
			return result;
		}

		private bool ValidateDisplayClassDefinition(ITypeDefinition definition)
		{
			if (definition == null)
				return false;
			if (definition.ParentModule.MetadataFile != context.PEFile)
				return false;
			// We do not want to accidentially transform state-machines and thus destroy them.
			var token = (TypeDefinitionHandle)definition.MetadataToken;
			var metadata = definition.ParentModule.MetadataFile.Metadata;
			if (YieldReturnDecompiler.IsCompilerGeneratorEnumerator(token, metadata))
				return false;
			if (AsyncAwaitDecompiler.IsCompilerGeneratedStateMachine(token, metadata))
				return false;
			if (!context.Settings.AggressiveScalarReplacementOfAggregates)
			{
				if (definition.DeclaringTypeDefinition == null)
					return false;
				if (!IsPotentialClosure(context, definition))
					return false;
			}
			return true;
		}

		internal static bool ValidateConstructor(ILTransformContext context, IMethod method)
		{
			try
			{
				// VB closures use a copy constructor 'Closure(Closure other)' that copies the
				// captured fields from another instance, instead of the parameterless constructor
				// emitted for C# display classes. Accept both shapes here; NormalizeVisualBasicClosures
				// rewrites the copy-constructor call's argument to null when the copy is dead, so by the
				// time SROA dissolves the closure the constructor behaves as an empty initializer.
				bool isCopyConstructor = method.Parameters.Count == 1
					&& method.Parameters[0].Type.GetDefinition() is { } parameterType
					&& parameterType.Equals(method.DeclaringTypeDefinition);
				if (method.Parameters.Count != 0 && !isCopyConstructor)
					return false;
				var handle = (MethodDefinitionHandle)method.MetadataToken;
				var module = (MetadataModule)method.ParentModule;
				var file = module.MetadataFile;
				if (handle.IsNil || file != context.PEFile)
					return false;
				var def = file.Metadata.GetMethodDefinition(handle);
				if (def.RelativeVirtualAddress == 0)
					return false;
				var body = file.GetMethodBody(def.RelativeVirtualAddress);
				// some compilers produce ctors with unused local variables
				// see https://github.com/icsharpcode/ILSpy/issues/2174
				//if (!body.LocalSignature.IsNil)
				//	return false;
				if (body.ExceptionRegions.Length != 0)
					return false;
				var reader = body.GetILReader();
				if (reader.Length < 7)
					return false;
				// IL_0000: ldarg.0
				// IL_0001: call instance void [mscorlib]System.Object::.ctor()
				// IL_0006: ret
				var opCode = DecodeOpCodeSkipNop(ref reader);
				switch (opCode)
				{
					case ILOpCode.Ldarg:
					case ILOpCode.Ldarg_s:
						if (reader.DecodeIndex(opCode) != 0)
							return false;
						break;
					case ILOpCode.Ldarg_0:
						// OK
						break;
					default:
						return false;
				}
				if (DecodeOpCodeSkipNop(ref reader) != ILOpCode.Call)
					return false;
				var baseCtorHandle = MetadataTokenHelpers.EntityHandleOrNil(reader.ReadInt32());
				if (baseCtorHandle.IsNil)
					return false;
				var objectCtor = module.ResolveMethod(baseCtorHandle, new TypeSystem.GenericContext());
				if (!objectCtor.DeclaringType.IsKnownType(KnownTypeCode.Object))
					return false;
				if (!objectCtor.IsConstructor || objectCtor.Parameters.Count != 0)
					return false;
				if (DecodeOpCodeSkipNop(ref reader) == ILOpCode.Ret)
					return true;
				// A VB copy constructor continues past the base call with field copies from the
				// argument (optionally guarded by a null check); the parameterless C# constructor
				// returns immediately. Accept the former only when this is actually a copy constructor.
				return isCopyConstructor;
			}
			catch (BadImageFormatException)
			{
				return false;
			}
		}

		/// <summary>
		/// Returns whether constructing the display class carries nothing across that dissolving it
		/// would lose. A C# display class is built by a parameterless constructor and so carries
		/// nothing to begin with. A Visual Basic one is built by a copy constructor, which is only
		/// safe when the copy cannot carry anything observable: either the argument is null, which is
		/// what <see cref="NormalizeVisualBasicClosures"/> leaves once it has shown every copied field to
		/// be reassigned before it can be read, or it is the variable being overwritten. A copy taken
		/// from anything else is live, and scalarizing the class would silently drop it.
		/// </summary>
		static bool CopiesNothingObservable(NewObj newObj, ILVariable target)
		{
			foreach (var argument in newObj.Arguments)
			{
				if (argument.MatchLdNull())
					continue;
				// The Visual Basic per-iteration form builds the instance out of the variable it is
				// about to overwrite, so the copy can only carry that variable's previous value, which
				// the closure handling already accounts for. Live-range splitting gives the two ends
				// separate ILVariables for the one slot, so they are compared the way that split is
				// undone rather than by identity.
				if (argument.MatchLdLoc(out var argumentVariable)
					&& ILVariableEqualityComparer.Instance.Equals(argumentVariable, target))
				{
					continue;
				}
				return false;
			}
			return true;
		}

		static ILOpCode DecodeOpCodeSkipNop(ref BlobReader reader)
		{
			ILOpCode code;
			do
			{
				code = reader.DecodeOpCode();
			} while (code == ILOpCode.Nop);
			return code;
		}

		VariableToDeclare AddVariable(DisplayClass result, StObj statement, IField field)
		{
			VariableToDeclare variable = new VariableToDeclare(result, field);
			if (statement != null)
			{
				variable.Propagate(ResolveVariableToPropagate(statement.Value, field.Type));
				variable.Initializers.Add(statement);
			}
			variable.UsesInitialValue =
				result.Type.IsReferenceType != false || result.Variable.UsesInitialValue;
			return variable;
		}

		/// <summary>
		/// Resolves references to variables that can be propagated.
		/// If a value does not match either LdLoc or a LdObj LdLdFlda* LdLoc chain, null is returned.
		/// The if any of the variables/fields in the chain cannot be propagated, null is returned.
		/// </summary>
		ILVariable ResolveVariableToPropagate(ILInstruction value, IType expectedType = null)
		{
			ILVariable v;
			switch (value)
			{
				case LdLoc load:
					v = load.Variable;
					if (v.Kind == VariableKind.Parameter)
					{
						if (v.LoadCount != 1 && !v.IsThis())
						{
							// If the variable is a parameter and it is used elsewhere, we cannot propagate it.
							// "dc.field = v; dc.field.mutate(); use(v);" cannot turn to "v.mutate(); use(v)"
							return null;
						}
					}
					else
					{
						// Non-parameter propagation will later be checked, and will only be allowed for display classes
						if (v.Type.IsReferenceType != true)
						{
							// don't allow propagation for display structs (as used with local functions)
							return null;
						}
					}
					if (!v.IsSingleDefinition)
					{
						// "dc.field = v; v = 42; use(dc.field)" cannot turn to "v = 42; use(v);"
						return null;
					}
					if (!(expectedType == null || v.Kind == VariableKind.StackSlot || NormalizeTypeVisitor.IgnoreNullability.EquivalentTypes(v.Type, expectedType)))
						return null;
					return v;
				case LdObj ldfld:
					DisplayClass currentDisplayClass = null;
					foreach (var item in ldfld.Target.Descendants)
					{
						if (IsDisplayClassLoad(item, out v))
						{
							if (!displayClasses.TryGetValue(v, out currentDisplayClass))
								return null;
						}
						if (currentDisplayClass == null)
							return null;
						if (item is LdFlda ldf && currentDisplayClass.VariablesToDeclare.TryGetValue((IField)ldf.Field.MemberDefinition, out var vd))
						{
							if (!vd.CanPropagate)
								return null;
							if (!displayClasses.TryGetValue(vd.GetOrDeclare(), out currentDisplayClass))
								return null;
						}
					}
					return currentDisplayClass.Variable;
				default:
					return null;
			}
		}

		private void Transform(ILFunction function)
		{
			VisitILFunction(function);
			context.Step($"ResetHasInitialValueFlag", function);
			foreach (var f in function.Descendants.OfType<ILFunction>())
			{
				RemoveDeadVariableInit.ResetUsesInitialValueFlag(f, context);
				f.CapturedVariables.RemoveWhere(v => v.IsDead);
			}
		}

		internal static bool IsClosure(ILTransformContext context, ILVariable variable, out ITypeDefinition closureType, out ILInstruction initializer)
		{
			closureType = null;
			initializer = null;
			if (variable.IsSingleDefinition && variable.StoreInstructions.SingleOrDefault() is StLoc inst)
			{
				initializer = inst;
				if (IsClosureInit(context, inst, out closureType))
				{
					return true;
				}
			}
			closureType = variable.Type.GetDefinition();
			if (context.Settings.LocalFunctions && closureType?.Kind == TypeKind.Struct
				&& variable.UsesInitialValue && IsPotentialClosure(context, closureType))
			{
				initializer = Block.GetContainingStatement(variable.AddressInstructions.OrderBy(i => i.StartILOffset).First());
				return true;
			}
			return false;
		}

		static bool IsClosureInit(ILTransformContext context, StLoc inst, out ITypeDefinition closureType)
		{
			if (inst.Value is NewObj newObj)
			{
				closureType = newObj.Method.DeclaringTypeDefinition;
				return closureType != null && IsPotentialClosure(context, newObj);
			}
			closureType = null;
			return false;
		}

		bool IsMonoNestedCaptureScope(ITypeDefinition closureType)
		{
			if (!closureType.Name.Contains("AnonStorey"))
				return false;
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Method);
			return closureType.Fields.Any(f => IsPotentialClosure(decompilationContext.CurrentTypeDefinition, f.ReturnType.GetDefinition()));
		}

		/// <summary>
		/// mcs likes to optimize closures in yield state machines away by moving the captured variables' fields into the state machine type,
		/// We construct a <see cref="DisplayClass"/> that spans the whole method body.
		/// </summary>
		DisplayClass HandleMonoStateMachine(ILFunction function, ILVariable thisVariable)
		{
			if (!(function.StateMachineCompiledWithMono && thisVariable.IsThis()))
				return null;
			// Special case for Mono-compiled yield state machines
			ITypeDefinition closureType = thisVariable.Type.GetDefinition();
			if (!(closureType != decompilationContext.CurrentTypeDefinition
				&& IsPotentialClosure(decompilationContext.CurrentTypeDefinition, closureType, allowTypeImplementingInterfaces: true)))
				return null;

			var displayClass = new DisplayClass(thisVariable, thisVariable.Type.GetDefinition());
			displayClass.CaptureScope = (BlockContainer)function.Body;
			foreach (var stateMachineVariable in function.Variables)
			{
				if (stateMachineVariable.StateMachineField == null || displayClass.VariablesToDeclare.ContainsKey(stateMachineVariable.StateMachineField))
					continue;
				VariableToDeclare variableToDeclare = new VariableToDeclare(displayClass, stateMachineVariable.StateMachineField, stateMachineVariable);
				displayClass.VariablesToDeclare.Add(stateMachineVariable.StateMachineField, variableToDeclare);
			}
			if (!function.Method.IsStatic && FindThisField(out var thisField))
			{
				var thisVar = function.Variables
					.FirstOrDefault(t => t.IsThis() && t.Type.GetDefinition() == decompilationContext.CurrentTypeDefinition);
				if (thisVar == null)
				{
					thisVar = new ILVariable(VariableKind.Parameter, decompilationContext.CurrentTypeDefinition, -1) {
						Name = "this", StateMachineField = thisField
					};
					function.Variables.Add(thisVar);
				}
				else if (thisVar.StateMachineField != null && displayClass.VariablesToDeclare.ContainsKey(thisVar.StateMachineField))
				{
					// "this" was already added previously, no need to add it twice.
					return displayClass;
				}
				VariableToDeclare variableToDeclare = new VariableToDeclare(displayClass, thisField, thisVar);
				displayClass.VariablesToDeclare.Add(thisField, variableToDeclare);
			}
			return displayClass;

			bool FindThisField(out IField foundField)
			{
				foundField = null;
				foreach (var field in closureType.GetFields(f2 => !f2.IsStatic && !displayClass.VariablesToDeclare.ContainsKey(f2) && f2.Type.GetDefinition() == decompilationContext.CurrentTypeDefinition))
				{
					thisField = field;
					return true;
				}
				return false;
			}
		}

		internal static bool IsPotentialClosure(ILTransformContext context, NewObj inst)
		{
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Ancestors.OfType<ILFunction>().Last().Method);
			return IsPotentialClosure(decompilationContext.CurrentTypeDefinition, inst.Method.DeclaringTypeDefinition);
		}

		internal static bool IsPotentialClosure(ILTransformContext context, ITypeDefinition potentialDisplayClass)
		{
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Ancestors.OfType<ILFunction>().Last().Method);
			return IsPotentialClosure(decompilationContext.CurrentTypeDefinition, potentialDisplayClass);
		}

		internal static bool IsPotentialClosure(ITypeDefinition decompiledTypeDefinition, ITypeDefinition potentialDisplayClass, bool allowTypeImplementingInterfaces = false)
		{
			if (potentialDisplayClass == null || !potentialDisplayClass.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return false;
			switch (potentialDisplayClass.Kind)
			{
				case TypeKind.Struct:
					break;
				case TypeKind.Class:
					if (!allowTypeImplementingInterfaces)
					{
						if (!potentialDisplayClass.DirectBaseTypes.All(t => t.IsKnownType(KnownTypeCode.Object)))
							return false;
					}
					break;
				default:
					return false;
			}

			// Make sure that potentialDisplayCLass and decompiledTypeDefinition are part of the same type tree
			// Either decompiledTypeDefinition is an ancestor type of potentialDisplayClass or both have
			// at least one common ancestor.
			var potentialDisplayClassAncestors = new HashSet<ITypeDefinition>();
			var potentialDisplayClassParent = potentialDisplayClass.DeclaringTypeDefinition;
			while (potentialDisplayClassParent != null)
			{
				potentialDisplayClassAncestors.Add(potentialDisplayClassParent);
				potentialDisplayClassParent = potentialDisplayClassParent.DeclaringTypeDefinition;
			}

			var decompiledTypeDefinitionOrAncestor = decompiledTypeDefinition;

			while (decompiledTypeDefinitionOrAncestor != null)
			{
				if (potentialDisplayClassAncestors.Contains(decompiledTypeDefinitionOrAncestor))
					return true;
				decompiledTypeDefinitionOrAncestor = decompiledTypeDefinitionOrAncestor.DeclaringTypeDefinition;
			}
			return false;
		}

		readonly Stack<ILFunction> currentFunctions = new Stack<ILFunction>();

		protected internal override void VisitILFunction(ILFunction function)
		{
			context.StepStartGroup("Visit " + function.Name);
			try
			{
				this.currentFunctions.Push(function);
				base.VisitILFunction(function);
			}
			finally
			{
				this.currentFunctions.Pop();
				context.StepEndGroup(keepIfEmpty: true);
			}
		}

		protected override void Default(ILInstruction inst)
		{
			ILInstruction next;
			for (var child = inst.Children.FirstOrDefault(); child != null; child = next)
			{
				next = child.GetNextSibling();
				child.AcceptVisitor(this);
			}
		}

		protected internal override void VisitStLoc(StLoc inst)
		{
			DisplayClass displayClass;
			if (inst.Parent is Block parentBlock && inst.Variable.IsSingleDefinition)
			{
				if ((inst.Variable.Kind == VariableKind.Local || inst.Variable.Kind == VariableKind.StackSlot) && inst.Variable.LoadCount == 0)
				{
					// traverse pre-order, so that we do not have to deal with more special cases afterwards
					base.VisitStLoc(inst);
					if (inst.Value is StLoc || inst.Value is CompoundAssignmentInstruction)
					{
						context.Step($"Remove unused variable assignment {inst.Variable.Name}", inst);
						var replacement = inst.Value;
						inst.ReplaceWith(replacement);
						context.EndStep(replacement);
					}
					return;
				}
				if (displayClasses.TryGetValue(inst.Variable, out displayClass) && displayClass.Initializer == inst)
				{
					// inline contents of object initializer block
					if (inst.Value is Block initBlock && initBlock.Kind == BlockKind.ObjectInitializer)
					{
						context.Step($"Remove initializer of {inst.Variable.Name}", inst);
						ILInstruction firstInlinedStore = null;
						for (int i = 1; i < initBlock.Instructions.Count; i++)
						{
							var stobj = (StObj)initBlock.Instructions[i];
							var variable = displayClass.VariablesToDeclare[(IField)((LdFlda)stobj.Target).Field.MemberDefinition];
							var inlinedStore = new StLoc(variable.GetOrDeclare(), stobj.Value).WithILRange(stobj);
							parentBlock.Instructions.Insert(inst.ChildIndex + i, inlinedStore);
							firstInlinedStore ??= inlinedStore;
						}
						context.EndStep(firstInlinedStore);
					}
					context.Step($"Remove initializer of {inst.Variable.Name}", inst);
					parentBlock.Instructions.Remove(inst);
					return;
				}
				if (inst.Value is LdLoc || inst.Value is LdObj)
				{
					// in some cases (e.g. if inlining fails), there can be a reference to a display class in a stack slot,
					// in that case it is necessary to resolve the reference and iff it can be propagated, replace all loads
					// of the single-definition.
					if (!displayClassCopyMap.TryGetValue(inst.Variable, out var referencedDisplayClass))
					{
						referencedDisplayClass = ResolveVariableToPropagate(inst.Value);
					}
					if (referencedDisplayClass != null && displayClasses.TryGetValue(referencedDisplayClass, out _))
					{
						context.Step($"Propagate reference to {referencedDisplayClass.Name} in {inst.Variable}", inst);
						ILInstruction firstReplacement = null;
						foreach (var ld in inst.Variable.LoadInstructions.ToArray())
						{
							var newLoad = new LdLoc(referencedDisplayClass).WithILRange(ld);
							ld.ReplaceWith(newLoad);
							firstReplacement ??= newLoad;
						}
						context.EndStep(firstReplacement);
						parentBlock.Instructions.Remove(inst);
						return;
					}
				}
			}
			base.VisitStLoc(inst);
		}

		protected internal override void VisitStObj(StObj inst)
		{
			if (IsDisplayClassFieldAccess(inst.Target, out var v, out var displayClass, out var field))
			{
				VariableToDeclare vd = displayClass.VariablesToDeclare[(IField)field.MemberDefinition];
				if (vd.CanPropagate && vd.Initializers.Contains(inst))
				{
					if (inst.Parent is Block containingBlock)
					{
						context.Step($"Remove initializer of {v.Name}.{vd.Name} due to propagation", inst);
						containingBlock.Instructions.Remove(inst);
						return;
					}
				}
				if (inst.Value is LdLoc ldLoc && ldLoc.Variable is { IsSingleDefinition: true, CaptureScope: null }
					&& ldLoc.Variable.StoreInstructions.FirstOrDefault() is StLoc stloc
					&& stloc.Parent is Block block)
				{
					ILInlining.InlineOneIfPossible(block, stloc.ChildIndex, InliningOptions.None, context);
				}
			}
			base.VisitStObj(inst);
			EarlyExpressionTransforms.StObjToStLoc(inst, context);
		}

		protected internal override void VisitLdObj(LdObj inst)
		{
			base.VisitLdObj(inst);
			EarlyExpressionTransforms.LdObjToLdLoc(inst, context);
		}

		private bool IsDisplayClassLoad(ILInstruction target, out ILVariable variable)
		{
			// We cannot use MatchLdLocRef here because local functions use ref parameters
			if (!target.MatchLdLoc(out variable) && !target.MatchLdLoca(out variable))
				return false;
			if (displayClassCopyMap.TryGetValue(variable, out ILVariable other))
				variable = other;
			return true;
		}

		private bool IsDisplayClassFieldAccess(ILInstruction inst,
			out ILVariable displayClassVar, out DisplayClass displayClass, out IField field)
		{
			displayClass = null;
			displayClassVar = null;
			field = null;
			if (inst is not LdFlda ldflda)
				return false;
			field = ldflda.Field;
			return IsDisplayClassLoad(ldflda.Target, out displayClassVar)
				&& displayClasses.TryGetValue(displayClassVar, out displayClass);
		}

		protected internal override void VisitLdFlda(LdFlda inst)
		{
			base.VisitLdFlda(inst);
			// Get display class info
			if (!IsDisplayClassFieldAccess(inst, out _, out DisplayClass displayClass, out IField field))
				return;
			var keyField = (IField)field.MemberDefinition;
			var v = displayClass.VariablesToDeclare[keyField];
			context.Step($"Replace {field.Name} with captured variable {v.Name}", inst);
			ILVariable variable = v.GetOrDeclare();
			var replacement = new LdLoca(variable).WithILRange(inst);
			inst.ReplaceWith(replacement);
			context.EndStep(replacement);
			// add captured variable to all descendant functions from the declaring function to this use-site function
			foreach (var f in currentFunctions)
			{
				if (f == variable.Function)
					break;
				f.CapturedVariables.Add(variable);
			}
		}
	}
}

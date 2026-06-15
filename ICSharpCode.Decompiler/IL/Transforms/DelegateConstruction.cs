// Copyright (c) 2011-2016 Siegfried Pammer
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
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Transforms anonymous methods and lambdas by creating nested ILFunctions.
	/// </summary>
	public class DelegateConstruction : IILTransform
	{
		ILTransformContext context;
		ITypeResolveContext decompilationContext;
		readonly Stack<MethodDefinitionHandle> activeMethods = new Stack<MethodDefinitionHandle>();

		void IILTransform.Run(ILFunction function, ILTransformContext context)
		{
			if (!context.Settings.AnonymousMethods)
				return;
			var prevContext = this.context;
			var prevDecompilationContext = this.decompilationContext;
			try
			{
				activeMethods.Push((MethodDefinitionHandle)function.Method.MetadataToken);
				this.context = context;
				this.decompilationContext = new SimpleTypeResolveContext(function.Method);
				var cancellationToken = context.CancellationToken;
				foreach (var inst in function.Descendants)
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (!MatchDelegateConstruction(inst, out var targetMethod, out var target,
						out var delegateType, allowTransformed: false))
						continue;
					context.StepStartGroup($"TransformDelegateConstruction {inst.StartILOffset}", inst);
					ILFunction f = TransformDelegateConstruction(inst, targetMethod, target, delegateType);
					if (f != null && target is IInstructionWithVariableOperand instWithVar)
					{
						var v = instWithVar.Variable;
						if (v.Kind == VariableKind.Local)
						{
							v.Kind = VariableKind.DisplayClassLocal;
						}
						if (v.IsSingleDefinition
							&& v.StoreInstructions.SingleOrDefault() is StLoc store
							&& store.Value is NewObj)
						{
							v.CaptureScope = BlockContainer.FindClosestContainer(store);
						}
					}
					context.StepEndGroup();
				}
			}
			finally
			{
				this.context = prevContext;
				this.decompilationContext = prevDecompilationContext;
				activeMethods.Pop();
			}
		}

		internal static bool MatchDelegateConstruction(ILInstruction inst, out IMethod targetMethod,
			out ILInstruction target, out IType delegateType, bool allowTransformed = false)
		{
			targetMethod = null;
			target = null;
			delegateType = null;
			switch (inst)
			{
				case NewObj call:
					if (call.Arguments.Count != 2)
						return false;
					target = call.Arguments[0];
					var opCode = call.Arguments[1].OpCode;
					delegateType = call.Method.DeclaringType;
					if (!(opCode == OpCode.LdFtn || opCode == OpCode.LdVirtFtn
						|| (allowTransformed && opCode == OpCode.ILFunction)))
						return false;
					targetMethod = ((IInstructionWithMethodOperand)call.Arguments[1]).Method;
					break;
				case LdVirtDelegate ldVirtDelegate:
					target = ldVirtDelegate.Argument;
					targetMethod = ldVirtDelegate.Method;
					delegateType = ldVirtDelegate.Type;
					break;
				default:
					return false;
			}
			return delegateType.Kind == TypeKind.Delegate || delegateType.Kind == TypeKind.Unknown;
		}

		static bool IsAnonymousMethod(ITypeDefinition decompiledTypeDefinition, IMethod method)
		{
			if (method == null)
				return false;
			if (!(method.HasGeneratedName()
				|| method.Name.Contains("$")
				|| method.IsCompilerGenerated()
				|| TransformDisplayClassUsage.IsPotentialClosure(
					decompiledTypeDefinition, method.DeclaringTypeDefinition)
				|| ContainsAnonymousType(method)))
			{
				return false;
			}
			return true;
		}

		static bool ContainsAnonymousType(IMethod method)
		{
			if (method.ReturnType.ContainsAnonymousType())
				return true;
			foreach (var p in method.Parameters)
			{
				if (p.Type.ContainsAnonymousType())
					return true;
			}
			return false;
		}

		static GenericContext? GenericContextFromTypeArguments(TypeParameterSubstitution subst)
		{
			var classTypeParameters = new List<ITypeParameter>();
			var methodTypeParameters = new List<ITypeParameter>();
			if (subst.ClassTypeArguments != null)
			{
				foreach (var t in subst.ClassTypeArguments)
				{
					if (t is ITypeParameter tp)
						classTypeParameters.Add(tp);
					else
						return null;
				}
			}
			if (subst.MethodTypeArguments != null)
			{
				foreach (var t in subst.MethodTypeArguments)
				{
					if (t is ITypeParameter tp)
						methodTypeParameters.Add(tp);
					else
						return null;
				}
			}
			return new GenericContext(classTypeParameters, methodTypeParameters);
		}

		ILFunction TransformDelegateConstruction(
			ILInstruction value, IMethod targetMethod,
			ILInstruction target, IType delegateType)
		{
			if (!IsAnonymousMethod(decompilationContext.CurrentTypeDefinition, targetMethod))
				return null;
			if (targetMethod.MetadataToken.IsNil)
				return null;
			if (LocalFunctionDecompiler.IsLocalFunctionMethod(targetMethod, context))
				return null;
			if (!ValidateDelegateTarget(target))
				return null;
			var handle = (MethodDefinitionHandle)targetMethod.MetadataToken;
			if (activeMethods.Contains(handle))
			{
				this.context.Function.Warnings.Add(" Found self-referencing delegate construction. Abort transformation to avoid stack overflow.");
				return null;
			}
			var methodDefinition = context.PEFile.Metadata.GetMethodDefinition((MethodDefinitionHandle)targetMethod.MetadataToken);
			if (!methodDefinition.HasBody())
				return null;
			var genericContext = GenericContextFromTypeArguments(targetMethod.Substitution);
			if (genericContext == null)
				return null;
			var ilReader = context.CreateILReader();
			var body = context.PEFile.GetMethodBody(methodDefinition.RelativeVirtualAddress);
			var function = ilReader.ReadIL((MethodDefinitionHandle)targetMethod.MetadataToken, body, genericContext.Value, ILFunctionKind.Delegate, context.CancellationToken);
			function.DelegateType = delegateType;
			// Embed the lambda into the parent function's ILAst, so that "Show steps" can show
			// how the lambda body is being transformed.
			value.ReplaceWith(function);
			function.CheckInvariant(ILPhase.Normal);

			var contextPrefix = targetMethod.Name;
			foreach (ILVariable v in function.Variables.Where(v => v.Kind != VariableKind.Parameter))
			{
				v.Name = contextPrefix + v.Name;
			}

			var nestedContext = new ILTransformContext(context, function);
			function.RunTransforms(CSharpDecompiler.GetILTransforms().TakeWhile(t => !(t is DelegateConstruction)).Concat(GetTransforms()), nestedContext);
			InlineVBLambdaRelay(function, targetMethod, nestedContext);
			nestedContext.Step("DelegateConstruction (ReplaceDelegateTargetVisitor)", function);
			function.AcceptVisitor(new ReplaceDelegateTargetVisitor(target, function.Variables.SingleOrDefault(VariableKindExtensions.IsThis)));
			// handle nested lambdas
			nestedContext.StepStartGroup("DelegateConstruction (nested lambdas)", function);
			((IILTransform)this).Run(function, nestedContext);
			nestedContext.StepEndGroup();
			function.AddILRange(target);
			function.AddILRange(value);
			if (value is Call call)
				function.AddILRange(call.Arguments[1]);
			return function;
		}

		/// <summary>
		/// The VB compiler emits a "delegate relaxation" stub for a lambda whose own signature does
		/// not match the target delegate (for example a value-returning lambda assigned to an Action,
		/// or a lambda with fewer parameters than the delegate). The stub is a sibling method on the
		/// same closure/display class: it ignores its own parameters, calls the real lambda body on
		/// the same instance, and discards the result. Both methods are named '_Lambda$__...'.
		///
		/// Decompiling the stub verbatim leaves the closure variable as the target of the inner
		/// '_Lambda$__N()' invocation, which blocks scalar replacement of the display class in
		/// <see cref="TransformDisplayClassUsage"/>. Inlining the real lambda body into the stub
		/// removes that invocation so the display class can be scalarized away.
		/// </summary>
		private void InlineVBLambdaRelay(ILFunction function, IMethod relayMethod, ILTransformContext context)
		{
			if (relayMethod.DeclaringTypeDefinition == null)
				return;
			// Locate the single call to a sibling lambda body whose only argument loads the stub's 'this'.
			ILVariable thisVariable = function.Variables.SingleOrDefault(VariableKindExtensions.IsThis);
			if (thisVariable == null)
				return;
			Call relayCall = null;
			foreach (var call in function.Descendants.OfType<Call>())
			{
				if (!IsVBLambdaRelayTargetCall(call, relayMethod, thisVariable))
					continue;
				if (relayCall != null)
					return; // not a strict single-forward relay
				relayCall = call;
			}
			if (relayCall == null)
				return;
			IMethod lambdaMethod = relayCall.Method;
			if (lambdaMethod.MetadataToken.IsNil)
				return;
			var handle = (MethodDefinitionHandle)lambdaMethod.MetadataToken;
			if (activeMethods.Contains(handle))
				return;
			var methodDefinition = context.PEFile.Metadata.GetMethodDefinition(handle);
			if (!methodDefinition.HasBody())
				return;
			var genericContext = GenericContextFromTypeArguments(lambdaMethod.Substitution);
			if (genericContext == null)
				return;
			activeMethods.Push(handle);
			try
			{
				var ilReader = context.CreateILReader();
				var body = context.PEFile.GetMethodBody(methodDefinition.RelativeVirtualAddress);
				var lambdaFunction = ilReader.ReadIL(handle, body, genericContext.Value,
					ILFunctionKind.Delegate, context.CancellationToken);
				var lambdaContext = new ILTransformContext(context, lambdaFunction);
				lambdaFunction.RunTransforms(CSharpDecompiler.GetILTransforms()
					.TakeWhile(t => !(t is DelegateConstruction)).Concat(GetTransforms()), lambdaContext);
				// The real lambda body must reduce to a single 'return <expr>;' so that the call can be
				// replaced by <expr> in-place, preserving the stub's surrounding value/void context.
				if (!(lambdaFunction.Body is BlockContainer container && container.Blocks.Count == 1))
					return;
				var lambdaThis = lambdaFunction.Variables.SingleOrDefault(VariableKindExtensions.IsThis);
				var block = container.Blocks[0];
				if (block.Instructions.Count != 1 || !block.Instructions[0].MatchLeave(container, out var returnValue))
					return;
				if (returnValue.MatchNop())
					return;
				// The stub forwards no arguments, so the inlined expression may reference only the closure's
				// 'this'; any other variable would be orphaned once the rest of the lambda body is dropped.
				foreach (var inlinedInst in returnValue.Descendants.Prepend(returnValue))
				{
					if (inlinedInst is IInstructionWithVariableOperand varInst && varInst.Variable != lambdaThis)
						return;
				}
				context.Step("Inline VB lambda relay body of " + lambdaMethod.Name, relayCall);
				// Clone so the moved expression is not still owned by the discarded lambda function, then
				// rebind references to the lambda's 'this' onto the stub's closure instance.
				ILInstruction inlinedExpression = returnValue.Clone();
				if (lambdaThis != null)
				{
					if (inlinedExpression is LdLoc rootLoad && rootLoad.Variable == lambdaThis)
					{
						inlinedExpression = new LdLoc(thisVariable).WithILRange(rootLoad);
					}
					else
					{
						foreach (var load in inlinedExpression.Descendants.ToArray())
						{
							if (load is LdLoc ldloc && ldloc.Variable == lambdaThis)
								ldloc.ReplaceWith(new LdLoc(thisVariable).WithILRange(ldloc));
						}
					}
				}
				inlinedExpression.AddILRange(relayCall);
				relayCall.ReplaceWith(inlinedExpression);
			}
			finally
			{
				activeMethods.Pop();
			}
		}

		/// <summary>
		/// Matches a call inside a VB delegate-relaxation stub to the sibling lambda body it forwards to:
		/// an instance call on the same compiler-generated closure type whose single argument is the
		/// stub's own 'this'.
		/// </summary>
		private static bool IsVBLambdaRelayTargetCall(Call call, IMethod relayMethod, ILVariable thisVariable)
		{
			IMethod lambdaMethod = call.Method;
			if (lambdaMethod.IsStatic || lambdaMethod.IsConstructor)
				return false;
			if (!lambdaMethod.Name.StartsWith("_Lambda$__", StringComparison.Ordinal))
				return false;
			if (!relayMethod.Name.StartsWith("_Lambda$__", StringComparison.Ordinal))
				return false;
			if (lambdaMethod.MetadataToken.IsNil || lambdaMethod.MetadataToken.Kind != HandleKind.MethodDefinition)
				return false;
			if (lambdaMethod.MetadataToken == relayMethod.MetadataToken)
				return false;
			if (!Equals(lambdaMethod.DeclaringTypeDefinition, relayMethod.DeclaringTypeDefinition))
				return false;
			if (!lambdaMethod.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return false;
			// The stub forwards no arguments: the only call argument is the load of the stub's 'this'.
			if (call.Arguments.Count != 1)
				return false;
			return call.Arguments[0].MatchLdLoc(thisVariable);
		}

		private static bool ValidateDelegateTarget(ILInstruction inst)
		{
			switch (inst)
			{
				case LdNull _:
					return true;
				case LdLoc ldloc:
					return ldloc.Variable.IsSingleDefinition;
				case LdObj ldobj:
					// TODO : should make sure that the display-class 'this' is unused,
					// if the delegate target is ldobj(ldsflda field).
					if (ldobj.Target is LdsFlda)
						return true;
					// TODO : ldfld chains must be validated more thoroughly, i.e., we should make sure
					// that the value of the field is never changed.
					ILInstruction target = ldobj;
					while (target is LdObj || target is LdFlda)
					{
						if (target is LdObj o)
						{
							target = o.Target;
							continue;
						}
						if (target is LdFlda f)
						{
							target = f.Target;
							continue;
						}
					}
					return target is LdLoc;
				default:
					return false;
			}
		}

		private IEnumerable<IILTransform> GetTransforms()
		{
			yield return new CombineExitsTransform();
		}

		/// <summary>
		/// Replaces loads of 'this' with the target expression.
		/// Async delegates use: ldobj(ldloca this).
		/// </summary>
		internal class ReplaceDelegateTargetVisitor : ILVisitor
		{
			readonly ILVariable thisVariable;
			readonly ILInstruction target;

			public ReplaceDelegateTargetVisitor(ILInstruction target, ILVariable thisVariable)
			{
				this.target = target;
				this.thisVariable = thisVariable;
			}

			protected override void Default(ILInstruction inst)
			{
				foreach (var child in inst.Children)
				{
					child.AcceptVisitor(this);
				}
			}

			protected internal override void VisitILFunction(ILFunction function)
			{
				if (function == thisVariable?.Function)
				{
					ILVariable v = null;
					switch (target)
					{
						case LdLoc l:
							v = l.Variable;
							break;
						case LdObj lo:
							ILInstruction inner = lo.Target;
							while (inner is LdFlda ldf)
							{
								inner = ldf.Target;
							}
							if (inner is LdLoc l2)
								v = l2.Variable;
							break;
					}
					if (v != null)
						function.CapturedVariables.Add(v);
				}
				base.VisitILFunction(function);
			}

			protected internal override void VisitLdLoc(LdLoc inst)
			{
				if (inst.Variable == thisVariable)
				{
					inst.ReplaceWith(target.Clone());
					return;
				}
				base.VisitLdLoc(inst);
			}

			protected internal override void VisitLdObj(LdObj inst)
			{
				if (inst.Target.MatchLdLoca(thisVariable))
				{
					inst.ReplaceWith(target.Clone());
					return;
				}
				base.VisitLdObj(inst);
			}
		}
	}
}

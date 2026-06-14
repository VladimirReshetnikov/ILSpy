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
using System.Diagnostics;
using System.Threading;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// DataFlowVisitor that determines, for each 'out' parameter of a function, whether the
	/// value behind the parameter reference is definitely written on every code path that
	/// returns from the function.
	///
	/// Unlike a C# 'out' parameter (whose definite assignment the C# compiler already proved),
	/// a parameter that the metadata renders as 'out' may originate from a language that does not
	/// enforce definite assignment (e.g. Visual Basic). Such a body can return without writing the
	/// value, which would not compile as C#. The result of this analysis lets a transform inject a
	/// leading default-value assignment only for the parameters that need it.
	/// </summary>
	class OutParameterAssignmentVisitor : DataFlowVisitor<OutParameterAssignmentVisitor.State>
	{
		/// <summary>
		/// State for the out-parameter assignment analysis.
		/// </summary>
		[DebuggerDisplay("{bits}")]
		public struct State : IDataFlowState<State>
		{
			/// <summary>
			/// bits[i]: There is a code path from the entry point to this state's position
			///          that does not write through outParameters[i].
			///          (i.e. the value behind the out parameter is not definitely written
			///          at the state's position)
			///
			/// Initial state: all bits set = nothing is definitely written
			/// Bottom state: all bits clear
			/// </summary>
			readonly BitSet bits;

			public State(int outParameterCount)
			{
				this.bits = new BitSet(outParameterCount);
				this.bits.Set(0, outParameterCount);
			}

			private State(BitSet bits)
			{
				this.bits = bits;
			}

			public bool LessThanOrEqual(State otherState)
			{
				return bits.IsSubsetOf(otherState.bits);
			}

			public State Clone()
			{
				return new State(bits.Clone());
			}

			public void ReplaceWith(State newContent)
			{
				bits.ReplaceWith(newContent.bits);
			}

			public void JoinWith(State incomingState)
			{
				bits.UnionWith(incomingState.bits);
			}

			public void TriggerFinally(State finallyState)
			{
				// A value is definitely written after try-finally if it is definitely written in
				// either the try or the finally block. As the bits are the opposite of "written",
				// the surviving "potentially unwritten" bits are those set in both inputs.
				bits.IntersectWith(finallyState.bits);
			}

			public void ReplaceWithBottom()
			{
				bits.ClearAll();
			}

			public bool IsBottom {
				get { return !bits.Any(); }
			}

			public void MarkWritten(int outParameterIndex)
			{
				bits.Clear(outParameterIndex);
			}

			public bool IsPotentiallyUnwritten(int outParameterIndex)
			{
				return bits[outParameterIndex];
			}
		}

		readonly CancellationToken cancellationToken;
		readonly ILFunction scope;

		/// <summary>
		/// Maps each tracked 'out' parameter variable to its index in the analysis state.
		/// </summary>
		readonly Dictionary<ILVariable, int> outParameterIndices = new Dictionary<ILVariable, int>();
		readonly BitSet potentiallyUnwrittenAtReturn;

		public OutParameterAssignmentVisitor(ILFunction scope, IReadOnlyList<ILVariable> outParameters, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.cancellationToken = cancellationToken;
			this.scope = scope;
			for (int i = 0; i < outParameters.Count; i++)
			{
				outParameterIndices.Add(outParameters[i], i);
			}
			this.potentiallyUnwrittenAtReturn = new BitSet(outParameters.Count);
			Initialize(new State(outParameters.Count));
		}

		/// <summary>
		/// Gets whether there is a path returning from the function on which the value behind
		/// the given 'out' parameter is not definitely written.
		/// </summary>
		public bool IsPotentiallyUnwrittenOnReturn(ILVariable outParameter)
		{
			return potentiallyUnwrittenAtReturn[outParameterIndices[outParameter]];
		}

		void HandleWrite(ILInstruction target)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (target is LdLoc ldloc && ldloc.Variable.Function == scope
				&& outParameterIndices.TryGetValue(ldloc.Variable, out int index))
			{
				// Mark the out value as written. As with the loads tracked by the base visitor,
				// this also runs for stores in unreachable code, where the bottom state ignores it.
				state.MarkWritten(index);
			}
		}

		protected override void HandleMatchStore(MatchInstruction inst)
		{
			// A 'match' stores its captured value into a local pattern variable, never through an
			// 'out' parameter reference, so it does not affect this analysis.
		}

		protected internal override void VisitStObj(StObj inst)
		{
			inst.Target.AcceptVisitor(this);
			inst.Value.AcceptVisitor(this);
			HandleWrite(inst.Target);
		}

		protected internal override void VisitCall(Call inst)
		{
			HandleCall(inst);
		}

		protected internal override void VisitCallVirt(CallVirt inst)
		{
			HandleCall(inst);
		}

		protected internal override void VisitNewObj(NewObj inst)
		{
			HandleCall(inst);
		}

		void HandleCall(CallInstruction call)
		{
			DebugStartPoint(call);
			foreach (var arg in call.Arguments)
			{
				arg.AcceptVisitor(this);
			}
			// Forwarding an 'out' parameter reference to another 'out' parameter writes the value,
			// just like the C# compiler treats the call as a definite assignment of the argument.
			// Mark such writes only after the whole call, mirroring the order of a real assignment.
			foreach (var arg in call.Arguments)
			{
				if (call.GetParameter(arg.ChildIndex)?.ReferenceKind == ReferenceKind.Out)
				{
					HandleWrite(arg);
				}
			}
			DebugEndPoint(call);
		}

		protected internal override void VisitCallIndirect(CallIndirect inst)
		{
			DebugStartPoint(inst);
			foreach (var child in inst.Children)
			{
				child.AcceptVisitor(this);
			}
			// A function-pointer call writes through an 'out' argument, just like a direct call.
			// Mark such writes only after the whole call, mirroring the order of a real assignment.
			int firstArgument = inst.IsInstance ? 1 : 0;
			var referenceKinds = inst.FunctionPointerType.ParameterReferenceKinds;
			for (int i = firstArgument; i < inst.Arguments.Count; i++)
			{
				if (referenceKinds[i - firstArgument] == ReferenceKind.Out)
				{
					HandleWrite(inst.Arguments[i]);
				}
			}
			DebugEndPoint(inst);
		}

		protected internal override void VisitDynamicInvokeMemberInstruction(DynamicInvokeMemberInstruction inst)
		{
			DebugStartPoint(inst);
			foreach (var arg in inst.Arguments)
			{
				arg.AcceptVisitor(this);
			}
			// A dynamically bound call writes through an 'out' argument: the runtime binder treats it
			// as a definite assignment of that argument, exactly as a statically bound call does.
			for (int i = 0; i < inst.Arguments.Count; i++)
			{
				if (inst.ArgumentInfo[i].HasFlag(CSharpArgumentInfoFlags.IsOut))
				{
					HandleWrite(inst.Arguments[i]);
				}
			}
			DebugEndPoint(inst);
		}

		protected internal override void VisitBlockContainer(BlockContainer container)
		{
			base.VisitBlockContainer(container);
			if (container.Parent == scope)
			{
				// 'state' now holds the function-body container's exit state: the join over all
				// paths that return from the function (after any finally blocks have run).
				// A still-set bit means the value is not definitely written on some return path.
				foreach (var entry in outParameterIndices)
				{
					if (state.IsPotentiallyUnwritten(entry.Value))
					{
						potentiallyUnwrittenAtReturn.Set(entry.Value);
					}
				}
			}
		}

		protected internal override void VisitILFunction(ILFunction function)
		{
			DebugStartPoint(function);
			if (function == scope)
			{
				function.Body.AcceptVisitor(this);
			}
			else
			{
				// Nested lambdas and local functions are visited at their point of declaration.
				// They might write through a captured 'out' parameter, but we don't know when (or
				// whether) they run, so we don't let those writes contribute to definite assignment.
				// The state after the nested body is therefore discarded by restoring the state from
				// before it. Treating such writes as not definitely happening keeps the analysis
				// conservative: at worst it injects an unnecessary, but harmless, default assignment.
				State stateBeforeFunction = state.Clone();
				State stateOnExceptionBeforeFunction = currentStateOnException.Clone();
				function.Body.AcceptVisitor(this);
				foreach (var nestedFunction in function.LocalFunctions)
				{
					nestedFunction.AcceptVisitor(this);
				}
				currentStateOnException = stateOnExceptionBeforeFunction;
				state = stateBeforeFunction;
			}
			DebugEndPoint(function);
		}
	}
}

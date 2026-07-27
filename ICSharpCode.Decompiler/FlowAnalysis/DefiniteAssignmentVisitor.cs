// Copyright (c) 2016 Daniel Grunwald
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
using System.Threading;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// DataFlowVisitor that performs definite assignment analysis.
	/// </summary>
	class DefiniteAssignmentVisitor : DataFlowVisitor<DefiniteAssignmentVisitor.State>
	{
		/// <summary>
		/// State for definite assignment analysis.
		/// </summary>
		[DebuggerDisplay("{bits}")]
		public struct State : IDataFlowState<State>
		{
			/// <summary>
			/// bits[i]: There is a code path from the entry point to this state's position
			///          that does not write to function.Variables[i].
			///          (i.e. the variable is not definitely assigned at the state's position)
			/// 
			/// Initial state: all bits set = nothing is definitely assigned
			/// Bottom state: all bits clear
			/// </summary>
			readonly BitSet bits;

			/// <summary>
			/// Creates the initial state.
			/// </summary>
			public State(int variableCount)
			{
				this.bits = new BitSet(variableCount);
				this.bits.Set(0, variableCount);
			}

			private State(BitSet bits)
			{
				this.bits = bits;
			}

			/// <summary>
			/// Determines whether this state is less than or equal to <paramref name="otherState"/> in the lattice ordering.
			/// </summary>
			/// <param name="otherState">The state to compare against.</param>
			/// <returns>
			/// <see langword="true"/> when every variable that might be uninitialized in this state
			/// might also be uninitialized in <paramref name="otherState"/>.
			/// </returns>
			public bool LessThanOrEqual(State otherState)
			{
				return bits.IsSubsetOf(otherState.bits);
			}

			/// <summary>
			/// Creates a deep copy of this state.
			/// </summary>
			/// <returns>A structurally independent state with the same tracked variable bits.</returns>
			public State Clone()
			{
				return new State(bits.Clone());
			}

			/// <summary>
			/// Replaces the current state contents with <paramref name="newContent"/>.
			/// </summary>
			/// <param name="newContent">The state to copy from.</param>
			public void ReplaceWith(State newContent)
			{
				bits.ReplaceWith(newContent.bits);
			}

			/// <summary>
			/// Joins <paramref name="incomingState"/> into this state.
			/// </summary>
			/// <param name="incomingState">A state from another control-flow predecessor.</param>
			/// <remarks>
			/// Definite assignment uses union because a variable is potentially uninitialized after a join
			/// if any predecessor can reach the join without assigning that variable.
			/// </remarks>
			public void JoinWith(State incomingState)
			{
				bits.UnionWith(incomingState.bits);
			}

			/// <summary>
			/// Merges this state with the state observed after executing a <c>finally</c> block.
			/// </summary>
			/// <param name="finallyState">The state at the end of the corresponding finally block.</param>
			public void TriggerFinally(State finallyState)
			{
				// If there is no path to the end of the try-block that leaves a variable v
				// uninitialized, then there is no such path to the end of the whole try-finally either.
				// (the try-finally cannot complete successfully unless the try block does the same)
				// ==> any bits that are false in this.state must be false in the output state.

				// Or said otherwise: a variable is definitely assigned after try-finally if it is
				// definitely assigned in either the try or the finally block.
				// Given that the bits are the opposite of definite assignment, this gives us:
				//    !outputBits[i] == !bits[i] || !finallyState.bits[i].
				// and thus:
				//    outputBits[i] == bits[i] && finallyState.bits[i].
				bits.IntersectWith(finallyState.bits);
			}

			/// <summary>
			/// Replaces this state with the lattice bottom element.
			/// </summary>
			/// <remarks>
			/// Bottom in this analysis means either unreachable code or a point where all tracked variables
			/// are definitely initialized.
			/// </remarks>
			public void ReplaceWithBottom()
			{
				bits.ClearAll();
			}

			/// <summary>
			/// Gets whether this state is the lattice bottom element.
			/// </summary>
			/// <value>
			/// <see langword="true"/> when no variable is marked as potentially uninitialized.
			/// </value>
			public bool IsBottom {
				get { return !bits.Any(); }
			}

			/// <summary>
			/// Marks the variable at <paramref name="variableIndex"/> as definitely assigned.
			/// </summary>
			/// <param name="variableIndex">The index of the variable in the analyzed function.</param>
			public void MarkVariableInitialized(int variableIndex)
			{
				bits.Clear(variableIndex);
			}

			/// <summary>
			/// Gets whether the variable at <paramref name="variableIndex"/> is potentially uninitialized.
			/// </summary>
			/// <param name="variableIndex">The index of the variable in the analyzed function.</param>
			/// <returns>
			/// <see langword="true"/> if at least one currently known path can reach the current point without initializing the variable.
			/// </returns>
			public bool IsPotentiallyUninitialized(int variableIndex)
			{
				return bits[variableIndex];
			}
		}

		readonly CancellationToken cancellationToken;
		readonly ILFunction scope;
		readonly BitSet variablesWithUninitializedUsage;

		readonly Dictionary<IMethod, State> stateOfLocalFunctionUse = new Dictionary<IMethod, State>();
		readonly HashSet<IMethod> localFunctionsNeedingAnalysis = new HashSet<IMethod>();

		/// <summary>
		/// Creates a definite-assignment analyzer for all variables in <paramref name="scope"/>.
		/// </summary>
		/// <param name="scope">The function whose IL is analyzed.</param>
		/// <param name="cancellationToken">A token observed throughout the analysis.</param>
		public DefiniteAssignmentVisitor(ILFunction scope, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.cancellationToken = cancellationToken;
			this.scope = scope;
			this.variablesWithUninitializedUsage = new BitSet(scope.Variables.Count);
			base.flagsRequiringManualImpl |= InstructionFlags.MayReadLocals | InstructionFlags.MayWriteLocals;
			Initialize(new State(scope.Variables.Count));
		}

		/// <summary>
		/// Gets whether <paramref name="v"/> is observed as potentially used before assignment, either by being
		/// read or by having its address taken.
		/// </summary>
		/// <param name="v">A variable declared in the analyzed function.</param>
		/// <returns>
		/// <see langword="true"/> if at least one such use can happen on a path where <paramref name="v"/> was
		/// not definitely assigned.
		/// </returns>
		/// <remarks>
		/// Meaningful only after the visitor has walked the analyzed function; a fresh visitor reports
		/// <see langword="false"/> for every variable.
		/// </remarks>
		public bool IsPotentiallyUsedUninitialized(ILVariable v)
		{
			Debug.Assert(v.Function == scope);
			return variablesWithUninitializedUsage[v.IndexInFunction];
		}

		void HandleStore(ILVariable v)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (v.Function == scope)
			{
				// Mark the variable as initialized:
				state.MarkVariableInitialized(v.IndexInFunction);
				// Note that this gets called even if the store is in unreachable code,
				// but that's OK because bottomState.MarkVariableInitialized() has no effect.

				// After the state change, we have to call
				//  PropagateStateOnException() = currentStateOnException.JoinWith(state);
				// but because MarkVariableInitialized() only clears a bit,
				// this is guaranteed to be a no-op.
			}
		}

		void EnsureInitialized(ILVariable v)
		{
			if (v.Function == scope && state.IsPotentiallyUninitialized(v.IndexInFunction))
			{
				variablesWithUninitializedUsage.Set(v.IndexInFunction);
			}
		}

		/// <summary>
		/// Visits a local store and marks the written variable as initialized.
		/// </summary>
		/// <param name="inst">The store instruction.</param>
		protected internal override void VisitStLoc(StLoc inst)
		{
			inst.Value.AcceptVisitor(this);
			HandleStore(inst.Variable);
		}

		/// <summary>
		/// Handles the implicit variable assignment introduced by successful pattern matching.
		/// </summary>
		/// <param name="inst">The match instruction that writes its pattern variable.</param>
		protected override void HandleMatchStore(MatchInstruction inst)
		{
			HandleStore(inst.Variable);
		}

		/// <summary>
		/// Marks the catch variable as initialized before visiting a handler body.
		/// </summary>
		/// <param name="inst">The handler being entered.</param>
		protected override void BeginTryCatchHandler(TryCatchHandler inst)
		{
			HandleStore(inst.Variable);
			base.BeginTryCatchHandler(inst);
		}

		/// <summary>
		/// Visits a pinned region and marks the pinned local as initialized after its init expression.
		/// </summary>
		/// <param name="inst">The pinned region instruction.</param>
		protected internal override void VisitPinnedRegion(PinnedRegion inst)
		{
			inst.Init.AcceptVisitor(this);
			HandleStore(inst.Variable);
			inst.Body.AcceptVisitor(this);
		}

		/// <summary>
		/// Visits a local load and records a potential use-before-assignment.
		/// </summary>
		/// <param name="inst">The load instruction.</param>
		protected internal override void VisitLdLoc(LdLoc inst)
		{
			EnsureInitialized(inst.Variable);
		}

		/// <summary>
		/// Visits an address-of-local instruction and records a potential use before assignment, since a variable
		/// must be initialized before its address is taken. Address loads used as out arguments are handled by the
		/// call logic and never reach this method.
		/// </summary>
		/// <param name="inst">The address load instruction.</param>
		protected internal override void VisitLdLoca(LdLoca inst)
		{
			// A variable needs to be initialized before we can take it by reference.
			// The exception is if the variable is passed to an out parameter (handled in VisitCall).
			EnsureInitialized(inst.Variable);
		}

		/// <summary>
		/// Visits a direct call instruction.
		/// </summary>
		/// <param name="inst">The call instruction.</param>
		protected internal override void VisitCall(Call inst)
		{
			HandleCall(inst);
		}

		/// <summary>
		/// Visits a virtual call instruction.
		/// </summary>
		/// <param name="inst">The callvirt instruction.</param>
		protected internal override void VisitCallVirt(CallVirt inst)
		{
			HandleCall(inst);
		}

		/// <summary>
		/// Visits an object-construction call.
		/// </summary>
		/// <param name="inst">The constructor call instruction.</param>
		protected internal override void VisitNewObj(NewObj inst)
		{
			HandleCall(inst);
		}

		/// <summary>
		/// Visits a nested IL function (lambda or local function) using definite-assignment semantics.
		/// </summary>
		/// <param name="inst">The nested function instruction.</param>
		/// <remarks>
		/// Lambdas are evaluated at declaration time for captured-variable checks, while local functions are
		/// analyzed from discovered call sites using fixed-point iteration.
		/// </remarks>
		protected internal override void VisitILFunction(ILFunction inst)
		{
			DebugStartPoint(inst);
			State stateBeforeFunction = state.Clone();
			State stateOnExceptionBeforeFunction = currentStateOnException.Clone();
			// Note: lambdas are handled at their point of declaration.
			// We immediately visit their body, because captured variables need to be definitely initialized at this point.
			// We ignore the state after the lambda body (by resetting to the state before), because we don't know
			// when the lambda will be invoked.
			// This also makes this logic unsuitable for reaching definitions, as we wouldn't see the effect of stores in lambdas.
			// Only the simpler case of definite assignment can support lambdas.
			inst.Body.AcceptVisitor(this);

			// For local functions, the situation is similar to lambdas.
			// However, we don't use the state of the declaration site when visiting local functions,
			// but instead the state(s) of their point of use.
			// Because we might discover additional points of use within the local functions,
			// we use a fixed-point iteration.
			bool changed;
			do
			{
				changed = false;
				foreach (var nestedFunction in inst.LocalFunctions)
				{
					if (!localFunctionsNeedingAnalysis.Contains(nestedFunction.ReducedMethod))
						continue;
					localFunctionsNeedingAnalysis.Remove(nestedFunction.ReducedMethod);
					State stateOnEntry = stateOfLocalFunctionUse[nestedFunction.ReducedMethod];
					this.state.ReplaceWith(stateOnEntry);
					this.currentStateOnException.ReplaceWith(stateOnEntry);
					nestedFunction.AcceptVisitor(this);
					changed = true;
				}
			} while (changed);
			currentStateOnException = stateOnExceptionBeforeFunction;
			state = stateBeforeFunction;
			DebugEndPoint(inst);
		}

		/// <summary>
		/// Handles call-like instructions, including delayed initialization of <c>out</c> arguments.
		/// </summary>
		/// <param name="call">The call instruction to analyze.</param>
		void HandleCall(CallInstruction call)
		{
			DebugStartPoint(call);
			bool hasOutArgs = false;
			foreach (var arg in call.Arguments)
			{
				if (arg.MatchLdLoca(out var v) && call.GetParameter(arg.ChildIndex)?.ReferenceKind == ReferenceKind.Out)
				{
					// Visiting ldloca would require the variable to be initialized,
					// but we don't need out arguments to be initialized.
					hasOutArgs = true;
				}
				else
				{
					arg.AcceptVisitor(this);
				}
			}
			// Mark out arguments as initialized, but only after the whole call:
			if (hasOutArgs)
			{
				foreach (var arg in call.Arguments)
				{
					if (arg.MatchLdLoca(out var v) && call.GetParameter(arg.ChildIndex)?.ReferenceKind == ReferenceKind.Out)
					{
						HandleStore(v);
					}
				}
			}
			HandleLocalFunctionUse(call.Method);
			DebugEndPoint(call);
		}

		/// <summary>
		/// Records that the current state is a possible entry state for the referenced local function.
		/// </summary>
		/// <param name="method">The method operand from a call or function-pointer load.</param>
		void HandleLocalFunctionUse(IMethod method)
		{
			if (method.IsLocalFunction)
			{
				if (stateOfLocalFunctionUse.TryGetValue(method, out var stateOnEntry))
				{
					if (!state.LessThanOrEqual(stateOnEntry))
					{
						stateOnEntry.JoinWith(state);
						localFunctionsNeedingAnalysis.Add(method);
					}
				}
				else
				{
					stateOfLocalFunctionUse.Add(method, state.Clone());
					localFunctionsNeedingAnalysis.Add(method);
				}
			}
		}

		/// <summary>
		/// Visits a function-pointer load and records local-function usage when applicable.
		/// </summary>
		/// <param name="inst">The function-pointer load instruction.</param>
		protected internal override void VisitLdFtn(LdFtn inst)
		{
			DebugStartPoint(inst);
			HandleLocalFunctionUse(inst.Method);
			DebugEndPoint(inst);
		}
	}
}

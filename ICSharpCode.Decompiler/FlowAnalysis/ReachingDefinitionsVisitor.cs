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
using System.Linq;
using System.Threading;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// Implements the "reaching definitions" analysis.
	/// 
	/// https://en.wikipedia.org/wiki/Reaching_definition
	/// 
	/// By "definitions", we mean stores to local variables.
	/// </summary>
	/// <remarks>
	/// Possible "definitions" that store to a variable are:
	/// * <c>StLoc</c>
	/// * <c>TryCatchHandler</c> (for the exception variable)
	/// * <c>LdLoca</c> passed directly to an <c>out</c> parameter
	/// * <c>ReachingDefinitionsVisitor.UninitializedVariable</c> for uninitialized variables.
	/// Note that we do not otherwise keep track of <c>LdLoca</c>/references/pointers.
	/// The analysis will likely be wrong/incomplete for variables with <c>AddressCount != 0</c>.
	/// 
	/// Note: this class does not store the computed information, because doing so
	/// would significantly increase the number of states we need to store.
	/// The only way to get the computed information out of this class is to
	/// derive from the class and override the Visit methods at the points of interest
	/// (usually the load instructions).
	/// </remarks>
	class ReachingDefinitionsVisitor : DataFlowVisitor<ReachingDefinitionsVisitor.State>
	{
		#region State representation
		/// <summary>
		/// The state during the reaching definitions analysis.
		/// </summary>
		/// <remarks>
		/// A state can either be reachable, or unreachable:
		///  1) unreachable
		///     Note that during the analysis, "unreachable" just means we have not yet found a path
		///     from the entry point to the node. States transition from unreachable to reachable as
		///     the analysis processes more control flow paths.
		///  2) reachable
		///     In this case, the state contains, for each variable, the set of stores that might have
		///     written to the variable before the control flow reached the state's source code position.
		///     This set does not include stores that were definitely overwritten by other stores to the
		///     same variable.
		///     During the analysis, the set of stores gets extended as the analysis processes more code paths.
		/// 
		/// The reachable state could be represented as a `Dictionary{ILVariable, ISet{ILInstruction}}`.
		/// To consume less memory, we instead assign an integer index to all stores in the analyzed function ("store index"),
		/// and store the state as a `BitSet` instead.
		/// Each bit in the set corresponds to one store instruction, and is `true` iff the store is a reaching definition
		/// for the variable it is storing to.
		/// The `allStores` array has the same length as the bit sets and holds the corresponding `ILInstruction` objects (store instructions).
		/// All stores for a single variable occupy a contiguous segment of the `allStores` array (and thus also of the `state`),
		/// which allows us to efficient clear out all stores that get overwritten by a new store.
		/// </remarks>
		[DebuggerDisplay("{bits}")]
		public struct State : IDataFlowState<State>
		{
			/// <summary>
			/// This bitset contains three different kinds of bits:
			/// Reachable bit: (bit 0)
			///     This state's position is reachable from the entry point.
			/// 
			/// Reaching uninitialized variable bit: (bit si, where si > 0 and <c>allStores[si] == null</c>)
			///     There is a code path from the scope's entry point to this state's position
			///     that does not pass through any store to the variable.
			/// 
			/// <c>firstStoreIndexForVariable[v.IndexInScope]</c> gives the index of that variable's uninitialized bit.
			/// 
			/// Reaching store bit (bit si, where <c>allStores[si] != null</c>):
			///     There is a code path from the entry point to this state's position
			///     that passes through through <c>allStores[si]</c> and does not pass through another
			///     store to <c>allStores[si].Variable</c>.
			/// 
			/// The indices for a variable's reaching store bits are between <c>firstStoreIndexForVariable[v.IndexInScope]</c>
			/// to <c>firstStoreIndexForVariable[v.IndexInScope + 1]</c> (both endpoints exclusive!).
			/// </summary>
			/// <remarks>
			/// The initial state has the "reachable bit" and the "reaching uninitialized variable bits" set,
			/// and the "reaching store bits" unset.
			/// 
			/// The bottom state has all bits unset.
			/// </remarks>
			readonly BitSet bits;

			public State(BitSet bits)
			{
				this.bits = bits;
			}

			/// <summary>
			/// Determines whether this state is less than or equal to <paramref name="otherState"/> in the analysis lattice.
			/// </summary>
			/// <param name="otherState">The state to compare against.</param>
			/// <returns>
			/// <see langword="true"/> when every reaching-definition bit set in this state is also set in <paramref name="otherState"/>.
			/// </returns>
			public bool LessThanOrEqual(State otherState)
			{
				return bits.IsSubsetOf(otherState.bits);
			}

			/// <summary>
			/// Creates a deep copy of this state.
			/// </summary>
			/// <returns>A state with an independent copy of the underlying bitset.</returns>
			public State Clone()
			{
				return new State(bits.Clone());
			}

			/// <summary>
			/// Replaces this state's bits with a copy of <paramref name="newContent"/>.
			/// </summary>
			/// <param name="newContent">The state to copy from.</param>
			public void ReplaceWith(State newContent)
			{
				bits.ReplaceWith(newContent.bits);
			}

			public void JoinWith(State incomingState)
			{
				// When control flow is joined together, we can simply union our bitsets.
				// (a store is reachable iff it is reachable through either incoming path)
				bits.UnionWith(incomingState.bits);
			}

			/// <summary>
			/// Applies the try/finally transfer function for a branch that leaves the protected region.
			/// </summary>
			/// <param name="finallyState">The state observed at the end of the corresponding finally block.</param>
			public void TriggerFinally(State finallyState)
			{
				// Some cases to consider:
				//   try { v = 1; } finally { v = 2; }
				//     => only the store 2 is visible after the try-finally
				//   v = 1; try { v = 2; } finally { }
				//     => both stores are visible after the try-finally
				// In general, we're looking for the post-state of the finally-block
				// assume the finally-block was entered without throwing an exception.
				// But we don't have that information (it would require analyzing the finally block twice),
				// so the next best thing is to approximate it by just keeping the state after the finally
				// (i.e. doing nothing at all).
				// However, the DataFlowVisitor requires us to return bottom if the end-state of the
				// try-block was unreachable, so let's so at least that.
				// (note that in principle we could just AND the reachable and uninitialized bits,
				//  but we don't have a good solution for the normal store bits)
				if (IsReachable)
				{
					ReplaceWith(finallyState);
				}
			}

			/// <summary>
			/// Gets whether this state is the lattice bottom element.
			/// </summary>
			/// <value><see langword="true"/> when the reachable bit is clear.</value>
			public bool IsBottom {
				get { return !bits[ReachableBit]; }
			}

			/// <summary>
			/// Replaces this state with the lattice bottom element.
			/// </summary>
			public void ReplaceWithBottom()
			{
				// We need to clear all bits, not just ReachableBit, so that
				// the bottom state behaves as expected in joins.
				bits.ClearAll();
			}

			/// <summary>
			/// Gets whether this state has been marked as reachable.
			/// </summary>
			/// <value>
			/// <see langword="true"/> when at least one control-flow path from the function entry reaches this point.
			/// </value>
			public bool IsReachable {
				get { return bits[ReachableBit]; }
			}

			/// <summary>
			/// Clears all store bits between startStoreIndex (incl.) and endStoreIndex (excl.)
			/// </summary>
			public void KillStores(int startStoreIndex, int endStoreIndex)
			{
				Debug.Assert(startStoreIndex >= FirstStoreIndex);
				Debug.Assert(endStoreIndex >= startStoreIndex);
				bits.Clear(startStoreIndex, endStoreIndex);
			}

			/// <summary>
			/// Gets whether the store at <paramref name="storeIndex"/> is currently a reaching definition.
			/// </summary>
			/// <param name="storeIndex">A store index in the global <c>allStores</c> array.</param>
			/// <returns><see langword="true"/> if the store reaches the current program point.</returns>
			public bool IsReachingStore(int storeIndex)
			{
				return bits[storeIndex];
			}

			/// <summary>
			/// Finds the next reaching store bit between <paramref name="startIndex"/> (inclusive) and <paramref name="endIndex"/> (exclusive).
			/// </summary>
			/// <param name="startIndex">Inclusive lower bound for the search.</param>
			/// <param name="endIndex">Exclusive upper bound for the search.</param>
			/// <returns>
			/// The index of the next set bit, or <c>-1</c> if no set bit exists in the requested range.
			/// </returns>
			public int NextReachingStore(int startIndex, int endIndex)
			{
				return bits.NextSetBit(startIndex, endIndex);
			}

			/// <summary>
			/// Marks the store at <paramref name="storeIndex"/> as reaching.
			/// </summary>
			/// <param name="storeIndex">A non-reserved store index.</param>
			public void SetStore(int storeIndex)
			{
				Debug.Assert(storeIndex >= FirstStoreIndex);
				bits.Set(storeIndex);
			}
		}

		/// <summary>
		/// To distinguish unreachable from reachable states, we use the first bit in the bitset to store the 'reachable bit'.
		/// If this bit is set, the state is reachable, and the remaining bits
		/// </summary>
		const int ReachableBit = 0;

		/// <summary>
		/// Because bit number 0 is the ReachableBit, we start counting store indices at 1.
		/// </summary>
		const int FirstStoreIndex = 1;
		#endregion

		#region Documentation + member fields
		protected readonly CancellationToken cancellationToken;

		/// <summary>
		/// The function being analyzed.
		/// </summary>
		protected readonly ILFunction scope;

		/// <summary>
		/// All stores for all variables in the scope.
		/// 
		/// <c>state[storeIndex]</c> is true iff <c>allStores[storeIndex]</c> is a reaching definition.
		/// Invariant: <c>state.bits.Length == allStores.Length</c>.
		/// </summary>
		readonly ILInstruction[] allStores;

		/// <summary>
		/// Maps instructions appearing in <c>allStores</c> to their index.
		/// 
		/// Invariant: <c>allStores[storeIndexMap[inst]] == inst</c>
		/// 
		/// Does not contain <c>UninitializedVariable</c> (as that special instruction has multiple store indices, one per variable)
		/// </summary>
		readonly Dictionary<ILInstruction, int> storeIndexMap = new Dictionary<ILInstruction, int>();

		/// <summary>
		/// For all variables <c>v</c>: <c>allStores[firstStoreIndexForVariable[v.IndexInScope]]</c> is the <c>UninitializedVariable</c> entry for <c>v</c>.
		/// The next few stores (up to <c>firstStoreIndexForVariable[v.IndexInScope + 1]</c>, exclusive) are the full list of stores for <c>v</c>.
		/// </summary>
		/// <remarks>
		/// Invariant: <c>firstStoreIndexForVariable[scope.Variables.Count] == allStores.Length</c>
		/// </remarks>
		readonly int[] firstStoreIndexForVariable;

		/// <summary>
		/// <c>analyzedVariables[v.IndexInScope]</c> is true iff RD analysis is enabled for the variable.
		/// </summary>
		readonly BitSet analyzedVariables;
		#endregion

		#region Constructor
		/// <summary>
		/// Creates a reaching-definitions analyzer over <paramref name="scope"/> using a predicate to select variables.
		/// </summary>
		/// <param name="scope">The function to analyze.</param>
		/// <param name="pred">Predicate that selects which variables participate in the analysis.</param>
		/// <param name="cancellationToken">A token observed during setup and traversal.</param>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
		public ReachingDefinitionsVisitor(ILFunction scope, Predicate<ILVariable> pred, CancellationToken cancellationToken)
			: this(scope, GetActiveVariableBitSet(scope, pred), cancellationToken)
		{
			this.cancellationToken = cancellationToken;
		}

		static BitSet GetActiveVariableBitSet(ILFunction scope, Predicate<ILVariable> pred)
		{
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			BitSet activeVariables = new BitSet(scope.Variables.Count);
			for (int vi = 0; vi < scope.Variables.Count; vi++)
			{
				activeVariables[vi] = pred(scope.Variables[vi]);
			}
			return activeVariables;
		}

		/// <summary>
		/// Creates a reaching-definitions analyzer over <paramref name="scope"/> using an explicit variable-selection bitset.
		/// </summary>
		/// <param name="scope">The function to analyze.</param>
		/// <param name="analyzedVariables">
		/// A bitset aligned with <c>scope.Variables</c> where set bits indicate variables tracked by this analysis.
		/// </param>
		/// <param name="cancellationToken">A token observed while preparing analysis metadata.</param>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> or <paramref name="analyzedVariables"/> is <see langword="null"/>.</exception>
		public ReachingDefinitionsVisitor(ILFunction scope, BitSet analyzedVariables, CancellationToken cancellationToken)
		{
			if (scope == null)
				throw new ArgumentNullException(nameof(scope));
			if (analyzedVariables == null)
				throw new ArgumentNullException(nameof(analyzedVariables));
			this.scope = scope;
			this.analyzedVariables = analyzedVariables;
			base.flagsRequiringManualImpl |= InstructionFlags.MayWriteLocals;

			// Fill `allStores` and `storeIndexMap` and `firstStoreIndexForVariable`.
			var storesByVar = FindAllStoresByVariable(scope, analyzedVariables, cancellationToken);
			allStores = new ILInstruction[FirstStoreIndex + storesByVar.Sum(l => l != null ? l.Count : 0)];
			firstStoreIndexForVariable = new int[scope.Variables.Count + 1];
			int si = FirstStoreIndex;
			for (int vi = 0; vi < storesByVar.Length; vi++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				firstStoreIndexForVariable[vi] = si;
				var stores = storesByVar[vi];
				if (stores != null)
				{
					int expectedStoreCount = scope.Variables[vi].StoreInstructions.Count;
					expectedStoreCount += scope.Variables[vi].AddressInstructions.OfType<LdLoca>().Count(IsOutArgument);
					// Extra store for the uninitialized state.
					expectedStoreCount += 1;
					Debug.Assert(stores.Count == expectedStoreCount);
					stores.CopyTo(allStores, si);
					// Add all stores except for the first (representing the uninitialized state)
					// to storeIndexMap.
					for (int i = 1; i < stores.Count; i++)
					{
						storeIndexMap.Add(stores[i], si + i);
					}
					si += stores.Count;
				}
			}
			firstStoreIndexForVariable[scope.Variables.Count] = si;
			Debug.Assert(si == allStores.Length);

			Initialize(CreateInitialState());
		}

		/// <summary>
		/// Fill <c>allStores</c> and <c>storeIndexMap</c>.
		/// </summary>
		static List<ILInstruction>[] FindAllStoresByVariable(ILFunction scope, BitSet activeVariables, CancellationToken cancellationToken)
		{
			// For each variable, find the list of ILInstructions storing to that variable
			List<ILInstruction>[] storesByVar = new List<ILInstruction>[scope.Variables.Count];
			for (int vi = 0; vi < storesByVar.Length; vi++)
			{
				if (activeVariables[vi])
					storesByVar[vi] = new List<ILInstruction> { null };
			}
			foreach (var inst in scope.Descendants)
			{
				ILVariable v;
				if (inst.HasDirectFlag(InstructionFlags.MayWriteLocals))
				{
					v = ((IInstructionWithVariableOperand)inst).Variable;
				}
				else if (inst is LdLoca ldloca && IsOutArgument(ldloca))
				{
					v = ldloca.Variable;
				}
				else
				{
					continue;
				}
				cancellationToken.ThrowIfCancellationRequested();
				if (v.Function == scope && activeVariables[v.IndexInFunction])
				{
					storesByVar[v.IndexInFunction].Add(inst);
				}
			}
			return storesByVar;
		}

		protected static bool IsOutArgument(LdLoca inst)
		{
			return inst.Parent is CallInstruction call
				&& call.GetParameter(inst.ChildIndex)?.ReferenceKind == ReferenceKind.Out;
		}

		/// <summary>
		/// Create the initial state (reachable bit + uninit variable bits set, store bits unset).
		/// </summary>
		State CreateInitialState()
		{
			BitSet initialState = new BitSet(allStores.Length);
			initialState.Set(ReachableBit);
			for (int vi = 0; vi < scope.Variables.Count; vi++)
			{
				if (analyzedVariables[vi])
				{
					Debug.Assert(allStores[firstStoreIndexForVariable[vi]] == null);
					initialState.Set(firstStoreIndexForVariable[vi]);
				}
			}
			return new State(initialState);
		}
		#endregion

		#region Analysis
		void HandleStore(ILInstruction inst, ILVariable v)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (v.Function == scope && analyzedVariables[v.IndexInFunction] && state.IsReachable)
			{
				// Clear the set of stores for this variable:
				state.KillStores(firstStoreIndexForVariable[v.IndexInFunction], firstStoreIndexForVariable[v.IndexInFunction + 1]);
				// And replace it with this store:
				int si = storeIndexMap[inst];
				state.SetStore(si);

				// We should call PropagateStateOnException() here because we changed the state.
				// But that's equal to: currentStateOnException.UnionWith(state);

				// Because we're already guaranteed that state.LessThanOrEqual(currentStateOnException)
				// when entering HandleStore(), all we really need to do to achieve what PropagateStateOnException() does
				// is to add the single additional store to the exceptional state as well:
				currentStateOnException.SetStore(si);
			}
		}

		/// <summary>
		/// Visits a local-variable store and updates the reaching-definition set for the target variable.
		/// </summary>
		/// <param name="inst">The store instruction.</param>
		protected internal override void VisitStLoc(StLoc inst)
		{
			inst.Value.AcceptVisitor(this);
			HandleStore(inst, inst.Variable);
		}

		/// <summary>
		/// Handles the implicit variable store produced by a successful pattern match.
		/// </summary>
		/// <param name="inst">The match instruction introducing the store.</param>
		protected override void HandleMatchStore(MatchInstruction inst)
		{
			HandleStore(inst, inst.Variable);
		}

		/// <summary>
		/// Marks a catch-variable assignment as a reaching definition when entering a handler.
		/// </summary>
		/// <param name="inst">The handler being entered.</param>
		protected override void BeginTryCatchHandler(TryCatchHandler inst)
		{
			base.BeginTryCatchHandler(inst);
			HandleStore(inst, inst.Variable);
		}

		/// <summary>
		/// Visits a pinned region and treats the pin variable initialization as a store.
		/// </summary>
		/// <param name="inst">The pinned-region instruction.</param>
		protected internal override void VisitPinnedRegion(PinnedRegion inst)
		{
			inst.Init.AcceptVisitor(this);
			HandleStore(inst, inst.Variable);
			inst.Body.AcceptVisitor(this);
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
			foreach (var argument in call.Arguments)
			{
				argument.AcceptVisitor(this);
			}
			// An out argument is assigned only after all arguments have been evaluated.
			foreach (var argument in call.Arguments)
			{
				if (argument is LdLoca ldloca && IsOutArgument(ldloca))
				{
					HandleStore(ldloca, ldloca.Variable);
				}
			}
			DebugEndPoint(call);
		}

		/// <summary>
		/// Gets whether the variable is included in this analysis instance.
		/// </summary>
		/// <param name="v">The variable to query.</param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="v"/> belongs to the analyzed scope and its bit is set in <c>analyzedVariables</c>.
		/// </returns>
		public bool IsAnalyzedVariable(ILVariable v)
		{
			return v.Function == scope && analyzedVariables[v.IndexInFunction];
		}

		/// <summary>
		/// Gets all stores to <c>v</c> that reach the specified state.
		/// 
		/// Precondition: v is an analyzed variable.
		/// </summary>
		protected IEnumerable<ILInstruction> GetStores(State state, ILVariable v)
		{
			Debug.Assert(v.Function == scope && analyzedVariables[v.IndexInFunction]);
			int startIndex = firstStoreIndexForVariable[v.IndexInFunction] + 1;
			int endIndex = firstStoreIndexForVariable[v.IndexInFunction + 1];
			while (startIndex < endIndex)
			{
				int nextReachingStore = state.NextReachingStore(startIndex, endIndex);
				if (nextReachingStore == -1)
				{
					break;
				}
				Debug.Assert(((IInstructionWithVariableOperand)allStores[nextReachingStore]).Variable == v);
				yield return allStores[nextReachingStore];
				startIndex = nextReachingStore + 1;
			}
		}

		/// <summary>
		/// Gets whether <c>v</c> is potentially uninitialized in the specified state.
		/// 
		/// Precondition: v is an analyzed variable.
		/// </summary>
		protected bool IsPotentiallyUninitialized(State state, ILVariable v)
		{
			Debug.Assert(v.Function == scope && analyzedVariables[v.IndexInFunction]);
			return state.IsReachingStore(firstStoreIndexForVariable[v.IndexInFunction]);
		}
		#endregion
	}
}

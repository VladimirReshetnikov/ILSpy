#nullable enable
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

using System.Collections.Generic;

namespace ICSharpCode.Decompiler.IL.Patterns
{
	/// <summary>
	/// Data holder for a single list matching operation.
	/// </summary>
	/// <remarks>
	/// Notes on backtracking:
	/// PerformMatch() may save backtracking-savepoints to the ListMatch instance.
	/// Each backtracking-savepoints is a Stack{int} with instructions of how to restore the saved state.
	/// When a savepoint is created by a PerformMatch() call, that call may be nested in several other PerformMatch() calls that operate on
	/// the same list.
	/// When leaving those calls (whether with a successful match or not), the outer PerformMatch() calls may push additional state onto
	/// all of the added backtracking-savepoints.
	/// When the overall list match fails but savepoints exists, the most recently added savepoint is restored by calling PerformMatch()
	/// with listMatch.restoreStack set to that savepoint. Each PerformMatch() call must pop its state from that stack before
	/// recursively calling its child patterns.
	/// </remarks>
	public struct ListMatch
	{
		/// <summary>
		/// The main list matching logic.
		/// </summary>
		/// <returns>Returns whether the list match was successful.
		/// If the method returns true, it adds the capture groups (if any) to the match.
		/// If the method returns false, the match object remains in a partially-updated state and needs to be restored
		/// before it can be reused.</returns>
		internal static bool DoMatch(IReadOnlyList<ILInstruction> patterns, IReadOnlyList<ILInstruction?> syntaxList, ref Match match)
		{
			ListMatch listMatch = new ListMatch(syntaxList);
			do
			{
				if (PerformMatchSequence(patterns, ref listMatch, ref match))
				{
					// If we have a successful match and it matches the whole list,
					// we are done.
					if (listMatch.SyntaxIndex == syntaxList.Count)
						return true;
				}
				// Otherwise, restore a savepoint created by PerformMatch() and resume the matching logic at that savepoint.
			} while (listMatch.RestoreSavePoint(ref match));
			return false;
		}

		/// <summary>
		/// Attempts to match a contiguous sequence of patterns starting at the current syntax position.
		/// </summary>
		/// <param name="patterns">Patterns to evaluate in order.</param>
		/// <param name="listMatch">Mutable list-match state, including syntax index and savepoint stacks.</param>
		/// <param name="match">Capture state shared across nested pattern evaluations.</param>
		/// <returns>
		/// <see langword="true"/> when each pattern succeeds.
		/// On success, <paramref name="listMatch"/> is advanced to the first unmatched syntax element.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Nested calls can create backtracking savepoints; this method propagates loop-state data (<c>i</c>) into each new savepoint so restoration
		/// can resume at the correct pattern index.
		/// </para>
		/// <para>
		/// On failure, both <paramref name="listMatch"/> and <paramref name="match"/> may contain partial state. The caller is expected to invoke
		/// <see cref="RestoreSavePoint(ref Match)"/> (or abandon the values) before reuse.
		/// </para>
		/// </remarks>
		internal static bool PerformMatchSequence(IReadOnlyList<ILInstruction> patterns, ref ListMatch listMatch, ref Match match)
		{
			// The patterns may create savepoints, so we need to save the 'i' variable
			// as part of those checkpoints.
			for (int i = listMatch.PopFromSavePoint() ?? 0; i < patterns.Count; i++)
			{
				int startMarker = listMatch.GetSavePointStartMarker();
				bool success = patterns[i].PerformMatch(ref listMatch, ref match);
				listMatch.PushToSavePoints(startMarker, i);
				if (!success)
					return false;
			}
			return true;
		}

		/// <summary>
		/// A savepoint that the list matching operation can be restored from.
		/// </summary>
		struct SavePoint
		{
			internal readonly int CheckPoint;
			internal readonly int SyntaxIndex;
			internal readonly Stack<int> stack;

			/// <summary>
			/// Initializes a savepoint with the current capture checkpoint and syntax index.
			/// </summary>
			/// <param name="checkpoint">Capture checkpoint in <see cref="Match"/>.</param>
			/// <param name="syntaxIndex">Current syntax-list index.</param>
			public SavePoint(int checkpoint, int syntaxIndex)
			{
				this.CheckPoint = checkpoint;
				this.SyntaxIndex = syntaxIndex;
				this.stack = new Stack<int>();
			}
		}

		/// <summary>
		/// The syntax list we are matching against.
		/// </summary>
		internal readonly IReadOnlyList<ILInstruction?> SyntaxList;

		/// <summary>
		/// The current index in the syntax list.
		/// </summary>
		internal int SyntaxIndex;

		ListMatch(IReadOnlyList<ILInstruction?> syntaxList)
		{
			this.SyntaxList = syntaxList;
			this.SyntaxIndex = 0;
			this.backtrackingStack = null;
			this.restoreStack = null;
		}

		List<SavePoint>? backtrackingStack;
		Stack<int>? restoreStack;

		void AddSavePoint(SavePoint savepoint)
		{
			if (backtrackingStack == null)
				backtrackingStack = new List<SavePoint>();
			backtrackingStack.Add(savepoint);
		}

		/// <summary>
		/// Adds a savepoint that can restore both syntax position and capture state.
		/// </summary>
		/// <param name="match">Current match state used to record a rollback checkpoint.</param>
		/// <param name="data">First restore-stack value associated with the savepoint.</param>
		internal void AddSavePoint(ref Match match, int data)
		{
			var savepoint = new SavePoint(match.CheckPoint(), this.SyntaxIndex);
			savepoint.stack.Push(data);
			AddSavePoint(savepoint);
		}

		/// <summary>
		/// Returns a marker that identifies the current savepoint list length.
		/// </summary>
		/// <returns>Current number of savepoints, or <c>0</c> when no savepoints exist.</returns>
		internal int GetSavePointStartMarker()
		{
			return backtrackingStack != null ? backtrackingStack.Count : 0;
		}

		/// <summary>
		/// Pushes restore metadata to savepoints created after a start marker.
		/// </summary>
		/// <param name="startMarker">Start index previously returned by <see cref="GetSavePointStartMarker"/>.</param>
		/// <param name="data">Restore payload value to append.</param>
		internal void PushToSavePoints(int startMarker, int data)
		{
			if (backtrackingStack == null)
				return;
			for (int i = startMarker; i < backtrackingStack.Count; i++)
			{
				backtrackingStack[i].stack.Push(data);
			}
		}

		/// <summary>
		/// Pops the next restore value from the currently active restored savepoint.
		/// </summary>
		/// <returns>The next restore value, or <see langword="null"/> when no restored data remains.</returns>
		internal int? PopFromSavePoint()
		{
			if (restoreStack == null || restoreStack.Count == 0)
				return null;
			return restoreStack.Pop();
		}

		/// <summary>
		/// Restores the listmatch state from a savepoint.
		/// </summary>
		/// <returns>Returns whether a savepoint exists</returns>
		internal bool RestoreSavePoint(ref Match match)
		{
			if (backtrackingStack == null || backtrackingStack.Count == 0)
				return false;
			var savepoint = backtrackingStack[backtrackingStack.Count - 1];
			backtrackingStack.RemoveAt(backtrackingStack.Count - 1);
			match.RestoreCheckPoint(savepoint.CheckPoint);
			this.SyntaxIndex = savepoint.SyntaxIndex;
			restoreStack = savepoint.stack;
			return true;
		}
	}
}

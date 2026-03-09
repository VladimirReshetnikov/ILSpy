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
	/// Identifies a named capture slot used by IL pattern matching.
	/// </summary>
	/// <remarks>
	/// Capture groups are intentionally compared by object identity.
	/// A pattern producer typically stores static instances and reuses them to retrieve captured nodes from <see cref="Match"/>.
	/// </remarks>
	public class CaptureGroup { }

	/// <summary>
	/// Data holder for the overall pattern matching operation.
	/// </summary>
	/// <remarks>
	/// This type is a struct in order to prevent unnecessary memory allocations during pattern matching.
	/// The default value <c>default(Match)</c> represents an unsuccessful match.
	/// </remarks>
	public struct Match
	{
		static readonly List<KeyValuePair<CaptureGroup, ILInstruction>> emptyResults = new List<KeyValuePair<CaptureGroup, ILInstruction>>();

		List<KeyValuePair<CaptureGroup, ILInstruction>>? results;

		/// <summary>
		/// Gets whether the match was successful.
		/// </summary>
		public bool Success {
			get {
				return results != null;
			}
			internal set {
				if (value)
				{
					if (results == null)
						results = emptyResults;
				}
				else
				{
					results = null;
				}
			}
		}

		/// <summary>
		/// Gets whether the match was successful.
		/// </summary>
		public static bool operator true(Match m)
		{
			return m.Success;
		}

		/// <summary>
		/// Gets whether the match failed.
		/// </summary>
		public static bool operator false(Match m)
		{
			return !m.Success;
		}

		/// <summary>
		/// Adds a captured node to the specified capture group.
		/// </summary>
		/// <param name="g">Capture group identifier.</param>
		/// <param name="n">Captured instruction.</param>
		internal void Add(CaptureGroup g, ILInstruction n)
		{
			if (results == null)
				results = new List<KeyValuePair<CaptureGroup, ILInstruction>>();
			results.Add(new KeyValuePair<CaptureGroup, ILInstruction>(g, n));
		}

		/// <summary>
		/// Saves the current capture-list length so a caller can roll back partial captures.
		/// </summary>
		/// <returns>Current capture count.</returns>
		internal int CheckPoint()
		{
			return results != null ? results.Count : 0;
		}

		/// <summary>
		/// Removes any captures added after a previously saved checkpoint.
		/// </summary>
		/// <param name="checkPoint">Checkpoint value returned by <see cref="CheckPoint"/>.</param>
		internal void RestoreCheckPoint(int checkPoint)
		{
			if (results != null)
				results.RemoveRange(checkPoint, results.Count - checkPoint);
		}

		/// <summary>
		/// Enumerates all instructions captured for a specific capture group.
		/// </summary>
		/// <param name="captureGroup">Capture group to query.</param>
		/// <returns>
		/// A deferred sequence containing captures in insertion order.
		/// If no captures exist, the returned sequence is empty.
		/// </returns>
		/// <remarks>
		/// The sequence filters the internal capture list at enumeration time.
		/// Callers that need a stable snapshot should materialize the result before mutating the underlying <see cref="Match"/>.
		/// </remarks>
		public IEnumerable<ILInstruction> Get(CaptureGroup captureGroup)
		{
			if (results != null)
			{
				foreach (var pair in results)
				{
					if (pair.Key == captureGroup)
						yield return pair.Value;
				}
			}
		}
	}
}

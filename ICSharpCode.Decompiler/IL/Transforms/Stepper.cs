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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// The exception raised when a step-recording session reaches <see cref="Stepper.StepLimit"/>.
	/// </summary>
	public class StepLimitReachedException : Exception
	{
	}

	/// <summary>
	/// Records a hierarchical trace of IL-transform steps for debugging and diagnostics.
	/// </summary>
	/// <remarks>
	/// The step tree is populated only when transform code calls the step APIs. Most built-in transform calls are
	/// conditionally compiled behind the <c>STEP</c> symbol, so release builds typically produce no entries unless
	/// a caller invokes <see cref="Step(string, ILInstruction)"/> directly.
	/// </remarks>
	public class Stepper
	{
		/// <summary>
		/// Gets whether stepping of built-in transforms is supported in this build of ICSharpCode.Decompiler.
		/// Usually only debug builds support transform stepping.
		/// </summary>
		public static bool SteppingAvailable {
			get {
#if STEP
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Gets the top-level step nodes captured for the current run.
		/// </summary>
		/// <value>
		/// A mutable list that contains either atomic steps or group roots in encounter order.
		/// </value>
		public IList<Node> Steps => steps;

		/// <summary>
		/// Gets or sets the maximum number of steps that can be recorded before stepping stops.
		/// </summary>
		/// <value>
		/// Defaults to <see cref="int.MaxValue"/>.
		/// </value>
		public int StepLimit { get; set; } = int.MaxValue;

		/// <summary>
		/// Gets or sets whether reaching <see cref="StepLimit"/> should break into a debugger.
		/// </summary>
		/// <value>
		/// When <see langword="true"/>, limit exhaustion triggers <see cref="Debugger.Break()"/>; otherwise
		/// <see cref="StepLimitReachedException"/> is thrown.
		/// </value>
		public bool IsDebug { get; set; }

		/// <summary>
		/// Represents one recorded step or grouped step range.
		/// </summary>
		public class Node
		{
			/// <summary>
			/// Gets the display label for this step.
			/// </summary>
			public string Description { get; }

			/// <summary>
			/// Gets or sets an instruction near which this step occurred.
			/// </summary>
			public ILInstruction? Position { get; set; }
			/// <summary>
			/// Gets or sets the inclusive step index where this node starts.
			/// </summary>
			public int BeginStep { get; set; }
			/// <summary>
			/// Gets or sets the exclusive step index where this node ends.
			/// </summary>
			public int EndStep { get; set; }

			/// <summary>
			/// Gets child nodes recorded while this node was the active group.
			/// </summary>
			public IList<Node> Children { get; } = new List<Node>();

			/// <summary>
			/// Initializes a step node.
			/// </summary>
			/// <param name="description">Human-readable label for the node.</param>
			public Node(string description)
			{
				Description = description;
			}
		}

		readonly Stack<Node> groups;
		readonly IList<Node> steps;
		int step = 0;

		/// <summary>
		/// Initializes a new step recorder with no recorded nodes.
		/// </summary>
		public Stepper()
		{
			steps = new List<Node>();
			groups = new Stack<Node>();
		}

		/// <summary>
		/// Records an individual transform step.
		/// </summary>
		/// <param name="description">Human-readable label for the step.</param>
		/// <param name="near">Instruction near which the step occurred, or <see langword="null"/>.</param>
		/// <exception cref="StepLimitReachedException">
		/// The step limit was reached and <see cref="IsDebug"/> is <see langword="false"/>.
		/// </exception>
		[DebuggerStepThrough]
		public void Step(string description, ILInstruction? near = null)
		{
			StepInternal(description, near);
		}

		[DebuggerStepThrough]
		private Node StepInternal(string description, ILInstruction? near)
		{
			if (step == StepLimit)
			{
				if (IsDebug)
					Debugger.Break();
				else
					throw new StepLimitReachedException();
			}
			var stepNode = new Node($"{step}: {description}") {
				Position = near,
				BeginStep = step,
				EndStep = step + 1
			};
			var p = groups.PeekOrDefault();
			if (p != null)
				p.Children.Add(stepNode);
			else
				steps.Add(stepNode);
			step++;
			return stepNode;
		}

		/// <summary>
		/// Starts a grouped step and pushes it onto the current group stack.
		/// </summary>
		/// <param name="description">Group label.</param>
		/// <param name="near">Instruction near which the group starts, or <see langword="null"/>.</param>
		[DebuggerStepThrough]
		public void StartGroup(string description, ILInstruction? near = null)
		{
			groups.Push(StepInternal(description, near));
		}

		/// <summary>
		/// Ends the most recently started group.
		/// </summary>
		/// <param name="keepIfEmpty">
		/// <see langword="true"/> to keep groups without child steps; otherwise empty groups are removed.
		/// </param>
		/// <exception cref="InvalidOperationException">No open group exists.</exception>
		public void EndGroup(bool keepIfEmpty = false)
		{
			var node = groups.Pop();
			if (!keepIfEmpty && node.Children.Count == 0)
			{
				var col = groups.PeekOrDefault()?.Children ?? steps;
				Debug.Assert(col.Last() == node);
				col.RemoveAt(col.Count - 1);
			}
			node.EndStep = step;
		}
	}
}

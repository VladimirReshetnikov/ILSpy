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

using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.Decompiler.IL.ControlFlow;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Defines a transform that operates on a single control-flow <see cref="Block"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The block pipeline visits blocks in dominator-tree order. Implementations can rely on the guarantee that
	/// changes remain local to the current dominance region.
	/// </para>
	/// <para>
	/// Implementations must preserve <see cref="ILPhase.Normal"/> invariants for the modified subtree.
	/// </para>
	/// </remarks>
	public interface IBlockTransform
	{
		/// <summary>
		/// Executes the transform on <paramref name="block"/>.
		/// </summary>
		/// <param name="block">The block currently being transformed.</param>
		/// <param name="context">Shared services and dominance metadata for this invocation.</param>
		/// <remarks>
		/// The transform may only modify <paramref name="block"/>, its descendants, and sibling blocks dominated by
		/// <paramref name="block"/>.
		/// </remarks>
		void Run(Block block, BlockTransformContext context);
	}

	/// <summary>
	/// Supplies per-block state for <see cref="IBlockTransform"/> executions.
	/// </summary>
	/// <remarks>
	/// The context extends <see cref="ILTransformContext"/> with control-flow graph state captured before block
	/// transforms start. The graph is intentionally not rebuilt after each mutation.
	/// </remarks>
	public class BlockTransformContext : ILTransformContext
	{
		/// <summary>
		/// Gets or sets the block currently being processed.
		/// </summary>
		/// <value>
		/// The value should match the block argument passed to
		/// <see cref="IBlockTransform.Run(Block, BlockTransformContext)"/>.
		/// </value>
		public Block Block { get; set; }

		/// <summary>
		/// Gets or sets the control-flow graph node associated with <see cref="Block"/>.
		/// </summary>
		/// <value>
		/// Equivalent to calling <c>ControlFlowGraph.GetNode(Block)</c> on the snapshot graph.
		/// </value>
		/// <remarks>
		/// The graph is created once at the beginning of block transforms (before loop detection), so it is a snapshot
		/// and may not reflect later structural edits.
		/// </remarks>
		public ControlFlowNode ControlFlowNode { get; set; }

		/// <summary>
		/// Gets or sets the control-flow graph snapshot for the current container.
		/// </summary>
		/// <value>
		/// A graph built before block-level mutations for the current <see cref="BlockContainer"/>.
		/// </value>
		public ControlFlowGraph ControlFlowGraph { get; set; }

		/// <summary>
		/// Gets or sets the first instruction index that has already been processed by statement transforms.
		/// </summary>
		/// <value>
		/// Initially <c>Block.Instructions.Count</c> for each visited block. When transforms merge in already-processed
		/// instructions (for example <see cref="ConditionDetection"/>), they update this value so
		/// <see cref="StatementTransform"/> can skip the preserved prefix.
		/// </value>
		public int IndexOfFirstAlreadyTransformedInstruction { get; set; }

		/// <summary>
		/// Initializes a block-transform context that shares state with an existing IL-transform context.
		/// </summary>
		/// <param name="context">Parent context providing settings, type-system state, and step recorder.</param>
		public BlockTransformContext(ILTransformContext context) : base(context)
		{
		}
	}

	/// <summary>
	/// Runs configured <see cref="IBlockTransform"/> passes over every block in a function.
	/// </summary>
	/// <remarks>
	/// The transform walks each <see cref="BlockContainer"/> along its dominator tree, executing
	/// <see cref="PreOrderTransforms"/> before children and <see cref="PostOrderTransforms"/> after children.
	/// </remarks>
	public class BlockILTransform : IILTransform
	{
		/// <summary>
		/// Gets transforms that run during dominator-tree pre-order traversal.
		/// </summary>
		public IList<IBlockTransform> PreOrderTransforms { get; } = new List<IBlockTransform>();

		/// <summary>
		/// Gets transforms that run during dominator-tree post-order traversal.
		/// </summary>
		public IList<IBlockTransform> PostOrderTransforms { get; } = new List<IBlockTransform>();

		bool running;

		/// <summary>
		/// Returns a debugger-oriented description containing the configured child transform type names.
		/// </summary>
		/// <returns>A string that identifies this block transform and its current pass list.</returns>
		public override string ToString()
		{
			return $"{nameof(BlockILTransform)} ({string.Join(", ", PreOrderTransforms.Concat(PostOrderTransforms).Select(t => t.GetType().Name))})";
		}

		/// <summary>
		/// Executes all configured block transforms for each block container in <paramref name="function"/>.
		/// </summary>
		/// <param name="function">Function whose block containers will be visited.</param>
		/// <param name="context">Run context shared across transform stages.</param>
		/// <exception cref="InvalidOperationException">
		/// A previous invocation is still active. <see cref="BlockILTransform"/> is not reentrant.
		/// </exception>
		public void Run(ILFunction function, ILTransformContext context)
		{
			if (running)
				throw new InvalidOperationException("Reentrancy detected. Transforms (and the CSharpDecompiler) are neither thread-safe nor re-entrant.");
			try
			{
				running = true;
				var blockContext = new BlockTransformContext(context);
				Debug.Assert(blockContext.Function == function);
				foreach (var container in function.Descendants.OfType<BlockContainer>().ToList())
				{
					context.CancellationToken.ThrowIfCancellationRequested();
					blockContext.ControlFlowGraph = new ControlFlowGraph(container, context.CancellationToken);
					VisitBlock(blockContext.ControlFlowGraph.GetNode(container.EntryPoint), blockContext);
				}
			}
			finally
			{
				running = false;
			}
		}

		/// <summary>
		/// Walks the dominator subtree rooted at <paramref name="entryNode"/> and applies configured passes.
		/// </summary>
		/// <param name="entryNode">Entry node of the dominator subtree for one block container.</param>
		/// <param name="context">Shared block-transform context for the current container.</param>
		void VisitBlock(ControlFlowNode entryNode, BlockTransformContext context)
		{
			IEnumerable<ControlFlowNode> Preorder(ControlFlowNode cfgNode)
			{
				// preorder processing:
				Block block = (Block)cfgNode.UserData;
				context.StepStartGroup(block.Label, block);

				context.ControlFlowNode = cfgNode;
				context.Block = block;
				context.IndexOfFirstAlreadyTransformedInstruction = block.Instructions.Count;
				block.RunTransforms(PreOrderTransforms, context);

				// process the children
				return cfgNode.DominatorTreeChildren;
			}

			foreach (var cfgNode in TreeTraversal.PostOrder(entryNode, Preorder))
			{
				// in post-order:
				Block block = (Block)cfgNode.UserData;
				context.ControlFlowNode = cfgNode;
				context.Block = block;
				context.IndexOfFirstAlreadyTransformedInstruction = block.Instructions.Count;
				block.RunTransforms(PostOrderTransforms, context);
				context.StepEndGroup();
			}
		}
	}
}

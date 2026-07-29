// Copyright (c) 2015 Daniel Grunwald
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
using System.Threading;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.DebugSteps;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Defines a transform that rewrites a complete <see cref="ILFunction"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations participate in an ordered pipeline, and each transform observes the output of all
	/// previous transforms in that pipeline.
	/// </para>
	/// <para>
	/// Implementations are expected to preserve IL invariants for <see cref="ILPhase.Normal"/> before returning,
	/// because the pipeline validates invariants between transform invocations.
	/// </para>
	/// </remarks>
	public interface IILTransform
	{
		/// <summary>
		/// Executes the transform against <paramref name="function"/>.
		/// </summary>
		/// <param name="function">The function currently being normalized or reduced.</param>
		/// <param name="context">Per-run services and settings shared across transforms.</param>
		void Run(ILFunction function, ILTransformContext context);
	}

	/// <summary>
	/// Parameter class holding various arguments for <see cref="IILTransform.Run(ILFunction, ILTransformContext)"/>.
	/// </summary>
	public class ILTransformContext
	{
		/// <summary>
		/// Gets the function currently being transformed.
		/// </summary>
		public ILFunction Function { get; }

		/// <summary>
		/// Gets the semantic type-system model used by transforms while analyzing metadata and symbols.
		/// </summary>
		public IDecompilerTypeSystem TypeSystem { get; }

		/// <summary>
		/// Gets debug-symbol access for the current decompilation run, if symbols are available.
		/// </summary>
		public IDebugInfoProvider? DebugInfo { get; }

		/// <summary>
		/// Gets the decompiler settings that control transform behavior.
		/// </summary>
		public DecompilerSettings Settings { get; }

		/// <summary>
		/// Gets or sets the cancellation token that transforms should observe during long-running work.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Gets or sets the step recorder used by transform debugging instrumentation.
		/// </summary>
		/// <remarks>
		/// Derived contexts intentionally share the same <see cref="Stepper"/> instance so that nested transforms
		/// contribute to one ordered step stream.
		/// </remarks>
		public Stepper Stepper { get; set; }

		/// <summary>
		/// Gets the metadata file of the main module currently being decompiled.
		/// </summary>
		public Metadata.MetadataFile PEFile => TypeSystem.MainModule.MetadataFile;

		internal DecompileRun? DecompileRun { get; set; }
		internal UsingScope? UsingScope => DecompileRun?.UsingScope;

		CSharpResolver? csharpResolver;

		internal CSharpResolver CSharpResolver {
			get {
				var resolver = LazyInit.VolatileRead(ref csharpResolver);
				if (resolver != null)
					return resolver;
				// Resolve from inside the decompiled member's namespace, as the emitted file will.
				// C# consults enclosing namespaces before using directives, so a bare scope answers
				// name and extension-method lookups differently from the printed code - an extension
				// method declared in an enclosing namespace wins there, invisible to a bare scope.
				// The AST transforms nest their resolver the same way (IntroduceExtensionMethods
				// .InitializeContext); IL transforms predicting those stages must agree with them.
				var usingScope = UsingScope;
				var declaringType = Function?.Method?.DeclaringTypeDefinition;
				if (usingScope != null && !string.IsNullOrEmpty(declaringType?.Namespace))
				{
					foreach (string ns in declaringType.Namespace.Split('.'))
					{
						usingScope = usingScope.WithNestedNamespace(ns);
					}
				}
				return LazyInit.GetOrSet(ref csharpResolver, new CSharpResolver(
					new CSharpTypeResolveContext(TypeSystem.MainModule, usingScope, declaringType)));
			}
		}

		/// <summary>
		/// Initializes a transform context for a specific function.
		/// </summary>
		/// <param name="function">The function that transforms will mutate.</param>
		/// <param name="typeSystem">The type-system graph that owns <paramref name="function"/>.</param>
		/// <param name="debugInfo">Debug-symbol provider to use during transforms, or <see langword="null"/>.</param>
		/// <param name="settings">Decompiler settings for this run. If <see langword="null"/>, default settings are used.</param>
		/// <exception cref="ArgumentNullException"><paramref name="function"/> or <paramref name="typeSystem"/> is <see langword="null"/>.</exception>
		public ILTransformContext(ILFunction function, IDecompilerTypeSystem typeSystem, IDebugInfoProvider? debugInfo, DecompilerSettings? settings = null)
		{
			this.Function = function ?? throw new ArgumentNullException(nameof(function));
			this.TypeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
			this.Settings = settings ?? new DecompilerSettings();
			this.DebugInfo = debugInfo;
			Stepper = new Stepper();
		}

		/// <summary>
		/// Initializes a child context that reuses state from another context.
		/// </summary>
		/// <param name="context">The context to copy shared services and run state from.</param>
		/// <param name="function">Optional replacement for <see cref="Function"/>. If <see langword="null"/>, the original function is kept.</param>
		public ILTransformContext(ILTransformContext context, ILFunction? function = null)
		{
			this.Function = function ?? context.Function;
			this.TypeSystem = context.TypeSystem;
			this.DebugInfo = context.DebugInfo;
			this.Settings = context.Settings;
			this.DecompileRun = context.DecompileRun;
			this.CancellationToken = context.CancellationToken;
			this.Stepper = context.Stepper;
		}

		/// <summary>
		/// Creates an <see cref="ILReader"/> configured to decode additional methods in the same module.
		/// </summary>
		/// <returns>
		/// A new reader initialized with <see cref="Settings"/> and <see cref="DebugInfo"/> from this context.
		/// </returns>
		internal ILReader CreateILReader()
		{
			return new ILReader(TypeSystem.MainModule) {
				UseDebugSymbols = Settings.UseDebugSymbols,
				UseRefLocalsForAccurateOrderOfEvaluation = Settings.UseRefLocalsForAccurateOrderOfEvaluation,
				DebugInfo = DebugInfo
			};
		}

		/// <summary>
		/// Records a single transform step in STEP-enabled builds.
		/// </summary>
		/// <param name="description">Human-readable label for the step.</param>
		/// <param name="near">Instruction near which the step occurred, or <see langword="null"/>.</param>
		/// <remarks>
		/// Unlike direct calls to <see cref="Stepper.Step(string, DebugStepNodeInfo?)"/>, calls to this wrapper are removed
		/// from non-STEP builds due to the <see cref="ConditionalAttribute"/> on the method.
		/// </remarks>
		[Conditional("STEP")]
		[DebuggerStepThrough]
		internal void Step(string description, ILInstruction? near)
		{
			Stepper.Step(description, CreateNodeInfo(near));
		}

		/// <summary>
		/// Starts a grouped step region in STEP-enabled builds.
		/// </summary>
		/// <param name="description">Group label used in the step tree.</param>
		/// <param name="near">Instruction near which the group starts, or <see langword="null"/>.</param>
		[Conditional("STEP")]
		[DebuggerStepThrough]
		internal void StepStartGroup(string description, ILInstruction? near = null)
		{
			Stepper.StartGroup(description, CreateNodeInfo(near));
		}

		/// <summary>
		/// Ends the current grouped step region in STEP-enabled builds.
		/// </summary>
		/// <param name="keepIfEmpty">
		/// <see langword="true"/> to keep empty groups in the recorded output; otherwise empty groups are removed.
		/// </param>
		[Conditional("STEP")]
		internal void StepEndGroup(bool keepIfEmpty = false)
		{
			Stepper.EndGroup(keepIfEmpty);
		}

		/// <summary>
		/// Points the most recently recorded step at the instruction its mutation produced.
		/// Call this after a <see cref="Step"/> whose modified instruction only comes into
		/// existence during the mutation (e.g. the result of a ReplaceWith or a freshly
		/// inserted instruction). The step already carries the original position and its
		/// ancestors as fallback candidates (see <see cref="Stepper"/>); this prepends the
		/// produced instruction so it is preferred.
		/// </summary>
		[Conditional("STEP")]
		internal void EndStep(ILInstruction? modifiedNode)
		{
			if (Stepper.LastStep is { } step && modifiedNode != null)
			{
				step.ModifiedNode = modifiedNode;
				step.RecordModifiedNode(modifiedNode, insertFirst: true);
			}
		}

		static DebugStepNodeInfo? CreateNodeInfo(ILInstruction? instruction)
		{
			if (instruction == null)
				return null;
			object? next = null, previous = null;
			if (instruction.Parent is { } parent)
			{
				int index = instruction.ChildIndex;
				if (index + 1 < parent.Children.Count)
					next = parent.Children[index + 1];
				if (index - 1 >= 0)
					previous = parent.Children[index - 1];
			}
			return new DebugStepNodeInfo(instruction, next, previous, Ancestors(instruction));

			static IEnumerable<object> Ancestors(ILInstruction instruction)
			{
				for (var node = instruction.Parent; node != null; node = node.Parent)
					yield return node;
			}
		}
	}
}

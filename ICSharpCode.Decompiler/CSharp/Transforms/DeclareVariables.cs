// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Insert variable declarations.
	/// </summary>
	public class DeclareVariables : IAstTransform
	{
		/// <summary>
		/// Represents a position immediately before nextNode.
		/// nextNode is either an ExpressionStatement in a BlockStatement, or an initializer in a for-loop.
		/// </summary>
		[DebuggerDisplay("level = {level}, nextNode = {nextNode}")]
		struct InsertionPoint
		{
			/// <summary>
			/// The nesting level of `nextNode` within the AST.
			/// Used to speed up FindCommonParent().
			/// </summary>
			internal int level;
			internal AstNode nextNode;

			/// <summary>Go up one level</summary>
			internal InsertionPoint Up()
			{
				return new InsertionPoint {
					level = level - 1,
					// Insertion points live inside a method body, so walking up always finds a parent.
					nextNode = nextNode.Parent!
				};
			}

			internal InsertionPoint UpTo(int targetLevel)
			{
				InsertionPoint result = this;
				while (result.level > targetLevel)
				{
					result.nextNode = result.nextNode.Parent!;
					result.level -= 1;
				}
				return result;
			}
		}

		enum VariableInitKind
		{
			None,
			NeedsDefaultValue,
			NeedsSkipInit
		}

		[DebuggerDisplay("VariableToDeclare(Name={Name})")]
		class VariableToDeclare
		{
			public readonly ILVariable ILVariable;
			public IType Type => ILVariable.Type;
			// Variables reaching this transform have already been assigned a name.
			public string Name => ILVariable.Name!;

			/// <summary>
			/// Whether the variable needs to be default-initialized.
			/// </summary>
			public VariableInitKind DefaultInitialization;

			/// <summary>
			/// Integer value that can be used to compare to VariableToDeclare instances
			/// to determine which variable was used first in the source code.
			/// 
			/// The variable with the lower SourceOrder value has the insertion point
			/// that comes first in the source code.
			/// </summary>
			public int SourceOrder;

			/// <summary>
			/// The insertion point, i.e. the node before which the variable declaration should be inserted.
			/// </summary>
			public InsertionPoint InsertionPoint;

			/// <summary>
			/// The first use of the variable.
			/// </summary>
			public IdentifierExpression FirstUse;

			public VariableToDeclare? ReplacementDueToCollision;
			public bool InvolvedInCollision;
			public bool RemovedDueToCollision => ReplacementDueToCollision != null;
			public bool DeclaredInDeconstruction;

			public VariableToDeclare(ILVariable variable, InsertionPoint insertionPoint, IdentifierExpression firstUse, int sourceOrder)
			{
				this.ILVariable = variable;
				if (variable.UsesInitialValue
					|| (variable.StoreCount == 0 && variable.AddressCount == 0 && variable.LoadCount > 0))
				{
					// Earlier transforms can remove a closure local's sole definition while a
					// preserved nested method still captures it. Such cross-function loads are
					// not always reflected in UsesInitialValue, but C# still requires a definite
					// assignment before the nested function can read the local (CS0165).
					if (variable.InitialValueIsInitialized)
					{
						this.DefaultInitialization = VariableInitKind.NeedsDefaultValue;
					}
					else
					{
						this.DefaultInitialization = VariableInitKind.NeedsSkipInit;
					}
				}
				else
				{
					this.DefaultInitialization = VariableInitKind.None;
				}
				this.InsertionPoint = insertionPoint;
				this.FirstUse = firstUse;
				this.SourceOrder = sourceOrder;
			}
		}

		readonly Dictionary<ILVariable, VariableToDeclare> variableDict = new Dictionary<ILVariable, VariableToDeclare>();
		[AllowNull]
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			try
			{
				if (this.context != null)
					throw new InvalidOperationException("Reentrancy in DeclareVariables?");
				this.context = context;
				variableDict.Clear();
				EnsureExpressionStatementsAreValid(rootNode);
				FindInsertionPoints(rootNode, 0);
				MoveDeclarationsOutsideGotoRanges(rootNode);
				ResolveCollisions();
				InsertDeconstructionVariableDeclarations();
				InsertVariableDeclarations(context);
				UpdateAnnotations(rootNode);
			}
			finally
			{
				this.context = null;
				variableDict.Clear();
			}
		}
		/// <summary>
		/// Analyze the input AST (containing undeclared variables)
		/// for where those variables would be declared by this transform.
		/// Analysis does not modify the AST.
		/// </summary>
		public void Analyze(AstNode rootNode)
		{
			variableDict.Clear();
			FindInsertionPoints(rootNode, 0);
			MoveDeclarationsOutsideGotoRanges(rootNode);
			ResolveCollisions();
		}

		/// <summary>
		/// Get the position where the declaration for the variable will be inserted.
		/// </summary>
		public AstNode GetDeclarationPoint(ILVariable variable)
		{
			VariableToDeclare v = variableDict[variable];
			while (v.ReplacementDueToCollision != null)
			{
				v = v.ReplacementDueToCollision;
			}
			return v.InsertionPoint.nextNode;
		}

		/// <summary>
		/// Determines whether a variable was merged with other variables.
		/// </summary>
		public bool WasMerged(ILVariable variable)
		{
			VariableToDeclare v = variableDict[variable];
			return v.InvolvedInCollision || v.RemovedDueToCollision;
		}

		public void ClearAnalysisResults()
		{
			variableDict.Clear();
		}

		#region EnsureExpressionStatementsAreValid
		void EnsureExpressionStatementsAreValid(AstNode rootNode)
		{
			foreach (var stmt in rootNode.DescendantsAndSelf.OfType<ExpressionStatement>().ToArray())
			{
				if (stmt.Expression is DirectionExpression dir && IsValidInStatementExpression(dir.Expression))
				{
					context.Step("Unwrap direction expression statement", stmt);
					stmt.Expression = dir.Expression.Detach();
				}
				else if (stmt.Expression is NullReferenceExpression or PrimitiveExpression
					&& stmt.Parent is BlockStatement)
				{
					// A literal on its own does nothing and is not a statement C# accepts. Assigning it
					// to a discard is no better: a discard has no type to infer from 'null' (CS8183).
					context.Step("Remove literal expression statement", stmt);
					stmt.Remove();
				}
				else if (!IsValidInStatementExpression(stmt.Expression))
				{
					// fetch ILFunction
					var function = stmt.Ancestors.SelectMany(a => a.Annotations.OfType<ILFunction>()).First(f => f.Parent == null);
					// if possible use C# 7.0 discard-assignment
					if (context.Settings.Discards && !ExpressionBuilder.HidesVariableWithName(function, "_"))
					{
						context.Step("Assign invalid expression statement to discard", stmt);
						stmt.Expression = new AssignmentExpression(
							new IdentifierExpression("_"), // no ResolveResult
							stmt.Expression.Detach());
					}
					else
					{
						context.Step("Assign invalid expression statement to temporary", stmt);
						// assign result to dummy variable
						var type = stmt.Expression.GetResolveResult().Type;
						var v = function.RegisterVariable(
							VariableKind.StackSlot,
							type,
							AssignVariableNames.GenerateVariableName(function, type, context.DecompileRun.UsingScope,
								stmt.Expression.Annotations.OfType<ILInstruction>()
									.Where(AssignVariableNames.IsSupportedInstruction).FirstOrDefault(),
								mustResolveConflicts: true)
						);
						stmt.Expression = new AssignmentExpression(
							new IdentifierExpression(v.Name!).WithRR(new ILVariableResolveResult(v, v.Type)),
							stmt.Expression.Detach());
					}
				}
			}
		}

		private static bool IsValidInStatementExpression(Expression expr)
		{
			switch (expr)
			{
				case InvocationExpression _:
				case ObjectCreateExpression _:
				case AssignmentExpression _:
				case ErrorExpression _:
					return true;
				case UnaryOperatorExpression uoe:
					switch (uoe.Operator)
					{
						case UnaryOperatorType.PostIncrement:
						case UnaryOperatorType.PostDecrement:
						case UnaryOperatorType.Increment:
						case UnaryOperatorType.Decrement:
						case UnaryOperatorType.Await:
							return true;
						case UnaryOperatorType.NullConditionalRewrap:
							return IsValidInStatementExpression(uoe.Expression);
						default:
							return false;
					}
				default:
					return false;
			}
		}
		#endregion

		#region FindInsertionPoints
		List<(InsertionPoint InsertionPoint, BlockContainer Scope)> scopeTracking = new List<(InsertionPoint, BlockContainer)>();

		/// <summary>
		/// Finds insertion points for all variables used within `node`
		/// and adds them to the variableDict.
		/// 
		/// `level` == nesting depth of `node` within root node.
		/// </summary>
		/// <remarks>
		/// Insertion point for a variable = common parent of all uses of that variable
		/// = smallest possible scope that contains all the uses of the variable
		/// </remarks>
		void FindInsertionPoints(AstNode node, int nodeLevel)
		{
			BlockContainer? scope = node.Annotation<BlockContainer>();
			if (scope != null && IsRelevantScope(scope))
			{
				// track loops and function bodies as scopes, for comparison with CaptureScope.
				scopeTracking.Add((new InsertionPoint { level = nodeLevel, nextNode = node }, scope));
			}
			else if (node is LambdaExpression { Body: Expression expr })
			{
				// expression-bodied lambdas don't have a BlockStatement linking to the BlockContainer
				scope = node.Annotation<ILFunction>()?.Body as BlockContainer;
				if (scope != null)
				{
					scopeTracking.Add((new InsertionPoint { level = nodeLevel + 1, nextNode = expr }, scope));
				}
			}
			else
			{
				scope = null; // don't remove a scope if we didn't add one
			}
			try
			{
				for (AstNode? child = node.FirstChild; child != null; child = child.NextSibling)
				{
					FindInsertionPoints(child, nodeLevel + 1);
				}
				if (node is IdentifierExpression identExpr)
				{
					var rr = identExpr.GetResolveResult() as ILVariableResolveResult;
					if (rr != null && VariableNeedsDeclaration(rr.Variable.Kind))
					{
						FindInsertionPointForVariable(rr.Variable);
					}
					else if (identExpr.Annotation<ILFunction>() is ILFunction localFunction && localFunction.Kind == ILFunctionKind.LocalFunction)
					{
						foreach (var v in localFunction.CapturedVariables)
						{
							if (VariableNeedsDeclaration(v.Kind))
								FindInsertionPointForVariable(v);
						}
					}

					void FindInsertionPointForVariable(ILVariable variable)
					{
						InsertionPoint newPoint;
						int startIndex = scopeTracking.Count - 1;
						BlockContainer? captureScope = variable.CaptureScope;
						while (captureScope != null && !IsRelevantScope(captureScope))
						{
							captureScope = BlockContainer.FindClosestContainer(captureScope.Parent);
						}
						if (captureScope != null && startIndex > 0 && captureScope != scopeTracking[startIndex].Scope)
						{
							while (startIndex > 0 && scopeTracking[startIndex].Scope != captureScope)
								startIndex--;
							newPoint = scopeTracking[startIndex + 1].InsertionPoint;
						}
						else
						{
							newPoint = new InsertionPoint { level = nodeLevel, nextNode = identExpr };
							if (variable.UsesInitialValue)
							{
								// Uninitialized variables are logically initialized at the beginning of the function
								// Because it's possible that the variable has a loop-carried dependency,
								// declare it outside of any loops.
								while (startIndex >= 0)
								{
									if (scopeTracking[startIndex].Scope.EntryPoint.IncomingEdgeCount > 1)
									{
										// declare variable outside of loop
										newPoint = scopeTracking[startIndex].InsertionPoint;
									}
									else if (scopeTracking[startIndex].Scope.Parent is ILFunction)
									{
										// stop at beginning of function
										break;
									}
									startIndex--;
								}
							}
						}
						if (variableDict.TryGetValue(variable, out var v))
						{
							v.InsertionPoint = FindCommonParent(v.InsertionPoint, newPoint);
						}
						else
						{
							v = new VariableToDeclare(variable, newPoint,
								identExpr, sourceOrder: variableDict.Count);
							variableDict.Add(variable, v);
						}
					}
				}
			}
			finally
			{
				if (scope != null)
					scopeTracking.RemoveAt(scopeTracking.Count - 1);
			}
		}

		void MoveDeclarationsOutsideGotoRanges(AstNode rootNode)
		{
			var nodes = rootNode.DescendantsAndSelf.ToList();
			var sourceOrder = nodes.Select((node, index) => (node, index))
				.ToDictionary(item => item.node, item => item.index);
			var labels = nodes.OfType<LabelStatement>()
				.ToDictionary(label => (Scope: GetControlFlowScope(label), label.Label));
			var branches = nodes.OfType<GotoStatement>()
				.Where(gotoStatement => gotoStatement.Label != null
					&& labels.ContainsKey((GetControlFlowScope(gotoStatement), gotoStatement.Label)))
				.Select(gotoStatement => (Goto: gotoStatement,
					Target: labels[(GetControlFlowScope(gotoStatement), gotoStatement.Label!)]))
				.ToList();

			foreach (var variable in variableDict.Values)
			{
				while (true)
				{
					bool moved = false;
					AstNode variableScope = GetControlFlowScope(variable.InsertionPoint.nextNode);
					foreach (var branch in branches.Where(branch => GetControlFlowScope(branch.Goto) == variableScope))
					{
						int declarationOrder = sourceOrder[variable.InsertionPoint.nextNode];
						int gotoOrder = sourceOrder[branch.Goto];
						int targetOrder = sourceOrder[branch.Target];
						int firstOrder = Math.Min(gotoOrder, targetOrder);
						int lastOrder = Math.Max(gotoOrder, targetOrder);
						if (declarationOrder <= firstOrder || declarationOrder >= lastOrder)
							continue;

						AstNode firstNode = gotoOrder < targetOrder ? branch.Goto : branch.Target;
						var firstPoint = new InsertionPoint {
							level = GetNodeLevel(firstNode),
							nextNode = firstNode
						};
						variable.InsertionPoint = FindCommonParent(firstPoint, variable.InsertionPoint);
						variable.DefaultInitialization = variable.ILVariable.InitialValueIsInitialized
							? VariableInitKind.NeedsDefaultValue
							: VariableInitKind.NeedsSkipInit;
						moved = true;
						break;
					}
					if (!moved)
						break;
				}
			}

			AstNode GetControlFlowScope(AstNode node)
			{
				return node.AncestorsAndSelf.FirstOrDefault(ancestor => ancestor is LambdaExpression
					or AnonymousMethodExpression or LocalFunctionDeclarationStatement or EntityDeclaration) ?? rootNode;
			}

			static int GetNodeLevel(AstNode node)
			{
				int level = 0;
				for (AstNode? parent = node.Parent; parent != null; parent = parent.Parent)
					level++;
				return level;
			}
		}

		private static bool IsRelevantScope(BlockContainer scope)
		{
			return scope.EntryPoint.IncomingEdgeCount > 1 || scope.Parent is ILFunction;
		}

		internal static bool VariableNeedsDeclaration(VariableKind kind)
		{
			switch (kind)
			{
				case VariableKind.PinnedRegionLocal:
				case VariableKind.Parameter:
				case VariableKind.ExceptionLocal:
				case VariableKind.ExceptionStackSlot:
				case VariableKind.UsingLocal:
				case VariableKind.ForeachLocal:
				case VariableKind.PatternLocal:
					return false;
				default:
					return true;
			}
		}

		/// <summary>
		/// Finds an insertion point in a common parent instruction.
		/// </summary>
		InsertionPoint FindCommonParent(InsertionPoint oldPoint, InsertionPoint newPoint)
		{
			// First ensure we're looking at nodes on the same level:
			oldPoint = oldPoint.UpTo(newPoint.level);
			newPoint = newPoint.UpTo(oldPoint.level);
			Debug.Assert(newPoint.level == oldPoint.level);
			// Then go up the tree until both points share the same parent:
			while (oldPoint.nextNode.Parent != newPoint.nextNode.Parent)
			{
				oldPoint = oldPoint.Up();
				newPoint = newPoint.Up();
			}
			// return oldPoint as that one comes first in the source code
			return oldPoint;
		}
		#endregion

		/// <summary>
		/// Some variable declarations in C# are illegal (colliding),
		/// even though the variable live ranges are not overlapping.
		/// 
		/// Multiple declarations in same block:
		/// <code>
		/// int i = 1; use(1);
		/// int i = 2; use(2);
		/// </code>
		/// 
		/// "Hiding" declaration in nested block:
		/// <code>
		/// int i = 1; use(1);
		/// if (...) {
		///   int i = 2; use(2);
		/// }
		/// </code>
		/// 
		/// Nested blocks are illegal even if the parent block
		/// declares the variable later:
		/// <code>
		/// if (...) {
		///   int i = 1; use(i);
		/// }
		/// int i = 2; use(i);
		/// </code>
		/// 
		/// ResolveCollisions() detects all these cases, and combines the variable declarations
		/// to a single declaration that is usable for the combined scopes.
		/// </summary>
		void ResolveCollisions()
		{
			var multiDict = new MultiDictionary<string, VariableToDeclare>();
			foreach (var v in variableDict.Values)
			{
				// We can only insert variable declarations in blocks, but FindInsertionPoints() didn't
				// guarantee that it finds only blocks.
				// Fix that up now.
				while (!(v.InsertionPoint.nextNode.Parent is BlockStatement or LambdaExpression))
				{
					if (v.InsertionPoint.nextNode.Parent is ForStatement f && v.InsertionPoint.nextNode == f.Initializers.FirstOrDefault() && IsMatchingAssignment(v, out _))
					{
						// Special case: the initializer of a ForStatement can also declare a variable (with scope local to the for loop).
						break;
					}
					v.InsertionPoint = v.InsertionPoint.Up();
				}
				// Note: 'out var', pattern matching etc. is not considered a valid insertion point here, because the scope of the
				// resulting variable is not restricted to the parent node of the insertion point, but extends to the whole BlockStatement.
				// We moved up the insertion point to the whole BlockStatement so that we can resolve collisions,
				// later we might decide to declare the variable more locally (as 'out var') instead if still possible.

				// Go through all potentially colliding variables:
				foreach (var prev in multiDict[v.Name])
				{
					if (prev.RemovedDueToCollision)
						continue;
					// A collision can only be resolved by merging two declarations into one, which is
					// valid only within a single method body. Two variables that belong to different
					// ILFunctions live in separate C# declaration spaces (a lambda or local-function body
					// versus its enclosing method), where reusing an enclosing name is legal and no merge
					// is needed. Merging across that boundary would move the inner variable's declaration
					// out into the enclosing scope, so its uses would bind to the enclosing local - which
					// inside a static local function is illegal (CS8421).
					if (prev.ILVariable.Function != v.ILVariable.Function)
						continue;
					// Go up until both nodes are on the same level:
					InsertionPoint point1 = prev.InsertionPoint.UpTo(v.InsertionPoint.level);
					InsertionPoint point2 = v.InsertionPoint.UpTo(prev.InsertionPoint.level);
					Debug.Assert(point1.level == point2.level);
					if (point1.nextNode.Parent == point2.nextNode.Parent)
					{
						Debug.Assert(prev.Type.Equals(v.Type));
						// We found a collision!
						v.InvolvedInCollision = true;
						prev.ReplacementDueToCollision = v;
						// Continue checking other entries in multiDict against the new position of `v`.
						if (prev.SourceOrder < v.SourceOrder)
						{
							// Switch v's insertion point to prev's insertion point:
							v.InsertionPoint = point1;
							// Since prev was first, it has the correct SourceOrder/FirstUse values
							// for the new combined variable:
							v.SourceOrder = prev.SourceOrder;
							v.FirstUse = prev.FirstUse;
						}
						else
						{
							// v is first in source order, so it keeps its old insertion point
							// (and other properties), except that the insertion point is
							// moved up to prev's level.
							v.InsertionPoint = point2;
						}
						v.DefaultInitialization |= prev.DefaultInitialization;
						// I think we don't need to re-check the dict entries that we already checked earlier,
						// because the new v.InsertionPoint only collides with another point x if either
						// the old v.InsertionPoint or the old prev.InsertionPoint already collided with x.
					}
				}

				multiDict.Add(v.Name, v);
			}
		}

		private void InsertDeconstructionVariableDeclarations()
		{
			var usedVariables = new HashSet<ILVariable>();
			foreach (var g in variableDict.Values.GroupBy(v => v.InsertionPoint.nextNode))
			{
				if (!(g.Key is ExpressionStatement { Expression: AssignmentExpression { Left: TupleExpression left, Operator: AssignmentOperatorType.Assign } assignment }))
					continue;
				usedVariables.Clear();
				var deconstruct = assignment.Annotation<DeconstructInstruction>();
				if (deconstruct == null || deconstruct.Init.Count > 0 || deconstruct.Conversions.Instructions.Count > 0)
					continue;
				if (!deconstruct.Assignments.Instructions.All(IsDeclarableVariable))
					continue;

				var designation = StatementBuilder.TranslateDeconstructionDesignation(deconstruct, isForeach: false);
				context.Step("Declare deconstruction variables", left);
				var declarationExpression = new DeclarationExpression { Type = new SimpleType("var"), Designation = designation };
				left.ReplaceWith(declarationExpression);
				context.EndStep(declarationExpression);

				foreach (var v in usedVariables)
				{
					variableDict[v].DeclaredInDeconstruction = true;
				}

				bool IsDeclarableVariable(ILInstruction inst)
				{
					if (!inst.MatchStLoc(out var v, out var value))
						return false;
					if (!g.Any(vd => vd.ILVariable == v && !vd.RemovedDueToCollision))
						return false;
					if (!usedVariables.Add(v))
						return false;
					var expectedType = ((LdLoc)value).Variable.Type;
					if (!v.Type.Equals(expectedType))
						return false;
					if (!(v.Kind == VariableKind.StackSlot || v.Kind == VariableKind.Local))
						return false;
					return true;
				}
			}
		}

		bool IsMatchingAssignment(VariableToDeclare v, [NotNullWhen(true)] out AssignmentExpression? assignment)
		{
			assignment = v.InsertionPoint.nextNode as AssignmentExpression;
			if (assignment == null)
			{
				assignment = (v.InsertionPoint.nextNode as ExpressionStatement)?.Expression as AssignmentExpression;
				if (assignment == null)
					return false;
			}
			return assignment.Operator == AssignmentOperatorType.Assign
				&& assignment.Left is IdentifierExpression identExpr
				&& identExpr.Identifier == v.Name
				&& identExpr.TypeArguments.Count == 0;
		}

		bool CombineDeclarationAndInitializer(VariableToDeclare v, TransformContext context)
		{
			if (v.Type.IsByRefLike)
				return true; // by-ref-like variables always must be initialized at their declaration.

			if (v.InsertionPoint.nextNode.Slot?.Kind == Slots.ForInitializer)
				return true; // for-statement initializers always should combine declaration and initialization.

			return !context.Settings.SeparateLocalVariableDeclarations;
		}

		/// <summary>
		/// Whether the right-hand side of a matching declaration+initializer assignment reads the
		/// variable being declared - directly, or through the body of a nested anonymous method,
		/// lambda, or local function that captures it. Such a self-reference makes the merged
		/// single-initializer form 'T v = initializer;' illegal (CS0165): inside its own initializer
		/// the variable is not definitely assigned, so a read of it there is a use of an unassigned local.
		/// </summary>
		bool InitializerReferencesVariable(AssignmentExpression assignment, VariableToDeclare v)
		{
			// DescendantsAndSelf walks into nested anonymous-method/lambda bodies, so a read of the
			// variable from within an inline closure captured by the initializer is found here.
			foreach (AstNode node in assignment.Right.DescendantsAndSelf)
			{
				if (node is not IdentifierExpression identifier)
					continue;
				if (ResolveVariableToDeclare(identifier.GetILVariable()) == v)
					return true;
				// A local function is referenced by name rather than inlined; its body is not a
				// descendant of the initializer. Detect the self-reference by inspecting whether the
				// referenced local function captures the variable being declared.
				if (identifier.Annotation<ILFunction>() is { Kind: ILFunctionKind.LocalFunction } localFunction
					&& localFunction.CapturedVariables.Any(cv => ResolveVariableToDeclare(cv) == v))
				{
					return true;
				}
			}
			return false;
		}

		void InsertVariableDeclarations(TransformContext context)
		{
			var replacements = new List<(AstNode OldNode, Func<AstNode> CreateNewNode, string StepDescription)>();
			foreach (var (ilVariable, v) in variableDict)
			{
				if (v.RemovedDueToCollision || v.DeclaredInDeconstruction)
					continue;

				bool splitSelfReference = CombineDeclarationAndInitializer(v, context)
					&& IsMatchingAssignment(v, out var assignment)
					&& !v.Type.IsByRefLike
					&& v.InsertionPoint.nextNode is ExpressionStatement
					&& InitializerReferencesVariable(assignment, v);
				if (splitSelfReference && v.DefaultInitialization == VariableInitKind.None)
				{
					// A closure-captured local whose sole initializer reads the variable itself
					// (through the body of a nested anonymous method, lambda, or local function)
					// cannot be merged into 'T v = <init>;': inside its own initializer the
					// variable is not definitely assigned, so the merged form fails to compile
					// (CS0165). Force a default-initialized declaration followed by the plain
					// assignment, e.g. 'T v = default; v = <init>;'.
					v.DefaultInitialization = VariableInitKind.NeedsDefaultValue;
				}
				if (!splitSelfReference && CombineDeclarationAndInitializer(v, context) && IsMatchingAssignment(v, out assignment))
				{
					// 'int v; v = expr;' can be combined to 'int v = expr;'
					AstType type;
					if (context.Settings.AnonymousTypes && v.Type.ContainsAnonymousType())
					{
						type = new SimpleType("var");
					}
					else
					{
						type = context.TypeSystemAstBuilder.ConvertType(v.Type);
					}
					if (v.ILVariable.IsRefReadOnly && type is ComposedType composedType && composedType.HasRefSpecifier)
					{
						composedType.HasReadOnlySpecifier = true;
					}
					if (v.ILVariable.Kind == VariableKind.PinnedLocal)
					{
						type.AddTrailingTrivia(new Comment("pinned", CommentType.MultiLine));
					}
					replacements.Add((v.InsertionPoint.nextNode, () => {
						var vds = new VariableDeclarationStatement(type, v.Name, assignment.Right.Detach());
						var init = vds.Variables.Single();
						init.AddAnnotation(assignment.Left.GetResolveResult());
						foreach (object annotation in assignment.Left.Annotations.Concat(assignment.Annotations))
						{
							if (!(annotation is ResolveResult))
							{
								init.AddAnnotation(annotation);
							}
						}
						return vds;
					}, "Combine variable declaration with initializer"));
				}
				else if (CanBeDeclaredAsOutVariable(v, out var dirExpr))
				{
					// 'T v; SomeCall(out v);' can be combined to 'SomeCall(out T v);'
					AstType type;
					bool isOutVar = false;
					if (context.Settings.AnonymousTypes && v.Type.ContainsAnonymousType())
					{
						type = new SimpleType("var");
						isOutVar = true;
					}
					else if (dirExpr.Annotation<UseImplicitlyTypedOutAnnotation>() != null
						&& !IsReferencedWithinDeclaringCall(dirExpr, v))
					{
						type = new SimpleType("var");
						isOutVar = true;
					}
					else
					{
						type = context.TypeSystemAstBuilder.ConvertType(v.Type);
					}
					string name;
					// Variable is not used and discards are allowed, we can simplify this to 'out T _'.
					// TODO: if no variable named _ is declared and var is used instead of T, use out _.
					// Note: ExpressionBuilder.HidesVariableWithName produces inaccurate results, because it
					// does not take lambdas and local functions into account, that are defined in the same
					// scope as v.
					// A collision merge combines multiple IL live ranges into one C# local. Even if
					// the surviving range is unused, replacing its declaration with a discard would
					// leave references from the other merged ranges without a declaration.
					if (context.Settings.Discards && !v.InvolvedInCollision && v.ILVariable.LoadCount == 0
						&& v.ILVariable.StoreCount == 0 && v.ILVariable.AddressCount == 1)
					{
						name = "_";
					}
					else
					{
						name = v.Name;
					}
					var ovd = new OutVarDeclarationExpression(type, name);
					ovd.Variable.AddAnnotation(new ILVariableResolveResult(ilVariable));
					ovd.CopyAnnotationsFrom(dirExpr);
					if (isOutVar)
					{
						ovd.RemoveAnnotations<ResolveResult>();
						ovd.AddAnnotation(new OutVarResolveResult(v.Type));
					}
					replacements.Add((dirExpr, () => ovd, "Declare out variable"));
				}
				else
				{
					// Insert a separate declaration statement.
					Expression? initializer = null;
					AstType type = context.TypeSystemAstBuilder.ConvertType(v.Type);
					if (v.Type.Kind != TypeKind.ByReference
						&& (v.DefaultInitialization == VariableInitKind.NeedsDefaultValue
							|| (v.DefaultInitialization == VariableInitKind.NeedsSkipInit && v.Type.Kind == TypeKind.Pointer)))
					{
						// A pointer cannot be a generic type argument, so Unsafe.SkipInit<T> has no
						// spelling for one (CS0306). A null pointer is the closest stand-in: the local
						// is assigned before it is read either way, so what it starts as is not
						// observable.
						initializer = new DefaultValueExpression(type.Clone());
					}
					var vds = new VariableDeclarationStatement(type, v.Name, initializer);
					vds.Variables.Single().AddAnnotation(new ILVariableResolveResult(ilVariable));
					if (context.Settings.ScopedRef && v.Type.IsByRefLike
						&& ShouldDeclareByRefLikeLocalScoped(v))
					{
						vds.IsScopedRef = true;
					}
					context.Step("Insert variable declaration", v.InsertionPoint.nextNode);
					if (v.InsertionPoint.nextNode.Parent is LambdaExpression lambda)
					{
						Debug.Assert(lambda.Body is not BlockStatement);
						lambda.Body = new BlockStatement() {
							new ReturnStatement((Expression)lambda.Body.Detach())
						};
					}
					if (v.InsertionPoint.nextNode.Parent is ReturnStatement)
					{
						v.InsertionPoint = v.InsertionPoint.Up();
					}
					Debug.Assert(v.InsertionPoint.nextNode.Slot?.Kind == Slots.Statement);
					// The insertion point is a statement within a block, so it always has a parent.
					AstNode insertionNode = v.InsertionPoint.nextNode;
					AstNode insertionParent = insertionNode.Parent
						?? throw new InvalidOperationException("Variable insertion point has no parent.");
					if (v.Type.Kind == TypeKind.ByReference
						|| (v.DefaultInitialization == VariableInitKind.NeedsSkipInit && v.Type.Kind != TypeKind.Pointer))
					{
						AstType unsafeType = context.TypeSystemAstBuilder.ConvertType(
							context.TypeSystem.FindType(KnownTypeCode.Unsafe));
						AstNode insertedNode;
						if (v.Type.Kind == TypeKind.ByReference)
						{
							// A ref local cannot be left uninitialized in C#, whatever the definite
							// assignment analysis says, and it cannot be passed as an 'out' argument:
							// 'ref T x;' (CS8174) and 'Unsafe.SkipInit(out ref T x)' (CS8388) are both
							// illegal. The IL leaves the managed pointer unassigned and reassigns it (via
							// 'x = ref ...') before any read, so bind the ref local to a null reference at
							// its declaration. 'ref T x = ref Unsafe.NullRef<T>();' is the closest legal
							// equivalent and keeps the later ref reassignments valid.
							//
							// 'Unsafe.NullRef<T>()' returns a reference whose ref-safe-context is the
							// calling method, so a plain 'ref T x' would reject a later reassignment from a
							// method-local reference (CS8374). Such a ref local starts unassigned, so it
							// never legitimately escapes; declare it 'scoped' to narrow its ref-safe-context
							// enough to admit those reassignments.
							if (v.ILVariable.IsRefReadOnly && type is ComposedType composedRefType
								&& composedRefType.HasRefSpecifier)
							{
								composedRefType.HasReadOnlySpecifier = true;
							}
							AstType elementType = context.TypeSystemAstBuilder.ConvertType(
								((ByReferenceType)v.Type).ElementType);
							vds.Variables.Single().Initializer = new DirectionExpression(
								FieldDirection.Ref,
								new InvocationExpression {
									Target = new MemberReferenceExpression {
										Target = new TypeReferenceExpression(unsafeType),
										MemberName = "NullRef",
										TypeArguments = { elementType }
									}
								});
							if (context.Settings.ScopedRef)
							{
								vds.IsScopedRef = true;
							}
							insertionParent.InsertChildBefore(
								v.InsertionPoint.nextNode,
								vds,
								Slots.Statement);
							insertedNode = vds;
						}
						else if (context.Settings.OutVariables)
						{
							var outVarDecl = new OutVarDeclarationExpression(type.Clone(), v.Name);
							outVarDecl.Variable.AddAnnotation(new ILVariableResolveResult(ilVariable));
							var skipInitStatement = new ExpressionStatement {
								Expression = new InvocationExpression {
									Target = new MemberReferenceExpression {
										Target = new TypeReferenceExpression(unsafeType),
										MemberName = "SkipInit"
									},
									Arguments = {
										outVarDecl
									}
								}
							};
							insertionParent.InsertChildBefore(
								v.InsertionPoint.nextNode,
								skipInitStatement,
								Slots.Statement);
							insertedNode = skipInitStatement;
						}
						else
						{
							insertionParent.InsertChildBefore(
								v.InsertionPoint.nextNode,
								vds,
								Slots.Statement);
							insertedNode = vds;
							var skipInitStatement = new ExpressionStatement {
								Expression = new InvocationExpression {
									Target = new MemberReferenceExpression {
										Target = new TypeReferenceExpression(unsafeType),
										MemberName = "SkipInit"
									},
									Arguments = {
										new DirectionExpression(
											FieldDirection.Out,
											new IdentifierExpression(v.Name)
												.WithRR(new ILVariableResolveResult(ilVariable))
										)
									}
								}
							};
							insertionParent.InsertChildBefore(
								v.InsertionPoint.nextNode,
								skipInitStatement,
								Slots.Statement);
						}
						context.EndStep(insertedNode);
					}
					else
					{
						insertionParent.InsertChildBefore(
							v.InsertionPoint.nextNode,
							vds,
							Slots.Statement);
						context.EndStep(vds);
					}
				}
			}
			// perform replacements at end, so that we don't replace a node while it is still referenced by a VariableToDeclare
			foreach (var (oldNode, createNewNode, stepDescription) in replacements)
			{
				context.Step(stepDescription, oldNode);
				var newNode = createNewNode();
				oldNode.ReplaceWith(newNode);
				context.EndStep(newNode);
			}
		}

		/// <summary>
		/// Gets whether the variable declared by <paramref name="dirExpr"/> is referenced again within
		/// another argument of the call containing the declaration. In that case the declaration must use
		/// the explicit type: referencing an implicitly-typed out variable is not permitted until overload
		/// resolution of the declaring call has inferred its type (CS8196).
		/// </summary>
		bool IsReferencedWithinDeclaringCall(DirectionExpression dirExpr, VariableToDeclare v)
		{
			AstNode? call = dirExpr.Parent;
			if (call == null)
				return false;
			for (AstNode? argument = call.FirstChild; argument != null; argument = argument.NextSibling)
			{
				if (argument == dirExpr)
					continue;
				foreach (AstNode node in argument.DescendantsAndSelf)
				{
					if (node is IdentifierExpression identifier && ResolveVariableToDeclare(identifier.GetILVariable()) == v)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines whether a by-ref-like (ref struct) local that is emitted as a bare hoisted
		/// declaration (assigned in multiple branches and merged, so it cannot be combined with a
		/// single initializer) should carry the C# 11 <c>scoped</c> modifier.
		///
		/// A bare ref-struct local's safe-context is the caller context (it may be returned), so
		/// assigning it a stack-referring value such as the result of a <c>stackalloc</c> is a
		/// compile error (CS8352/CS8353). <c>scoped</c> restricts the safe-context to the current
		/// method and makes the narrow assignment legal - but only when the local genuinely does not
		/// escape. This check is therefore deliberately asymmetric: it returns <see langword="true"/>
		/// only when a stack-referring value is assigned <em>and</em> every use is provably confined
		/// to the method. Any use whose confinement cannot be proven leaves the declaration bare
		/// (the status quo), because a false "does not escape" would inject a new ref-safety error on
		/// a legitimately escaping local, which is strictly worse than the missing modifier.
		/// </summary>
		bool ShouldDeclareByRefLikeLocalScoped(VariableToDeclare v)
		{
			// All uses of the variable live within the block that receives the declaration; the
			// insertion point is the common ancestor of every reference (see FindInsertionPoints).
			if (v.InsertionPoint.nextNode.Parent is not BlockStatement scope)
				return false;
			// Resolving whether an assignment's right-hand side is stack-referring may have to follow
			// local-copy chains (e.g. 'span = otherSpan.Slice(...)' where 'otherSpan' was itself
			// assigned a stackalloc). Those helper locals may be declared in an enclosing block, so
			// the search for their assignments spans the whole method body, not just this block.
			BlockStatement methodBody = OutermostBlock(scope);
			bool sawAssignmentOfStackReferringValue = false;
			bool sawUse = false;
			foreach (AstNode node in scope.Descendants)
			{
				if (node is not IdentifierExpression identifier)
					continue;
				if (ResolveVariableToDeclare(identifier.GetILVariable()) != v)
					continue;
				sawUse = true;
				if (identifier.Parent is AssignmentExpression { Operator: AssignmentOperatorType.Assign } assignment
					&& assignment.Left == identifier
					&& IsStackReferringValue(assignment.Right, methodBody))
				{
					sawAssignmentOfStackReferringValue = true;
				}
			}
			if (!sawUse || !sawAssignmentOfStackReferringValue)
				return false;
			// Every use of the local must be provably confined to the method. Confinement follows the
			// value through copies into other locals, so track visited locals to guarantee termination.
			return !LocalMayEscape(v, methodBody, new HashSet<VariableToDeclare>());
		}

		/// <summary>
		/// Whether <paramref name="expression"/> produces a value whose safe-context is the current
		/// method rather than the caller, i.e. a value that requires the receiving ref-struct local
		/// to be <c>scoped</c>. Besides a direct <c>stackalloc</c>, a value provably derived from one
		/// is recognized: a slice-like call of a stack-referring receiver, a copy of another local
		/// that itself holds a stack-referring value, and a ref-struct-returning call that is handed a
		/// stack-referring ref-struct argument (conservatively assumed to forward it). Only
		/// stack-derived values are recognized; a wide value such as a span over an array or a field
		/// stays unrecognized, so <c>scoped</c> is added only where it is genuinely required.
		/// </summary>
		bool IsStackReferringValue(Expression expression, BlockStatement methodBody)
		{
			return IsStackReferringValue(expression, methodBody, new HashSet<VariableToDeclare>());
		}

		bool IsStackReferringValue(Expression expression, BlockStatement methodBody, HashSet<VariableToDeclare> visitedLocals)
		{
			while (true)
			{
				switch (expression)
				{
					case ParenthesizedExpression paren:
						expression = paren.Expression;
						break;
					case CastExpression cast:
						expression = cast.Expression;
						break;
					case StackAllocExpression:
						return true;
					case IdentifierExpression identifier:
						// A copy of another ref-struct local: stack-referring if that local was itself
						// assigned a stack-referring value somewhere in the method.
						return LocalHoldsStackReferringValue(identifier.GetILVariable(), methodBody, visitedLocals);
					case InvocationExpression invocation:
						return InvocationYieldsStackReferringValue(invocation, methodBody, visitedLocals);
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Whether the ref-struct <paramref name="variable"/> is, at some assignment in the method, given
		/// a stack-referring value. Non-ref-struct locals (e.g. an array or pointer backing a wide span)
		/// cannot carry a stack reference into the local under analysis and are rejected. The visited set
		/// guards against cyclic local-copy chains.
		/// </summary>
		bool LocalHoldsStackReferringValue(ILVariable? variable, BlockStatement methodBody, HashSet<VariableToDeclare> visitedLocals)
		{
			VariableToDeclare? local = ResolveVariableToDeclare(variable);
			if (local == null || !local.Type.IsByRefLike)
				return false;
			if (!visitedLocals.Add(local))
				return false;
			foreach (AstNode node in methodBody.DescendantsAndSelf)
			{
				if (node is IdentifierExpression identifier
					&& ResolveVariableToDeclare(identifier.GetILVariable()) == local
					&& identifier.Parent is AssignmentExpression { Operator: AssignmentOperatorType.Assign } assignment
					&& assignment.Left == identifier
					&& IsStackReferringValue(assignment.Right, methodBody, visitedLocals))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Whether a ref-struct-returning call yields a stack-referring value. A slice-like instance or
		/// extension call forwards the references of its receiver, so it is stack-referring when the
		/// receiver is. Otherwise, a call handed a stack-referring ref-struct argument is conservatively
		/// assumed to be able to return that argument's stack references. A call returning a
		/// non-ref-struct value cannot carry a stack reference into the ref-struct local and is rejected.
		/// </summary>
		bool InvocationYieldsStackReferringValue(InvocationExpression invocation, BlockStatement methodBody, HashSet<VariableToDeclare> visitedLocals)
		{
			if (!invocation.GetResolveResult().Type.IsByRefLike)
				return false;
			if (invocation.Target is MemberReferenceExpression member
				&& IsStackReferringValue(member.Target, methodBody, visitedLocals))
			{
				return true;
			}
			foreach (Expression argument in invocation.Arguments)
			{
				Expression value = argument is DirectionExpression direction ? direction.Expression : argument;
				if (value.GetResolveResult().Type.IsByRefLike
					&& IsStackReferringValue(value, methodBody, visitedLocals))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// The outermost block enclosing <paramref name="block"/> - for a method-body block this is the
		/// block itself; for a nested block it walks up to the method body. Sibling members are never
		/// ancestors, so this stays within the current method.
		/// </summary>
		static BlockStatement OutermostBlock(BlockStatement block)
		{
			BlockStatement result = block;
			for (AstNode? node = block.Parent; node != null; node = node.Parent)
			{
				if (node is BlockStatement outer)
					result = outer;
			}
			return result;
		}

		/// <summary>
		/// Whether any reference held by the ref-struct local <paramref name="local"/> may escape the
		/// current method. Every use of the local is examined; a value copied into another local is
		/// followed into that local, so a chain of confined copies stays confined. <paramref name="visited"/>
		/// records the locals already on the current path and guarantees termination on cyclic copies.
		/// </summary>
		bool LocalMayEscape(VariableToDeclare local, BlockStatement methodBody, HashSet<VariableToDeclare> visited)
		{
			if (!visited.Add(local))
				return false;
			foreach (AstNode node in methodBody.DescendantsAndSelf)
			{
				if (node is IdentifierExpression identifier
					&& ResolveVariableToDeclare(identifier.GetILVariable()) == local
					&& ReferenceMayEscape(identifier, methodBody, visited))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Conservatively determines whether the references held by the ref-struct value at
		/// <paramref name="current"/> (derived from the local under analysis) may escape the current
		/// method. Returns <see langword="true"/> whenever escape cannot be ruled out; only contexts
		/// that provably confine the value return <see langword="false"/>: a store back into the value
		/// itself, a store into another local that is itself confined, a discarded statement value, or
		/// flowing into a value that cannot carry references outward.
		/// </summary>
		bool ReferenceMayEscape(Expression current, BlockStatement methodBody, HashSet<VariableToDeclare> visited)
		{
			switch (current.Parent)
			{
				case ParenthesizedExpression paren:
					return ReferenceMayEscape(paren, methodBody, visited);
				case CastExpression cast:
					// References survive a cast only if the target type is itself reference-carrying.
					if (TypeCarriesReferences(cast.GetResolveResult().Type))
						return ReferenceMayEscape(cast, methodBody, visited);
					return false;
				case AssignmentExpression assignment:
					// Storing into the current expression (its own initializer) does not let it escape.
					if (assignment.Left == current)
						return false;
					// The value is stored into assignment.Left. A store into another local escapes only
					// if that local's own value escapes; a store into a field, array element, or any
					// other heap-reachable l-value is a conservative escape.
					if (assignment.Left is IdentifierExpression targetIdentifier
						&& ResolveVariableToDeclare(targetIdentifier.GetILVariable()) is { } targetLocal)
					{
						return LocalMayEscape(targetLocal, methodBody, visited);
					}
					return true;
				case ReturnStatement:
				case YieldReturnStatement:
					return true;
				case DirectionExpression:
					// Passed by ref/out/in: a reference is taken; conservatively an escape.
					return true;
				case ExpressionStatement:
					// The value is computed as a statement and discarded, so it cannot escape.
					return false;
				case MemberReferenceExpression member when member.Target == current:
					// Method group of an invocation: the produced value is the call result.
					if (member.Parent is InvocationExpression memberInvocation && memberInvocation.Target == member)
						return ReferenceMayEscape(member, methodBody, visited);
					// Field/property access: references survive only through a reference-carrying member.
					if (TypeCarriesReferences(member.GetResolveResult().Type))
						return ReferenceMayEscape(member, methodBody, visited);
					return false;
				case IndexerExpression indexer when indexer.Target == current:
					if (TypeCarriesReferences(indexer.GetResolveResult().Type))
						return ReferenceMayEscape(indexer, methodBody, visited);
					return false;
				case InvocationExpression invocation:
					// A by-ref/out argument could receive the current value's references.
					if (HasReferenceCarryingRefArgument(invocation.Arguments))
						return true;
					// Otherwise the value leaves the call only through a reference-carrying return value.
					if (TypeCarriesReferences(invocation.GetResolveResult().Type))
						return ReferenceMayEscape(invocation, methodBody, visited);
					return false;
				case ObjectCreateExpression objectCreate:
					if (HasReferenceCarryingRefArgument(objectCreate.Arguments))
						return true;
					if (TypeCarriesReferences(objectCreate.GetResolveResult().Type))
						return ReferenceMayEscape(objectCreate, methodBody, visited);
					return false;
				case BinaryOperatorExpression binary:
					// Operators such as == yield a non-reference-carrying result; anything else is
					// treated conservatively as an escape.
					return TypeCarriesReferences(binary.GetResolveResult().Type);
				default:
					// Any other context (variable initializer, conditional, foreach source, ...) is
					// not proven safe, so treat it as an escape.
					return true;
			}
		}

		/// <summary>
		/// Whether any argument is passed by ref/out/in with a reference-carrying (ref struct or
		/// by-ref) type, which could receive the local's references even when the call's own result
		/// cannot carry them outward.
		/// </summary>
		static bool HasReferenceCarryingRefArgument(AstNodeCollection<Expression> arguments)
		{
			foreach (Expression argument in arguments)
			{
				if (argument is DirectionExpression direction
					&& TypeCarriesReferences(direction.Expression.GetResolveResult().Type))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Whether a value of <paramref name="type"/> can carry references out of the method: a
		/// by-ref-like (ref struct) value, a managed reference, or an unresolved type (treated
		/// conservatively as reference-carrying so it never acts as a firewall).
		/// </summary>
		static bool TypeCarriesReferences(IType type)
		{
			switch (type.Kind)
			{
				case TypeKind.ByReference:
				case TypeKind.Unknown:
				case TypeKind.None:
					return true;
				default:
					return type.IsByRefLike;
			}
		}

		/// <summary>
		/// Maps an ILVariable to the variable declaration it will end up in, following merges
		/// performed by ResolveCollisions.
		/// </summary>
		VariableToDeclare? ResolveVariableToDeclare(ILVariable? variable)
		{
			if (variable == null || !variableDict.TryGetValue(variable, out VariableToDeclare? v))
				return null;
			while (v.ReplacementDueToCollision is { } replacement)
			{
				v = replacement;
			}
			return v;
		}

		private bool CanBeDeclaredAsOutVariable(VariableToDeclare v, [NotNullWhen(true)] out DirectionExpression? dirExpr)
		{
			dirExpr = v.FirstUse.Parent as DirectionExpression;
			if (dirExpr == null || dirExpr.FieldDirection != FieldDirection.Out)
				return false;
			if (!context.Settings.OutVariables)
				return false;
			if (v.DefaultInitialization != VariableInitKind.None)
				return false;
			for (AstNode? node = v.FirstUse; node != null; node = node.Parent)
			{
				if (node.Slot?.Kind == Slots.EmbeddedStatement)
				{
					return false;
				}
				switch (node)
				{
					case SwitchExpressionSection:
						// The scope of a variable declared in a switch-expression arm is
						// limited to that arm, not the containing statement or block.
						return false;
					case IfElseStatement:  // variable declared in if condition appears in parent scope
					case ExpressionStatement:
						return node == v.InsertionPoint.nextNode;
					case Statement:
						return false; // other statements (e.g. while) don't allow variables to be promoted to parent scope
					case LambdaExpression lambda:
						return lambda.Body == v.InsertionPoint.nextNode;
				}
			}
			return false;
		}

		/// <summary>
		/// Update ILVariableResolveResult annotations of all ILVariables that have been replaced by ResolveCollisions.
		/// </summary>
		void UpdateAnnotations(AstNode rootNode)
		{
			foreach (var node in rootNode.Descendants)
			{
				ILVariable? ilVar;
				switch (node)
				{
					case IdentifierExpression id:
						ilVar = id.GetILVariable();
						break;
					case VariableInitializer vi:
						ilVar = vi.GetILVariable();
						break;
					default:
						continue;
				}
				if (ilVar == null || !VariableNeedsDeclaration(ilVar.Kind))
					continue;
				var v = variableDict[ilVar];
				if (!v.RemovedDueToCollision)
					continue;
				while (v.ReplacementDueToCollision is { } replacement)
				{
					v = replacement;
				}
				node.RemoveAnnotations<ILVariableResolveResult>();
				node.AddAnnotation(new ILVariableResolveResult(v.ILVariable, v.Type));
			}
		}
	}
}

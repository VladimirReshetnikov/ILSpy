// Copyright (c) 2026 Vladimir Reshetnikov
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
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Restores C# 12 collection expressions.
	///
	/// A collection expression has no type of its own, so it can only be written where the
	/// surrounding context supplies a target type. The transform therefore has two halves: a
	/// recognizer that proves an expression was lowered from <c>[...]</c>, and a placement check
	/// that the position it occupies can still target-type it. Where either fails, the ordinary
	/// decompilation stands.
	///
	/// The shapes recognized here are the ones the compiler emits for a collection expression and
	/// for nothing else. A bare array creation is deliberately not among them: <c>[1, 2]</c> and
	/// <c>new int[] { 1, 2 }</c> compile to the same IL, so rewriting one would be a guess about
	/// source that the metadata does not record.
	/// </summary>
	class IntroduceCollectionExpressions : DepthFirstAstVisitor, IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			if (!context.Settings.CollectionExpressions)
				return;
			rootNode.AcceptVisitor(this);
		}

		public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
		{
			base.VisitArrayCreateExpression(arrayCreateExpression);
			if (arrayCreateExpression.Annotation<Block>() is not { Kind: BlockKind.CollectionExpression })
				return;
			if (arrayCreateExpression.Initializer is not { } initializer)
				return;
			if (!CanTargetType(arrayCreateExpression))
				return;
			ReplaceWithCollectionExpression(arrayCreateExpression, initializer.Elements);
		}

		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			base.VisitInvocationExpression(invocationExpression);
			// `ImmutableCollectionsMarshal.AsImmutableArray(new T[] { ... })` is how a collection
			// expression targeting ImmutableArray<T> is built: the freshly allocated array becomes
			// the immutable array's storage without a copy.
			if (invocationExpression.GetSymbol() is not IMethod {
				Name: "AsImmutableArray",
				DeclaringType.FullName: "System.Runtime.InteropServices.ImmutableCollectionsMarshal"
			})
			{
				return;
			}
			if (invocationExpression.Arguments.Count != 1)
				return;
			if (invocationExpression.Arguments.Single() is not ArrayCreateExpression { Initializer: { } initializer })
				return;
			if (!CanTargetType(invocationExpression))
				return;
			ReplaceWithCollectionExpression(invocationExpression, initializer.Elements);
		}

		public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			base.VisitObjectCreateExpression(objectCreateExpression);
			// The IL pipeline strips the read-only collection wrappers wherever it can see through
			// them; whatever reaches here it could not. The wrapper type is hidden from the output, so
			// it has to go either way: as `[...]` when the argument is an array the compiler built for
			// it, and otherwise as the bare argument, which satisfies the same read-only interfaces.
			if (objectCreateExpression.GetSymbol() is not IMethod { IsConstructor: true } constructor)
				return;
			if (!IsReadOnlyCollectionWrapper(constructor.DeclaringTypeDefinition))
				return;
			if (objectCreateExpression.Arguments.Count != 1)
				return;
			var argument = objectCreateExpression.Arguments.Single();
			if (argument is ArrayCreateExpression { Initializer: { } initializer } && CanTargetType(objectCreateExpression))
			{
				ReplaceWithCollectionExpression(objectCreateExpression, initializer.Elements);
				return;
			}
			argument.Remove();
			objectCreateExpression.ReplaceWith(argument);
		}

		static bool IsReadOnlyCollectionWrapper(ITypeDefinition? declaringType)
		{
			return declaringType is {
				DeclaringTypeDefinition: null,
				Kind: TypeKind.Class,
				IsSealed: true,
				TypeParameterCount: 1
			}
				&& declaringType.Name is "<>z__ReadOnlyArray" or "<>z__ReadOnlySingleElementList"
				&& declaringType.HasAttribute(KnownAttribute.CompilerGenerated);
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			base.VisitBlockStatement(blockStatement);
			// A match consumes the statements that follow it, so iterate over a snapshot and skip
			// whatever an earlier match already removed from the block.
			foreach (var statement in blockStatement.Statements.ToArray())
			{
				if (statement.Parent != blockStatement)
					continue;
				if (TransformListFill(statement))
					continue;
				TransformInlineArrayBuffer(statement);
			}
		}

		/// <summary>
		/// Folds the fill of a pre-sized list back into a collection expression:
		/// <code>
		/// List&lt;T&gt; list = new List&lt;T&gt;(n);
		/// CollectionsMarshal.SetCount(list, n);
		/// Span&lt;T&gt; span = CollectionsMarshal.AsSpan(list);
		/// span[0] = v0; ... span[n-1] = vn-1;
		/// </code>
		/// The <c>CollectionsMarshal</c> pair is how the compiler builds a <c>List&lt;T&gt;</c>-typed
		/// collection expression whose length is known up front; a hand-written list never fills
		/// itself this way, since <c>SetCount</c> exposes uninitialized elements until every slot has
		/// been written.
		/// </summary>
		bool TransformListFill(Statement statement)
		{
			if (statement is not VariableDeclarationStatement { Variables: [{ Initializer: ObjectCreateExpression create } listVariable] } declaration)
				return false;
			if (declaration.Type.IsVar())
				return false;
			if (create.GetSymbol() is not IMethod { IsConstructor: true, Parameters.Count: 1 } listConstructor)
				return false;
			if (listConstructor.DeclaringType is not { FullName: "System.Collections.Generic.List", TypeParameterCount: 1 })
				return false;
			var list = listVariable.GetILVariable();
			if (list == null)
				return false;

			// SetCount(list, capacity), with the same capacity the constructor was given.
			if (statement.GetNextSibling(n => n is Statement) is not ExpressionStatement { Expression: InvocationExpression setCount } setCountStatement)
				return false;
			if (!IsCollectionsMarshalCall(setCount, "SetCount") || setCount.Arguments.Count != 2)
				return false;
			if (!IsReferenceTo(setCount.Arguments.First(), list))
				return false;
			if (!TryGetConstantCount(create.Arguments.Single(), statement, out int count, out var countDeclaration))
				return false;
			if (!TryGetConstantCount(setCount.Arguments.Last(), statement, out int setCountValue, out _) || setCountValue != count)
				return false;

			// Span<T> span = AsSpan(list), followed by one store per element.
			if (setCountStatement.GetNextSibling(n => n is Statement) is not VariableDeclarationStatement {
				Variables: [{ Initializer: InvocationExpression asSpan } spanVariable]
			} spanDeclaration)
			{
				return false;
			}
			if (!IsCollectionsMarshalCall(asSpan, "AsSpan") || asSpan.Arguments.Count != 1)
				return false;
			if (!IsReferenceTo(asSpan.Arguments.Single(), list))
				return false;
			var span = spanVariable.GetILVariable();
			if (span == null)
				return false;
			if (!TryGetElementStores(spanDeclaration, span, count, out var elements, out var lastStore))
				return false;

			// The span and the capacity local exist only to fill the list.
			var memberRoot = GetMemberRoot(statement);
			if (CountReferences(memberRoot, span) != count)
				return false;

			ReplaceWithCollectionExpression(create, elements);
			RemoveRange(setCountStatement, lastStore);
			countDeclaration?.Remove();
			return true;
		}

		/// <summary>
		/// Folds an inline-array buffer that is filled and then handed out as a span:
		/// <code>
		/// &lt;&gt;y__InlineArrayN&lt;T&gt; buffer = default;
		/// buffer[0] = v0; ... buffer[n-1] = vn-1;
		/// Use(buffer);
		/// </code>
		/// The compiler synthesizes these buffers for a collection expression targeting
		/// <c>Span&lt;T&gt;</c> or <c>ReadOnlySpan&lt;T&gt;</c> when the elements fit on the stack.
		/// </summary>
		bool TransformInlineArrayBuffer(Statement statement)
		{
			if (statement is not VariableDeclarationStatement { Variables: [{ Initializer: DefaultValueExpression } bufferVariable] })
				return false;
			var buffer = bufferVariable.GetILVariable();
			if (buffer == null || !IsCompilerGeneratedInlineArray(buffer.Type))
				return false;
			if (buffer.Type.GetInlineArrayLength() is not int count || count < 1)
				return false;
			if (!TryGetElementStores(statement, buffer, count, out var elements, out var lastStore))
				return false;

			// Exactly one use is left over: the span the buffer was built for.
			var memberRoot = GetMemberRoot(statement);
			var references = memberRoot.Descendants.OfType<IdentifierExpression>()
				.Where(identifier => identifier.GetILVariable() == buffer).ToArray();
			if (references.Length != count + 1)
				return false;
			var candidates = references
				.Where(identifier => identifier.Parent is not IndexerExpression && !identifier.Ancestors.Contains(lastStore))
				.ToArray();
			if (candidates.Length != 1)
				return false;
			var use = candidates[0];
			if (use.GetParent<Statement>() is not { } useStatement || useStatement.GetPrevSibling(n => n is Statement) != lastStore)
				return false;
			if (!IsSpanTargetedPosition(use, buffer.Type.GetInlineArrayElementType()))
				return false;
			if (!NothingWithSideEffectsPrecedes(use, useStatement) && !elements.All(IsSideEffectFree))
				return false;

			ReplaceWithCollectionExpression(use, elements);
			RemoveRange(statement, lastStore);
			return true;
		}

		/// <summary>
		/// Matches <c>target[0] = v0; ... target[count-1] = vcount-1;</c> immediately following
		/// <paramref name="statement"/>, in index order and with no index written twice.
		/// </summary>
		static bool TryGetElementStores(Statement statement, ILVariable target, int count,
			out Expression[] elements, out Statement lastStore)
		{
			elements = null!;
			lastStore = null!;
			var values = new Expression[count];
			Statement current = statement;
			for (int i = 0; i < count; i++)
			{
				if (current.GetNextSibling(n => n is Statement) is not ExpressionStatement {
					Expression: AssignmentExpression {
						Operator: AssignmentOperatorType.Assign,
						Left: IndexerExpression { Arguments: [PrimitiveExpression index] } indexer,
						Right: var value
					}
				} store)
				{
					return false;
				}
				if (!IsReferenceTo(indexer.Target, target) || index.Value is not int indexValue || indexValue != i)
					return false;
				values[i] = value;
				current = store;
			}
			elements = values;
			lastStore = current;
			return true;
		}

		/// <summary>
		/// Resolves an element count that the compiler either inlined as a literal or hoisted into a
		/// local. A hoisted local is reported through <paramref name="countDeclaration"/> so the
		/// caller can drop it along with the statements it served.
		/// </summary>
		static bool TryGetConstantCount(Expression expression, Statement statement, out int count, out Statement? countDeclaration)
		{
			count = 0;
			countDeclaration = null;
			switch (expression)
			{
				case PrimitiveExpression { Value: int literal }:
					count = literal;
					return true;
				case IdentifierExpression identifier when identifier.GetILVariable() is { } variable:
					var memberRoot = GetMemberRoot(statement);
					var declarations = memberRoot.Descendants.OfType<VariableInitializer>()
						.Where(v => v.GetILVariable() == variable).ToArray();
					if (declarations is not [{ Initializer: PrimitiveExpression { Value: int value } } declaration])
						return false;
					// Two reads: the constructor capacity and the SetCount argument.
					if (CountReferences(memberRoot, variable) != 2)
						return false;
					// A declaration of several variables cannot be dropped along with this one.
					if (declaration.Parent is not VariableDeclarationStatement { Variables.Count: 1 } declarationStatement)
						return false;
					count = value;
					countDeclaration = declarationStatement;
					return true;
				default:
					return false;
			}
		}

		static bool IsCollectionsMarshalCall(InvocationExpression invocation, string name)
		{
			return invocation.GetSymbol() is IMethod method
				&& method.Name == name
				&& method.DeclaringType.FullName == "System.Runtime.InteropServices.CollectionsMarshal";
		}

		static bool IsReferenceTo(Expression? expression, ILVariable variable)
		{
			return expression is IdentifierExpression identifier && identifier.GetILVariable() == variable;
		}

		/// <summary>
		/// Recognizes the single-element-type buffer a compiler uses to stage a span-targeted
		/// collection expression. Older compilers synthesize <c>&lt;&gt;y__InlineArrayN&lt;T&gt;</c>
		/// into the assembly; newer ones bind to the runtime's
		/// <c>System.Runtime.CompilerServices.InlineArrayN&lt;T&gt;</c> of the same shape. A buffer the
		/// source declared itself carries neither name, and must keep its own syntax.
		/// </summary>
		static bool IsCompilerGeneratedInlineArray(IType type)
		{
			if (type.Kind != TypeKind.Struct || type.GetDefinition() is not { } definition)
				return false;
			if (definition.DeclaringTypeDefinition != null || definition.TypeParameterCount != 1)
				return false;
			return definition.Name.StartsWith("<>y__InlineArray", StringComparison.Ordinal)
				|| (definition.Namespace == "System.Runtime.CompilerServices"
					&& definition.Name.StartsWith("InlineArray", StringComparison.Ordinal));
		}

		/// <summary>
		/// Returns whether <paramref name="expression"/> stands where a <c>Span&lt;T&gt;</c> or
		/// <c>ReadOnlySpan&lt;T&gt;</c> over <paramref name="elementType"/> is expected, which is what
		/// gives a collection expression in that position its target type.
		/// </summary>
		static bool IsSpanTargetedPosition(Expression expression, IType elementType)
		{
			switch (expression.Parent)
			{
				case VariableInitializer { Parent: VariableDeclarationStatement declaration }:
					return !declaration.Type.IsVar()
						&& IsSpanOf(declaration.Type.GetResolveResult().Type, elementType);
				case AssignmentExpression { Operator: AssignmentOperatorType.Assign } assignment when assignment.Right == expression:
					return IsSpanOf(assignment.Left.GetResolveResult().Type, elementType);
				case InvocationExpression invocation:
					return IsSpanParameter(invocation.GetSymbol() as IMethod, invocation.Arguments, expression, elementType);
				case ObjectCreateExpression create:
					return IsSpanParameter(create.GetSymbol() as IMethod, create.Arguments, expression, elementType);
				default:
					return false;
			}
		}

		static bool IsSpanParameter(IMethod? method, AstNodeCollection<Expression> arguments, Expression argument, IType elementType)
		{
			if (method == null)
				return false;
			int index = arguments.TakeWhile(a => a != argument).Count();
			if (index >= method.Parameters.Count)
				return false;
			return IsSpanOf(method.Parameters[index].Type, elementType);
		}

		static bool IsSpanOf(IType type, IType elementType)
		{
			return (type.IsKnownType(KnownTypeCode.SpanOfT) || type.IsKnownType(KnownTypeCode.ReadOnlySpanOfT))
				&& type.TypeArguments.Count == 1
				&& type.TypeArguments[0].Equals(elementType);
		}

		/// <summary>
		/// Returns whether moving the element values into <paramref name="use"/> preserves evaluation
		/// order, i.e. whether everything the statement evaluates before that position is free of
		/// side effects.
		/// </summary>
		static bool NothingWithSideEffectsPrecedes(Expression use, Statement statement)
		{
			AstNode node = use;
			while (node != statement && node.Parent != null)
			{
				foreach (var sibling in node.Parent.Children)
				{
					if (sibling == node)
						break;
					if (sibling is Expression expression && !IsSideEffectFree(expression))
						return false;
				}
				node = node.Parent;
			}
			return true;
		}

		static bool IsSideEffectFree(Expression expression)
		{
			switch (expression)
			{
				case PrimitiveExpression:
				case NullReferenceExpression:
				case ThisReferenceExpression:
				case BaseReferenceExpression:
				case TypeReferenceExpression:
				case TypeOfExpression:
				case SizeOfExpression:
				case DefaultValueExpression:
					return true;
				case IdentifierExpression identifier:
					return identifier.GetILVariable() != null || identifier.GetSymbol() is IField;
				case MemberReferenceExpression member:
					return member.GetSymbol() is IField && IsSideEffectFree(member.Target);
				case ParenthesizedExpression parenthesized:
					return IsSideEffectFree(parenthesized.Expression);
				case CastExpression cast:
					return cast.GetSymbol() is not IMethod && IsSideEffectFree(cast.Expression);
				default:
					return false;
			}
		}

		static int CountReferences(AstNode root, ILVariable variable)
		{
			return root.Descendants.OfType<IdentifierExpression>().Count(identifier => identifier.GetILVariable() == variable);
		}

		/// <summary>
		/// Returns the outermost node still belonging to the member being decompiled, so that a
		/// variable's uses can be counted across the whole body rather than one block.
		/// </summary>
		static AstNode GetMemberRoot(AstNode node)
		{
			AstNode root = node;
			while (root.Parent is { } parent && parent is not EntityDeclaration && parent is not SyntaxTree)
			{
				root = parent;
			}
			return root.Parent ?? root;
		}

		static void RemoveRange(Statement first, Statement last)
		{
			Statement? current = first;
			while (current != null)
			{
				var next = current == last ? null : current.GetNextSibling(n => n is Statement) as Statement;
				current.Remove();
				current = next;
			}
		}

		/// <summary>
		/// Replaces <paramref name="expression"/> with a collection expression over
		/// <paramref name="elements"/>, which are detached from their current parent.
		/// </summary>
		static void ReplaceWithCollectionExpression(Expression expression, IEnumerable<Expression> elements)
		{
			var detached = elements.ToArray();
			foreach (var element in detached)
			{
				element.Remove();
			}
			var collectionExpression = new CollectionExpression(detached).CopyAnnotationsFrom(expression);
			expression.ReplaceWith(collectionExpression);
		}

		/// <summary>
		/// Returns whether the position <paramref name="expression"/> occupies gives a collection
		/// expression a target type. The list is an allow-list: a position that is not known to
		/// target-type its operand keeps the original syntax.
		/// </summary>
		static bool CanTargetType(Expression expression)
		{
			switch (expression.Parent)
			{
				case VariableInitializer { Parent: VariableDeclarationStatement declaration }:
					// `var x = [1, 2];` has nothing to infer the type from.
					return !declaration.Type.IsVar();
				case AssignmentExpression { Operator: AssignmentOperatorType.Assign } assignment:
					return assignment.Right == expression;
				case ReturnStatement:
				case InvocationExpression:
				case ObjectCreateExpression:
					return true;
				default:
					return false;
			}
		}
	}
}

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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using ICSharpCode.Decompiler.Semantics;
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
		[AllowNull]
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			if (!context.Settings.CollectionExpressions)
				return;
			this.context = context;
			try
			{
				rootNode.AcceptVisitor(this);
			}
			finally
			{
				this.context = null;
				this.memberIndexes.Clear();
			}
		}

		public override void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
		{
			base.VisitArrayCreateExpression(arrayCreateExpression);
			if (arrayCreateExpression.Annotation<Block>() is not { Kind: BlockKind.CollectionExpression })
				return;
			if (arrayCreateExpression.Initializer is not { } initializer)
				return;
			if (!CanTargetType(arrayCreateExpression, out var castTo))
				return;
			ReplaceWithCollectionExpression(arrayCreateExpression, initializer.Elements, castTo);
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
			if (!CanTargetType(invocationExpression, out var castTo))
				return;
			switch (invocationExpression.Arguments.Single())
			{
				case ArrayCreateExpression { Initializer: { } initializer }:
					ReplaceWithCollectionExpression(invocationExpression, initializer.Elements, castTo);
					break;
				// A collection expression that is nothing but one spread copies the source wholesale.
				case InvocationExpression toArray when TryGetToArraySource(toArray, out var source):
					source.Remove();
					var spread = new CollectionExpression(new SpreadElement(source)).CopyAnnotationsFrom(invocationExpression);
					invocationExpression.ReplaceWith(Cast(spread, castTo, invocationExpression.GetResolveResult()));
					break;
			}
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
			if (argument is ArrayCreateExpression { Initializer: { } initializer }
				&& CanTargetType(objectCreateExpression, out var castTo))
			{
				ReplaceWithCollectionExpression(objectCreateExpression, initializer.Elements, castTo);
				return;
			}
			argument.Remove();
			if (constructor.DeclaringTypeDefinition?.Name == "<>z__ReadOnlySingleElementList"
				&& constructor.DeclaringType.TypeArguments is [var elementType])
			{
				// This wrapper's constructor argument is the single element, not a collection, so
				// the bare argument would be type-incorrect. A one-element array is the closest
				// expressible stand-in satisfying the same read-only interfaces.
				var arrayCreate = new ArrayCreateExpression {
					Type = context.TypeSystemAstBuilder.ConvertType(elementType),
					Initializer = new ArrayInitializerExpression(argument)
				};
				arrayCreate.WithRR(new ResolveResult(new ArrayType(context.TypeSystem, elementType)));
				objectCreateExpression.ReplaceWith(arrayCreate);
				return;
			}
			objectCreateExpression.ReplaceWith(KeepArgumentTyped(objectCreateExpression, argument));
		}

		/// <summary>
		/// Restores the type the wrapper contributed to the call, for a wrapper that could not become
		/// a collection expression. The wrapper is what picked the overload - it implements only the
		/// read-only collection interfaces - whereas its argument is an array, which several
		/// overloads can accept. Only argument positions need this; elsewhere the one target type
		/// converts the array on its own.
		/// </summary>
		Expression KeepArgumentTyped(Expression wrapper, Expression argument)
		{
			if (wrapper.Parent is not (InvocationExpression or ObjectCreateExpression))
				return argument;
			if (!TryGetParameterTypeIgnoringOverloads(wrapper, out var parameterType))
				return argument;
			if (parameterType.Equals(argument.GetResolveResult().Type))
				return argument;
			return new CastExpression(context.TypeSystemAstBuilder.ConvertType(parameterType), argument)
				.WithRR(new ConversionResolveResult(parameterType, argument.GetResolveResult(), Conversion.ImplicitReferenceConversion));
		}

		static bool TryGetParameterTypeIgnoringOverloads(Expression argument, [NotNullWhen(true)] out IType? parameterType)
		{
			parameterType = argument.Parent switch {
				InvocationExpression invocation => GetParameterTypeAt(invocation.GetSymbol() as IMethod, invocation.Arguments, argument),
				ObjectCreateExpression create => GetParameterTypeAt(create.GetSymbol() as IMethod, create.Arguments, argument),
				_ => null
			};
			return parameterType is { Kind: not TypeKind.Unknown and not TypeKind.None };
		}

		/// <summary>
		/// Recovers the collection a <c>ToArray</c> call copies. Extension-method syntax is restored
		/// later in the pipeline, so the call still appears in whichever form the call site had.
		/// </summary>
		static bool TryGetToArraySource(InvocationExpression invocation, [NotNullWhen(true)] out Expression? source)
		{
			source = null;
			if (invocation.GetSymbol() is not IMethod { Name: "ToArray" } method)
				return false;
			// Only ToArray implementations whose result is exactly the receiver's enumeration
			// qualify. An arbitrary method that merely shares the name may return contents (or
			// an element type) unrelated to spreading its receiver, so the rewrite to a spread
			// would change meaning or not compile at all.
			string? declaringType = method.DeclaringTypeDefinition?.ReflectionName;
			if (method.IsStatic && invocation.Arguments.Count == 1
				&& declaringType == "System.Linq.Enumerable")
			{
				source = invocation.Arguments.Single();
			}
			else if (!method.IsStatic && invocation.Arguments.Count == 0
				&& declaringType is "System.Collections.Generic.List`1" or "System.Span`1" or "System.ReadOnlySpan`1"
				&& invocation.Target is MemberReferenceExpression { Target: { } receiver })
			{
				source = receiver;
			}
			// The name alone says nothing: plenty of types offer a ToArray that copies out of
			// something a spread cannot read, MemoryStream among them. Spreading one of those
			// produces C# that does not compile, so the source has to be enumerable in its own right.
			if (source != null && !IsSpreadable(source.GetResolveResult().Type))
				source = null;
			return source != null;
		}

		/// <summary>
		/// Returns whether a spread element may read from <paramref name="type"/>. A spread asks for
		/// what foreach asks for: either the type implements IEnumerable, or it just offers a
		/// GetEnumerator to bind against, which is how Span and ReadOnlySpan qualify while
		/// implementing neither.
		/// </summary>
		static bool IsSpreadable(IType type)
		{
			if (type.Kind is TypeKind.Array or TypeKind.Dynamic)
				return true;
			if (type.Kind is TypeKind.Unknown or TypeKind.None)
				return false;
			if (type.GetAllBaseTypes().Any(baseType => baseType.IsKnownType(KnownTypeCode.IEnumerable)))
				return true;
			return type.GetMethods(method => method.Name == "GetEnumerator" && !method.IsStatic).Any();
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
				if (TransformSpreadIntoArray(statement))
					continue;
				if (TransformSpreadIntoList(statement))
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
		/// Folds the builder the compiler emits for a collection expression that contains a spread
		/// whose length is known before the collection is allocated:
		/// <code>
		/// int index = 0;
		/// T[] array = new T[1 + source.Length];
		/// array[index] = v0;
		/// index++;
		/// ReadOnlySpan&lt;T&gt; span = new ReadOnlySpan&lt;T&gt;(source);
		/// span.CopyTo(new Span&lt;T&gt;(array).Slice(index, span.Length));
		/// index += span.Length;
		/// </code>
		/// which is <c>[v0, .. source]</c>. The allocation is sized from the spread sources up front
		/// and every slot is then written exactly once through a running index - a shape that only
		/// arises from lowering, and one whose length expression pins down which elements the
		/// collection has.
		/// </summary>
		bool TransformSpreadIntoArray(Statement statement)
		{
			if (statement is not VariableDeclarationStatement {
				Variables: [{ Initializer: ArrayCreateExpression create } arrayVariable]
			})
			{
				return false;
			}
			if (create.Arguments.Count != 1 || create.Initializer is { Elements.Count: > 0 })
				return false;
			if (arrayVariable.GetILVariable() is not { } array)
				return false;
			if (statement.GetPrevSibling(n => n is Statement) is not VariableDeclarationStatement {
				Variables: [{ Initializer: PrimitiveExpression { Value: 0 } } indexVariable]
			} indexDeclaration)
			{
				return false;
			}
			if (indexVariable.GetILVariable() is not { } index)
				return false;

			bool IsArray(Expression? expression) => IsReferenceTo(expression, array);
			bool IsArrayAsSpan(Expression? expression)
			{
				return expression is ObjectCreateExpression { Arguments: [var wrapped] } spanCreate
					&& spanCreate.GetSymbol() is IMethod { IsConstructor: true } spanConstructor
					&& spanConstructor.DeclaringType.IsKnownType(KnownTypeCode.SpanOfT)
					&& IsArray(wrapped);
			}

			if (!TryCollectBuilderElements(statement, index, IsArray, IsArrayAsSpan, out var elements, out var lastStatement))
				return false;
			return CompleteSpreadBuilder(indexDeclaration, statement, lastStatement,
				create.Arguments.Single(), array, new[] { index }, elements);
		}

		/// <summary>
		/// The <c>List&lt;T&gt;</c> counterpart of <see cref="TransformSpreadIntoArray"/>: the list is
		/// allocated and sized up front, and the elements are written through the span over its
		/// storage rather than into an array.
		/// <code>
		/// int capacity = 1 + source.Length;
		/// List&lt;T&gt; list = new List&lt;T&gt;(capacity);
		/// CollectionsMarshal.SetCount(list, capacity);
		/// Span&lt;T&gt; span = CollectionsMarshal.AsSpan(list);
		/// int index = 0;
		/// span[index] = v0;
		/// index++;
		/// ...
		/// </code>
		/// </summary>
		bool TransformSpreadIntoList(Statement statement)
		{
			if (statement is not VariableDeclarationStatement {
				Variables: [{ Initializer: ObjectCreateExpression create } listVariable]
			} declaration)
			{
				return false;
			}
			if (declaration.Type.IsVar())
				return false;
			if (create.GetSymbol() is not IMethod { IsConstructor: true, Parameters.Count: 1 } listConstructor)
				return false;
			if (listConstructor.DeclaringType is not { FullName: "System.Collections.Generic.List", TypeParameterCount: 1 })
				return false;
			if (listVariable.GetILVariable() is not { } list)
				return false;

			// The capacity is computed into its own local, which the constructor and SetCount share.
			if (create.Arguments.Single() is not IdentifierExpression capacityReference)
				return false;
			if (capacityReference.GetILVariable() is not { } capacity)
				return false;
			if (statement.GetPrevSibling(n => n is Statement) is not VariableDeclarationStatement {
				Variables: [{ Initializer: { } lengthExpression } capacityVariable]
			} capacityDeclaration)
			{
				return false;
			}
			if (capacityVariable.GetILVariable() != capacity)
				return false;

			if (statement.GetNextSibling(n => n is Statement) is not ExpressionStatement {
				Expression: InvocationExpression setCount
			} setCountStatement)
			{
				return false;
			}
			if (!IsCollectionsMarshalCall(setCount, "SetCount") || setCount.Arguments.Count != 2)
				return false;
			if (!IsReferenceTo(setCount.Arguments.First(), list) || !IsReferenceTo(setCount.Arguments.Last(), capacity))
				return false;

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
			if (spanVariable.GetILVariable() is not { } span)
				return false;

			if (spanDeclaration.GetNextSibling(n => n is Statement) is not VariableDeclarationStatement {
				Variables: [{ Initializer: PrimitiveExpression { Value: 0 } } indexVariable]
			} indexDeclaration)
			{
				return false;
			}
			if (indexVariable.GetILVariable() is not { } index)
				return false;

			bool IsSpan(Expression? expression) => IsReferenceTo(expression, span);

			if (!TryCollectBuilderElements(indexDeclaration, index, IsSpan, IsSpan, out var elements, out var lastStatement))
				return false;
			return CompleteSpreadBuilder(capacityDeclaration, statement, lastStatement,
				lengthExpression, list, new[] { index, span, capacity }, elements);
		}

		/// <summary>
		/// Checks what the element collection could not see - that the length the collection was
		/// allocated with is exactly the elements found, that the builder's locals do not outlive it,
		/// and that the one remaining use of the result can target-type a collection expression - and
		/// then replaces that use with <c>[...]</c>, dropping the builder.
		/// </summary>
		bool CompleteSpreadBuilder(Statement firstStatement, Statement resultStatement, Statement lastStatement,
			Expression lengthExpression, ILVariable result, ILVariable[] builderLocals, List<BuilderElement> elements)
		{
			if (!elements.Any(element => element.IsSpread))
				return false; // without a spread the constant-index forms apply, or nothing does
			if (!LengthMatchesElements(lengthExpression, elements))
				return false;

			var memberRoot = GetMemberRoot(resultStatement);
			var consumed = StatementsBetween(firstStatement, lastStatement);
			foreach (var local in builderLocals)
			{
				if (ReferencesOutside(memberRoot, local, consumed).Any())
					return false;
			}
			foreach (var element in elements)
			{
				if (element.SpanTemp != null && ReferencesOutside(memberRoot, element.SpanTemp, consumed).Any())
					return false;
			}
			if (ReferencesOutside(memberRoot, result, consumed) is not [var use])
				return false;
			if (use.GetParent<Statement>() is not { } useStatement || useStatement.GetPrevSibling(n => n is Statement) != lastStatement)
				return false;
			if (!CanTargetType(use, out var castTo))
				return false;

			var hoisted = HoistedDeclarationsBefore(firstStatement);
			var values = elements.Select(element => InlineHoistedValue(element, elements, memberRoot, consumed, hoisted)).ToArray();
			if (!NothingWithSideEffectsPrecedes(use, useStatement) && !values.All(IsSideEffectFree))
				return false;

			var detached = new Expression[elements.Count];
			for (int i = 0; i < elements.Count; i++)
			{
				values[i].Remove();
				detached[i] = elements[i].IsSpread ? new SpreadElement(values[i]) : values[i];
			}
			var collectionExpression = new CollectionExpression(detached).CopyAnnotationsFrom(use);
			use.ReplaceWith(Cast(collectionExpression, castTo, use.GetResolveResult()));
			foreach (var element in elements)
			{
				element.HoistedDeclaration?.Remove();
			}
			RemoveRange(firstStatement, lastStatement);
			return true;
		}

		/// <summary>
		/// One element of a lowered collection expression: its value (the spread source, for a
		/// spread), the span temp the spread was copied through, and the declaration of the local the
		/// value was hoisted into, if any.
		/// </summary>
		sealed class BuilderElement
		{
			public BuilderElement(Expression value, bool isSpread, ILVariable? spanTemp)
			{
				Value = value;
				IsSpread = isSpread;
				SpanTemp = spanTemp;
			}

			public Expression Value { get; }
			public bool IsSpread { get; }
			public ILVariable? SpanTemp { get; }
			public VariableDeclarationStatement? HoistedDeclaration { get; set; }
		}

		/// <summary>
		/// Collects the elements written into a collection through a running index, in order, until a
		/// statement that is neither an element store nor a spread copy.
		/// </summary>
		bool TryCollectBuilderElements(Statement start, ILVariable index,
			Func<Expression?, bool> isWriteTarget, Func<Expression?, bool> isCopyDestination,
			out List<BuilderElement> elements, out Statement lastStatement)
		{
			elements = new List<BuilderElement>();
			Statement current = start;
			lastStatement = start;
			// The running index must visibly advance between elements; only the last element may
			// leave the increment out (the compiler elides it there because nothing reads the index
			// afterwards). Without this, two stores through the same index value - an overwrite of
			// one slot, not two elements - would collect as two elements.
			bool indexAdvancePending = false;
			while (current.GetNextSibling(n => n is Statement) is Statement next)
			{
				if (next is ExpressionStatement {
					Expression: AssignmentExpression {
						Operator: AssignmentOperatorType.Assign,
						Left: IndexerExpression { Arguments: [var indexArgument] } indexer,
						Right: var value
					}
				}
					&& isWriteTarget(indexer.Target) && IsReferenceTo(indexArgument, index))
				{
					if (indexAdvancePending)
						return false;
					elements.Add(new BuilderElement(value, isSpread: false, spanTemp: null));
					current = SkipIndexIncrement(next, index);
					indexAdvancePending = current == next;
					continue;
				}
				if (TryMatchSpreadCopy(next, index, isCopyDestination, out var spread, out var afterSpread, out bool advancedIndex))
				{
					if (indexAdvancePending)
						return false;
					elements.Add(spread);
					current = afterSpread;
					indexAdvancePending = !advancedIndex;
					continue;
				}
				break;
			}
			lastStatement = current;
			return elements.Count > 0;
		}

		/// <summary>
		/// Matches the copy of a spread source into the collection being built, optionally preceded by
		/// the declaration of the span it is copied from and followed by the advance of the running
		/// index. Both are absent when the compiler had no use for them.
		/// </summary>
		static bool TryMatchSpreadCopy(Statement statement, ILVariable index, Func<Expression?, bool> isCopyDestination,
			[NotNullWhen(true)] out BuilderElement? element, [NotNullWhen(true)] out Statement? lastStatement,
			out bool advancedIndex)
		{
			element = null;
			lastStatement = null;
			advancedIndex = false;
			Statement copyStatement = statement;
			ILVariable? spanTemp = null;
			Expression? source = null;
			if (statement is VariableDeclarationStatement { Variables: [{ Initializer: { } spanInitializer } spanVariable] })
			{
				if (spanVariable.GetILVariable() is not { } temp)
					return false;
				if (statement.GetNextSibling(n => n is Statement) is not Statement afterDeclaration)
					return false;
				spanTemp = temp;
				source = UnwrapSpanConstruction(spanInitializer);
				copyStatement = afterDeclaration;
			}
			if (copyStatement is not ExpressionStatement { Expression: InvocationExpression copy })
				return false;
			if (copy.GetSymbol() is not IMethod { Name: "CopyTo" })
				return false;
			if (copy.Target is not MemberReferenceExpression { Target: { } copySource })
				return false;
			if (copy.Arguments is not [InvocationExpression slice])
				return false;
			if (slice.GetSymbol() is not IMethod { Name: "Slice" })
				return false;
			if (slice.Target is not MemberReferenceExpression { Target: { } sliceTarget })
				return false;
			if (!isCopyDestination(sliceTarget))
				return false;
			if (slice.Arguments is not [var sliceStart, var sliceLength])
				return false;
			if (!IsReferenceTo(sliceStart, index) || !IsLengthOf(sliceLength, copySource))
				return false;
			if (spanTemp == null)
				source = copySource;
			else if (!IsReferenceTo(copySource, spanTemp))
				return false;

			lastStatement = copyStatement;
			if (copyStatement.GetNextSibling(n => n is Statement) is ExpressionStatement { Expression: AssignmentExpression assignment } advance
				&& IsIndexAdvancedBy(assignment, index, copySource))
			{
				lastStatement = advance;
				advancedIndex = true;
			}
			element = new BuilderElement(source!, isSpread: true, spanTemp);
			return true;
		}

		/// <summary>
		/// Returns the collection a span was built over, so the spread reads as <c>.. source</c>
		/// rather than as the span the compiler copied it through.
		/// </summary>
		static Expression UnwrapSpanConstruction(Expression initializer)
		{
			switch (initializer)
			{
				case ObjectCreateExpression { Arguments: [var wrapped] } create
					when create.GetSymbol() is IMethod { IsConstructor: true } constructor
						&& (constructor.DeclaringType.IsKnownType(KnownTypeCode.SpanOfT)
							|| constructor.DeclaringType.IsKnownType(KnownTypeCode.ReadOnlySpanOfT)):
					return wrapped;
				case InvocationExpression { Arguments: [var collection] } invocation
					when IsCollectionsMarshalCall(invocation, "AsSpan"):
					return collection;
				default:
					return initializer;
			}
		}

		/// <summary>
		/// Matches the advance of the running index past a spread, in either the compound form
		/// (<c>index += span.Length</c>) or the plain one (<c>index = index + span.Length</c>). The
		/// compound form appears when ExpressionBuilder's compound-assignment translation engaged;
		/// the plain form is what remains when it did not.
		/// </summary>
		static bool IsIndexAdvancedBy(AssignmentExpression assignment, ILVariable index, Expression source)
		{
			if (!IsReferenceTo(assignment.Left, index))
				return false;
			return assignment.Operator switch {
				AssignmentOperatorType.Add => IsLengthOf(assignment.Right, source),
				AssignmentOperatorType.Assign => assignment.Right is BinaryOperatorExpression {
					Operator: BinaryOperatorType.Add, Left: { } addend, Right: { } increment
				} && IsReferenceTo(addend, index) && IsLengthOf(increment, source),
				_ => false
			};
		}

		/// <summary>
		/// Skips the statement that steps the running index past a single element, written either as
		/// <c>index++</c> or as <c>index = index + 1</c>.
		/// </summary>
		static Statement SkipIndexIncrement(Statement statement, ILVariable index)
		{
			if (statement.GetNextSibling(n => n is Statement) is not ExpressionStatement next)
				return statement;
			switch (next.Expression)
			{
				case UnaryOperatorExpression {
					Operator: UnaryOperatorType.Increment or UnaryOperatorType.PostIncrement,
					Expression: var incremented
				} when IsReferenceTo(incremented, index):
					return next;
				case AssignmentExpression {
					Operator: AssignmentOperatorType.Add, Left: var target, Right: PrimitiveExpression { Value: 1 }
				} when IsReferenceTo(target, index):
					return next;
				case AssignmentExpression {
					Operator: AssignmentOperatorType.Assign, Left: var target,
					Right: BinaryOperatorExpression { Operator: BinaryOperatorType.Add, Left: { } addend, Right: PrimitiveExpression { Value: 1 } }
				} when IsReferenceTo(target, index) && IsReferenceTo(addend, index):
					return next;
				default:
					return statement;
			}
		}

		/// <summary>
		/// Returns whether the length the collection was allocated with is the sum of the element
		/// count and the lengths of the spread sources - i.e. whether the elements found account for
		/// every slot. A mismatch would leave slots the collection expression cannot express.
		/// </summary>
		static bool LengthMatchesElements(Expression lengthExpression, List<BuilderElement> elements)
		{
			var terms = new List<Expression>();
			FlattenSum(lengthExpression, terms);
			var spreads = elements.Where(element => element.IsSpread).ToList();
			int expectedCount = elements.Count - spreads.Count;
			int spreadIndex = 0;
			int count = 0;
			bool sawCount = false;
			foreach (var term in terms)
			{
				if (term is PrimitiveExpression { Value: int literal })
				{
					if (sawCount)
						return false;
					sawCount = true;
					count = literal;
					continue;
				}
				if (spreadIndex >= spreads.Count)
					return false;
				var spread = spreads[spreadIndex];
				if (!IsLengthOf(term, spread.Value)
					&& !(spread.SpanTemp != null && IsLengthOfVariable(term, spread.SpanTemp)))
				{
					return false;
				}
				spreadIndex++;
			}
			return spreadIndex == spreads.Count && count == expectedCount && (sawCount || expectedCount == 0);
		}

		static void FlattenSum(Expression expression, List<Expression> terms)
		{
			switch (expression)
			{
				case ParenthesizedExpression parenthesized:
					FlattenSum(parenthesized.Expression, terms);
					break;
				case BinaryOperatorExpression { Operator: BinaryOperatorType.Add, Left: { } left, Right: { } right }:
					FlattenSum(left, terms);
					FlattenSum(right, terms);
					break;
				default:
					terms.Add(expression);
					break;
			}
		}

		static bool IsLengthOf(Expression expression, Expression collection)
		{
			return expression is MemberReferenceExpression { MemberName: "Length" or "Count" } member
				&& member.Target != null
				&& member.Target.IsMatch(collection);
		}

		static bool IsLengthOfVariable(Expression expression, ILVariable variable)
		{
			return expression is MemberReferenceExpression { MemberName: "Length" or "Count" } member
				&& IsReferenceTo(member.Target, variable);
		}

		/// <summary>
		/// Replaces an element value that is a read of a local the compiler hoisted the value into
		/// with the value itself, and records the declaration for removal. The hoist exists to keep
		/// the value's evaluation ahead of the allocation, which writing the element back into the
		/// collection expression restores on its own.
		/// </summary>
		Expression InlineHoistedValue(BuilderElement element, IReadOnlyList<BuilderElement> elements,
			AstNode memberRoot, HashSet<AstNode> consumed, HashSet<AstNode> hoisted)
		{
			if (element.Value is not IdentifierExpression identifier)
				return element.Value;
			if (identifier.GetILVariable() is not { } local)
				return element.Value;
			var declarations = AttachedDeclarations(memberRoot, local).ToArray();
			if (declarations is not [{ Initializer: { } value } declaration])
				return element.Value;
			if (declaration.Parent is not VariableDeclarationStatement declarationStatement || !hoisted.Contains(declarationStatement))
				return element.Value;
			var scope = new HashSet<AstNode>(consumed) { declarationStatement };
			if (ReferencesOutside(memberRoot, local, scope).Any())
				return element.Value;
			// The declaration goes away with the inline, so this element's read has to be the
			// local's only reference that survives into the collection expression. Another element
			// whose value also reads the local - a repeated spread, or a sibling computed from it -
			// would either dangle or end up fighting over the single initializer node.
			foreach (var other in elements)
			{
				if (other != element && other.Value.DescendantsAndSelf.OfType<IdentifierExpression>()
					.Any(reference => reference.GetILVariable() == local))
				{
					return element.Value;
				}
			}
			element.HoistedDeclaration = declarationStatement;
			return value;
		}

		/// <summary>
		/// Returns the run of single-variable declarations immediately preceding the builder. The
		/// compiler hoists element values into locals there so that they are evaluated before the
		/// collection is allocated; nothing else stands between them, so folding those values back
		/// into the collection expression cannot move them across anything.
		/// </summary>
		static HashSet<AstNode> HoistedDeclarationsBefore(Statement builderStart)
		{
			var declarations = new HashSet<AstNode>();
			var current = builderStart.GetPrevSibling(n => n is Statement);
			while (current is VariableDeclarationStatement { Variables.Count: 1 })
			{
				declarations.Add(current);
				current = current.GetPrevSibling(n => n is Statement);
			}
			return declarations;
		}

		static HashSet<AstNode> StatementsBetween(Statement first, Statement last)
		{
			var statements = new HashSet<AstNode>();
			Statement? current = first;
			while (current != null)
			{
				statements.Add(current);
				if (current == last)
					break;
				current = current.GetNextSibling(n => n is Statement) as Statement;
			}
			return statements;
		}

		IdentifierExpression[] ReferencesOutside(AstNode memberRoot, ILVariable variable, HashSet<AstNode> statements)
		{
			return AttachedReferences(memberRoot, variable)
				.Where(identifier => !identifier.Ancestors.Any(statements.Contains))
				.ToArray();
		}

		/// <summary>
		/// Per-member index of identifier references and declarations by IL variable. Walking every
		/// descendant of the member for each candidate would be quadratic in generated members full
		/// of builder shapes; this transform only moves or removes existing nodes (it never creates
		/// new identifier references), so the index built on first use stays complete and consumers
		/// merely skip entries that have since been detached from the member.
		/// </summary>
		sealed class MemberReferenceIndex
		{
			public readonly Dictionary<ILVariable, List<IdentifierExpression>> Identifiers = new();
			public readonly Dictionary<ILVariable, List<VariableInitializer>> Declarations = new();
		}

		readonly Dictionary<AstNode, MemberReferenceIndex> memberIndexes = new();

		MemberReferenceIndex GetMemberIndex(AstNode memberRoot)
		{
			if (memberIndexes.TryGetValue(memberRoot, out var index))
				return index;
			index = new MemberReferenceIndex();
			foreach (var node in memberRoot.Descendants)
			{
				switch (node)
				{
					case IdentifierExpression identifier when identifier.GetILVariable() is { } variable:
						AddToIndex(index.Identifiers, variable, identifier);
						break;
					case VariableInitializer initializer when initializer.GetILVariable() is { } declared:
						AddToIndex(index.Declarations, declared, initializer);
						break;
				}
			}
			memberIndexes.Add(memberRoot, index);
			return index;

			static void AddToIndex<T>(Dictionary<ILVariable, List<T>> map, ILVariable variable, T node)
			{
				if (!map.TryGetValue(variable, out var list))
				{
					list = new List<T>();
					map.Add(variable, list);
				}
				list.Add(node);
			}
		}

		IEnumerable<IdentifierExpression> AttachedReferences(AstNode memberRoot, ILVariable variable)
		{
			if (!GetMemberIndex(memberRoot).Identifiers.TryGetValue(variable, out var references))
				yield break;
			foreach (var reference in references)
			{
				if (reference.Ancestors.Contains(memberRoot))
					yield return reference;
			}
		}

		IEnumerable<VariableInitializer> AttachedDeclarations(AstNode memberRoot, ILVariable variable)
		{
			if (!GetMemberIndex(memberRoot).Declarations.TryGetValue(variable, out var declarations))
				yield break;
			foreach (var declaration in declarations)
			{
				if (declaration.Ancestors.Contains(memberRoot))
					yield return declaration;
			}
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
			if (!IsSpanTargetedPosition(use, buffer.Type.GetInlineArrayElementType(), out var castTo))
				return false;
			if (!NothingWithSideEffectsPrecedes(use, useStatement) && !elements.All(IsSideEffectFree))
				return false;

			ReplaceWithCollectionExpression(use, elements, castTo);
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
		bool TryGetConstantCount(Expression expression, Statement statement, out int count, out Statement? countDeclaration)
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
					var declarations = AttachedDeclarations(memberRoot, variable).ToArray();
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
			return HasLengthSuffix(definition.Name, "<>y__InlineArray")
				|| (definition.Namespace == "System.Runtime.CompilerServices"
					&& HasLengthSuffix(definition.Name, "InlineArray"));
		}

		/// <summary>
		/// Returns whether <paramref name="name"/> is <paramref name="prefix"/> followed by the
		/// buffer's length. The length is what makes the name a generated buffer's rather than a
		/// coincidence, so a type merely starting with the prefix is not one of these.
		/// </summary>
		static bool HasLengthSuffix(string name, string prefix)
		{
			if (name.Length <= prefix.Length || !name.StartsWith(prefix, StringComparison.Ordinal))
				return false;
			for (int i = prefix.Length; i < name.Length; i++)
			{
				if (name[i] < '0' || name[i] > '9')
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns whether <paramref name="expression"/> stands where a <c>Span&lt;T&gt;</c> or
		/// <c>ReadOnlySpan&lt;T&gt;</c> over <paramref name="elementType"/> is expected, which is what
		/// gives a collection expression in that position its target type.
		/// </summary>
		bool IsSpanTargetedPosition(Expression expression, IType elementType, out AstType? castTo)
		{
			castTo = null;
			if (!IsSpanTargetedPosition(expression, elementType))
				return false;
			return CanTargetType(expression, out castTo);
		}

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
				case CastExpression cast:
					return IsSpanOf(cast.Type.GetResolveResult().Type, elementType);
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

		/// <summary>
		/// The element types are compared by name rather than by identity: the buffer's element type
		/// and the parameter's are resolved along different paths, and a reference set that carries
		/// two copies of the same assembly resolves them to distinct types that are nonetheless the
		/// same type.
		/// </summary>
		static bool IsSpanOf(IType type, IType elementType)
		{
			return (type.IsKnownType(KnownTypeCode.SpanOfT) || type.IsKnownType(KnownTypeCode.ReadOnlySpanOfT))
				&& type.TypeArguments.Count == 1
				&& type.TypeArguments[0].ReflectionName == elementType.ReflectionName;
		}

		/// <summary>
		/// Returns whether moving the element values into <paramref name="use"/> preserves evaluation
		/// order, i.e. whether everything the statement evaluates before that position is free of
		/// side effects.
		/// </summary>
		static bool NothingWithSideEffectsPrecedes(Expression use, Statement statement)
		{
			AstNode node = use;
			while (node != statement && node.Parent is { } parent)
			{
				foreach (var sibling in parent.Children)
				{
					if (sibling == node)
						break;
					// A member-name callee is named, not evaluated; what runs before the arguments
					// is only its receiver. Any other callee shape - a delegate-producing call, an
					// indexed handler - is a real expression evaluated before the arguments and has
					// to be side-effect-free as a whole.
					if (parent is InvocationExpression invocation && sibling == invocation.Target)
					{
						if (sibling is MemberReferenceExpression callee)
						{
							if (!IsSideEffectFree(callee.Target))
								return false;
						}
						else if (sibling is Expression calleeExpression && !IsSideEffectFree(calleeExpression))
						{
							return false;
						}
						continue;
					}
					// Likewise, the target of an assignment only evaluates what addresses the storage
					// location; the store itself happens after the value.
					if (parent is AssignmentExpression assignment && sibling == assignment.Left)
					{
						if (!IsTargetEvaluationSideEffectFree(assignment.Left))
							return false;
						continue;
					}
					if (sibling is Expression expression && !IsSideEffectFree(expression))
						return false;
				}
				node = parent;
			}
			return true;
		}

		/// <summary>
		/// Returns whether naming the storage location <paramref name="target"/> is free of side
		/// effects, ignoring the store itself: a member access evaluates its receiver, an element
		/// access also its indices, and a local or field name evaluates nothing.
		/// </summary>
		static bool IsTargetEvaluationSideEffectFree(Expression target)
		{
			switch (target)
			{
				case IdentifierExpression:
					return true;
				case MemberReferenceExpression { Target: { } receiver }:
					return IsSideEffectFree(receiver);
				case IndexerExpression { Target: { } collection } indexer:
					return IsSideEffectFree(collection) && indexer.Arguments.All(IsSideEffectFree);
				default:
					return IsSideEffectFree(target);
			}
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
					// A method name is the target of the call being built, not a call of its own.
					return identifier.GetILVariable() != null || identifier.GetSymbol() is IField or IMethod;
				case MemberReferenceExpression member:
					return member.GetSymbol() is IField or IMethod && IsSideEffectFree(member.Target);
				case ParenthesizedExpression parenthesized:
					return IsSideEffectFree(parenthesized.Expression);
				case CastExpression cast:
					return cast.GetSymbol() is not IMethod && IsSideEffectFree(cast.Expression);
				default:
					return false;
			}
		}

		int CountReferences(AstNode root, ILVariable variable)
		{
			return AttachedReferences(root, variable).Count();
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
		static void ReplaceWithCollectionExpression(Expression expression, IEnumerable<Expression> elements, AstType? castTo = null)
		{
			var detached = elements.ToArray();
			foreach (var element in detached)
			{
				element.Remove();
			}
			var collectionExpression = new CollectionExpression(detached).CopyAnnotationsFrom(expression);
			expression.ReplaceWith(Cast(collectionExpression, castTo, expression.GetResolveResult()));
		}

		/// <summary>
		/// Casts a collection expression to <paramref name="castTo"/>, which pins down the one
		/// conversion the position had before. Without it the collection expression would offer
		/// itself to every collection overload at once.
		/// </summary>
		static Expression Cast(Expression collectionExpression, AstType? castTo, ResolveResult resolveResult)
		{
			if (castTo == null)
				return collectionExpression;
			return new CastExpression(castTo, collectionExpression)
				.WithRR(new ConversionResolveResult(castTo.GetResolveResult().Type, resolveResult, Conversion.IdentityConversion));
		}

		/// <summary>
		/// Returns whether the position <paramref name="expression"/> occupies gives a collection
		/// expression a target type. The positions are an allow-list: one that is not known to
		/// target-type its operand keeps the original syntax. <paramref name="castTo"/> reports the
		/// type the collection expression must be cast to for the position to keep its meaning, and
		/// is null where the position types it on its own.
		/// </summary>
		bool CanTargetType(Expression expression, out AstType? castTo)
		{
			castTo = null;
			if (!TryGetTargetType(expression, out var targetType, out bool needsCast))
				return false;
			if (!IsCollectionExpressionTarget(targetType))
				return false;
			if (!needsCast)
				return true;
			var type = context.TypeSystemAstBuilder.ConvertType(targetType);
			if (!ParsesAsCastOfCollectionExpression(type))
				return false;
			castTo = type;
			return true;
		}

		/// <summary>
		/// Returns whether <c>(type)[...]</c> reads as a cast rather than as an element access on a
		/// parenthesized expression. The two are told apart by the type syntax alone: a bare name
		/// could equally be an expression, while type arguments, an array or pointer suffix, a
		/// predefined type keyword or a <c>global::</c> qualifier could not.
		/// </summary>
		static bool ParsesAsCastOfCollectionExpression(AstType type)
		{
			switch (type)
			{
				case Syntax.PrimitiveType:
				case ComposedType:
					return true;
				case SimpleType simple:
					return simple.TypeArguments.Count > 0;
				case MemberType member:
					return member.TypeArguments.Count > 0 || member.IsDoubleColon;
				default:
					return false;
			}
		}

		/// <summary>
		/// Determines the type the position imposes on its operand. A position whose type cannot be
		/// read off the syntax is reported as no target at all, so the collection expression is not
		/// written there.
		/// </summary>
		static bool TryGetTargetType(Expression expression, [NotNullWhen(true)] out IType? targetType, out bool needsCast)
		{
			targetType = null;
			needsCast = false;
			switch (expression.Parent)
			{
				case VariableInitializer { Parent: VariableDeclarationStatement declaration }:
					// `var x = [1, 2];` has nothing to infer the type from.
					if (declaration.Type.IsVar())
						return false;
					targetType = declaration.Type.GetResolveResult().Type;
					break;
				case AssignmentExpression { Operator: AssignmentOperatorType.Assign } assignment when assignment.Right == expression:
					targetType = assignment.Left.GetResolveResult().Type;
					break;
				// A member of an object initializer is typed by the member being assigned.
				case NamedExpression named:
					targetType = (named.GetSymbol() as IMember)?.ReturnType;
					break;
				case ReturnStatement returnStatement:
					targetType = GetReturnedType(returnStatement);
					break;
				case InvocationExpression invocation:
					targetType = GetParameterType(invocation.GetSymbol() as IMethod, invocation.Arguments, expression, out needsCast);
					break;
				case ObjectCreateExpression create:
					targetType = GetParameterType(create.GetSymbol() as IMethod, create.Arguments, expression, out needsCast);
					break;
				// An expression already carrying a cast is typed by it; the spread builders in
				// particular end up inside the cast the wrapper strip leaves behind.
				case CastExpression cast:
					targetType = cast.Type.GetResolveResult().Type;
					break;
				default:
					return false;
			}
			return targetType is { Kind: not TypeKind.Unknown and not TypeKind.None };
		}

		static IType? GetReturnedType(ReturnStatement returnStatement)
		{
			foreach (var ancestor in returnStatement.Ancestors)
			{
				switch (ancestor)
				{
					case LambdaExpression or AnonymousMethodExpression:
						// The delegate the lambda was converted to is what types its returns.
						var delegateType = ancestor.Annotation<ILFunction>()?.DelegateType ?? ancestor.GetResolveResult().Type;
						return delegateType?.GetDelegateInvokeMethod()?.ReturnType;
					case EntityDeclaration entity:
						return (entity.GetSymbol() as IMethod)?.ReturnType;
				}
			}
			return null;
		}

		static IType? GetParameterType(IMethod? method, AstNodeCollection<Expression> arguments, Expression argument, out bool needsCast)
		{
			needsCast = false;
			if (method == null)
				return null;
			int index = arguments.TakeWhile(a => a != argument).Count();
			if (index >= method.Parameters.Count)
				return null;
			needsCast = !KeepsOverloadResolution(method, index);
			return method.Parameters[index].Type;
		}

		static IType? GetParameterTypeAt(IMethod? method, AstNodeCollection<Expression> arguments, Expression argument)
		{
			if (method == null)
				return null;
			int index = arguments.TakeWhile(a => a != argument).Count();
			return index < method.Parameters.Count ? method.Parameters[index].Type : null;
		}

		/// <summary>
		/// Returns whether writing a collection expression at an argument position still selects the
		/// overload the lowered call selected. The lowered argument has a definite type and so can
		/// name one particular overload, while a collection expression converts to every collection
		/// parameter type at once: a second overload taking a collection there makes the call
		/// ambiguous.
		/// </summary>
		static bool KeepsOverloadResolution(IMethod method, int index)
		{
			var chosen = method.Parameters[index].Type;
			var candidates = method.IsConstructor
				? method.DeclaringType.GetConstructors()
				: method.DeclaringType.GetMethods(m => m.Name == method.Name);
			foreach (var candidate in candidates)
			{
				if (candidate.Parameters.Count != method.Parameters.Count)
					continue;
				var parameterType = candidate.Parameters[index].Type;
				if (parameterType.ReflectionName == chosen.ReflectionName)
					continue;
				if (IsCollectionExpressionTarget(parameterType))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns whether a collection expression converts to <paramref name="type"/>: an array, a
		/// span, one of the generic collection interfaces, a type that names its builder, or a
		/// collection the compiler can construct and then fill one element at a time. Types that
		/// merely happen to be enumerable, such as <c>string</c>, and types that are only a base of
		/// the collection, such as <c>object</c>, are not targets.
		/// </summary>
		static bool IsCollectionExpressionTarget(IType type)
		{
			if (type.Kind == TypeKind.Array)
				return true;
			if (type.IsKnownType(KnownTypeCode.SpanOfT) || type.IsKnownType(KnownTypeCode.ReadOnlySpanOfT))
				return true;
			if (type.Kind == TypeKind.Interface)
			{
				// Only these five generic interfaces are collection-expression targets. The
				// non-generic System.Collections.IEnumerable is not one of them: it is neither
				// constructible nor on the list, so `[...]` there is CS9174.
				return type.IsKnownType(KnownTypeCode.IEnumerableOfT)
					|| type.IsKnownType(KnownTypeCode.ICollectionOfT)
					|| type.IsKnownType(KnownTypeCode.IListOfT)
					|| type.IsKnownType(KnownTypeCode.IReadOnlyCollectionOfT)
					|| type.IsKnownType(KnownTypeCode.IReadOnlyListOfT);
			}
			if (type.Kind is not (TypeKind.Class or TypeKind.Struct))
				return false;
			if (type.GetDefinition() is { } definition
				&& definition.GetAttributes().Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CollectionBuilderAttribute"))
			{
				return true;
			}
			return IsFillableCollection(type);
		}

		/// <summary>
		/// Returns whether the compiler could build <paramref name="type"/> by allocating it and
		/// adding one element at a time. That requires an enumerable type it can actually construct:
		/// an abstract type has no instance to fill (CS0144), and one whose constructors all demand
		/// arguments cannot be allocated at all (CS9214).
		/// </summary>
		static bool IsFillableCollection(IType type)
		{
			if (type.GetDefinition() is not { IsAbstract: false })
				return false;
			if (!type.GetAllBaseTypes().Any(baseType => baseType.IsKnownType(KnownTypeCode.IEnumerable)))
				return false;
			// A private constructor is reachable only from inside the type itself; treating it as
			// unusable here at worst leaves the original syntax in place.
			if (!type.GetConstructors(m => m.Accessibility != Accessibility.Private
				&& m.Parameters.All(p => p.IsOptional || p.IsParams)).Any())
			{
				return false;
			}
			return type.GetMethods(m => !m.IsStatic && m.Name == "Add" && m.Parameters.Count == 1).Any();
		}
	}
}

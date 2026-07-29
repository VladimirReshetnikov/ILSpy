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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	public class IntroduceUnsafeModifier : DepthFirstAstVisitor<bool>, IAstTransform
	{
		TransformContext? context;

		public void Run(AstNode compilationUnit, TransformContext context)
		{
			this.context = context;
			try
			{
				compilationUnit.AcceptVisitor(this);
			}
			finally
			{
				this.context = null;
			}
		}

		public static bool IsUnsafe(AstNode node)
		{
			return node.AcceptVisitor(new IntroduceUnsafeModifier());
		}

		// Run() sets the context, but the static IsUnsafe() entry point drives this visitor with no
		// context, so step recording must tolerate a null context. Both helpers compile out entirely
		// in non-STEP (Release) builds, so the null check only exists in debug step-recording builds.
		[Conditional("STEP")]
		void Step(string description, AstNode node)
		{
			if (context != null)
				context.Step(description, node);
		}

		[Conditional("STEP")]
		void EndStep(AstNode node)
		{
			if (context != null)
				context.EndStep(node);
		}

		protected override bool VisitChildren(AstNode node)
		{
			bool result = false;
			AstNode? next;
			for (AstNode? child = node.FirstChild; child != null; child = next)
			{
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				result |= child.AcceptVisitor(this);
			}
			if (result && node is EntityDeclaration entity && !(node is Accessor))
			{
				// C# forbids 'await' anywhere inside an unsafe context, so a method whose body
				// lexically contains an await (an async method, or one holding an async lambda or
				// local function) cannot carry the 'unsafe' modifier. Wrap only the pointer-using
				// statements in inner 'unsafe' blocks that exclude the awaits instead.
				if (entity is MethodDeclaration { Body: BlockStatement body } && ContainsAwait(body))
				{
					Step("Introduce unsafe blocks", node);
					IntroduceUnsafeBlocks(body);
					return false;
				}
				Step("Add unsafe modifier", node);
				entity.Modifiers |= Modifiers.Unsafe;
				return false;
			}
			return result;
		}

		/// <summary>
		/// Scopes the pointer usage inside <paramref name="block"/> to inner 'unsafe' blocks that
		/// never lexically contain an await. Consecutive await-free statements that need an unsafe
		/// context are grouped into a single block; a statement that carries both pointer usage and
		/// an await (e.g. a pointer-using lambda passed to an awaited call) is descended into so the
		/// unsafe block can be placed around the nested body that actually needs it.
		/// </summary>
		void IntroduceUnsafeBlocks(BlockStatement block)
		{
			var runs = new List<(Statement first, Statement? afterExclusive)>();
			var descend = new List<Statement>();
			Statement? runStart = null;
			for (Statement? stmt = block.Statements.FirstOrDefault(); stmt != null; stmt = stmt.GetNextStatement())
			{
				if (ContainsUnenclosedUnsafe(stmt))
				{
					if (!ContainsAwait(stmt))
					{
						runStart ??= stmt;
						continue;
					}
					descend.Add(stmt);
				}
				if (runStart != null)
				{
					runs.Add((runStart, stmt));
					runStart = null;
				}
			}
			if (runStart != null)
				runs.Add((runStart, null));

			// Mutate only after the scan so the statement iteration above is not disturbed.
			foreach (var (first, afterExclusive) in runs)
				WrapStatementsInUnsafe(first, afterExclusive);
			foreach (var stmt in descend)
				DescendForUnsafe(stmt);
		}

		/// <summary>
		/// Finds the nested blocks and lambda/local-function bodies that carry the pointer usage
		/// inside a statement that cannot itself be wrapped (because it also contains an await),
		/// and introduces the unsafe blocks there.
		/// </summary>
		void DescendForUnsafe(AstNode node)
		{
			for (AstNode? child = node.FirstChild; child != null; child = child.NextSibling)
			{
				if (!ContainsUnenclosedUnsafe(child))
					continue;
				switch (child)
				{
					case BlockStatement block:
						IntroduceUnsafeBlocks(block);
						break;
					case AnonymousMethodExpression { Body: { } anonBody }:
						IntroduceUnsafeBlocks(anonBody);
						break;
					case LambdaExpression { Body: BlockStatement lambdaBlock }:
						IntroduceUnsafeBlocks(lambdaBlock);
						break;
					case LambdaExpression lambda:
						WrapExpressionLambdaBody(lambda);
						break;
					case LocalFunctionDeclarationStatement { Declaration.Body: BlockStatement localFunctionBody }:
						IntroduceUnsafeBlocks(localFunctionBody);
						break;
					default:
						DescendForUnsafe(child);
						break;
				}
			}
		}

		/// <summary>
		/// Moves the statements in the range [<paramref name="first"/>, <paramref name="afterExclusive"/>)
		/// into a fresh 'unsafe' block placed where <paramref name="first"/> was.
		/// </summary>
		void WrapStatementsInUnsafe(Statement first, Statement? afterExclusive)
		{
			Step("Add unsafe block", first);
			var newBlock = new BlockStatement();
			Statement? next;
			for (Statement? stmt = first.GetNextStatement(); stmt != null && stmt != afterExclusive; stmt = next)
			{
				next = stmt.GetNextStatement();
				newBlock.Add(stmt.Detach());
			}
			var unsafeStatement = new UnsafeStatement { Body = newBlock };
			first.ReplaceWith(unsafeStatement);
			newBlock.Statements.InsertAfter(null, first);
			EndStep(unsafeStatement);
		}

		/// <summary>
		/// Rewrites an expression-bodied lambda into a block-bodied one whose single statement is
		/// wrapped in an 'unsafe' block, for the rare case where a pointer-using expression lambda
		/// sits inside an awaited call.
		/// </summary>
		void WrapExpressionLambdaBody(LambdaExpression lambda)
		{
			if (lambda.Body is not Expression expression)
				return;
			Step("Wrap lambda body in unsafe block", lambda);
			Statement inner = LambdaReturnsVoid(lambda, expression)
				? new ExpressionStatement(expression.Detach())
				: new ReturnStatement(expression.Detach());
			var innerBlock = new BlockStatement();
			innerBlock.Add(inner);
			var lambdaBlock = new BlockStatement();
			lambdaBlock.Add(new UnsafeStatement { Body = innerBlock });
			lambda.Body = lambdaBlock;
			EndStep(lambda);
		}

		/// <summary>
		/// Whether the block the lambda body becomes must discard the value rather than return it.
		/// The delegate decides that, not the expression: <c>Action a = () =&gt; i++;</c> has a body
		/// of type int that the delegate throws away, and returning it would not compile (CS0127).
		/// The expression's own type only stands in where the delegate is unknown.
		/// </summary>
		static bool LambdaReturnsVoid(LambdaExpression lambda, Expression body)
		{
			var delegateType = lambda.Annotation<ILFunction>()?.DelegateType ?? lambda.GetResolveResult()?.Type;
			if (delegateType?.GetDelegateInvokeMethod() is { } invokeMethod)
				return invokeMethod.ReturnType.Kind == TypeKind.Void;
			return body.GetResolveResult()?.Type.Kind == TypeKind.Void;
		}

		/// <summary>
		/// Whether <paramref name="node"/> lexically contains an await expression, descending into
		/// nested lambdas and local functions (an unsafe modifier or block would cover those too).
		/// </summary>
		static bool ContainsAwait(AstNode node)
		{
			if (node is UnaryOperatorExpression { Operator: UnaryOperatorType.Await })
				return true;
			for (AstNode? child = node.FirstChild; child != null; child = child.NextSibling)
			{
				if (ContainsAwait(child))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Whether <paramref name="node"/> contains pointer usage that is not already enclosed in an
		/// 'unsafe' scope (an unsafe-modified entity or an existing unsafe block). Mirrors the
		/// leaf conditions of the visitor above.
		/// </summary>
		bool ContainsUnenclosedUnsafe(AstNode node)
		{
			switch (node)
			{
				case UnsafeStatement:
					return false;
				case EntityDeclaration entity when (entity.Modifiers & Modifiers.Unsafe) != 0:
					return false;
				case PointerReferenceExpression:
				case SizeOfExpression:
				case FixedVariableInitializer:
				case FunctionPointerAstType:
					return true;
				case ComposedType { PointerRank: > 0 }:
					return true;
				case UnaryOperatorExpression { Operator: UnaryOperatorType.Dereference }:
				case UnaryOperatorExpression { Operator: UnaryOperatorType.AddressOf }:
					return true;
				case MemberReferenceExpression:
				case IdentifierExpression:
				case StackAllocExpression:
				case InvocationExpression:
					if (HasUnsafeResolveResult(node))
						return true;
					break;
			}
			for (AstNode? child = node.FirstChild; child != null; child = child.NextSibling)
			{
				if (ContainsUnenclosedUnsafe(child))
					return true;
			}
			return false;
		}

		public override bool VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			base.VisitPointerReferenceExpression(pointerReferenceExpression);
			return true;
		}

		public override bool VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			// C# sizeof(MyStruct) requires unsafe{}
			// (not for sizeof(int), but that gets constant-folded and thus decompiled to 4)
			base.VisitSizeOfExpression(sizeOfExpression);
			return true;
		}

		public override bool VisitComposedType(ComposedType composedType)
		{
			if (composedType.PointerRank > 0)
				return true;
			else
				return base.VisitComposedType(composedType);
		}

		public override bool VisitFunctionPointerType(FunctionPointerAstType functionPointerType)
		{
			return true;
		}

		public override bool VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			bool result = base.VisitUnaryOperatorExpression(unaryOperatorExpression);
			if (unaryOperatorExpression.Operator == UnaryOperatorType.Dereference)
			{
				var bop = unaryOperatorExpression.Expression as BinaryOperatorExpression;
				if (bop != null && bop.Operator == BinaryOperatorType.Add
					&& bop.GetResolveResult() is OperatorResolveResult orr
					&& orr.Operands.FirstOrDefault()?.Type.Kind == TypeKind.Pointer)
				{
					Step("Replace pointer addition with indexer", unaryOperatorExpression);
					// transform "*(ptr + int)" to "ptr[int]"
					IndexerExpression indexer = new IndexerExpression();
					indexer.Target = bop.Left!.Detach();
					indexer.Arguments.Add(bop.Right!.Detach());
					indexer.CopyAnnotationsFrom(unaryOperatorExpression);
					indexer.CopyAnnotationsFrom(bop);
					unaryOperatorExpression.ReplaceWith(indexer);
					EndStep(indexer);
				}
				return true;
			}
			else if (unaryOperatorExpression.Operator == UnaryOperatorType.AddressOf)
			{
				return true;
			}
			else
			{
				return result;
			}
		}

		public override bool VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			bool result = base.VisitMemberReferenceExpression(memberReferenceExpression);
			UnaryOperatorExpression? uoe = memberReferenceExpression.Target as UnaryOperatorExpression;
			if (uoe != null && uoe.Operator == UnaryOperatorType.Dereference)
			{
				Step("Replace pointer member access", memberReferenceExpression);
				PointerReferenceExpression pre = new PointerReferenceExpression();
				pre.Target = uoe.Expression.Detach();
				pre.MemberName = memberReferenceExpression.MemberName;
				memberReferenceExpression.TypeArguments.MoveTo(pre.TypeArguments);
				pre.CopyAnnotationsFrom(uoe);
				pre.RemoveAnnotations<ResolveResult>(); // only copy the ResolveResult from the MRE
				pre.CopyAnnotationsFrom(memberReferenceExpression);
				memberReferenceExpression.ReplaceWith(pre);
				EndStep(pre);
			}
			if (HasUnsafeResolveResult(memberReferenceExpression))
				return true;
			return result;
		}

		public override bool VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			bool result = base.VisitIdentifierExpression(identifierExpression);
			if (HasUnsafeResolveResult(identifierExpression))
				return true;
			return result;
		}

		public override bool VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			bool result = base.VisitStackAllocExpression(stackAllocExpression);
			if (HasUnsafeResolveResult(stackAllocExpression))
				return true;
			return result;
		}

		public override bool VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			bool result = base.VisitInvocationExpression(invocationExpression);
			if (HasUnsafeResolveResult(invocationExpression))
				return true;
			return result;
		}

		public override bool VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			bool result = base.VisitObjectCreateExpression(objectCreateExpression);
			if (HasUnsafeResolveResult(objectCreateExpression))
				return true;
			return result;
		}

		public override bool VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			base.VisitFixedVariableInitializer(fixedVariableInitializer);
			return true;
		}

		private bool HasUnsafeResolveResult(AstNode node)
		{
			var rr = node.GetResolveResult();
			if (rr == null)
				return false;
			if (IsUnsafeType(rr.Type))
				return true;
			if ((rr as MemberResolveResult)?.Member is IParameterizedMember pm)
			{
				if (pm.Parameters.Any(p => IsUnsafeType(p.Type)))
					return true;
			}
			else if (rr is MethodGroupResolveResult)
			{
				var chosenMethod = node.GetSymbol();
				if (chosenMethod is IParameterizedMember pm2)
				{
					if (IsUnsafeType(pm2.ReturnType))
						return true;
					if (pm2.Parameters.Any(p => IsUnsafeType(p.Type)))
						return true;
				}
			}
			return false;
		}

		private bool IsUnsafeType(IType type)
		{
			switch (type?.Kind)
			{
				case TypeKind.Pointer:
				case TypeKind.FunctionPointer:
					return true;
				case TypeKind.ByReference:
				case TypeKind.Array:
					return IsUnsafeType(((Decompiler.TypeSystem.Implementation.TypeWithElementType)type).ElementType);
				default:
					return false;
			}
		}
	}
}

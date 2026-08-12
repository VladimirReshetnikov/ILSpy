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
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Rewrites a narrow Visual Basic iterator pattern into legal C# when a yield occurs in a
	/// try block with a catch clause.
	/// </summary>
	/// <remarks>
	/// C# does not permit the yield itself in the protected region. For a foreach using an
	/// enumerator that can never require disposal, the potentially throwing work can instead be
	/// performed one element at a time inside the original try/catch and its result yielded just
	/// outside it. The intentionally strict matcher keeps unsupported shapes in their faithful
	/// source-like form rather than risking a change in exception or disposal behavior.
	/// </remarks>
	sealed class LegalizeYieldInTryCatch : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			foreach (var tryCatch in rootNode.DescendantsAndSelf.OfType<TryCatchStatement>().ToArray())
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				TryRewrite(tryCatch, context);
			}
		}

		static void TryRewrite(TryCatchStatement tryCatch, TransformContext context)
		{
			if (tryCatch.Parent is not BlockStatement parentBlock
				|| tryCatch.FinallyBlock is not null
				|| tryCatch.CatchClauses.Count != 1
				|| tryCatch.TryBlock.Statements.Count != 1
				|| tryCatch.TryBlock.Statements.First() is not ForeachStatement foreachStatement
				|| foreachStatement.IsAsync
				|| foreachStatement.VariableDesignation is not SingleVariableDesignation designation
				|| designation.Annotation<ResolveResult>() is not ILVariableResolveResult itemResolveResult
				|| foreachStatement.EmbeddedStatement is not BlockStatement foreachBody
				|| foreachBody.Statements.Count != 1
				|| foreachBody.Statements.First() is not YieldReturnStatement yieldReturn
				|| foreachStatement.Annotation<ForeachAnnotation>() is not { } foreachAnnotation)
			{
				return;
			}

			var catchClause = tryCatch.CatchClauses.Single();
			if (catchClause.Condition is not null
				|| catchClause.Body.Statements.Any(statement => statement is not ExpressionStatement))
			{
				return;
			}

			var function = tryCatch.AncestorsAndSelf
				.SelectMany(node => node.Annotations.OfType<ILFunction>())
				.FirstOrDefault();
			if (function is null
				|| function.Method is not { DeclaringTypeDefinition: { } } functionMethod
				|| !function.IsIterator || function.IsAsync)
			{
				return;
			}

			if (foreachAnnotation.GetEnumeratorCall is not CallInstruction getEnumeratorCall
				|| foreachAnnotation.MoveNextCall is not CallInstruction moveNextCall
				|| foreachAnnotation.GetCurrentCall is not CallInstruction getCurrentCall
				|| !HasNoDisposeForeachProvenance(foreachStatement, moveNextCall)
				|| getCurrentCall.Method.AccessorOwner is not IProperty { CanGet: true, IsIndexer: false } currentProperty
				|| !AssignVariableNames.IsValidName(currentProperty.Name)
				|| currentProperty.Getter is not { } currentGetter
				|| !currentGetter.Equals(getCurrentCall.Method, NormalizeTypeVisitor.TypeErasure))
			{
				return;
			}

			var resolver = new CSharpResolver(new CSharpTypeResolveContext(
				context.TypeSystem.MainModule,
				context.DecompileRun.UsingScope,
				functionMethod.DeclaringTypeDefinition,
				functionMethod));
			var memberLookup = resolver.CreateMemberLookup();
			var collectionResolveResult = foreachStatement.InExpression.GetResolveResult();
			if (!TryValidateEnumeratorCalls(getEnumeratorCall, moveNextCall, getCurrentCall,
				currentProperty, collectionResolveResult, function, resolver, out _)
				|| !HasAccessibleGetter(currentProperty, memberLookup)
				|| !moveNextCall.Method.ReturnType.IsKnownType(KnownTypeCode.Boolean)
				|| getCurrentCall.Method.ReturnType is ByReferenceType)
			{
				return;
			}

			var enumeratorType = getEnumeratorCall.Method.ReturnType;
			var enumeratorDefinition = enumeratorType.GetDefinition();
			if (enumeratorDefinition is not { Kind: TypeKind.Struct, IsByRefLike: false }
				|| !IsCSharpNameableAndAccessible(enumeratorType, memberLookup)
				|| EnumeratorRequiresDisposal(enumeratorType))
			{
				return;
			}

			var itemVariable = itemResolveResult.Variable;
			if (itemVariable.Kind != VariableKind.ForeachLocal
				|| !itemVariable.Type.Equals(getCurrentCall.Method.ReturnType)
				|| !IsCSharpNameableAndAccessible(itemVariable.Type, memberLookup)
				|| !IsForeachVariableConfined(itemVariable, getCurrentCall, yieldReturn,
					designation, tryCatch.AncestorsAndSelf.Last(), function)
				|| yieldReturn.Expression.DescendantsAndSelf.Any(node => node is LambdaExpression
					or AnonymousMethodExpression or LocalFunctionDeclarationStatement))
			{
				return;
			}

			var elementType = function.ReturnType.GetElementTypeFromIEnumerable(context.TypeSystem, true, out _);
			if (elementType.Kind is TypeKind.Unknown or TypeKind.None
				|| elementType.IsByRefLike
				|| !IsCSharpNameableAndAccessible(elementType, memberLookup)
				|| !yieldReturn.Expression.GetResolveResult().Type.Equals(elementType))
			{
				return;
			}

			context.Step("Move yield outside Visual Basic try/catch", tryCatch);

			var boolType = context.TypeSystem.FindType(KnownTypeCode.Boolean);
			var rewrittenEnumeratorVariable = RegisterVariable(function, enumeratorType, context);
			var initializedVariable = RegisterVariable(function, boolType, context);
			var completedVariable = RegisterVariable(function, boolType, context);
			var hasValueVariable = RegisterVariable(function, boolType, context);
			var resultVariable = RegisterVariable(function, elementType, context);
			itemVariable.Kind = VariableKind.Local;

			var collectionExpression = foreachStatement.InExpression.Detach();
			var yieldExpression = yieldReturn.Expression.Detach();

			parentBlock.Statements.InsertBefore(tryCatch,
				AssignmentStatement(rewrittenEnumeratorVariable, DefaultValue(enumeratorType, context)));
			parentBlock.Statements.InsertBefore(tryCatch,
				AssignmentStatement(initializedVariable, BooleanLiteral(false, boolType)));
			parentBlock.Statements.InsertBefore(tryCatch,
				AssignmentStatement(completedVariable, BooleanLiteral(false, boolType)));

			var protectedBody = new BlockStatement();
			protectedBody.Statements.Add(new IfElseStatement {
				Condition = LogicalNot(Variable(initializedVariable), boolType),
				TrueStatement = new BlockStatement {
					AssignmentStatement(rewrittenEnumeratorVariable,
						Invocation(collectionExpression, getEnumeratorCall)),
					AssignmentStatement(initializedVariable, BooleanLiteral(true, boolType))
				}
			});

			var hasNextBody = new BlockStatement {
				AssignmentStatement(itemVariable, MemberAccess(Variable(rewrittenEnumeratorVariable), currentProperty, getCurrentCall)),
				AssignmentStatement(resultVariable, yieldExpression),
				AssignmentStatement(hasValueVariable, BooleanLiteral(true, boolType))
			};
			protectedBody.Statements.Add(new IfElseStatement {
				Condition = Invocation(Variable(rewrittenEnumeratorVariable), moveNextCall),
				TrueStatement = hasNextBody,
				FalseStatement = new BlockStatement {
					AssignmentStatement(completedVariable, BooleanLiteral(true, boolType))
				}
			});
			tryCatch.TryBlock = protectedBody;
			catchClause.Body.Statements.Add(AssignmentStatement(completedVariable, BooleanLiteral(true, boolType)));

			var outsideYield = new YieldReturnStatement {
				Expression = Variable(resultVariable)
			}.CopyAnnotationsFrom(yieldReturn);
			var loopBody = new BlockStatement {
				AssignmentStatement(hasValueVariable, BooleanLiteral(false, boolType)),
				AssignmentStatement(resultVariable, DefaultValue(elementType, context))
			};
			var whileStatement = new WhileStatement {
				Condition = LogicalNot(Variable(completedVariable), boolType),
				EmbeddedStatement = loopBody
			}.CopyAnnotationsFrom(foreachStatement);

			tryCatch.ReplaceWith(whileStatement);
			loopBody.Statements.Add(tryCatch);
			loopBody.Statements.Add(new IfElseStatement {
				Condition = Variable(hasValueVariable),
				TrueStatement = new BlockStatement { outsideYield }
			});
			context.EndStep(whileStatement);
		}

		static bool TryValidateEnumeratorCalls(CallInstruction getEnumeratorCall,
			CallInstruction moveNextCall, CallInstruction getCurrentCall, IProperty currentProperty,
			ResolveResult collectionResolveResult, ILFunction function, CSharpResolver resolver,
			out ILVariable enumeratorVariable)
		{
			enumeratorVariable = null!;
			if (collectionResolveResult.IsError
				|| collectionResolveResult is ConversionResolveResult
				|| !IsSimpleInstanceCall(getEnumeratorCall, "GetEnumerator")
				|| getEnumeratorCall.ConstrainedTo is not null
				|| !HasExactCallOpcode(getEnumeratorCall, collectionResolveResult.Type)
				|| collectionResolveResult.Type.Kind == TypeKind.Interface
				|| !NormalizeTypeVisitor.TypeErasure.EquivalentTypes(
					collectionResolveResult.Type, getEnumeratorCall.Method.DeclaringType)
				|| getEnumeratorCall.Arguments.Count != 1
				|| getEnumeratorCall.Parent is not StLoc getEnumeratorStore
				|| getEnumeratorStore.Value != getEnumeratorCall
				|| !getEnumeratorStore.IsDescendantOf(function)
				|| !moveNextCall.IsDescendantOf(function)
				|| !getCurrentCall.IsDescendantOf(function))
			{
				return false;
			}

			enumeratorVariable = getEnumeratorStore.Variable;
			var enumeratorType = getEnumeratorCall.Method.ReturnType;
			if (enumeratorVariable.Function != function
				|| enumeratorVariable.UsesInitialValue
				|| enumeratorVariable.CaptureScope is not null
				|| enumeratorVariable.StoreInstructions.Count != 1
				|| enumeratorVariable.StoreInstructions[0] != getEnumeratorStore
				|| enumeratorVariable.LoadCount != 0
				|| enumeratorVariable.AddressCount != 2
				|| !NormalizeTypeVisitor.TypeErasure.EquivalentTypes(enumeratorVariable.Type, enumeratorType)
				|| !IsExactEnumeratorCall(moveNextCall, "MoveNext", enumeratorType, enumeratorVariable)
				|| !IsExactEnumeratorCall(getCurrentCall, "get_Current", enumeratorType, enumeratorVariable))
			{
				return false;
			}

			var moveNextReceiver = (LdLoca)moveNextCall.Arguments[0];
			var currentReceiver = (LdLoca)getCurrentCall.Arguments[0];
			if (!enumeratorVariable.AddressInstructions.Contains(moveNextReceiver)
				|| !enumeratorVariable.AddressInstructions.Contains(currentReceiver)
				|| !TryResolveInvocation(resolver, collectionResolveResult, getEnumeratorCall.Method)
				|| !TryResolveInvocation(resolver, new ResolveResult(enumeratorType), moveNextCall.Method)
				|| resolver.ResolveMemberAccess(new ResolveResult(enumeratorType), currentProperty.Name,
					Array.Empty<IType>()) is not MemberResolveResult currentResolveResult
				|| currentResolveResult.IsError
				|| !currentResolveResult.Member.Equals(currentProperty, NormalizeTypeVisitor.TypeErasure))
			{
				return false;
			}

			return true;
		}

		static bool IsSimpleInstanceCall(CallInstruction call, string expectedName)
		{
			return call.IsInstanceCall
				&& call.Method.Name == expectedName
				&& call.Method.Parameters.Count == 0
				&& call.Method.TypeParameters.Count == 0
				&& !call.Method.IsExplicitInterfaceImplementation;
		}

		internal static bool HasNoDisposeForeachProvenance(ForeachStatement foreachStatement,
			CallInstruction moveNextCall)
		{
			return foreachStatement.Annotation<UsingInstruction>() is null
				&& foreachStatement.Annotations.OfType<BlockContainer>()
					.Any(container => container.Kind == ContainerKind.While
						&& moveNextCall.IsDescendantOf(container));
		}

		internal static bool HasExactCallOpcode(CallInstruction call, IType receiverType)
		{
			return receiverType.IsReferenceType switch {
				true => call.OpCode == OpCode.CallVirt,
				false => receiverType.Kind == TypeKind.Struct && call.OpCode == OpCode.Call,
				_ => false
			};
		}

		internal static bool IsExactEnumeratorCall(CallInstruction call, string expectedName,
			IType receiverType, ILVariable receiverVariable)
		{
			return call.ConstrainedTo is null
				&& IsSimpleInstanceCall(call, expectedName)
				&& receiverType.Kind != TypeKind.Interface
				&& HasExactCallOpcode(call, receiverType)
				&& call.Arguments.Count == 1
				&& (receiverType.IsReferenceType == true
					? call.Arguments[0].MatchLdLoc(receiverVariable)
					: call.Arguments[0].MatchLdLoca(receiverVariable))
				&& NormalizeTypeVisitor.TypeErasure.EquivalentTypes(call.Method.DeclaringType, receiverType);
		}

		static bool TryResolveInvocation(CSharpResolver resolver, ResolveResult target, IMethod expectedMethod)
		{
			var memberAccess = resolver.ResolveMemberAccess(target, expectedMethod.Name,
				Array.Empty<IType>(), NameLookupMode.InvocationTarget);
			var invocation = resolver.ResolveInvocation(memberAccess, Array.Empty<ResolveResult>());
			return invocation is CSharpInvocationResolveResult {
				IsError: false,
				IsExtensionMethodInvocation: false
			} invocationResolveResult
				&& invocationResolveResult.Member.Equals(expectedMethod, NormalizeTypeVisitor.TypeErasure);
		}

		internal static bool IsCSharpNameableAndAccessible(IType type, MemberLookup lookup)
		{
			if (type.IsAnonymousType())
				return false;
			switch (type.Kind)
			{
				case TypeKind.Class:
				case TypeKind.Interface:
				case TypeKind.Struct:
				case TypeKind.Delegate:
				case TypeKind.Enum:
					var definition = type.GetDefinition();
					return definition is not null
						&& HasValidCSharpTypeName(definition.Name, definition.Namespace)
						&& lookup.IsAccessible(definition, allowProtectedAccess: false)
						&& (type.DeclaringType is null
							|| IsCSharpNameableAndAccessible(type.DeclaringType, lookup))
						&& type.TypeArguments.All(argument => IsCSharpNameableAndAccessible(argument, lookup));
				case TypeKind.Array:
					return type is ArrayType arrayType
						&& IsCSharpNameableAndAccessible(arrayType.ElementType, lookup);
				default:
					return false;
			}
		}

		internal static bool HasValidCSharpTypeName(string name, string @namespace)
		{
			return AssignVariableNames.IsValidName(name)
				&& (string.IsNullOrEmpty(@namespace)
					|| @namespace.Split('.').All(AssignVariableNames.IsValidName));
		}

		internal static bool HasAccessibleGetter(IProperty property, MemberLookup lookup)
		{
			return property.Getter is { } getter
				&& lookup.IsAccessible(property, allowProtectedAccess: false)
				&& lookup.IsAccessible(getter, allowProtectedAccess: false);
		}

		internal static bool EnumeratorRequiresDisposal(IType enumeratorType)
		{
			return enumeratorType.GetAllBaseTypes()
					.Any(type => type.IsKnownType(KnownTypeCode.IDisposable))
				|| enumeratorType.GetMethods(method => method.Name == "Dispose"
					&& !method.IsStatic && method.Parameters.Count == 0
					&& method.TypeParameters.Count == 0).Any();
		}

		internal static bool IsForeachVariableConfined(ILVariable itemVariable,
			CallInstruction getCurrentCall, YieldReturnStatement yieldReturn,
			SingleVariableDesignation designation, AstNode astRoot, ILFunction function)
		{
			if (itemVariable.Function != function
				|| itemVariable.UsesInitialValue
				|| itemVariable.CaptureScope is not null
				|| itemVariable.AddressCount != 0
				|| !itemVariable.IsSingleDefinition
				|| itemVariable.StoreInstructions.Count != 1
				|| itemVariable.StoreInstructions[0] is not StLoc itemStore
				|| itemStore.Value != getCurrentCall
				|| getCurrentCall.Parent != itemStore
				|| !itemStore.IsDescendantOf(function)
				|| yieldReturn.Annotation<IL.YieldReturn>() is not { } yieldInstruction
				|| !yieldInstruction.IsDescendantOf(function)
				|| itemVariable.LoadInstructions.Any(load => !load.IsDescendantOf(yieldInstruction.Value)))
			{
				return false;
			}

			foreach (var node in astRoot.DescendantsAndSelf)
			{
				if (!node.Annotations.OfType<ILVariableResolveResult>()
					.Any(resolveResult => resolveResult.Variable == itemVariable))
				{
					continue;
				}
				if (node == designation)
					continue;
				if (node is IdentifierExpression
					&& (node == yieldReturn.Expression || node.Ancestors.Contains(yieldReturn.Expression)))
				{
					continue;
				}
				return false;
			}
			return true;
		}

		static ILVariable RegisterVariable(ILFunction function, IType type, TransformContext context)
		{
			var name = AssignVariableNames.GenerateVariableName(function, type,
				context.DecompileRun.UsingScope, mustResolveConflicts: true);
			return function.RegisterVariable(VariableKind.Local, type, name);
		}

		static IdentifierExpression Variable(ILVariable variable)
		{
			var expression = new IdentifierExpression(variable.Name!);
			expression.AddAnnotation(new ILVariableResolveResult(variable, variable.Type));
			return expression;
		}

		static Expression BooleanLiteral(bool value, IType boolType)
		{
			return new PrimitiveExpression(value)
				.WithRR(new ConstantResolveResult(boolType, value));
		}

		static Expression DefaultValue(IType type, TransformContext context)
		{
			return new DefaultValueExpression(context.TypeSystemAstBuilder.ConvertType(type))
				.WithRR(new ResolveResult(type));
		}

		static Expression LogicalNot(Expression expression, IType boolType)
		{
			return new UnaryOperatorExpression(UnaryOperatorType.Not, expression)
				.WithRR(new ResolveResult(boolType));
		}

		static ExpressionStatement AssignmentStatement(ILVariable variable, Expression value)
		{
			return new ExpressionStatement {
				Expression = new AssignmentExpression(Variable(variable), value)
					.WithRR(new ResolveResult(variable.Type))
			};
		}

		static InvocationExpression Invocation(Expression target, CallInstruction call)
		{
			var targetResolveResult = target.GetResolveResult();
			var memberReference = new MemberReferenceExpression(target, call.Method.Name);
			memberReference.AddAnnotation(new MemberResolveResult(targetResolveResult, call.Method));
			var invocation = new InvocationExpression(memberReference);
			invocation.AddAnnotation(new CSharpInvocationResolveResult(targetResolveResult, call.Method,
				Array.Empty<ResolveResult>()));
			invocation.AddAnnotation(call);
			return invocation;
		}

		static MemberReferenceExpression MemberAccess(Expression target, IProperty property, CallInstruction call)
		{
			var memberReference = new MemberReferenceExpression(target, property.Name);
			memberReference.AddAnnotation(new MemberResolveResult(target.GetResolveResult(), property));
			memberReference.AddAnnotation(call);
			return memberReference;
		}
	}
}

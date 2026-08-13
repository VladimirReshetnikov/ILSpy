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
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Decompiles query expressions.
	/// Based on C# 4.0 spec, §7.16.2 Query expression translation
	/// </summary>
	public class IntroduceQueryExpressions : IAstTransform
	{
		[AllowNull]
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			if (!context.Settings.QueryExpressions)
				return;
			this.context = context;
			DecompileQueries(rootNode);
			// After all queries were decompiled, detect degenerate queries (queries not property terminated with 'select' or 'group')
			// and fix them, either by adding a degenerate select, or by combining them with another query.
			foreach (QueryExpression query in rootNode.Descendants.OfType<QueryExpression>())
			{
				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				if (IsDegenerateQuery(query))
				{
					// introduce select for degenerate query
					context.Step("Add degenerate query select clause", query);
					query.Clauses.Add(new QuerySelectClause { Expression = new IdentifierExpression(fromClause.Identifier).CopyAnnotationsFrom(fromClause) });
				}
				// See if the data source of this query is a degenerate query,
				// and combine the queries if possible.
				QueryExpression? innerQuery = fromClause.Expression as QueryExpression;
				while (innerQuery != null && IsDegenerateQuery(innerQuery))
				{
					QueryFromClause innerFromClause = (QueryFromClause)innerQuery.Clauses.First();
					ILVariable? innerVariable = innerFromClause.Annotation<ILVariableResolveResult>()?.Variable;
					ILVariable? rangeVariable = fromClause.Annotation<ILVariableResolveResult>()?.Variable;
					context.Step("Combine nested query clauses", fromClause);
					// Replace the fromClause with all clauses from the inner query
					fromClause.Remove();
					QueryClause? insertionPos = null;
					foreach (var clause in innerQuery.Clauses)
					{
						CombineRangeVariables(clause, innerVariable, rangeVariable);
						query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
					}
					context.EndStep(innerFromClause);
					fromClause = innerFromClause;
					innerQuery = fromClause.Expression as QueryExpression;
				}
			}
		}

		/// <summary>
		/// A query range variable is not an addressable lvalue, so it cannot be passed to an
		/// 'in' (readonly-ref) parameter with an explicit 'in' modifier: C# rejects that as CS8159.
		/// At the IL level the original 'ldarga' of the lambda parameter is indistinguishable from
		/// passing an addressable variable by reference, so the call builder renders the argument as
		/// 'in rangeVariable'. Once the lambda parameter becomes a query range variable this is no
		/// longer valid, so drop the explicit 'in' and pass by value; the compiler makes the hidden
		/// readonly copy that an 'in' parameter requires. 'ref'/'out' are left untouched: they demand
		/// an lvalue and cannot be satisfied by a range variable at all.
		/// Dropping the modifier re-runs overload resolution without it: when the invoked member has
		/// a sibling overload whose parameter list differs only in that parameter's by-value/'in'-ness,
		/// the by-value overload would silently win. In that case this method returns false without
		/// touching the query, and the caller gives the query up in favor of the method-call chain,
		/// where the explicit 'in' remains legal.
		/// </summary>
		private bool RemoveInModifierFromRangeVariableArguments(QueryExpression query)
		{
			var rangeVariables = new HashSet<ILVariable>();
			foreach (var fromClause in query.Clauses.OfType<QueryFromClause>())
			{
				if (fromClause.Annotation<ILVariableResolveResult>()?.Variable is ILVariable variable)
					rangeVariables.Add(variable);
			}
			foreach (var joinClause in query.Clauses.OfType<QueryJoinClause>())
			{
				if (joinClause.JoinIdentifierToken.Annotation<ILVariableResolveResult>()?.Variable is ILVariable joinVariable)
					rangeVariables.Add(joinVariable);
				if (joinClause.IntoIdentifierToken?.Annotation<ILVariableResolveResult>()?.Variable is ILVariable intoVariable)
					rangeVariables.Add(intoVariable);
			}
			if (rangeVariables.Count == 0)
				return true;
			foreach (var directionExpression in query.Descendants.OfType<DirectionExpression>().ToArray())
			{
				if (directionExpression.FieldDirection != FieldDirection.In)
					continue;
				if (directionExpression.Expression is not IdentifierExpression identifierExpression)
					continue;
				if (identifierExpression.Annotation<ILVariableResolveResult>()?.Variable is not ILVariable variable
					|| !rangeVariables.Contains(variable))
				{
					continue;
				}
				if (!CanRemoveInModifierWithoutRebinding(directionExpression))
					return false;
				context.Step("Remove 'in' modifier from query range variable argument", directionExpression);
				var value = identifierExpression.Detach();
				directionExpression.ReplaceWith(value);
				context.EndStep(value);
			}
			return true;
		}

		/// <summary>
		/// Determines whether dropping the explicit 'in' modifier from the argument held by
		/// <paramref name="directionExpression"/> is guaranteed to keep the call bound to the same
		/// member: the invoked member must be identifiable, the argument must map to an 'in'
		/// parameter, and no sibling overload may differ from the invoked member only by that
		/// parameter's by-value/'in'-ness. Anything that cannot be analyzed counts as unsafe.
		/// </summary>
		static bool CanRemoveInModifierWithoutRebinding(DirectionExpression directionExpression)
		{
			AstNode? argumentHolder = directionExpression.Parent;
			AstNodeCollection<Expression>? arguments = argumentHolder switch {
				InvocationExpression invocation => invocation.Arguments,
				ObjectCreateExpression objectCreate => objectCreate.Arguments,
				IndexerExpression indexer => indexer.Arguments,
				_ => null,
			};
			if (arguments is null || argumentHolder?.GetSymbol() is not IParameterizedMember member)
				return false;
			int argumentIndex = arguments.IndexOf(directionExpression);
			if (argumentIndex < 0 || argumentIndex >= member.Parameters.Count
				|| member.Parameters[argumentIndex].ReferenceKind != ReferenceKind.In)
			{
				return false;
			}
			return !HasSiblingOverloadDifferingOnlyByInModifier(member, argumentIndex);
		}

		/// <summary>
		/// Normalization applied before comparing parameter types of sibling overloads; mirrors the
		/// settings of <see cref="ParameterListComparer"/>, in particular treating the type
		/// parameters of two different generic methods as equal by their position.
		/// </summary>
		static readonly NormalizeTypeVisitor overloadComparisonNormalization = new NormalizeTypeVisitor {
			ReplaceClassTypeParametersWithDummy = false,
			ReplaceMethodTypeParametersWithDummy = true,
			DynamicAndObject = true,
			TupleToUnderlyingType = true,
		};

		/// <summary>
		/// Returns true if the declaring type of <paramref name="member"/> also declares an overload
		/// whose parameter list matches the member's except that the parameter at
		/// <paramref name="parameterIndex"/> is by-value instead of 'in'. Members whose overload set
		/// cannot be enumerated are treated as having such a sibling.
		/// </summary>
		static bool HasSiblingOverloadDifferingOnlyByInModifier(IParameterizedMember member, int parameterIndex)
		{
			if (member.MemberDefinition is not IParameterizedMember definition)
				return true;
			if (definition.DeclaringTypeDefinition is not ITypeDefinition declaringType)
				return true;
			IEnumerable<IParameterizedMember>? siblings = definition switch {
				IMethod => declaringType.Methods,
				IProperty => declaringType.Properties,
				_ => null,
			};
			if (siblings is null)
				return true;
			foreach (IParameterizedMember sibling in siblings)
			{
				if (sibling.Name != definition.Name)
					continue;
				if (sibling is IMethod siblingMethod && definition is IMethod definitionMethod
					&& siblingMethod.TypeParameters.Count != definitionMethod.TypeParameters.Count)
				{
					continue;
				}
				if (ParameterListsDifferOnlyByInModifier(definition.Parameters, sibling.Parameters, parameterIndex))
					return true;
			}
			return false;
		}

		static bool ParameterListsDifferOnlyByInModifier(IReadOnlyList<IParameter> parameters,
			IReadOnlyList<IParameter> siblingParameters, int parameterIndex)
		{
			if (siblingParameters.Count != parameters.Count)
				return false;
			for (int i = 0; i < parameters.Count; i++)
			{
				IType parameterType = parameters[i].Type;
				if (i == parameterIndex)
				{
					if (siblingParameters[i].ReferenceKind != ReferenceKind.None)
						return false;
					// an 'in' parameter's type carries the managed reference; the by-value
					// sibling parameter is compared against the referenced element type
					if (parameterType is ByReferenceType byReference)
						parameterType = byReference.ElementType;
				}
				else if (siblingParameters[i].ReferenceKind != parameters[i].ReferenceKind)
				{
					return false;
				}
				if (!overloadComparisonNormalization.EquivalentTypes(parameterType, siblingParameters[i].Type))
					return false;
			}
			return true;
		}

		private void CombineRangeVariables(QueryClause clause, ILVariable? oldVariable, ILVariable? newVariable)
		{
			if (oldVariable == null || newVariable == null)
				return;
			foreach (var identifier in clause.DescendantNodes().OfType<Identifier>())
			{
				var parent = identifier.Parent;
				if (parent == null)
					continue;
				var variable = parent.Annotation<ILVariableResolveResult>()?.Variable;
				if (variable == oldVariable)
				{
					context.Step("Combine query range variables", identifier);
					parent.RemoveAnnotations<ILVariableResolveResult>();
					parent.AddAnnotation(new ILVariableResolveResult(newVariable));
					var newIdentifier = Identifier.Create(newVariable.Name!);
					identifier.ReplaceWith(newIdentifier);
					context.EndStep(newIdentifier);
				}
			}
		}

		bool IsDegenerateQuery(QueryExpression? query)
		{
			if (query == null)
				return false;
			var lastClause = query.Clauses.LastOrDefault();
			return !(lastClause is QuerySelectClause || lastClause is QueryGroupClause);
		}

		void DecompileQueries(AstNode node)
		{
			// A query built from a chain that cannot be fully translated leaves its transparent
			// identifiers visible, and those are not names C# can write. An untouched copy of the
			// call chain lets such a query go back to being method calls. The copy is taken lazily
			// by BeginQueryBuild, just before the first build detaches anything: cloning every
			// operator-named invocation up front would deep-clone each segment of every LINQ chain
			// even though most never become query expressions.
			currentChainRoot = node as InvocationExpression;
			untouchedChainForRollback = null;
			Expression? query = DecompileQuery(node as InvocationExpression);
			Expression? callChain = untouchedChainForRollback;
			if (query is QueryExpression queryExpression)
			{
				if (RemoveInModifierFromRangeVariableArguments(queryExpression))
				{
					if (callChain != null)
						queryExpression.AddAnnotation(new UntranslatedQueryAnnotation(callChain));
				}
				else
				{
					// An explicit 'in' argument that cannot be dropped safely makes the query
					// unwritable (CS8159), so give the translation up. DecompileQuery only accepts
					// invocations of query operator names, so the call-chain copy exists whenever
					// a query was built.
					Debug.Assert(callChain != null);
					query = callChain;
				}
			}
			if (query != null)
			{
				if (query is QueryExpression && node.Parent is ExpressionStatement && CanUseDiscardAssignment())
					query = new AssignmentExpression(new IdentifierExpression("_"), query);
				node.ReplaceWith(query);
				context.EndStep(query);
			}

			AstNode? next;
			for (AstNode? child = (query ?? node).FirstChild; child != null; child = next)
			{
				// store reference to next child before transformation
				next = child.NextSibling;
				DecompileQueries(child);
			}
		}

		static bool IsQueryOperatorName(string name)
		{
			switch (name)
			{
				case "Select":
				case "SelectMany":
				case "Where":
				case "OrderBy":
				case "OrderByDescending":
				case "ThenBy":
				case "ThenByDescending":
				case "GroupBy":
				case "Join":
				case "GroupJoin":
				case "Cast":
					return true;
				default:
					return false;
			}
		}

		bool CanUseDiscardAssignment()
		{
			// TODO : check whether there exists a variable named '_' in scope.
			return context.Settings.Discards;
		}

		// The outermost operator-named invocation DecompileQueries is currently processing, and
		// the untouched copy of it captured by BeginQueryBuild once a build actually starts.
		InvocationExpression? currentChainRoot;
		Expression? untouchedChainForRollback;

		/// <summary>
		/// Marks the start of building a query from the current chain: captures the rollback copy
		/// of the whole untouched chain (once - nested builds keep the outermost copy) before the
		/// build detaches any of its pieces, then records the transform step.
		/// </summary>
		void BeginQueryBuild(string stepName, InvocationExpression invocation)
		{
			untouchedChainForRollback ??= (Expression?)currentChainRoot?.Clone();
			context.Step(stepName, invocation);
		}

		QueryExpression? DecompileQuery(InvocationExpression? invocation)
		{
			if (invocation == null)
				return null;
			MemberReferenceExpression? mre = invocation.Target as MemberReferenceExpression;
			if (mre == null || ReceiverChainHasNullConditional(mre.Target))
				return null;
			// Query syntax cannot spell generic method type arguments. If call reconstruction
			// retained them, source inference selected a different specialization and dropping
			// them here could change the result type or make the query fail to compile.
			if (mre.TypeArguments.Count > 0)
				return null;
			switch (mre.MemberName)
			{
				case "Select":
				{
					if (invocation.Arguments.Count != 1)
						return null;
					if (!IsComplexQuery(mre))
						return null;
					Expression expr = invocation.Arguments.Single();
					if (MatchSimpleLambda(expr, out var parameter, out var body))
					{
						BeginQueryBuild("Build select query", invocation);
						QueryExpression query = new QueryExpression();
						query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
						query.Clauses.Add(new QuerySelectClause { Expression = WrapExpressionInParenthesesIfNecessary(body.Detach(), parameter.Name!) }.CopyAnnotationsFrom(expr));
						return query;
					}
					return null;
				}
				case "GroupBy":
				{
					if (invocation.Arguments.Count == 2)
					{
						Expression keyLambda = invocation.Arguments.ElementAt(0);
						Expression projectionLambda = invocation.Arguments.ElementAt(1);
						if (MatchSimpleLambda(keyLambda, out var parameter1, out var keySelector)
							&& MatchSimpleLambda(projectionLambda, out var parameter2, out var elementSelector)
							&& parameter1.Name == parameter2.Name)
						{
							BeginQueryBuild("Build group query", invocation);
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(parameter1, mre.Target.Detach()));
							var queryGroupClause = new QueryGroupClause {
								Projection = elementSelector.Detach(),
								Key = keySelector.Detach()
							};
							// The matched group-by lambdas always carry their ILFunction annotation.
							queryGroupClause.AddAnnotation(new QueryGroupClauseAnnotation(keyLambda.Annotation<IL.ILFunction>()!, projectionLambda.Annotation<IL.ILFunction>()!));
							query.Clauses.Add(queryGroupClause);
							return query;
						}
					}
					else if (invocation.Arguments.Count == 1)
					{
						Expression lambda = invocation.Arguments.Single();
						if (MatchSimpleLambda(lambda, out var parameter, out var keySelector))
						{
							BeginQueryBuild("Build group query", invocation);
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
							query.Clauses.Add(new QueryGroupClause { Projection = new IdentifierExpression(parameter.Name!).CopyAnnotationsFrom(parameter), Key = keySelector.Detach() });
							return query;
						}
					}
					return null;
				}
				case "SelectMany":
				{
					if (invocation.Arguments.Count != 2)
						return null;
					var fromExpressionLambda = invocation.Arguments.ElementAt(0);
					if (!MatchSimpleLambda(fromExpressionLambda, out var parameter, out var collectionSelector))
						return null;
					if (IsNullConditional(collectionSelector))
						return null;
					LambdaExpression? lambda = invocation.Arguments.ElementAt(1) as LambdaExpression;
					if (lambda != null && !lambda.IsAsync && lambda.Parameters.Count == 2 && lambda.Body is Expression)
					{
						ParameterDeclaration p1 = lambda.Parameters.ElementAt(0);
						ParameterDeclaration p2 = lambda.Parameters.ElementAt(1);
						if (p1.Name == parameter.Name)
						{
							BeginQueryBuild("Build select-many query", invocation);
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(p1, mre.Target.Detach()));
							query.Clauses.Add(MakeFromClause(p2, collectionSelector.Detach()).CopyAnnotationsFrom(fromExpressionLambda));
							query.Clauses.Add(new QuerySelectClause { Expression = WrapExpressionInParenthesesIfNecessary(((Expression)lambda.Body).Detach(), parameter.Name!) });
							return query;
						}
					}
					return null;
				}
				case "Where":
				{
					if (invocation.Arguments.Count != 1)
						return null;
					if (!IsComplexQuery(mre))
						return null;
					Expression expr = invocation.Arguments.Single();
					if (MatchSimpleLambda(expr, out var parameter, out var body))
					{
						BeginQueryBuild("Build where query", invocation);
						QueryExpression query = new QueryExpression();
						query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
						query.Clauses.Add(new QueryWhereClause { Condition = body.Detach() }.CopyAnnotationsFrom(expr));
						return query;
					}
					return null;
				}
				case "OrderBy":
				case "OrderByDescending":
				case "ThenBy":
				case "ThenByDescending":
				{
					if (invocation.Arguments.Count != 1)
						return null;
					if (!IsComplexQuery(mre))
						return null;
					var lambda = invocation.Arguments.Single();
					if (MatchSimpleLambda(lambda, out var parameter, out var orderExpression))
					{
						if (ValidateThenByChain(invocation, parameter.Name!))
						{
							BeginQueryBuild("Build order query", invocation);
							QueryOrderClause orderClause = new QueryOrderClause();
							// Each OrderBy/ThenBy lambda introduces its own parameter ILVariable, but they all
							// denote the single range variable of the resulting query. Only the final (OrderBy)
							// parameter becomes the query's 'from' range variable, so collect the ThenBy
							// parameter variables and rebind their ordering keys onto it below.
							var thenByVariables = new List<ILVariable>();
							while (mre.MemberName == "ThenBy" || mre.MemberName == "ThenByDescending")
							{
								// insert new ordering at beginning
								orderClause.Orderings.InsertAfter(
									null, new QueryOrdering {
										Expression = orderExpression.Detach(),
										Direction = (mre.MemberName == "ThenBy" ? QueryOrderingDirection.None : QueryOrderingDirection.Descending)
									}.CopyAnnotationsFrom(lambda));

								if (parameter!.Annotation<ILVariableResolveResult>()?.Variable is ILVariable thenByVariable)
									thenByVariables.Add(thenByVariable);

								InvocationExpression tmp = (InvocationExpression)mre.Target;
								mre = (MemberReferenceExpression)tmp.Target;
								lambda = tmp.Arguments.Single();
								// ValidateThenByChain already confirmed every clause in the chain is a simple
								// lambda, so this match always succeeds.
								bool matched = MatchSimpleLambda(lambda, out parameter, out orderExpression);
								Debug.Assert(matched);
								Debug.Assert(orderExpression != null);
							}
							// The chain was validated and every iteration re-matched a simple lambda,
							// so the final range-variable parameter is set.
							Debug.Assert(parameter != null);
							// insert new ordering at beginning
							orderClause.Orderings.InsertAfter(
								null, new QueryOrdering {
									Expression = orderExpression.Detach(),
									Direction = (mre.MemberName == "OrderBy" ? QueryOrderingDirection.None : QueryOrderingDirection.Descending)
								}.CopyAnnotationsFrom(lambda));

							// Rebind every ThenBy ordering key onto the OrderBy parameter's range variable so
							// all keys share it. A later range-variable rename (CombineRangeVariables, e.g. when
							// this degenerate order query is combined into a following query) rewrites identifiers
							// by ILVariable; without this, only the OrderBy-bound keys would be renamed and the
							// ThenBy-bound keys would keep a now out-of-scope name (CS0103).
							if (parameter.Annotation<ILVariableResolveResult>()?.Variable is ILVariable orderByVariable)
							{
								foreach (var thenByVariable in thenByVariables)
									CombineRangeVariables(orderClause, thenByVariable, orderByVariable);
							}

							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(parameter, mre.Target.Detach()));
							query.Clauses.Add(orderClause);
							return query;
						}
					}
					return null;
				}
				case "Join":
				case "GroupJoin":
				{
					if (invocation.Arguments.Count != 4)
						return null;
					Expression source1 = mre.Target;
					Expression source2 = invocation.Arguments.ElementAt(0);
					if (IsNullConditional(source2))
						return null;
					Expression outerLambda = invocation.Arguments.ElementAt(1);
					if (!MatchSimpleLambda(outerLambda, out var element1, out var key1))
						return null;
					Expression innerLambda = invocation.Arguments.ElementAt(2);
					if (!MatchSimpleLambda(innerLambda, out var element2, out var key2))
						return null;
					LambdaExpression? lambda = invocation.Arguments.ElementAt(3) as LambdaExpression;
					if (lambda != null && !lambda.IsAsync && lambda.Parameters.Count == 2 && lambda.Body is Expression)
					{
						ParameterDeclaration p1 = lambda.Parameters.ElementAt(0);
						ParameterDeclaration p2 = lambda.Parameters.ElementAt(1);
						if (ValidateParameter(p1) && ValidateParameter(p2)
							&& p1.Name == element1.Name && (p2.Name == element2.Name || mre.MemberName == "GroupJoin"))
						{
							BeginQueryBuild(mre.MemberName == "GroupJoin" ? "Build group join query" : "Build join query", invocation);
							QueryExpression query = new QueryExpression();
							query.Clauses.Add(MakeFromClause(element1, source1.Detach()));
							QueryJoinClause joinClause = new QueryJoinClause();
							joinClause.JoinIdentifier = element2.Name!;    // join elementName2
							joinClause.JoinIdentifierToken.CopyAnnotationsFrom(element2);
							joinClause.InExpression = source2.Detach();  // in source2
							joinClause.OnExpression = key1.Detach();     // on key1
							joinClause.EqualsExpression = key2.Detach(); // equals key2
							if (mre.MemberName == "GroupJoin")
							{
								joinClause.IntoIdentifier = p2.Name; // into p2.Name
								joinClause.IntoIdentifierToken!.CopyAnnotationsFrom(p2);
							}
							// The matched join key-selector lambdas always carry their ILFunction annotation.
							joinClause.AddAnnotation(new QueryJoinClauseAnnotation(outerLambda.Annotation<IL.ILFunction>()!, innerLambda.Annotation<IL.ILFunction>()!));
							query.Clauses.Add(joinClause);
							query.Clauses.Add(new QuerySelectClause { Expression = ((Expression)lambda.Body).Detach() }.CopyAnnotationsFrom(lambda));
							return query;
						}
					}
					return null;
				}
				default:
					return null;
			}
		}

		static bool IsComplexQuery(MemberReferenceExpression mre)
		{
			return ((mre.Target is InvocationExpression && mre.Parent is InvocationExpression) || mre.Parent?.Parent is QueryClause);
		}

		QueryFromClause MakeFromClause(ParameterDeclaration parameter, Expression body)
		{
			QueryFromClause fromClause = new QueryFromClause {
				Identifier = parameter.Name!,
				Expression = body
			};
			fromClause.CopyAnnotationsFrom(parameter);
			return fromClause;
		}

		class ApplyAnnotationVisitor : DepthFirstAstVisitor<AstNode>
		{
			private LetIdentifierAnnotation annotation;
			private string identifier;

			public ApplyAnnotationVisitor(LetIdentifierAnnotation annotation, string identifier)
			{
				this.annotation = annotation;
				this.identifier = identifier;
			}

			public override AstNode VisitIdentifier(Identifier identifier)
			{
				if (identifier.Name == this.identifier)
					identifier.AddAnnotation(annotation);
				return identifier;
			}
		}

		bool IsNullConditional(Expression target) => target switch {
			UnaryOperatorExpression { Operator: UnaryOperatorType.NullConditional } => true,
			MemberReferenceExpression member => IsNullConditional(member.Target),
			InvocationExpression invocation => IsNullConditional(invocation.Target),
			IndexerExpression { Target: { } indexerTarget } => IsNullConditional(indexerTarget),
			_ => false
		};

		/// <summary>
		/// Determines whether a null-conditional access ('?.') appears anywhere in the receiver chain
		/// of <paramref name="expression"/>. A query expression cannot host a '?.'-rooted source: the
		/// '?.' makes the whole method-call chain nullable, but moving the source into a 'from' clause
		/// leaves the query result (and anything applied to it outside the query) non-nullable, which
		/// does not compile (e.g. a following '?? fallback' on a now non-nullable result, CS0019).
		/// </summary>
		static bool ReceiverChainHasNullConditional(Expression? expression)
		{
			while (true)
			{
				switch (expression)
				{
					case UnaryOperatorExpression { Operator: UnaryOperatorType.NullConditional }:
						return true;
					case MemberReferenceExpression mre:
						expression = mre.Target;
						break;
					case InvocationExpression ie:
						expression = ie.Target;
						break;
					case IndexerExpression ix:
						expression = ix.Target;
						break;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// This fixes #437: Decompilation of query expression loses material parentheses
		/// We wrap the expression in parentheses if:
		/// - the Select-call is explicit (see caller(s))
		/// - the expression is a plain identifier matching the parameter name
		/// </summary>
		Expression WrapExpressionInParenthesesIfNecessary(Expression expression, string parameterName)
		{
			if (expression is IdentifierExpression ident && parameterName.Equals(ident.Identifier, StringComparison.Ordinal))
				return new ParenthesizedExpression(expression);
			return expression;
		}

		/// <summary>
		/// Ensure that all ThenBy's are correct, and that the list of ThenBy's is terminated by an 'OrderBy' invocation.
		/// </summary>
		bool ValidateThenByChain(InvocationExpression? invocation, string expectedParameterName)
		{
			if (invocation == null || invocation.Arguments.Count != 1)
				return false;
			if (!(invocation.Target is MemberReferenceExpression mre))
				return false;
			// Same reason DecompileQuery refuses one on the outermost call: an ordering clause has
			// nowhere to put explicit type arguments, and this chain is discarded whole once it is
			// accepted, so any the inner calls carry would go with it.
			if (mre.TypeArguments.Count > 0)
				return false;
			if (!MatchSimpleLambda(invocation.Arguments.Single(), out var parameter, out _))
				return false;
			if (parameter.Name != expectedParameterName)
				return false;

			if (mre.MemberName == "OrderBy" || mre.MemberName == "OrderByDescending")
				return !IsNullConditional(mre.Target);
			else if (mre.MemberName == "ThenBy" || mre.MemberName == "ThenByDescending")
				return ValidateThenByChain(mre.Target as InvocationExpression, expectedParameterName);
			else
				return false;
		}

		/// <summary>Matches simple lambdas of the form "a => b"</summary>
		bool MatchSimpleLambda(Expression expr, [NotNullWhen(true)] out ParameterDeclaration? parameter, [NotNullWhen(true)] out Expression? body)
		{
			// An async lambda must never be folded into a query clause: a clause keeps only the lambda
			// body and drops the 'async', but 'await' is illegal in every clause position built from a
			// lambda body (select/where/let/orderby/group and the join key selectors), CS1995. The only
			// query positions where 'await' is legal -- the initial 'from' source and a 'join' collection
			// -- come from non-lambda expressions and never route through here. Leaving an async-lambda
			// Select/Where/etc. as an ordinary method call keeps the result compilable.
			if (expr is LambdaExpression { IsAsync: false } lambda && lambda.Parameters.Count == 1 && lambda.Body is Expression)
			{
				ParameterDeclaration p = lambda.Parameters.Single();
				if (ValidateParameter(p))
				{
					parameter = p;
					body = (Expression)lambda.Body;
					return true;
				}
			}
			parameter = null;
			body = null;
			return false;
		}

		private static bool ValidateParameter(ParameterDeclaration p)
		{
			return p.ParameterModifier == Decompiler.TypeSystem.ReferenceKind.None && p.Attributes.Count == 0;
		}
	}
}

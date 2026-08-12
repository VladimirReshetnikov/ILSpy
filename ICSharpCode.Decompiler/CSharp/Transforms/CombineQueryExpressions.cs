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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Combines query expressions and removes transparent identifiers.
	/// </summary>
	public class CombineQueryExpressions : IAstTransform
	{
		[AllowNull] TransformContext context;

		readonly struct QueryVariableReference
		{
			public string Identifier { get; }

			public object? Annotation { get; }

			public QueryVariableReference(string identifier, object? annotation)
			{
				Identifier = identifier;
				Annotation = annotation;
			}
		}

		public void Run(AstNode rootNode, TransformContext context)
		{
			if (!context.Settings.QueryExpressions)
				return;
			this.context = context;
			try
			{
				CombineQueries(rootNode, new Dictionary<string, QueryVariableReference>());
				RevertQueriesWithTransparentIdentifiers(rootNode);
			}
			finally
			{
				this.context = null;
			}
		}

		/// <summary>
		/// Puts back the call chain of every query whose transparent identifiers survived the
		/// translation. A transparent identifier is a name the compiler invented for an anonymous
		/// type it threads through the query; once one is still visible the query cannot be written
		/// down, whereas the calls it was built from always can.
		/// </summary>
		void RevertQueriesWithTransparentIdentifiers(AstNode rootNode)
		{
			foreach (var query in rootNode.DescendantsAndSelf.OfType<QueryExpression>().ToArray())
			{
				if (!query.Ancestors.Contains(rootNode) && query != rootNode)
					continue;
				if (query.Annotation<UntranslatedQueryAnnotation>() is not { } annotation)
					continue;
				if (!query.DescendantsAndSelf.OfType<Identifier>()
					.Any(identifier => CSharpDecompiler.IsTransparentIdentifier(identifier.Name)))
				{
					continue;
				}
				context.Step("Revert query with unresolved transparent identifier", query);
				query.ReplaceWith(annotation.CallChain);
				context.EndStep(annotation.CallChain);
			}
		}

		static readonly InvocationExpression castPattern = new InvocationExpression {
			Target = new MemberReferenceExpression {
				Target = new AnyNode("inExpr"),
				MemberName = "Cast",
				TypeArguments = { new AnyNode("targetType") }
			}
		};

		void CombineQueries(AstNode node, Dictionary<string, QueryVariableReference> fromOrLetIdentifiers)
		{
			AstNode? next;
			for (AstNode? child = node.FirstChild; child != null; child = next)
			{
				// store reference to next child before transformation
				next = child.NextSibling;
				CombineQueries(child, fromOrLetIdentifiers);
			}
			if (node is QueryExpression query)
			{
				QueryFromClause fromClause = (QueryFromClause)query.Clauses.First();
				if (fromClause.Expression is QueryExpression innerQuery)
				{
					if (TryRemoveTransparentIdentifier(query, fromClause, innerQuery, fromOrLetIdentifiers))
					{
						RemoveTransparentIdentifierReferences(query, fromOrLetIdentifiers);
					}
					else
					{
						QueryContinuationClause continuation = new QueryContinuationClause();
						context.Step("Introduce query continuation", fromClause);
						continuation.PrecedingQuery = innerQuery.Detach();
						continuation.Identifier = fromClause.Identifier;
						continuation.CopyAnnotationsFrom(fromClause);
						fromClause.ReplaceWith(continuation);
						context.EndStep(continuation);
					}
				}
				else
				{
					Match m = castPattern.Match(fromClause.Expression);
					if (m.Success)
					{
						var inExpr = m.Get<Expression>("inExpr").Single();
						// Folding "X.Cast<T>()" into "from T x in X" would strand a trailing null-conditional
						// operator: "X?.Cast<T>()" becomes "from T x in X?", which is not valid C#. Keep the
						// explicit Cast call as the query source in that case so the result still parses.
						if (inExpr is not UnaryOperatorExpression { Operator: UnaryOperatorType.NullConditional })
						{
							context.Step("Move Cast type into from clause", fromClause);
							fromClause.Type = m.Get<AstType>("targetType").Single().Detach();
							fromClause.Expression = inExpr.Detach();
						}
					}
				}
			}
		}

		static readonly QuerySelectClause selectTransparentIdentifierPattern = new QuerySelectClause {
			Expression = new AnonymousTypeCreateExpression {
				Initializers = {
					new Repeat(
						new Choice {
							new IdentifierExpression(Pattern.AnyString).WithName("expr"), // name is equivalent to name = name
							new MemberReferenceExpression(new AnyNode(), Pattern.AnyString).WithName("expr"), // expr.name is equivalent to name = expr.name
							new NamedExpression {
								Name = Pattern.AnyString,
								Expression = new AnyNode()
							}.WithName("expr")
						}
					) { MinCount = 1 }
				}
			}
		};

		bool TryRemoveTransparentIdentifier(QueryExpression query, QueryFromClause fromClause, QueryExpression innerQuery, Dictionary<string, QueryVariableReference> letClauses)
		{
			if (!CSharpDecompiler.IsTransparentIdentifier(fromClause.Identifier))
				return false;
			QuerySelectClause? selectClause = innerQuery.Clauses.Last() as QuerySelectClause;
			if (selectClause == null)
				return false;
			Match match = selectTransparentIdentifierPattern.Match(selectClause);
			if (!match.Success)
				return false;

			// from * in (from x in ... select new { members of anonymous type }) ...
			// =>
			// from x in ... { let x = ... } ...
			context.Step("Remove transparent query identifier", fromClause);
			fromClause.Remove();
			selectClause.Remove();
			// Move clauses from innerQuery to query
			QueryClause? insertionPos = null;
			foreach (var clause in innerQuery.Clauses)
			{
				query.Clauses.InsertAfter(insertionPos, insertionPos = clause.Detach());
			}
			context.EndStep(query.Clauses.First());

			foreach (var expr in match.Get<Expression>("expr"))
			{
				switch (expr)
				{
					case IdentifierExpression identifier:
						letClauses[identifier.Identifier] = new QueryVariableReference(identifier.Identifier, identifier.Annotation<ILVariableResolveResult>());
						break;
					case MemberReferenceExpression member:
						AddQueryLetClause(member.MemberName, member);
						break;
					case NamedExpression namedExpression:
						if (namedExpression.Expression is IdentifierExpression identifierExpression
							&& (namedExpression.Name == identifierExpression.Identifier || IsRangeVariable(identifierExpression)))
						{
							letClauses[namedExpression.Name] = new QueryVariableReference(identifierExpression.Identifier, identifierExpression.Annotation<ILVariableResolveResult>());
							continue;
						}
						AddQueryLetClause(namedExpression.Name, namedExpression.Expression);
						break;
				}
			}
			return true;

			bool IsRangeVariable(IdentifierExpression identifier)
			{
				var variable = identifier.Annotation<ILVariableResolveResult>()?.Variable;
				if (variable == null)
					return false;
				return query.Clauses.OfType<QueryFromClause>()
					.Any(clause => clause.Annotation<ILVariableResolveResult>()?.Variable == variable)
					|| query.Clauses.OfType<QueryContinuationClause>()
						.Any(clause => clause.Annotation<ILVariableResolveResult>()?.Variable == variable)
					|| query.Clauses.OfType<QueryJoinClause>()
						.Any(clause => clause.JoinIdentifierToken.Annotation<ILVariableResolveResult>()?.Variable == variable
							|| clause.IntoIdentifierToken?.Annotation<ILVariableResolveResult>()?.Variable == variable);
			}

			void AddQueryLetClause(string name, Expression expression)
			{
				QueryLetClause letClause = new QueryLetClause { Identifier = name, Expression = expression.Detach() };
				var annotation = new LetIdentifierAnnotation();
				letClause.AddAnnotation(annotation);
				letClauses[name] = new QueryVariableReference(name, annotation);
				query.Clauses.InsertAfter(insertionPos, letClause);
			}
		}

		/// <summary>
		/// Removes all occurrences of transparent identifiers
		/// </summary>
		void RemoveTransparentIdentifierReferences(AstNode node, Dictionary<string, QueryVariableReference> fromOrLetIdentifiers)
		{
			foreach (AstNode child in node.Children)
			{
				RemoveTransparentIdentifierReferences(child, fromOrLetIdentifiers);
			}
			if (node is MemberReferenceExpression mre && mre.Target is IdentifierExpression ident
				&& CSharpDecompiler.IsTransparentIdentifier(ident.Identifier)
				&& fromOrLetIdentifiers.TryGetValue(mre.MemberName, out var variableReference))
			{
				// Only strip the transparent-identifier qualifier from a member that was actually
				// introduced as a range or let variable. A member of a transparent identifier that
				// survives as an active range variable (its access was not unfolded into scope) must
				// keep its qualifier, otherwise it becomes an undefined name.
				IdentifierExpression newIdent = new IdentifierExpression(variableReference.Identifier);
				mre.TypeArguments.MoveTo(newIdent.TypeArguments);
				newIdent.CopyAnnotationsFrom(mre);
				newIdent.RemoveAnnotations<Semantics.MemberResolveResult>(); // remove the reference to the property of the anonymous type
				if (variableReference.Annotation != null)
					newIdent.AddAnnotation(variableReference.Annotation);
				context.Step("Replace transparent query identifier reference", mre);
				mre.ReplaceWith(newIdent);
				context.EndStep(newIdent);
				return;
			}
		}
	}

	public class LetIdentifierAnnotation
	{
	}
}

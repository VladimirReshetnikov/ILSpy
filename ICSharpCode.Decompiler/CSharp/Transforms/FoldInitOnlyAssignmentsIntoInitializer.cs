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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Folds assignments to init-only members back into the object initializer they came from.
	///
	/// An init-only member can only be assigned in an object initializer, so an assignment left
	/// outside one does not compile (CS8852). The compiler splits an initializer whenever a member's
	/// value needs statements of its own: the remaining members are then set one statement at a time,
	/// sometimes through a copy of the initializer's temporary rather than the temporary itself.
	///
	/// The assignments are folded back in the order they appear, which is the order they had, and a
	/// value hoisted into a temporary immediately before its assignment comes with it. Anything else
	/// between the allocation and an assignment stops the fold, since moving an assignment across it
	/// could change what runs when.
	/// </summary>
	class FoldInitOnlyAssignmentsIntoInitializer : DepthFirstAstVisitor, IAstTransform
	{
		[AllowNull]
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			this.context = context;
			try
			{
				rootNode.AcceptVisitor(this);
			}
			finally
			{
				this.context = null;
			}
		}

		public override void VisitBlockStatement(BlockStatement blockStatement)
		{
			base.VisitBlockStatement(blockStatement);
			foreach (var statement in blockStatement.Statements.ToArray())
			{
				if (statement.Parent == blockStatement)
					TryFold(statement);
			}
		}

		bool TryFold(Statement statement)
		{
			if (statement is not VariableDeclarationStatement { Variables: [{ Initializer: not null } variable] })
				return false;
			if (variable.GetILVariable() is not { } target)
				return false;
			// A copy of a struct is the other place an init-only member can still be assigned: what
			// follows the copy is a with-expression. Copying a class only copies the reference, so
			// the assignment there is a mutation of the original and means something else entirely.
			ObjectCreateExpression? create = variable.Initializer as ObjectCreateExpression;
			if (create == null && target.Type.Kind is not (TypeKind.Struct or TypeKind.Enum))
				return false;

			// Locals that are nothing but a second name for the target: the compiler assigns through
			// them once the initializer has been split.
			var aliases = new HashSet<ILVariable>();
			var aliasDeclarations = new List<(Statement Statement, ILVariable Variable)>();
			// An initializer names each member at most once (CS1912), so members the creation
			// already initializes are off limits for the fold.
			var usedMemberNames = new HashSet<string>();
			if (create?.Initializer is { } existingInitializer)
			{
				foreach (var element in existingInitializer.Elements)
				{
					if (element is NamedExpression named)
						usedMemberNames.Add(named.Name);
				}
			}
			var absorbed = new List<(Statement Statement, string MemberName, Expression Value, Statement? Hoisted,
				Expression? HoistedValue, IdentifierExpression[]? HoistedReads)>();
			bool sawInitOnly = false;
			Statement current = statement;
			while (current.GetNextSibling(n => n is Statement) is Statement next)
			{
				if (IsAliasDeclaration(next, target, aliases, out var alias))
				{
					aliases.Add(alias);
					aliasDeclarations.Add((next, alias));
					current = next;
					continue;
				}
				Statement? hoisted = null;
				Expression? hoistedValue = null;
				var candidate = next;
				if (IsHoistedValueDeclaration(candidate, out var hoistedVariable, out hoistedValue))
				{
					hoisted = candidate;
					if (candidate.GetNextSibling(n => n is Statement) is not Statement afterHoist)
						break;
					candidate = afterHoist;
				}
				if (!IsMemberAssignment(candidate, target, aliases, out var member, out var value, out bool isInitOnly))
					break;
				// A second assignment to a member the initializer already carries is an ordinary
				// later mutation; absorbing it would name the member twice (CS1912). It and
				// everything after it stay behind as statements.
				if (!usedMemberNames.Add(member))
					break;
				IdentifierExpression[]? hoistedReads = null;
				if (hoisted != null)
				{
					// The temporary disappears with the assignment, so the initializer has to carry the
					// value the temporary held rather than a reference to it. A read left anywhere else
					// would be looking at a local that no longer exists.
					var reads = value.DescendantsAndSelf.OfType<IdentifierExpression>()
						.Where(identifier => identifier.GetILVariable() == hoistedVariable)
						.ToArray();
					if (reads.Length == 0)
						break;
					if (IsReadAnywhere(hoistedVariable!, statement.Ancestors.OfType<BlockStatement>().Last(),
						except: reads))
					{
						break;
					}
					// A single read can be replaced directly. Multiple reads need a one-arm switch
					// expression so that an impure value is still evaluated exactly once, at this
					// member's original position. Only do that when var preserves the local's type.
					if (reads.Length > 1 && (!context.Settings.PatternMatching || !context.Settings.SwitchExpressions
						|| !hoistedValue!.GetResolveResult().Type.Equals(hoistedVariable!.Type)))
					{
						break;
					}
					hoistedReads = reads;
					if (ReadsTarget(hoistedValue!, target, aliases))
						break;
				}
				if (ReadsTarget(value, target, aliases))
					break;
				absorbed.Add((candidate, member, value, hoisted, hoistedValue, hoistedReads));
				sawInitOnly |= isInitOnly;
				current = candidate;
			}

			if (!sawInitOnly)
				return false;

			ArrayInitializerExpression initializer;
			if (create != null)
			{
				// Folding the assignments back in is exactly the object-initializer syntax.
				if (!context.Settings.ObjectOrCollectionInitializers)
					return false;
				initializer = create.Initializer ?? new ArrayInitializerExpression();
				if (create.Initializer == null)
					create.Initializer = initializer;
			}
			else
			{
				if (!context.Settings.WithExpressions)
					return false;
				initializer = new ArrayInitializerExpression();
				var copied = variable.Initializer!.Detach();
				variable.Initializer = new WithInitializerExpression {
					Expression = copied,
					Initializer = initializer
				};
			}
			foreach (var (assignment, memberName, value, hoisted, hoistedValue, hoistedReads) in absorbed)
			{
				Expression element = value;
				if (hoistedValue != null)
				{
					if (hoistedReads!.Length > 1)
					{
						var boundVariable = hoistedReads[0].GetILVariable()!;
						var designation = new SingleVariableDesignation { Identifier = boundVariable.Name! };
						designation.AddAnnotation(new ILVariableResolveResult(boundVariable, boundVariable.Type));
						var declaration = new DeclarationExpression {
							Type = new SimpleType("var"),
							Designation = designation
						};
						var switchExpression = new SwitchExpression { Expression = hoistedValue.Detach() };
						switchExpression.SwitchSections.Add(new SwitchExpressionSection {
							Pattern = declaration,
							Body = element.Detach()
						});
						element = switchExpression;
					}
					else if (hoistedReads[0] == element)
					{
						element = hoistedValue;
					}
					else
					{
						hoistedReads[0].ReplaceWith(hoistedValue.Detach());
					}
				}
				element.Remove();
				initializer.Elements.Add(new NamedExpression(memberName, element));
				assignment.Remove();
				hoisted?.Remove();
			}
			// An alias exists only to carry the assignments that have just moved; once they are gone it
			// usually has no reader left.
			foreach (var (declaration, alias) in aliasDeclarations)
			{
				if (!IsReadAnywhere(alias, statement.Ancestors.OfType<BlockStatement>().Last()))
					declaration.Remove();
			}
			return true;
		}

		static bool IsAliasDeclaration(Statement statement, ILVariable target, HashSet<ILVariable> aliases,
			out ILVariable alias)
		{
			alias = null!;
			if (statement is not VariableDeclarationStatement { Variables: [{ Initializer: IdentifierExpression source } variable] })
				return false;
			if (source.GetILVariable() is not { } sourceVariable)
				return false;
			if (sourceVariable != target && !aliases.Contains(sourceVariable))
				return false;
			if (variable.GetILVariable() is not { } aliasVariable)
				return false;
			// Only a reference type gets a second name this way. Copying a value type yields an
			// independent value, so the assignments that follow belong to the copy - that is a
			// with-expression over the copy, and folding it into the source's initializer would
			// assign to the wrong variable. Left out here, the copy is folded on its own turn.
			if (aliasVariable.Type.IsReferenceType != true)
				return false;
			alias = aliasVariable;
			return true;
		}

		static bool IsHoistedValueDeclaration(Statement statement, out ILVariable? variable, out Expression? value)
		{
			variable = null;
			value = null;
			if (statement is not VariableDeclarationStatement { Variables: [{ Initializer: not null } declared] } declaration)
				return false;
			if (declaration.Type.IsVar())
				return false;
			variable = declared.GetILVariable();
			value = declared.Initializer;
			return variable != null;
		}

		static bool IsMemberAssignment(Statement statement, ILVariable target, HashSet<ILVariable> aliases,
			out string memberName, out Expression value, out bool isInitOnly)
		{
			memberName = null!;
			value = null!;
			isInitOnly = false;
			if (statement is not ExpressionStatement {
				Expression: AssignmentExpression {
					Operator: AssignmentOperatorType.Assign,
					Left: MemberReferenceExpression { Target: IdentifierExpression receiver } memberReference,
					Right: var assigned
				}
			})
			{
				return false;
			}
			if (receiver.GetILVariable() is not { } receiverVariable)
				return false;
			if (receiverVariable != target && !aliases.Contains(receiverVariable))
				return false;
			if (memberReference.GetSymbol() is not IMember member)
				return false;
			isInitOnly = member is IProperty { Setter.IsInitOnly: true };
			memberName = member.Name;
			value = assigned;
			return true;
		}

		static bool IsReadAnywhere(ILVariable variable, AstNode scope,
			IReadOnlyCollection<IdentifierExpression>? except = null)
		{
			return scope.DescendantsAndSelf.OfType<IdentifierExpression>()
				.Any(identifier => (except == null || !except.Contains(identifier))
					&& identifier.GetILVariable() == variable);
		}

		static bool ReadsTarget(Expression value, ILVariable target, HashSet<ILVariable> aliases)
		{
			return value.DescendantsAndSelf.OfType<IdentifierExpression>().Any(identifier => {
				var variable = identifier.GetILVariable();
				return variable != null && (variable == target || aliases.Contains(variable));
			});
		}
	}
}

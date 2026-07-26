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
		public void Run(AstNode rootNode, TransformContext context)
		{
			rootNode.AcceptVisitor(this);
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
			if (statement is not VariableDeclarationStatement {
				Variables: [{ Initializer: ObjectCreateExpression create } variable]
			})
			{
				return false;
			}
			if (variable.GetILVariable() is not { } target)
				return false;

			// Locals that are nothing but a second name for the target: the compiler assigns through
			// them once the initializer has been split.
			var aliases = new HashSet<ILVariable>();
			var aliasDeclarations = new List<(Statement Statement, ILVariable Variable)>();
			var absorbed = new List<(Statement Statement, string MemberName, Expression Value, Statement? HoistedValue)>();
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
				if (hoisted != null)
				{
					// The temporary disappears with the assignment, so the initializer has to carry the
					// value the temporary held rather than a reference to it.
					if (!IsSoleUseOfHoistedValue(hoistedVariable!, value))
						break;
					value = hoistedValue!;
				}
				if (ReadsTarget(value, target, aliases))
					break;
				absorbed.Add((candidate, member, value, hoisted));
				sawInitOnly |= isInitOnly;
				current = candidate;
			}

			if (!sawInitOnly)
				return false;

			var initializer = create.Initializer ?? new ArrayInitializerExpression();
			if (create.Initializer == null)
				create.Initializer = initializer;
			foreach (var (assignment, memberName, value, hoisted) in absorbed)
			{
				value.Remove();
				initializer.Elements.Add(new NamedExpression(memberName, value));
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

		static bool IsSoleUseOfHoistedValue(ILVariable hoisted, Expression value)
		{
			return value is IdentifierExpression identifier && identifier.GetILVariable() == hoisted;
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

		static bool IsReadAnywhere(ILVariable variable, AstNode scope)
		{
			return scope.DescendantsAndSelf.OfType<IdentifierExpression>()
				.Any(identifier => identifier.GetILVariable() == variable);
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

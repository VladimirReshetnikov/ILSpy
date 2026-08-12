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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Simplifies "x = x op y" into "x op= y" where possible.
	/// </summary>
	/// <remarks>
	/// Because the two "x" in "x = x op y" may refer to different ILVariables,
	/// this transform must run after DeclareVariables.
	/// 
	/// It must also run after ReplaceMethodCallsWithOperators (so that it can work for custom operator, too);
	/// and after AddCheckedBlocks (because "for (;; x = unchecked(x op y))" cannot be transformed into "x += y").
	/// </remarks>
	class PrettifyAssignments : DepthFirstAstVisitor, IAstTransform
	{
		[AllowNull]
		TransformContext context;

		public override void VisitAssignmentExpression(AssignmentExpression assignment)
		{
			base.VisitAssignmentExpression(assignment);
			// Combine "x = x op y" into "x op= y"
			// Also supports "x = (T)(x op y)" -> "x op= y" for a predefined operator op when
			// T is the type of x and y is implicitly convertible to T: that is exactly the
			// shape C# assigns to the compound form for predefined narrowing operators.
			// A user-defined operator's result must instead be implicitly convertible to the
			// type of x, so around one every cast has to stay in the source.
			Expression rhs = assignment.Right;
			CastExpression? cast = assignment.Right as CastExpression;
			if (cast != null)
			{
				rhs = cast.Expression;
			}
			if (rhs is BinaryOperatorExpression binary && assignment.Operator == AssignmentOperatorType.Assign)
			{
				var newOperator = GetAssignmentOperatorForBinaryOperator(binary.Operator);
				if (newOperator != AssignmentOperatorType.Assign
					&& CanConvertToCompoundAssignment(assignment.Left) && assignment.Left.IsMatch(binary.Left)
					&& binary.Right != null
					&& (cast == null || CastKeepsCompoundAssignmentSemantics(assignment.Left, binary, cast))
					&& NoCompoundAssignmentOperatorTakesOver(assignment.Left, binary.Operator))
				{
					context.Step("Convert assignment to compound assignment", assignment);
					assignment.Operator = newOperator;
					// If we found a shorter operator, get rid of the BinaryOperatorExpression:
					assignment.CopyAnnotationsFrom(binary);
					assignment.Right = binary.Right;
				}
			}
			if (context.Settings.IntroduceIncrementAndDecrement && assignment.Operator is AssignmentOperatorType.Add or AssignmentOperatorType.Subtract)
			{
				// detect increment/decrement; ++/-- on float/double compiles to the same IL as
				// adding/subtracting a constant 1 (which both types represent exactly), so a
				// constant 1 right-hand side qualifies there just like for integers
				var rr = assignment.Right.GetResolveResult();
				if (rr.IsCompileTimeConstant
					&& (rr.Type.IsCSharpPrimitiveIntegerType() || rr.Type.IsKnownType(KnownTypeCode.Single) || rr.Type.IsKnownType(KnownTypeCode.Double))
					&& CSharpPrimitiveCast.Cast(rr.Type.GetTypeCode(), 1, false).Equals(rr.ConstantValue))
				{
					// only if it's not a custom operator
					if (assignment.Annotation<IL.CallInstruction>() == null && assignment.Annotation<IL.UserDefinedCompoundAssign>() == null && assignment.Annotation<IL.DynamicCompoundAssign>() == null
						&& NoIncrementOperatorTakesOver(assignment.Left, assignment.Operator == AssignmentOperatorType.Add))
					{
						UnaryOperatorType type;
						// When the parent is an expression statement, pre- or post-increment doesn't matter;
						// so we can pick post-increment which is more commonly used (for (int i = 0; i < x; i++))
						if (assignment.Parent is ExpressionStatement)
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement;
						else
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.Increment : UnaryOperatorType.Decrement;
						context.Step(type is UnaryOperatorType.Increment or UnaryOperatorType.PostIncrement ? "Convert assignment to increment" : "Convert assignment to decrement", assignment);
						var unaryOperator = new UnaryOperatorExpression(type, assignment.Left.Detach()).CopyAnnotationsFrom(assignment);
						assignment.ReplaceWith(unaryOperator);
						context.EndStep(unaryOperator);
					}
				}
			}

		}

		bool CastKeepsCompoundAssignmentSemantics(Expression left, BinaryOperatorExpression binary, CastExpression cast)
		{
			// "x op= y" only applies an explicit conversion from the operator result back to
			// the type of x when the selected operator is predefined; a user-defined operator
			// requires an implicit result conversion, so its cast cannot be folded away.
			// Everything that cannot be proven predefined keeps the expanded form.
			if (binary.GetResolveResult() is not OperatorResolveResult { UserDefinedOperatorMethod: null })
				return false;
			if (cast.GetSymbol() is IMember)
				return false;
			if (cast.GetResolveResult() is ConversionResolveResult { Conversion.IsUserDefined: true })
				return false;
			IType castTargetType = cast.Type.GetResolveResult().Type;
			if (castTargetType.Kind == TypeKind.Unknown)
				return false;
			IType leftType = left.GetResolveResult().Type;
			if (!NormalizeTypeVisitor.IgnoreNullabilityAndTuples.EquivalentTypes(leftType, castTargetType))
				return false;
			if (binary.Right is not { } operand)
				return false;
			var conversions = CSharpConversions.Get(context.TypeSystem);
			return conversions.ImplicitConversion(operand.GetResolveResult(), castTargetType).IsImplicit;
		}

		bool NoCompoundAssignmentOperatorTakesOver(Expression left, BinaryOperatorType op)
		{
			string? operatorMethodName = GetOperatorMethodName(op);
			if (operatorMethodName == null)
				return false;
			return IL.Transforms.CompoundAssignmentOperatorGuard.MayIntroduceCompoundAssignment(
				context.TypeSystem, left.GetResolveResult().Type, operatorMethodName);
		}

		bool NoIncrementOperatorTakesOver(Expression left, bool increment)
		{
			return IL.Transforms.CompoundAssignmentOperatorGuard.MayIntroduceCompoundAssignment(
				context.TypeSystem, left.GetResolveResult().Type, increment ? "op_Increment" : "op_Decrement");
		}

		static string? GetOperatorMethodName(BinaryOperatorType bop)
		{
			switch (bop)
			{
				case BinaryOperatorType.Add:
					return "op_Addition";
				case BinaryOperatorType.Subtract:
					return "op_Subtraction";
				case BinaryOperatorType.Multiply:
					return "op_Multiply";
				case BinaryOperatorType.Divide:
					return "op_Division";
				case BinaryOperatorType.Modulus:
					return "op_Modulus";
				case BinaryOperatorType.ShiftLeft:
					return "op_LeftShift";
				case BinaryOperatorType.ShiftRight:
					return "op_RightShift";
				case BinaryOperatorType.UnsignedShiftRight:
					return "op_UnsignedRightShift";
				case BinaryOperatorType.BitwiseAnd:
					return "op_BitwiseAnd";
				case BinaryOperatorType.BitwiseOr:
					return "op_BitwiseOr";
				case BinaryOperatorType.ExclusiveOr:
					return "op_ExclusiveOr";
				default:
					return null;
			}
		}

		public static AssignmentOperatorType GetAssignmentOperatorForBinaryOperator(BinaryOperatorType bop)
		{
			switch (bop)
			{
				case BinaryOperatorType.Add:
					return AssignmentOperatorType.Add;
				case BinaryOperatorType.Subtract:
					return AssignmentOperatorType.Subtract;
				case BinaryOperatorType.Multiply:
					return AssignmentOperatorType.Multiply;
				case BinaryOperatorType.Divide:
					return AssignmentOperatorType.Divide;
				case BinaryOperatorType.Modulus:
					return AssignmentOperatorType.Modulus;
				case BinaryOperatorType.ShiftLeft:
					return AssignmentOperatorType.ShiftLeft;
				case BinaryOperatorType.ShiftRight:
					return AssignmentOperatorType.ShiftRight;
				case BinaryOperatorType.UnsignedShiftRight:
					return AssignmentOperatorType.UnsignedShiftRight;
				case BinaryOperatorType.BitwiseAnd:
					return AssignmentOperatorType.BitwiseAnd;
				case BinaryOperatorType.BitwiseOr:
					return AssignmentOperatorType.BitwiseOr;
				case BinaryOperatorType.ExclusiveOr:
					return AssignmentOperatorType.ExclusiveOr;
				default:
					return AssignmentOperatorType.Assign;
			}
		}

		static bool CanConvertToCompoundAssignment(Expression left)
		{
			MemberReferenceExpression? mre = left as MemberReferenceExpression;
			if (mre != null)
				return IsWithoutSideEffects(mre.Target);
			IndexerExpression? ie = left as IndexerExpression;
			if (ie != null)
				return IsWithoutSideEffects(ie.Target) && ie.Arguments.All(IsWithoutSideEffects);
			UnaryOperatorExpression? uoe = left as UnaryOperatorExpression;
			if (uoe != null && uoe.Operator == UnaryOperatorType.Dereference)
				return IsWithoutSideEffects(uoe.Expression);
			return IsWithoutSideEffects(left);
		}

		static bool IsWithoutSideEffects(Expression? left)
		{
			return left is ThisReferenceExpression || left is IdentifierExpression || left is TypeReferenceExpression || left is BaseReferenceExpression;
		}

		void IAstTransform.Run(AstNode node, TransformContext context)
		{
			this.context = context;
			try
			{
				node.AcceptVisitor(this);
			}
			finally
			{
				this.context = null;
			}
		}
	}
}

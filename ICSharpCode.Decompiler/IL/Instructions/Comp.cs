#nullable enable
// Copyright (c) 2014 Daniel Grunwald
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

using System;
using System.Diagnostics;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
	/// <summary>
	/// Specifies the relational operation performed by a <see cref="Comp"/> instruction.
	/// </summary>
	public enum ComparisonKind : byte
	{
		/// <summary>
		/// Tests whether both operands are equal.
		/// </summary>
		Equality,
		/// <summary>
		/// Tests whether both operands are not equal.
		/// </summary>
		Inequality,
		/// <summary>
		/// Tests whether the left operand is strictly less than the right operand.
		/// </summary>
		LessThan,
		/// <summary>
		/// Tests whether the left operand is less than or equal to the right operand.
		/// </summary>
		LessThanOrEqual,
		/// <summary>
		/// Tests whether the left operand is strictly greater than the right operand.
		/// </summary>
		GreaterThan,
		/// <summary>
		/// Tests whether the left operand is greater than or equal to the right operand.
		/// </summary>
		GreaterThanOrEqual
	}

	static class ComparisonKindExtensions
	{
		/// <summary>
		/// Determines whether the comparison is an equality-style check.
		/// </summary>
		/// <param name="kind">The comparison kind to test.</param>
		/// <returns>
		/// <see langword="true"/> for <see cref="ComparisonKind.Equality"/> and <see cref="ComparisonKind.Inequality"/>;
		/// otherwise, <see langword="false"/>.
		/// </returns>
		public static bool IsEqualityOrInequality(this ComparisonKind kind)
		{
			return kind == ComparisonKind.Equality || kind == ComparisonKind.Inequality;
		}

		/// <summary>
		/// Computes the logical negation of a comparison operation.
		/// </summary>
		/// <param name="kind">The comparison kind to negate.</param>
		/// <returns>The comparison kind that represents the opposite truth condition.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is outside the supported <see cref="ComparisonKind"/> range.</exception>
		public static ComparisonKind Negate(this ComparisonKind kind)
		{
			switch (kind)
			{
				case ComparisonKind.Equality:
					return ComparisonKind.Inequality;
				case ComparisonKind.Inequality:
					return ComparisonKind.Equality;
				case ComparisonKind.LessThan:
					return ComparisonKind.GreaterThanOrEqual;
				case ComparisonKind.LessThanOrEqual:
					return ComparisonKind.GreaterThan;
				case ComparisonKind.GreaterThan:
					return ComparisonKind.LessThanOrEqual;
				case ComparisonKind.GreaterThanOrEqual:
					return ComparisonKind.LessThan;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Maps a comparison kind to the C# syntax tree operator used when emitting source expressions.
		/// </summary>
		/// <param name="kind">The comparison kind to map.</param>
		/// <returns>The corresponding <see cref="BinaryOperatorType"/> value.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is outside the supported <see cref="ComparisonKind"/> range.</exception>
		public static BinaryOperatorType ToBinaryOperatorType(this ComparisonKind kind)
		{
			switch (kind)
			{
				case ComparisonKind.Equality:
					return BinaryOperatorType.Equality;
				case ComparisonKind.Inequality:
					return BinaryOperatorType.InEquality;
				case ComparisonKind.LessThan:
					return BinaryOperatorType.LessThan;
				case ComparisonKind.LessThanOrEqual:
					return BinaryOperatorType.LessThanOrEqual;
				case ComparisonKind.GreaterThan:
					return BinaryOperatorType.GreaterThan;
				case ComparisonKind.GreaterThanOrEqual:
					return BinaryOperatorType.GreaterThanOrEqual;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Gets the textual operator token used when writing IL AST for the specified comparison kind.
		/// </summary>
		/// <param name="kind">The comparison kind whose token should be returned.</param>
		/// <returns>A token such as <c>==</c>, <c>!=</c>, <c>&lt;</c>, or <c>&gt;=</c>.</returns>
		public static string GetToken(this ComparisonKind kind)
		{
			return BinaryOperatorExpression.GetOperatorToken(kind.ToBinaryOperatorType());
		}
	}

	/// <summary>
	/// Describes how a <see cref="Comp"/> instruction handles nullable operands.
	/// </summary>
	public enum ComparisonLiftingKind
	{
		/// <summary>
		/// Not a lifted comparison.
		/// </summary>
		None,
		/// <summary>
		/// C#-style lifted comparison:
		/// * operands that have a ResultType != this.InputType are expected to return a value of
		///   type Nullable{T}, where T.GetStackType() == this.InputType.
		/// * if both operands are <c>null</c>, equality comparisons evaluate to 1, all other comparisons to 0.
		/// * if one operand is <c>null</c>, inequality comparisons evaluate to 1, all other comparisons to 0.
		/// * if neither operand is <c>null</c>, the underlying comparison is performed.
		/// 
		/// Note that even though C#-style lifted comparisons set IsLifted=true,
		/// the ResultType remains I4 as with normal comparisons.
		/// </summary>
		CSharp,
		/// <summary>
		/// SQL-style lifted comparison: works like a lifted binary numeric instruction,
		/// that is, if any input operand is <c>null</c>, the comparison evaluates to <c>null</c>.
		/// </summary>
		/// <remarks>
		/// This lifting kind is currently only used for operator! on bool?.
		/// </remarks>
		ThreeValuedLogic
	}

	partial class Comp : ILiftableInstruction
	{
		ComparisonKind kind;

		/// <summary>
		/// Gets or sets the concrete comparison operation represented by this instruction.
		/// </summary>
		public ComparisonKind Kind {
			get { return kind; }
			set {
				kind = value;
				MakeDirty();
			}
		}

		/// <summary>
		/// Defines the nullable lifting behavior used for this comparison.
		/// </summary>
		public ComparisonLiftingKind LiftingKind;

		/// <summary>
		/// Gets the stack type of the comparison inputs.
		/// For lifted comparisons, this is the underlying input type.
		/// </summary>
		public StackType InputType;

		/// <summary>
		/// If this is an integer comparison, specifies the sign used to interpret the integers.
		/// </summary>
		public readonly Sign Sign;

		/// <summary>
		/// Initializes a non-lifted comparison instruction.
		/// </summary>
		/// <param name="kind">The comparison operation to perform.</param>
		/// <param name="sign">The signedness used for integer comparisons.</param>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		public Comp(ComparisonKind kind, Sign sign, ILInstruction left, ILInstruction right) : base(OpCode.Comp, left, right)
		{
			this.kind = kind;
			this.LiftingKind = ComparisonLiftingKind.None;
			this.InputType = left.ResultType;
			this.Sign = sign;
			Debug.Assert(left.ResultType == right.ResultType);
		}

		/// <summary>
		/// Initializes a comparison instruction with explicit lifting and input stack-type metadata.
		/// </summary>
		/// <param name="kind">The comparison operation to perform.</param>
		/// <param name="lifting">The nullable lifting mode.</param>
		/// <param name="inputType">The expected non-lifted stack type of the operands.</param>
		/// <param name="sign">The signedness used for integer comparisons.</param>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		public Comp(ComparisonKind kind, ComparisonLiftingKind lifting, StackType inputType, Sign sign, ILInstruction left, ILInstruction right) : base(OpCode.Comp, left, right)
		{
			this.kind = kind;
			this.LiftingKind = lifting;
			this.InputType = inputType;
			this.Sign = sign;
		}

		/// <summary>
		/// Gets the stack type produced by this comparison.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Most comparisons produce an <see cref="StackType.I4"/> boolean-like value.
		/// </para>
		/// <para>
		/// When <see cref="LiftingKind"/> is <see cref="ComparisonLiftingKind.ThreeValuedLogic"/>, the result is represented as
		/// a nullable object stack value (<see cref="StackType.O"/>) so that <see langword="null"/> can encode "unknown".
		/// </para>
		/// </remarks>
		public override StackType ResultType => LiftingKind == ComparisonLiftingKind.ThreeValuedLogic ? StackType.O : StackType.I4;

		/// <summary>
		/// Gets whether this instruction uses any nullable lifting mode.
		/// </summary>
		public bool IsLifted => LiftingKind != ComparisonLiftingKind.None;

		/// <summary>
		/// Gets the non-lifted comparison result stack type.
		/// </summary>
		public StackType UnderlyingResultType => StackType.I4;

		internal override void CheckInvariant(ILPhase phase)
		{
			base.CheckInvariant(phase);
			if (LiftingKind == ComparisonLiftingKind.None)
			{
				Debug.Assert(Left.ResultType == InputType);
				Debug.Assert(Right.ResultType == InputType);
			}
			else
			{
				Debug.Assert(Left.ResultType == InputType || Left.ResultType == StackType.O);
				Debug.Assert(Right.ResultType == InputType || Right.ResultType == StackType.O);
			}
		}

		protected override void WriteToCore(ITextOutput output, ILAstWritingOptions options)
		{
			WriteILRange(output, options);
			if (options.UseLogicOperationSugar && MatchLogicNot(out var arg))
			{
				output.Write("logic.not(");
				arg.WriteTo(output, options);
				output.Write(')');
				return;
			}
			output.Write(OpCode);
			output.Write('.');
			output.Write(InputType.ToString().ToLower());
			switch (Sign)
			{
				case Sign.Signed:
					output.Write(".signed");
					break;
				case Sign.Unsigned:
					output.Write(".unsigned");
					break;
			}
			switch (LiftingKind)
			{
				case ComparisonLiftingKind.CSharp:
					output.Write(".lifted[C#]");
					break;
				case ComparisonLiftingKind.ThreeValuedLogic:
					output.Write(".lifted[3VL]");
					break;
			}
			output.Write('(');
			Left.WriteTo(output, options);
			output.Write(' ');
			output.Write(Kind.GetToken());
			output.Write(' ');
			Right.WriteTo(output, options);
			output.Write(')');
		}

		/// <summary>
		/// Creates a canonical logical negation node by comparing an expression with zero.
		/// </summary>
		/// <param name="arg">The expression to negate.</param>
		/// <returns>A <see cref="Comp"/> instruction equivalent to <c>arg == 0</c>.</returns>
		public static Comp LogicNot(ILInstruction arg)
		{
			return new Comp(ComparisonKind.Equality, Sign.None, arg, new LdcI4(0));
		}

		/// <summary>
		/// Creates a logical negation node and chooses whether nullable three-valued lifting should be used.
		/// </summary>
		/// <param name="arg">The expression to negate.</param>
		/// <param name="isLifted">
		/// <see langword="true"/> to emit a three-valued nullable comparison; otherwise <see langword="false"/> for normal two-valued logic.
		/// </param>
		/// <returns>A <see cref="Comp"/> instruction equivalent to logical NOT over <paramref name="arg"/>.</returns>
		public static Comp LogicNot(ILInstruction arg, bool isLifted)
		{
			var liftingKind = isLifted ? ComparisonLiftingKind.ThreeValuedLogic : ComparisonLiftingKind.None;
			return new Comp(ComparisonKind.Equality, liftingKind, StackType.I4, Sign.None, arg, new LdcI4(0));
		}

		internal override bool CanInlineIntoSlot(int childIndex, ILInstruction expressionBeingMoved)
		{
			// ExpressionBuilder translates comp.o(a op b) for op not in (==, !=) into
			// Unsafe.As(ref a) op Unsafe.As(ref b), which requires that a and b are variables
			// and not expressions. Returning false in those cases prevents inlining.
			// However if one of the arguments is LdNull, then we don't need the Unsafe.As trickery, and can always inline.
			if (kind.IsEqualityOrInequality() || this.InputType != StackType.O)
			{
				// OK, won't need Unsafe.As.
				return true;
			}
			if (expressionBeingMoved is LdLoc || expressionBeingMoved.MatchLdsFld(out _))
			{
				// OK, can use variable/field name with Unsafe.As(ref x)
				return true;
			}
			if (Sign != Sign.Signed && (expressionBeingMoved is LdNull || Left is LdNull || Right is LdNull))
			{
				// OK, this is the "compare with null" special case that doesn't need Unsafe.As()
				return true;
			}
			return false;
		}
	}
}

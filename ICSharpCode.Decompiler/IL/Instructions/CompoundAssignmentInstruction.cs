#nullable enable
// Copyright (c) 2016 Siegfried Pammer
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
using System.Linq.Expressions;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
	/// <summary>
	/// Describes which value a compound-assignment instruction exposes as its expression result.
	/// </summary>
	/// <remarks>
	/// Prefix-style operations evaluate to the updated value, while postfix-style operations evaluate to the original value.
	/// The distinction is consumed by <see cref="ICSharpCode.Decompiler.CSharp.ExpressionBuilder"/> when selecting C# syntax.
	/// </remarks>
	public enum CompoundEvalMode : byte
	{
		/// <summary>
		/// The compound.assign instruction will evaluate to the old value.
		/// This mode is used only for post-increment/decrement.
		/// </summary>
		EvaluatesToOldValue,
		/// <summary>
		/// The compound.assign instruction will evaluate to the new value.
		/// This mode is used for compound assignments and pre-increment/decrement.
		/// </summary>
		EvaluatesToNewValue
	}

	/// <summary>
	/// Identifies how <see cref="CompoundAssignmentInstruction.Target"/> is represented in the IL tree.
	/// </summary>
	public enum CompoundTargetKind : byte
	{
		/// <summary>
		/// The target is an instruction computing an address,
		/// and the compound.assign will implicitly load/store from/to that address.
		/// </summary>
		Address,
		/// <summary>
		/// The Target must be a call to a property getter,
		/// and the compound.assign will implicitly call the corresponding property setter.
		/// </summary>
		Property,
		/// <summary>
		/// The target is a dynamic call.
		/// </summary>
		Dynamic
	}

	/// <summary>
	/// Base class for IL instructions that model compound assignments and increment/decrement updates.
	/// </summary>
	/// <remarks>
	/// Instances are produced by assignment transforms after proving that the load/compute/store sequence can be collapsed
	/// without changing side effects, conversion behavior, or target evaluation ordering.
	/// </remarks>
	public abstract partial class CompoundAssignmentInstruction : ILInstruction
	{
		/// <summary>
		/// Specifies whether the expression result is the old value or the updated value.
		/// </summary>
		public readonly CompoundEvalMode EvalMode;

		/// <summary>
		/// If TargetIsProperty is true, the Target must be a call to a property getter,
		/// and the compound.assign will implicitly call the corresponding property setter.
		/// Otherwise, the Target can be any instruction that evaluates to an address,
		/// and the compound.assign will implicit load and store from/to that address.
		/// </summary>
		public readonly CompoundTargetKind TargetKind;

		/// <summary>
		/// Initializes a compound-assignment instruction.
		/// </summary>
		/// <param name="opCode">The concrete instruction opcode.</param>
		/// <param name="evalMode">Whether the expression result is old-value or new-value based.</param>
		/// <param name="target">The instruction that locates the assignment target.</param>
		/// <param name="targetKind">How <paramref name="target"/> should be interpreted.</param>
		/// <param name="value">The right-hand operand participating in the update.</param>
		public CompoundAssignmentInstruction(OpCode opCode, CompoundEvalMode evalMode, ILInstruction target, CompoundTargetKind targetKind, ILInstruction value)
			: base(opCode)
		{
			this.EvalMode = evalMode;
			this.Target = target;
			this.TargetKind = targetKind;
			this.Value = value;
			CheckValidTarget();
		}

		internal override void CheckInvariant(ILPhase phase)
		{
			base.CheckInvariant(phase);
			CheckValidTarget();
		}

		[Conditional("DEBUG")]
		void CheckValidTarget()
		{
			switch (TargetKind)
			{
				case CompoundTargetKind.Address:
					Debug.Assert(target.ResultType == StackType.Ref || target.ResultType == StackType.I);
					break;
				case CompoundTargetKind.Property:
					Debug.Assert(target.OpCode == OpCode.Call || target.OpCode == OpCode.CallVirt);
					var owner = ((CallInstruction)target).Method.AccessorOwner as IProperty;
					Debug.Assert(owner != null && owner.CanSet);
					break;
				case CompoundTargetKind.Dynamic:
					Debug.Assert(target.OpCode == OpCode.DynamicGetMemberInstruction || target.OpCode == OpCode.DynamicGetIndexInstruction);
					break;
			}
		}

		protected void WriteSuffix(ITextOutput output)
		{
			switch (TargetKind)
			{
				case CompoundTargetKind.Address:
					output.Write(".address");
					break;
				case CompoundTargetKind.Property:
					output.Write(".property");
					break;
			}
			switch (EvalMode)
			{
				case CompoundEvalMode.EvaluatesToNewValue:
					output.Write(".new");
					break;
				case CompoundEvalMode.EvaluatesToOldValue:
					output.Write(".old");
					break;
			}
		}
	}

	/// <summary>
	/// Represents a compound assignment implemented by a built-in numeric operator.
	/// </summary>
	public partial class NumericCompoundAssign : CompoundAssignmentInstruction, ILiftableInstruction
	{
		/// <summary>
		/// Gets whether the instruction checks for overflow.
		/// </summary>
		public readonly bool CheckForOverflow;

		/// <summary>
		/// For integer operations that depend on the sign, specifies whether the operation
		/// is signed or unsigned.
		/// For instructions that produce the same result for either sign, returns Sign.None.
		/// </summary>
		public readonly Sign Sign;

		public readonly StackType LeftInputType;
		public readonly StackType RightInputType;
		/// <summary>
		/// Gets the non-lifted stack type produced by the underlying numeric operation.
		/// </summary>
		public StackType UnderlyingResultType { get; }

		/// <summary>
		/// The operator used by this assignment operator instruction.
		/// </summary>
		public readonly BinaryNumericOperator Operator;

		public bool IsLifted { get; }

		/// <summary>
		/// Initializes a numeric compound assignment from a previously matched binary operation.
		/// </summary>
		/// <param name="binary">The binary operation being folded into compound form.</param>
		/// <param name="target">The assignment target instruction.</param>
		/// <param name="targetKind">How <paramref name="target"/> should be interpreted.</param>
		/// <param name="value">The right-hand operand used by the operation.</param>
		/// <param name="type">The semantic type of the assignment target.</param>
		/// <param name="evalMode">Whether the expression result is old-value or new-value based.</param>
		public NumericCompoundAssign(BinaryNumericInstruction binary, ILInstruction target,
			CompoundTargetKind targetKind, ILInstruction value, IType type, CompoundEvalMode evalMode)
			: base(OpCode.NumericCompoundAssign, evalMode, target, targetKind, value)
		{
			Debug.Assert(IsBinaryCompatibleWithType(binary, type, null));
			this.CheckForOverflow = binary.CheckForOverflow;
			this.Sign = binary.Sign;
			this.LeftInputType = binary.LeftInputType;
			this.RightInputType = binary.RightInputType;
			this.UnderlyingResultType = binary.UnderlyingResultType;
			this.Operator = binary.Operator;
			this.IsLifted = binary.IsLifted;
			this.type = type;
			this.AddILRange(binary);
			Debug.Assert(evalMode == CompoundEvalMode.EvaluatesToNewValue || (Operator == BinaryNumericOperator.Add || Operator == BinaryNumericOperator.Sub));
			Debug.Assert(this.ResultType == (IsLifted ? StackType.O : UnderlyingResultType));
		}

		/// <summary>
		/// Gets whether the specific binary instruction is compatible with a compound operation on the specified type.
		/// </summary>
		/// <param name="binary">The binary instruction candidate.</param>
		/// <param name="type">The assignment target type that would receive the result.</param>
		/// <param name="settings">Decompiler settings that influence language-level legality checks.</param>
		/// <returns>
		/// <see langword="true"/> if the operation can be represented as a C# compound assignment without semantic changes;
		/// otherwise <see langword="false"/>.
		/// </returns>
		internal static bool IsBinaryCompatibleWithType(BinaryNumericInstruction binary, IType type, DecompilerSettings? settings)
		{
			if (binary.IsLifted)
			{
				if (!NullableType.IsNullable(type))
					return false;
				type = NullableType.GetUnderlyingType(type);
			}
			if (type.Kind == TypeKind.Unknown)
			{
				return false; // avoid introducing a potentially-incorrect compound assignment
			}
			else if (type.Kind == TypeKind.Enum)
			{
				switch (binary.Operator)
				{
					case BinaryNumericOperator.Add:
					case BinaryNumericOperator.Sub:
					case BinaryNumericOperator.BitAnd:
					case BinaryNumericOperator.BitOr:
					case BinaryNumericOperator.BitXor:
						break; // OK
					default:
						return false; // operator not supported on enum types
				}
			}
			else if (type.Kind == TypeKind.Pointer)
			{
				switch (binary.Operator)
				{
					case BinaryNumericOperator.Add:
					case BinaryNumericOperator.Sub:
						// ensure that the byte offset is a multiple of the pointer size
						return PointerArithmeticOffset.Detect(
							binary.Right,
							((PointerType)type).ElementType,
							checkForOverflow: binary.CheckForOverflow
						) != null;
					default:
						return false; // operator not supported on pointer types
				}
			}
			else if ((type.IsKnownType(KnownTypeCode.IntPtr) || type.IsKnownType(KnownTypeCode.UIntPtr)) && type.Kind is not (TypeKind.NInt or TypeKind.NUInt))
			{
				// If the LHS is C# 9 IntPtr (but not nint or C# 11 IntPtr):
				// "target.intptr *= 2;" is compiler error, but
				// "target.intptr *= (nint)2;" works
				if (settings != null && !settings.NativeIntegers)
				{
					// But if native integers are not available, we cannot use compound assignment.
					return false;
				}
				// The trick with casting the RHS to n(u)int doesn't work for shifts:
				switch (binary.Operator)
				{
					case BinaryNumericOperator.ShiftLeft:
					case BinaryNumericOperator.ShiftRight:
						return false;
				}
			}
			if (binary.Sign != Sign.None)
			{
				bool signMismatchAllowed = (binary.Sign == Sign.Unsigned && binary.Operator == BinaryNumericOperator.ShiftRight && (settings == null || settings.UnsignedRightShift));
				if (type.IsCSharpSmallIntegerType())
				{
					// C# will use numeric promotion to int, binary op must be signed
					if (binary.Sign != Sign.Signed && !signMismatchAllowed)
						return false;
				}
				else
				{
					// C# will use sign from type; except for right shift with C# 11 >>> operator.
					if (type.GetSign() != binary.Sign && !signMismatchAllowed)
						return false;
				}
			}
			// Can't transform if the RHS value would be need to be truncated for the LHS type.
			if (Transforms.TransformAssignment.IsImplicitTruncation(binary.Right, type, null, binary.IsLifted))
				return false;
			return true;
		}

		protected override InstructionFlags ComputeFlags()
		{
			var flags = Target.Flags | Value.Flags | InstructionFlags.SideEffect;
			if (CheckForOverflow || (Operator == BinaryNumericOperator.Div || Operator == BinaryNumericOperator.Rem))
				flags |= InstructionFlags.MayThrow;
			return flags;
		}

		public override InstructionFlags DirectFlags {
			get {
				var flags = InstructionFlags.SideEffect;
				if (CheckForOverflow || (Operator == BinaryNumericOperator.Div || Operator == BinaryNumericOperator.Rem))
					flags |= InstructionFlags.MayThrow;
				return flags;
			}
		}

		protected override void WriteToCore(ITextOutput output, ILAstWritingOptions options)
		{
			WriteILRange(output, options);
			output.Write(OpCode);
			output.Write("." + BinaryNumericInstruction.GetOperatorName(Operator));
			if (CheckForOverflow)
			{
				output.Write(".ovf");
			}
			if (Sign == Sign.Unsigned)
			{
				output.Write(".unsigned");
			}
			else if (Sign == Sign.Signed)
			{
				output.Write(".signed");
			}
			output.Write('.');
			output.Write(UnderlyingResultType.ToString().ToLowerInvariant());
			if (IsLifted)
			{
				output.Write(".lifted");
			}
			base.WriteSuffix(output);
			output.Write('(');
			Target.WriteTo(output, options);
			output.Write(", ");
			Value.WriteTo(output, options);
			output.Write(')');
		}
	}

	/// <summary>
	/// Represents a compound assignment implemented by a user-defined operator method.
	/// </summary>
	public partial class UserDefinedCompoundAssign : CompoundAssignmentInstruction
	{
		/// <summary>
		/// Gets the method that provides the operator semantics.
		/// </summary>
		public readonly IMethod Method;
		public bool IsLifted => false; // TODO: implement lifted user-defined compound assignments

		/// <summary>
		/// Initializes a user-defined compound assignment instruction.
		/// </summary>
		/// <param name="method">The operator method or supported string-concatenation helper.</param>
		/// <param name="evalMode">Whether the expression result is old-value or new-value based.</param>
		/// <param name="target">The assignment target instruction.</param>
		/// <param name="targetKind">How <paramref name="target"/> should be interpreted.</param>
		/// <param name="value">The right-hand operand or unary step value.</param>
		public UserDefinedCompoundAssign(IMethod method, CompoundEvalMode evalMode,
			ILInstruction target, CompoundTargetKind targetKind, ILInstruction value)
			: base(OpCode.UserDefinedCompoundAssign, evalMode, target, targetKind, value)
		{
			this.Method = method;
			Debug.Assert(Method.IsOperator || IsStringConcat(method));
			Debug.Assert(evalMode == CompoundEvalMode.EvaluatesToNewValue || IsIncrementOrDecrement(method));
		}

		/// <summary>
		/// Determines whether a method matches increment/decrement operator names accepted by the decompiler.
		/// </summary>
		/// <param name="method">The candidate operator method.</param>
		/// <param name="settings">Optional settings used to decide whether checked variants are enabled.</param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="method"/> can represent increment/decrement;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public static bool IsIncrementOrDecrement(IMethod method, DecompilerSettings? settings = null)
		{
			if (!(method.IsOperator && method.IsStatic))
				return false;
			if (method.Name is "op_Increment" or "op_Decrement")
				return true;
			if (method.Name is "op_CheckedIncrement" or "op_CheckedDecrement")
				return settings?.CheckedOperators ?? true;
			return false;
		}

		/// <summary>
		/// Determines whether a method is recognized as <see cref="string"/> concatenation in compound-assignment lowering.
		/// </summary>
		/// <param name="method">The candidate method.</param>
		/// <returns>
		/// <see langword="true"/> if <paramref name="method"/> is a static <c>System.String.Concat</c> overload;
		/// otherwise <see langword="false"/>.
		/// </returns>
		public static bool IsStringConcat(IMethod method)
		{
			return method.Name == "Concat" && method.IsStatic && method.DeclaringType.IsKnownType(KnownTypeCode.String);
		}

		public override StackType ResultType => Method.ReturnType.GetStackType();

		protected override void WriteToCore(ITextOutput output, ILAstWritingOptions options)
		{
			WriteILRange(output, options);
			output.Write(OpCode);
			base.WriteSuffix(output);
			output.Write(' ');
			Method.WriteTo(output);
			output.Write('(');
			this.Target.WriteTo(output, options);
			output.Write(", ");
			this.Value.WriteTo(output, options);
			output.Write(')');
		}
	}

	/// <summary>
	/// Represents a compound assignment whose semantics are deferred to the C# dynamic binder.
	/// </summary>
	public partial class DynamicCompoundAssign : CompoundAssignmentInstruction
	{
		/// <summary>
		/// Gets the expression-tree operation describing the dynamic assignment form.
		/// </summary>
		public ExpressionType Operation { get; }
		/// <summary>
		/// Gets binder metadata for the target operand.
		/// </summary>
		public CSharpArgumentInfo TargetArgumentInfo { get; }
		/// <summary>
		/// Gets binder metadata for the value operand.
		/// </summary>
		public CSharpArgumentInfo ValueArgumentInfo { get; }
		/// <summary>
		/// Gets binder flags that influence runtime binding behavior.
		/// </summary>
		public CSharpBinderFlags BinderFlags { get; }

		/// <summary>
		/// Initializes a dynamic compound-assignment instruction.
		/// </summary>
		/// <param name="op">The supported dynamic assignment operation.</param>
		/// <param name="binderFlags">Binder flags carried from call-site creation.</param>
		/// <param name="target">The instruction that resolves the dynamic target.</param>
		/// <param name="targetArgumentInfo">Binder metadata for <paramref name="target"/>.</param>
		/// <param name="value">The right-hand dynamic operand.</param>
		/// <param name="valueArgumentInfo">Binder metadata for <paramref name="value"/>.</param>
		/// <param name="targetKind">How <paramref name="target"/> should be interpreted.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="op"/> is not a supported assignment operation.
		/// </exception>
		public DynamicCompoundAssign(ExpressionType op, CSharpBinderFlags binderFlags,
			ILInstruction target, CSharpArgumentInfo targetArgumentInfo,
			ILInstruction value, CSharpArgumentInfo valueArgumentInfo,
			CompoundTargetKind targetKind = CompoundTargetKind.Dynamic)
			: base(OpCode.DynamicCompoundAssign, CompoundEvalModeFromOperation(op), target, targetKind, value)
		{
			if (!IsExpressionTypeSupported(op))
				throw new ArgumentOutOfRangeException(nameof(op));
			this.BinderFlags = binderFlags;
			this.Operation = op;
			this.TargetArgumentInfo = targetArgumentInfo;
			this.ValueArgumentInfo = valueArgumentInfo;
		}

		protected override void WriteToCore(ITextOutput output, ILAstWritingOptions options)
		{
			WriteILRange(output, options);
			output.Write(OpCode);
			output.Write("." + Operation.ToString().ToLower());
			DynamicInstruction.WriteBinderFlags(BinderFlags, output, options);
			base.WriteSuffix(output);
			output.Write(' ');
			DynamicInstruction.WriteArgumentList(output, options, (Target, TargetArgumentInfo), (Value, ValueArgumentInfo));
		}

		/// <summary>
		/// Determines whether an expression-tree operation can be represented by <see cref="DynamicCompoundAssign"/>.
		/// </summary>
		/// <param name="type">The operation kind to test.</param>
		/// <returns>
		/// <see langword="true"/> for supported dynamic assignment and increment/decrement operations;
		/// otherwise <see langword="false"/>.
		/// </returns>
		internal static bool IsExpressionTypeSupported(ExpressionType type)
		{
			return type == ExpressionType.AddAssign
				|| type == ExpressionType.AddAssignChecked
				|| type == ExpressionType.AndAssign
				|| type == ExpressionType.DivideAssign
				|| type == ExpressionType.ExclusiveOrAssign
				|| type == ExpressionType.LeftShiftAssign
				|| type == ExpressionType.ModuloAssign
				|| type == ExpressionType.MultiplyAssign
				|| type == ExpressionType.MultiplyAssignChecked
				|| type == ExpressionType.OrAssign
				|| type == ExpressionType.PostDecrementAssign
				|| type == ExpressionType.PostIncrementAssign
				|| type == ExpressionType.PreDecrementAssign
				|| type == ExpressionType.PreIncrementAssign
				|| type == ExpressionType.RightShiftAssign
				|| type == ExpressionType.SubtractAssign
				|| type == ExpressionType.SubtractAssignChecked;
		}

		static CompoundEvalMode CompoundEvalModeFromOperation(ExpressionType op)
		{
			switch (op)
			{
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PostDecrementAssign:
					return CompoundEvalMode.EvaluatesToOldValue;
				default:
					return CompoundEvalMode.EvaluatesToNewValue;
			}
		}
	}
}

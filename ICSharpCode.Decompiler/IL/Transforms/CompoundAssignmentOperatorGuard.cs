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
using System.Linq;
using System.Runtime.CompilerServices;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Decides whether a compound assignment ("x op= y", "x++", "x--") may be introduced
	/// for a store to a given target type.
	/// C# 14 lets a type declare instance compound-assignment operators (and extension
	/// blocks declare extension ones), and "x op= y" binds to those before falling back
	/// to the expansion "x = x op y". The shorthand is therefore only introduced when no
	/// such operator could take over. Mere existence blocks the conversion, without an
	/// applicability analysis and regardless of the current checked context: keeping the
	/// expanded form is always still correct.
	/// </summary>
	static class CompoundAssignmentOperatorGuard
	{
		/// <summary>
		/// Returns true when the compound form may be introduced for a store to
		/// <paramref name="targetType"/>. <paramref name="operatorMethodName"/> is the
		/// metadata name of the operator being folded (e.g. "op_Addition",
		/// "op_CheckedAddition", "op_Increment"); unrecognized names fail closed.
		/// </summary>
		public static bool MayIntroduceCompoundAssignment(ICompilation compilation, IType targetType, string operatorMethodName)
		{
			var (name, alternativeName) = GetCompoundAssignmentOperatorNames(operatorMethodName);
			if (name == null)
				return false;
			IType type = NullableType.GetUnderlyingType(targetType);
			if (type.Kind == TypeKind.Unknown)
				return false;
			if (!IsFixedLanguageType(type)
				&& type.GetMethods(m => m.Name == name || m.Name == alternativeName).Any())
			{
				return false;
			}
			return !ExtensionCompoundOperatorIndex.Get(compilation).CouldBind(type, name, alternativeName);
		}

		/// <summary>
		/// Maps a binary numeric IL operator to the operator metadata name accepted by
		/// <see cref="MayIntroduceCompoundAssignment"/>, or null for operators that have
		/// no compound form.
		/// </summary>
		public static string? GetOperatorMethodName(BinaryNumericOperator op)
		{
			switch (op)
			{
				case BinaryNumericOperator.Add:
					return "op_Addition";
				case BinaryNumericOperator.Sub:
					return "op_Subtraction";
				case BinaryNumericOperator.Mul:
					return "op_Multiply";
				case BinaryNumericOperator.Div:
					return "op_Division";
				case BinaryNumericOperator.Rem:
					return "op_Modulus";
				case BinaryNumericOperator.BitAnd:
					return "op_BitwiseAnd";
				case BinaryNumericOperator.BitOr:
					return "op_BitwiseOr";
				case BinaryNumericOperator.BitXor:
					return "op_ExclusiveOr";
				case BinaryNumericOperator.ShiftLeft:
					return "op_LeftShift";
				case BinaryNumericOperator.ShiftRight:
					return "op_RightShift";
				default:
					return null;
			}
		}

		/// <summary>
		/// Maps an operator's metadata name to the C# 14 compound-assignment operator
		/// names whose presence must block the compound form. The second name carries
		/// the checked variant where one exists; for "op_RightShift" it instead carries
		/// the unsigned variant, so that a shift whose signedness is not modeled here
		/// still blocks on either.
		/// </summary>
		static (string? Name, string? AlternativeName) GetCompoundAssignmentOperatorNames(string operatorMethodName)
		{
			switch (operatorMethodName)
			{
				case "op_Addition":
				case "op_CheckedAddition":
					return ("op_AdditionAssignment", "op_CheckedAdditionAssignment");
				case "op_Subtraction":
				case "op_CheckedSubtraction":
					return ("op_SubtractionAssignment", "op_CheckedSubtractionAssignment");
				case "op_Multiply":
				case "op_CheckedMultiply":
					return ("op_MultiplicationAssignment", "op_CheckedMultiplicationAssignment");
				case "op_Division":
				case "op_CheckedDivision":
					return ("op_DivisionAssignment", "op_CheckedDivisionAssignment");
				case "op_Modulus":
					return ("op_ModulusAssignment", null);
				case "op_BitwiseAnd":
					return ("op_BitwiseAndAssignment", null);
				case "op_BitwiseOr":
					return ("op_BitwiseOrAssignment", null);
				case "op_ExclusiveOr":
					return ("op_ExclusiveOrAssignment", null);
				case "op_LeftShift":
					return ("op_LeftShiftAssignment", null);
				case "op_RightShift":
					return ("op_RightShiftAssignment", "op_UnsignedRightShiftAssignment");
				case "op_UnsignedRightShift":
					return ("op_UnsignedRightShiftAssignment", null);
				case "op_Increment":
				case "op_CheckedIncrement":
					return ("op_IncrementAssignment", "op_CheckedIncrementAssignment");
				case "op_Decrement":
				case "op_CheckedDecrement":
					return ("op_DecrementAssignment", "op_CheckedDecrementAssignment");
				default:
					return (null, null);
			}
		}

		static bool IsFixedLanguageType(IType type)
		{
			// Sealed core-library types whose operator meaning the language itself defines
			// cannot acquire instance compound-assignment operators; skipping the member
			// walk keeps the common "i += 1" path cheap.
			return type.IsCSharpPrimitiveIntegerType()
				|| type.IsKnownType(KnownTypeCode.Boolean)
				|| type.IsKnownType(KnownTypeCode.Char)
				|| type.IsKnownType(KnownTypeCode.Single)
				|| type.IsKnownType(KnownTypeCode.Double)
				|| type.IsKnownType(KnownTypeCode.Decimal)
				|| type.IsKnownType(KnownTypeCode.String);
		}

		sealed class ExtensionCompoundOperatorIndex
		{
			static readonly ConditionalWeakTable<ICompilation, ExtensionCompoundOperatorIndex> perCompilation = new();
			static readonly ConditionalWeakTable<MetadataFile, StrongBox<bool>> extensionShapedNames = new();

			readonly List<(IType Receiver, string Name)> operators = [];

			public static ExtensionCompoundOperatorIndex Get(ICompilation compilation)
			{
				return perCompilation.GetValue(compilation, static c => new ExtensionCompoundOperatorIndex(c));
			}

			ExtensionCompoundOperatorIndex(ICompilation compilation)
			{
				// Extension blocks can declare compound-assignment operators, and those bind
				// through extension member lookup, whose using scope is not yet known while
				// the transforms run. Index every candidate in the compilation and treat any
				// possible receiver match as blocking. The raw-name prescan keeps the cost
				// near zero for modules without extension blocks.
				foreach (var module in compilation.Modules)
				{
					if (module is not MetadataModule metadataModule)
						continue;
					if (!HasExtensionShapedTypeNames(metadataModule.MetadataFile))
						continue;
					foreach (var td in metadataModule.TopLevelTypeDefinitions)
					{
						var info = td.ExtensionInfo;
						if (info == null)
							continue;
						foreach (var (marker, _) in info.ExtensionGroups)
						{
							IType receiver = marker.Parameters[0].Type;
							foreach (var member in info.GetMembersOfGroup(marker))
							{
								string n = member.Name;
								if (n.StartsWith("op_", StringComparison.Ordinal) && n.EndsWith("Assignment", StringComparison.Ordinal))
									operators.Add((receiver, n));
							}
						}
					}
				}
			}

			static bool HasExtensionShapedTypeNames(MetadataFile file)
			{
				var box = extensionShapedNames.GetValue(file, static f => {
					var metadata = f.Metadata;
					bool found = false;
					foreach (var handle in metadata.TypeDefinitions)
					{
						var td = metadata.GetTypeDefinition(handle);
						if (metadata.StringComparer.StartsWith(td.Name, "<>E__")
							|| metadata.StringComparer.StartsWith(td.Name, "<G>$"))
						{
							found = true;
							break;
						}
					}
					return new StrongBox<bool>(found);
				});
				return box.Value;
			}

			public bool CouldBind(IType targetType, string name, string? alternativeName)
			{
				if (operators.Count == 0)
					return false;
				foreach (var (receiver, opName) in operators)
				{
					if (opName != name && opName != alternativeName)
						continue;
					if (ReceiverCouldMatch(receiver, targetType))
						return true;
				}
				return false;
			}

			static bool ReceiverCouldMatch(IType receiver, IType targetType)
			{
				// A value-type extension receiver is declared by-ref; compare its element type.
				if (receiver is ByReferenceType byReference)
					receiver = byReference.ElementType;
				var receiverDefinition = receiver.GetDefinition();
				if (receiverDefinition == null)
				{
					// Type parameters, arrays, pointers: nothing to compare, assume it could bind.
					return true;
				}
				foreach (var baseType in targetType.GetAllBaseTypes())
				{
					if (receiverDefinition.Equals(baseType.GetDefinition()))
						return true;
				}
				return false;
			}
		}
	}
}

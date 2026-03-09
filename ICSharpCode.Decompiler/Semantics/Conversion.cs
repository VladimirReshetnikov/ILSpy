// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Immutable;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents the resolver's classification for a conversion between two expressions or types.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Instances are produced by <see cref="CSharp.Resolver.CSharpConversions"/> and then consumed by overload resolution,
	/// resolve results, and code generation/decompilation rewrites. The object carries conversion category flags rather than
	/// executable conversion logic.
	/// </para>
	/// <para>
	/// Conversion instances are immutable and can be compared using <see cref="Equals(Conversion)"/>. Some categories are
	/// represented as shared singleton instances (for example <see cref="IdentityConversion"/>), while category variants that
	/// carry additional metadata (for example user-defined operators and tuple element conversions) are allocated per classification.
	/// </para>
	/// </remarks>
	public abstract class Conversion : IEquatable<Conversion>
	{
		#region Conversion factory methods
		/// <summary>
		/// Represents the absence of a valid conversion.
		/// </summary>
		/// <remarks>
		/// This value is returned by conversion classification APIs when no language-defined conversion exists between
		/// the source and target. Downstream consumers typically interpret it as an error unless a caller-specific fallback
		/// (such as unknown-type tolerance) applies.
		/// </remarks>
		public static readonly Conversion None = new InvalidConversion();

		/// <summary>
		/// Identity conversion.
		/// </summary>
		public static readonly Conversion IdentityConversion = new BuiltinConversion(true, 0);

		/// <summary>
		/// Represents an implicit numeric conversion.
		/// </summary>
		public static readonly Conversion ImplicitNumericConversion = new NumericOrEnumerationConversion(true, false);

		/// <summary>
		/// Represents an explicit numeric conversion.
		/// </summary>
		public static readonly Conversion ExplicitNumericConversion = new NumericOrEnumerationConversion(false, false);

		/// <summary>
		/// Represents an implicit lifted numeric conversion for nullable operands.
		/// </summary>
		public static readonly Conversion ImplicitLiftedNumericConversion = new NumericOrEnumerationConversion(true, true);

		/// <summary>
		/// Represents an explicit lifted numeric conversion for nullable operands.
		/// </summary>
		public static readonly Conversion ExplicitLiftedNumericConversion = new NumericOrEnumerationConversion(false, true);

		/// <summary>
		/// Creates an enumeration conversion classification.
		/// </summary>
		/// <param name="isImplicit"><see langword="true"/> for implicit enum conversions; <see langword="false"/> for explicit conversions.</param>
		/// <param name="isLifted"><see langword="true"/> when the conversion is lifted to nullable enum operands.</param>
		/// <returns>A conversion object with <see cref="IsEnumerationConversion"/> set to <see langword="true"/>.</returns>
		public static Conversion EnumerationConversion(bool isImplicit, bool isLifted)
		{
			return new NumericOrEnumerationConversion(isImplicit, isLifted, true);
		}

		/// <summary>
		/// Represents conversion of the <see langword="null"/> literal to a compatible reference or nullable target type.
		/// </summary>
		public static readonly Conversion NullLiteralConversion = new BuiltinConversion(true, 1);

		/// <summary>
		/// The numeric conversion of a constant expression.
		/// </summary>
		public static readonly Conversion ImplicitConstantExpressionConversion = new BuiltinConversion(true, 2);

		/// <summary>
		/// Represents implicit or explicit reference conversions.
		/// </summary>
		public static readonly Conversion ImplicitReferenceConversion = new BuiltinConversion(true, 3);

		/// <summary>
		/// Represents explicit reference conversions.
		/// </summary>
		public static readonly Conversion ExplicitReferenceConversion = new BuiltinConversion(false, 3);

		/// <summary>
		/// Represents implicit dynamic conversions.
		/// </summary>
		public static readonly Conversion ImplicitDynamicConversion = new BuiltinConversion(true, 4);

		/// <summary>
		/// Represents explicit dynamic conversions.
		/// </summary>
		public static readonly Conversion ExplicitDynamicConversion = new BuiltinConversion(false, 4);

		/// <summary>
		/// Represents implicit nullable conversions between <c>T</c> and <c>T?</c> forms.
		/// </summary>
		public static readonly Conversion ImplicitNullableConversion = new BuiltinConversion(true, 5);

		/// <summary>
		/// Represents explicit nullable conversions between <c>T</c> and <c>T?</c> forms.
		/// </summary>
		public static readonly Conversion ExplicitNullableConversion = new BuiltinConversion(false, 5);

		/// <summary>
		/// Represents implicit pointer conversions.
		/// </summary>
		public static readonly Conversion ImplicitPointerConversion = new BuiltinConversion(true, 6);

		/// <summary>
		/// Represents explicit pointer conversions.
		/// </summary>
		public static readonly Conversion ExplicitPointerConversion = new BuiltinConversion(false, 6);

		public static readonly Conversion BoxingConversion = new BuiltinConversion(true, 7);
		public static readonly Conversion UnboxingConversion = new BuiltinConversion(false, 8);

		/// <summary>
		/// C# 'as' cast.
		/// </summary>
		public static readonly Conversion TryCast = new BuiltinConversion(false, 9);

		/// <summary>
		/// C# 6 string interpolation expression implicitly being converted to <see cref="System.IFormattable"/> or <see cref="System.FormattableString"/>.
		/// </summary>
		public static readonly Conversion ImplicitInterpolatedStringConversion = new BuiltinConversion(true, 10);

		/// <summary>
		/// C# 7 throw expression being converted to an arbitrary type.
		/// </summary>
		public static readonly Conversion ThrowExpressionConversion = new BuiltinConversion(true, 11);

		/// <summary>
		/// C# 12 inline array implicitly being converted to <see cref="System.Span{T}"/> or <see cref="System.ReadOnlySpan{T}"/>.
		/// </summary>
		public static readonly Conversion InlineArrayConversion = new BuiltinConversion(true, 12);

		/// <summary>
		/// C# 14 implicit span conversion from an array type to <see cref="System.Span{T}"/> or <see cref="System.ReadOnlySpan{T}"/>.
		/// </summary>
		public static readonly Conversion ImplicitSpanConversion = new BuiltinConversion(true, 13);

		/// <summary>
		/// Creates a conversion classification for user-defined <c>op_Implicit</c> or <c>op_Explicit</c> operators.
		/// </summary>
		/// <param name="operatorMethod">The operator method selected by overload resolution.</param>
		/// <param name="isImplicit"><see langword="true"/> if the selected operator is considered implicit in the calling context.</param>
		/// <param name="conversionBeforeUserDefinedOperator">Conversion applied from the original source type to the operator input type.</param>
		/// <param name="conversionAfterUserDefinedOperator">Conversion applied from the operator result type to the requested target type.</param>
		/// <param name="isLifted"><see langword="true"/> if the selected operator is lifted for nullable operands.</param>
		/// <param name="isAmbiguous"><see langword="true"/> when multiple candidates remain and the conversion is intentionally marked invalid.</param>
		/// <returns>A conversion that reports <see cref="IsUserDefined"/> as <see langword="true"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="operatorMethod"/> is <see langword="null"/>.</exception>
		public static Conversion UserDefinedConversion(IMethod operatorMethod, bool isImplicit, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted = false, bool isAmbiguous = false)
		{
			if (operatorMethod == null)
				throw new ArgumentNullException(nameof(operatorMethod));
			return new UserDefinedConv(isImplicit, operatorMethod, conversionBeforeUserDefinedOperator, conversionAfterUserDefinedOperator, isLifted, isAmbiguous);
		}

		/// <summary>
		/// Creates a valid method-group conversion to a delegate target.
		/// </summary>
		/// <param name="chosenMethod">Method selected from the method group.</param>
		/// <param name="isVirtualMethodLookup">Whether the eventual delegate invocation performs virtual dispatch lookup.</param>
		/// <param name="delegateCapturesFirstArgument">Whether the conversion binds the first argument as the delegate instance target.</param>
		/// <returns>A valid conversion with <see cref="IsMethodGroupConversion"/> set to <see langword="true"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="chosenMethod"/> is <see langword="null"/>.</exception>
		public static Conversion MethodGroupConversion(IMethod chosenMethod, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument)
		{
			if (chosenMethod == null)
				throw new ArgumentNullException(nameof(chosenMethod));
			return new MethodGroupConv(chosenMethod, isVirtualMethodLookup, delegateCapturesFirstArgument, isValid: true);
		}

		/// <summary>
		/// Creates an invalid method-group conversion placeholder.
		/// </summary>
		/// <param name="chosenMethod">Method that was tentatively selected from the group.</param>
		/// <param name="isVirtualMethodLookup">Whether virtual lookup would have been required for invocation.</param>
		/// <param name="delegateCapturesFirstArgument">Whether the first argument would have been captured by the delegate target.</param>
		/// <returns>A conversion with <see cref="IsMethodGroupConversion"/> set to <see langword="true"/> and <see cref="IsValid"/> set to <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="chosenMethod"/> is <see langword="null"/>.</exception>
		public static Conversion InvalidMethodGroupConversion(IMethod chosenMethod, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument)
		{
			if (chosenMethod == null)
				throw new ArgumentNullException(nameof(chosenMethod));
			return new MethodGroupConv(chosenMethod, isVirtualMethodLookup, delegateCapturesFirstArgument, isValid: false);
		}

		/// <summary>
		/// Creates a tuple conversion whose element-wise conversions are already classified.
		/// </summary>
		/// <param name="conversions">Per-element conversions in tuple element order.</param>
		/// <returns>A tuple conversion descriptor.</returns>
		/// <remarks>
		/// The resulting conversion is implicit only when every element conversion is implicit.
		/// </remarks>
		public static Conversion TupleConversion(ImmutableArray<Conversion> conversions)
		{
			return new TupleConv(conversions);
		}
		#endregion

		#region Inner classes
		sealed class InvalidConversion : Conversion
		{
			public override bool IsValid {
				get { return false; }
			}

			public override string ToString()
			{
				return "None";
			}
		}

		sealed class NumericOrEnumerationConversion : Conversion
		{
			readonly bool isImplicit;
			readonly bool isLifted;
			readonly bool isEnumeration;

			public NumericOrEnumerationConversion(bool isImplicit, bool isLifted, bool isEnumeration = false)
			{
				this.isImplicit = isImplicit;
				this.isLifted = isLifted;
				this.isEnumeration = isEnumeration;
			}

			public override bool IsImplicit {
				get { return isImplicit; }
			}

			public override bool IsExplicit {
				get { return !isImplicit; }
			}

			public override bool IsNumericConversion {
				get { return !isEnumeration; }
			}

			public override bool IsEnumerationConversion {
				get { return isEnumeration; }
			}

			public override bool IsLifted {
				get { return isLifted; }
			}

			public override string ToString()
			{
				return (isImplicit ? "implicit" : "explicit")
					+ (isLifted ? " lifted" : "")
					+ (isEnumeration ? " enumeration" : " numeric")
					+ " conversion";
			}

			public override bool Equals(Conversion other)
			{
				NumericOrEnumerationConversion o = other as NumericOrEnumerationConversion;
				return o != null && isImplicit == o.isImplicit && isLifted == o.isLifted && isEnumeration == o.isEnumeration;
			}

			public override int GetHashCode()
			{
				return (isImplicit ? 1 : 0) + (isLifted ? 2 : 0) + (isEnumeration ? 4 : 0);
			}
		}

		sealed class BuiltinConversion : Conversion
		{
			readonly bool isImplicit;
			readonly byte type;

			public BuiltinConversion(bool isImplicit, byte type)
			{
				this.isImplicit = isImplicit;
				this.type = type;
			}

			public override bool IsImplicit {
				get { return isImplicit; }
			}

			public override bool IsExplicit {
				get { return !isImplicit; }
			}

			public override bool IsIdentityConversion {
				get { return type == 0; }
			}

			public override bool IsNullLiteralConversion {
				get { return type == 1; }
			}

			public override bool IsConstantExpressionConversion {
				get { return type == 2; }
			}

			public override bool IsReferenceConversion {
				get { return type == 3; }
			}

			public override bool IsDynamicConversion {
				get { return type == 4; }
			}

			public override bool IsNullableConversion {
				get { return type == 5; }
			}

			public override bool IsPointerConversion {
				get { return type == 6; }
			}

			public override bool IsBoxingConversion {
				get { return type == 7; }
			}

			public override bool IsUnboxingConversion {
				get { return type == 8; }
			}

			public override bool IsTryCast {
				get { return type == 9; }
			}

			public override bool IsInterpolatedStringConversion => type == 10;

			public override bool IsThrowExpressionConversion {
				get { return type == 11; }
			}

			public override bool IsInlineArrayConversion => type == 12;
			public override bool IsImplicitSpanConversion => type == 13;

			public override string ToString()
			{
				string name = null;
				switch (type)
				{
					case 0:
						return "identity conversion";
					case 1:
						return "null-literal conversion";
					case 2:
						name = "constant-expression";
						break;
					case 3:
						name = "reference";
						break;
					case 4:
						name = "dynamic";
						break;
					case 5:
						name = "nullable";
						break;
					case 6:
						name = "pointer";
						break;
					case 7:
						return "boxing conversion";
					case 8:
						return "unboxing conversion";
					case 9:
						return "try cast";
					case 10:
						return "interpolated string";
					case 11:
						return "throw-expression conversion";
					case 12:
						return "inline array conversion";
					case 13:
						return "implicit span conversion";
				}
				return (isImplicit ? "implicit " : "explicit ") + name + " conversion";
			}
		}

		sealed class UserDefinedConv : Conversion
		{
			readonly IMethod method;
			readonly bool isLifted;
			readonly Conversion conversionBeforeUserDefinedOperator;
			readonly Conversion conversionAfterUserDefinedOperator;
			readonly bool isImplicit;
			readonly bool isValid;

			public UserDefinedConv(bool isImplicit, IMethod method, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted, bool isAmbiguous)
			{
				this.method = method;
				this.isLifted = isLifted;
				this.conversionBeforeUserDefinedOperator = conversionBeforeUserDefinedOperator;
				this.conversionAfterUserDefinedOperator = conversionAfterUserDefinedOperator;
				this.isImplicit = isImplicit;
				this.isValid = !isAmbiguous;
			}

			public override bool IsValid {
				get { return isValid; }
			}

			public override bool IsImplicit {
				get { return isImplicit; }
			}

			public override bool IsExplicit {
				get { return !isImplicit; }
			}

			public override bool IsLifted {
				get { return isLifted; }
			}

			public override bool IsUserDefined {
				get { return true; }
			}

			public override Conversion ConversionBeforeUserDefinedOperator {
				get { return conversionBeforeUserDefinedOperator; }
			}

			public override Conversion ConversionAfterUserDefinedOperator {
				get { return conversionAfterUserDefinedOperator; }
			}

			public override IMethod Method {
				get { return method; }
			}

			public override bool Equals(Conversion other)
			{
				UserDefinedConv o = other as UserDefinedConv;
				return o != null && isLifted == o.isLifted && isImplicit == o.isImplicit && isValid == o.isValid && method.Equals(o.method);
			}

			public override int GetHashCode()
			{
				return unchecked(method.GetHashCode() + (isLifted ? 31 : 27) + (isImplicit ? 71 : 61) + (isValid ? 107 : 109));
			}

			public override string ToString()
			{
				return (isImplicit ? "implicit" : "explicit")
					+ (isLifted ? " lifted" : "")
					+ (isValid ? "" : " ambiguous")
					+ "user-defined conversion (" + method + ")";
			}
		}

		sealed class MethodGroupConv : Conversion
		{
			readonly IMethod method;
			readonly bool isVirtualMethodLookup;
			readonly bool delegateCapturesFirstArgument;
			readonly bool isValid;

			public MethodGroupConv(IMethod method, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument, bool isValid)
			{
				this.method = method;
				this.isVirtualMethodLookup = isVirtualMethodLookup;
				this.delegateCapturesFirstArgument = delegateCapturesFirstArgument;
				this.isValid = isValid;
			}

			public override bool IsValid {
				get { return isValid; }
			}

			public override bool IsImplicit {
				get { return true; }
			}

			public override bool IsMethodGroupConversion {
				get { return true; }
			}

			public override bool IsVirtualMethodLookup {
				get { return isVirtualMethodLookup; }
			}

			public override bool DelegateCapturesFirstArgument {
				get { return delegateCapturesFirstArgument; }
			}

			public override IMethod Method {
				get { return method; }
			}

			public override bool Equals(Conversion other)
			{
				MethodGroupConv o = other as MethodGroupConv;
				return o != null && method.Equals(o.method);
			}

			public override int GetHashCode()
			{
				return method.GetHashCode();
			}
		}

		sealed class TupleConv : Conversion
		{
			public override bool IsImplicit { get; }
			public override bool IsExplicit => !IsImplicit;
			public override ImmutableArray<Conversion> ElementConversions { get; }
			public override bool IsTupleConversion => true;

			public TupleConv(ImmutableArray<Conversion> elementConversions)
			{
				this.ElementConversions = elementConversions;
				this.IsImplicit = elementConversions.All(c => c.IsImplicit);
			}

			public override bool Equals(Conversion other)
			{
				return other is TupleConv o
					&& ElementConversions.SequenceEqual(o.ElementConversions);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 0;
					foreach (var conv in ElementConversions)
					{
						hash *= 31;
						hash += conv.GetHashCode();
					}
					return hash;
				}
			}

			public override string ToString()
			{
				return (IsImplicit ? "implicit " : "explicit ") + " tuple conversion";
			}
		}
		#endregion

		/// <summary>
		/// Gets whether the conversion is valid.
		/// </summary>
		public virtual bool IsValid {
			get { return true; }
		}

		/// <summary>
		/// Gets whether the conversion is implicit in the current classification context.
		/// </summary>
		public virtual bool IsImplicit {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is explicit in the current classification context.
		/// </summary>
		public virtual bool IsExplicit {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is an '<c>as</c>' cast.
		/// </summary>
		public virtual bool IsTryCast {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion classifies conversion of a <see langword="throw"/> expression.
		/// </summary>
		public virtual bool IsThrowExpressionConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is an identity conversion.
		/// </summary>
		public virtual bool IsIdentityConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion classifies <see langword="null"/> literal conversion.
		/// </summary>
		public virtual bool IsNullLiteralConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is the constant-expression numeric conversion category.
		/// </summary>
		public virtual bool IsConstantExpressionConversion {
			get { return false; }
		}

		public virtual bool IsNumericConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is a lifted version of another conversion.
		/// </summary>
		public virtual bool IsLifted {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is dynamic.
		/// </summary>
		public virtual bool IsDynamicConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is a reference conversion.
		/// </summary>
		public virtual bool IsReferenceConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is an enumeration conversion.
		/// </summary>
		public virtual bool IsEnumerationConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether the conversion is a nullable conversion
		/// (conversion between a nullable type and the regular type).
		/// </summary>
		public virtual bool IsNullableConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is user-defined (op_Implicit or op_Explicit).
		/// </summary>
		public virtual bool IsUserDefined {
			get { return false; }
		}

		/// <summary>
		/// The conversion that is applied to the input before the user-defined conversion operator is invoked.
		/// </summary>
		public virtual Conversion ConversionBeforeUserDefinedOperator {
			get { return null; }
		}

		/// <summary>
		/// The conversion that is applied to the result of the user-defined conversion operator.
		/// </summary>
		public virtual Conversion ConversionAfterUserDefinedOperator {
			get { return null; }
		}

		/// <summary>
		/// Gets whether this conversion is a boxing conversion.
		/// </summary>
		public virtual bool IsBoxingConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is an unboxing conversion.
		/// </summary>
		public virtual bool IsUnboxingConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is a pointer conversion.
		/// </summary>
		public virtual bool IsPointerConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is a method group conversion.
		/// </summary>
		public virtual bool IsMethodGroupConversion {
			get { return false; }
		}

		/// <summary>
		/// For method-group conversions, gets whether to perform a virtual method lookup at runtime.
		/// </summary>
		public virtual bool IsVirtualMethodLookup {
			get { return false; }
		}

		/// <summary>
		/// For method-group conversions, gets whether the conversion captures the first argument.
		/// 
		/// For instance methods, this property always returns true for C# method-group conversions.
		/// For static methods, this property returns true for method-group conversions of an extension method performed on an instance (eg. <c>Func&lt;int&gt; f = myEnumerable.Single</c>).
		/// </summary>
		public virtual bool DelegateCapturesFirstArgument {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is an anonymous function conversion.
		/// </summary>
		public virtual bool IsAnonymousFunctionConversion {
			get { return false; }
		}

		/// <summary>
		/// Gets the method associated with this conversion.
		/// For user-defined conversions, this is the method being called.
		/// For method-group conversions, this is the method that was chosen from the group.
		/// </summary>
		/// <remarks>
		/// Returns <see langword="null"/> for built-in conversions that do not originate from method symbols.
		/// </remarks>
		public virtual IMethod Method {
			get { return null; }
		}

		/// <summary>
		/// Gets whether this conversion is a tuple conversion.
		/// </summary>
		public virtual bool IsTupleConversion => false;

		/// <summary>
		/// Gets whether this is an interpolated string conversion to <see cref="IFormattable" /> or <see cref="FormattableString"/>.
		/// </summary>
		public virtual bool IsInterpolatedStringConversion => false;

		/// <summary>
		/// Gets whether this is an inline array conversion to <see cref="System.Span{T}"/> or <see cref="System.ReadOnlySpan{T}"/>.
		/// </summary>
		public virtual bool IsInlineArrayConversion => false;

		/// <summary>
		/// Gets whether this is an implicit span conversion from an array type to <see cref="System.Span{T}"/> or <see cref="System.ReadOnlySpan{T}"/>.
		/// </summary>
		public virtual bool IsImplicitSpanConversion => false;

		/// <summary>
		/// For a tuple conversion, gets the individual tuple element conversions.
		/// </summary>
		public virtual ImmutableArray<Conversion> ElementConversions => default(ImmutableArray<Conversion>);

		public override sealed bool Equals(object obj)
		{
			return Equals(obj as Conversion);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Determines whether this conversion instance is equal to another conversion instance.
		/// </summary>
		/// <param name="other">The conversion to compare with the current instance.</param>
		/// <returns><see langword="true"/> when both instances classify the same conversion category and payload; otherwise <see langword="false"/>.</returns>
		public virtual bool Equals(Conversion other)
		{
			return this == other;
		}
	}
}

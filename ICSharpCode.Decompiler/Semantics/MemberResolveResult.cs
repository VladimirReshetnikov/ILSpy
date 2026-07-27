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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents the semantic result of accessing a member on an optional target expression.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This result shape is used for field, property, event, and constructor member semantics. Method-like calls are represented by
	/// <see cref="InvocationResolveResult"/>, which extends this type with argument and initializer data.
	/// </para>
	/// <para>
	/// The stored <see cref="ResolveResult.Type"/> can differ from <see cref="IMember.ReturnType"/> in two important cases: constructors resolve to the
	/// declaring type, and by-reference returns are normalized to the element type so that downstream consumers can reason about expression
	/// value types without manually unwrapping <see cref="ByReferenceType"/>.
	/// </para>
	/// </remarks>
	public class MemberResolveResult : ResolveResult
	{
		readonly IMember member;
		readonly bool isConstant;
		readonly object constantValue;
		readonly ResolveResult targetResult;
		readonly bool isVirtualCall;

		/// <summary>
		/// Initializes a member resolve result and infers virtual-call semantics from the target.
		/// </summary>
		/// <param name="targetResult">
		/// The resolved target expression, or <see langword="null"/> for static access.
		/// </param>
		/// <param name="member">The resolved member metadata.</param>
		/// <param name="returnTypeOverride">
		/// An optional type to expose from <see cref="ResolveResult.Type"/> instead of the computed member type.
		/// </param>
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnTypeOverride = null)
			: base(returnTypeOverride ?? ComputeType(member))
		{
			this.targetResult = targetResult;
			this.member = member;
			var thisRR = targetResult as ThisResolveResult;
			this.isVirtualCall = member.IsOverridable && !(thisRR != null && thisRR.CausesNonVirtualInvocation);

			IField field = member as IField;
			if (field != null)
			{
				isConstant = field.IsConst;
				if (isConstant)
					constantValue = field.GetConstantValue();
			}
		}

		/// <summary>
		/// Initializes a member resolve result with an explicit virtual dispatch flag.
		/// </summary>
		/// <param name="targetResult">The resolved target expression, or <see langword="null"/> for static access.</param>
		/// <param name="member">The resolved member metadata.</param>
		/// <param name="isVirtualCall">
		/// <see langword="true"/> when invocation semantics should be treated as virtual dispatch; otherwise <see langword="false"/>.
		/// </param>
		/// <param name="returnTypeOverride">
		/// An optional type to expose from <see cref="ResolveResult.Type"/> instead of the computed member type.
		/// </param>
		public MemberResolveResult(ResolveResult targetResult, IMember member, bool isVirtualCall, IType returnTypeOverride = null)
			: base(returnTypeOverride ?? ComputeType(member))
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isVirtualCall = isVirtualCall;
			IField field = member as IField;
			if (field != null)
			{
				isConstant = field.IsConst;
				if (isConstant)
					constantValue = field.GetConstantValue();
			}
		}

		static IType ComputeType(IMember member)
		{
			switch (member.SymbolKind)
			{
				case SymbolKind.Constructor:
					return member.DeclaringType ?? SpecialType.UnknownType;
				case SymbolKind.Field:
					//if (((IField)member).IsFixed)
					//	return new PointerType(member.ReturnType);
					break;
			}
			if (member.ReturnType.Kind == TypeKind.ByReference)
				return ((ByReferenceType)member.ReturnType).ElementType;
			return member.ReturnType;
		}

		/// <summary>
		/// Initializes a member resolve result with an explicit constant classification.
		/// </summary>
		/// <param name="targetResult">The resolved target expression, or <see langword="null"/> for static access.</param>
		/// <param name="member">The resolved member metadata.</param>
		/// <param name="returnType">The type exposed from <see cref="ResolveResult.Type"/>.</param>
		/// <param name="isConstant">
		/// <see langword="true"/> when this member access should be treated as a compile-time constant; otherwise <see langword="false"/>.
		/// </param>
		/// <param name="constantValue">The constant value to expose when <paramref name="isConstant"/> is <see langword="true"/>.</param>
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType, bool isConstant, object constantValue)
			: base(returnType)
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isConstant = isConstant;
			this.constantValue = constantValue;
		}

		/// <summary>
		/// Initializes a member resolve result with explicit constant and virtual-dispatch metadata.
		/// </summary>
		/// <param name="targetResult">The resolved target expression, or <see langword="null"/> for static access.</param>
		/// <param name="member">The resolved member metadata.</param>
		/// <param name="returnType">The type exposed from <see cref="ResolveResult.Type"/>.</param>
		/// <param name="isConstant">
		/// <see langword="true"/> when this member access should be treated as a compile-time constant; otherwise <see langword="false"/>.
		/// </param>
		/// <param name="constantValue">The constant value to expose when <paramref name="isConstant"/> is <see langword="true"/>.</param>
		/// <param name="isVirtualCall">
		/// <see langword="true"/> when invocation semantics should be treated as virtual dispatch; otherwise <see langword="false"/>.
		/// </param>
		public MemberResolveResult(ResolveResult targetResult, IMember member, IType returnType, bool isConstant, object constantValue, bool isVirtualCall)
			: base(returnType)
		{
			this.targetResult = targetResult;
			this.member = member;
			this.isConstant = isConstant;
			this.constantValue = constantValue;
			this.isVirtualCall = isVirtualCall;
		}

		/// <summary>
		/// Gets the resolved target expression, or <see langword="null"/> for static member access.
		/// </summary>
		public ResolveResult TargetResult {
			get { return targetResult; }
		}

		/// <summary>
		/// Gets the referenced member metadata.
		/// </summary>
		public IMember Member {
			get { return member; }
		}

		/// <summary>
		/// Gets a value indicating whether this member access should be emitted with virtual-call semantics.
		/// </summary>
		public bool IsVirtualCall {
			get { return isVirtualCall; }
		}

		/// <inheritdoc/>
		public override bool IsCompileTimeConstant {
			get { return isConstant; }
		}

		/// <inheritdoc/>
		public override object ConstantValue {
			get { return constantValue; }
		}

		/// <inheritdoc/>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (targetResult != null)
				return new[] { targetResult };
			else
				return Enumerable.Empty<ResolveResult>();
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1}]", GetType().Name, member);
		}
	}
}

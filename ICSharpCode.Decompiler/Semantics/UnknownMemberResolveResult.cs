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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents a failed member lookup on a known target type.
	/// </summary>
	/// <remarks>
	/// This result preserves the queried member name and type arguments so error reporting and fallback transforms can continue with
	/// context instead of losing the failed lookup intent.
	/// </remarks>
	public class UnknownMemberResolveResult : ResolveResult
	{
		readonly IType targetType;
		readonly string memberName;
		readonly ReadOnlyCollection<IType> typeArguments;

		/// <summary>
		/// Initializes an unknown-member result.
		/// </summary>
		/// <param name="targetType">The type on which lookup was attempted.</param>
		/// <param name="memberName">The member identifier that could not be resolved.</param>
		/// <param name="typeArguments">Type arguments that were supplied as part of lookup.</param>
		/// <exception cref="ArgumentNullException"><paramref name="targetType"/> is <see langword="null"/>.</exception>
		public UnknownMemberResolveResult(IType targetType, string memberName, IEnumerable<IType> typeArguments)
			: base(SpecialType.UnknownType)
		{
			if (targetType == null)
				throw new ArgumentNullException(nameof(targetType));
			this.targetType = targetType;
			this.memberName = memberName;
			this.typeArguments = new ReadOnlyCollection<IType>(typeArguments.ToArray());
		}

		/// <summary>
		/// Gets the type on which lookup was performed.
		/// </summary>
		public IType TargetType {
			get { return targetType; }
		}

		/// <summary>
		/// Gets the missing member name.
		/// </summary>
		public string MemberName {
			get { return memberName; }
		}

		/// <summary>
		/// Gets supplied type arguments captured during lookup.
		/// </summary>
		public ReadOnlyCollection<IType> TypeArguments {
			get { return typeArguments; }
		}

		public override bool IsError {
			get { return true; }
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1}.{2}]", GetType().Name, targetType, memberName);
		}
	}

	/// <summary>
	/// Represents a failed method lookup and preserves argument-shape metadata.
	/// </summary>
	public class UnknownMethodResolveResult : UnknownMemberResolveResult
	{
		readonly ReadOnlyCollection<IParameter> parameters;

		/// <summary>
		/// Initializes an unknown-method result.
		/// </summary>
		/// <param name="targetType">The type on which lookup was attempted.</param>
		/// <param name="methodName">The method name that could not be resolved.</param>
		/// <param name="typeArguments">Type arguments supplied for generic method lookup.</param>
		/// <param name="parameters">The argument/parameter shape used during lookup.</param>
		public UnknownMethodResolveResult(IType targetType, string methodName, IEnumerable<IType> typeArguments, IEnumerable<IParameter> parameters)
			: base(targetType, methodName, typeArguments)
		{
			this.parameters = new ReadOnlyCollection<IParameter>(parameters.ToArray());
		}

		/// <summary>
		/// Gets the captured argument/parameter shape used for method lookup.
		/// </summary>
		public ReadOnlyCollection<IParameter> Parameters {
			get { return parameters; }
		}
	}

	/// <summary>
	/// Represents a failed lookup for an identifier that could not be bound as a type, member, or namespace.
	/// </summary>
	public class UnknownIdentifierResolveResult : ResolveResult
	{
		readonly string identifier;
		readonly int typeArgumentCount;

		/// <summary>
		/// Initializes an unknown-identifier result.
		/// </summary>
		/// <param name="identifier">The unresolved identifier token.</param>
		/// <param name="typeArgumentCount">The number of type arguments supplied in the unresolved reference.</param>
		public UnknownIdentifierResolveResult(string identifier, int typeArgumentCount = 0)
			: base(SpecialType.UnknownType)
		{
			this.identifier = identifier;
			this.typeArgumentCount = typeArgumentCount;
		}

		/// <summary>
		/// Gets the unresolved identifier token.
		/// </summary>
		public string Identifier {
			get { return identifier; }
		}

		/// <summary>
		/// Gets the unresolved type-argument count associated with <see cref="Identifier"/>.
		/// </summary>
		public int TypeArgumentCount {
			get { return typeArgumentCount; }
		}

		public override bool IsError {
			get { return true; }
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1}]", GetType().Name, identifier);
		}
	}
}

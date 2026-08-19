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
using System.Linq.Expressions;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents semantic classification for unary, binary, or ternary operator expressions.
	/// </summary>
	/// <remarks>
	/// <see cref="OperatorType"/> identifies the LINQ expression-tree operator shape, while
	/// <see cref="UserDefinedOperatorMethod"/> and <see cref="IsLiftedOperator"/> capture overload-resolution details
	/// for user-defined and nullable-lifted operators.
	/// </remarks>
	public class OperatorResolveResult : ResolveResult
	{
		readonly ExpressionType operatorType;
		readonly IMethod userDefinedOperatorMethod;
		readonly IList<ResolveResult> operands;
		readonly bool isLiftedOperator;

		/// <summary>
		/// Initializes an operator result for predefined operators.
		/// </summary>
		/// <param name="resultType">The resulting expression type.</param>
		/// <param name="operatorType">The operator classification.</param>
		/// <param name="operands">Operand expressions in evaluation order.</param>
		/// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
		public OperatorResolveResult(IType resultType, ExpressionType operatorType, params ResolveResult[] operands)
			: base(resultType)
		{
			if (operands == null)
				throw new ArgumentNullException(nameof(operands));
			this.operatorType = operatorType;
			this.operands = operands;
		}

		/// <summary>
		/// Initializes an operator result for user-defined or lifted operators.
		/// </summary>
		/// <param name="resultType">The resulting expression type.</param>
		/// <param name="operatorType">The operator classification.</param>
		/// <param name="userDefinedOperatorMethod">The resolved operator method, or <see langword="null"/> for predefined operators.</param>
		/// <param name="isLiftedOperator">Whether nullable lifting semantics were applied.</param>
		/// <param name="operands">Operand expressions in evaluation order.</param>
		/// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
		public OperatorResolveResult(IType resultType, ExpressionType operatorType, IMethod userDefinedOperatorMethod, bool isLiftedOperator, IList<ResolveResult> operands)
			: base(resultType)
		{
			if (operands == null)
				throw new ArgumentNullException(nameof(operands));
			this.operatorType = operatorType;
			this.userDefinedOperatorMethod = userDefinedOperatorMethod;
			this.isLiftedOperator = isLiftedOperator;
			this.operands = operands;
		}

		/// <summary>
		/// Gets the resolved operator classification.
		/// </summary>
		public ExpressionType OperatorType {
			get { return operatorType; }
		}

		/// <summary>
		/// Gets operands in evaluation order.
		/// </summary>
		public IList<ResolveResult> Operands {
			get { return operands; }
		}

		/// <summary>
		/// Gets the user-defined operator implementation selected by overload resolution.
		/// </summary>
		/// <value>
		/// The resolved operator method, or <see langword="null"/> when the operator is predefined.
		/// </value>
		public IMethod UserDefinedOperatorMethod {
			get { return userDefinedOperatorMethod; }
		}

		/// <summary>
		/// Gets whether this is a lifted operator.
		/// </summary>
		public bool IsLiftedOperator {
			get { return isLiftedOperator; }
		}
	}
}

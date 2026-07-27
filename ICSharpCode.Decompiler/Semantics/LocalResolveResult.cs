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
using System.Globalization;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents semantic classification for reading a local variable or parameter symbol.
	/// </summary>
	/// <remarks>
	/// For <c>ref</c>/<c>in</c>/<c>out</c> parameters, <see cref="ResolveResult.Type"/> is normalized to the
	/// element type so regular expression typing reflects value semantics.
	/// </remarks>
	public class LocalResolveResult : ResolveResult
	{
		readonly IVariable variable;

		/// <summary>
		/// Initializes a local-or-parameter resolve result.
		/// </summary>
		/// <param name="variable">The symbol resolved for the expression.</param>
		/// <exception cref="ArgumentNullException"><paramref name="variable"/> is <see langword="null"/>.</exception>
		public LocalResolveResult(IVariable variable)
			: base(UnpackTypeIfByRefParameter(variable))
		{
			this.variable = variable;
		}

		static IType UnpackTypeIfByRefParameter(IVariable variable)
		{
			if (variable == null)
				throw new ArgumentNullException(nameof(variable));
			IType type = variable.Type;
			if (type.Kind == TypeKind.ByReference)
			{
				IParameter p = variable as IParameter;
				if (p != null && p.ReferenceKind != ReferenceKind.None)
					return ((ByReferenceType)type).ElementType;
			}
			return type;
		}

		/// <summary>
		/// Gets the resolved local or parameter symbol.
		/// </summary>
		public IVariable Variable {
			get { return variable; }
		}

		/// <summary>
		/// Gets whether <see cref="Variable"/> represents a parameter instead of a local.
		/// </summary>
		public bool IsParameter {
			get { return variable is IParameter; }
		}

		/// <summary>
		/// Gets whether the resolved symbol is declared as <c>const</c>.
		/// </summary>
		public override bool IsCompileTimeConstant {
			get { return variable.IsConst; }
		}

		/// <summary>
		/// Gets the constant value for const locals; parameters always return <see langword="null"/>.
		/// </summary>
		public override object ConstantValue {
			get { return IsParameter ? null : variable.GetConstantValue(); }
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[LocalResolveResult {0}]", variable);
		}
	}
}

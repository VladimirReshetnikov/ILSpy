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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents an argument supplied with an explicit parameter name.
	/// </summary>
	/// <remarks>
	/// This wrapper preserves source-level named-argument intent so overload-resolution consumers and emitters can distinguish positional
	/// arguments from explicit name binding.
	/// </remarks>
	public class NamedArgumentResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the member to which the parameter belongs.
		/// This field is <see langword="null"/> when the target member is not yet known.
		/// </summary>
		public readonly IParameterizedMember Member;

		/// <summary>
		/// Gets the parameter.
		/// This field is <see langword="null"/> when the target member is not yet known.
		/// </summary>
		public readonly IParameter Parameter;

		/// <summary>
		/// Gets the parameter name.
		/// </summary>
		public readonly string ParameterName;

		/// <summary>
		/// Gets the argument passed to the parameter.
		/// </summary>
		public readonly ResolveResult Argument;

		/// <summary>
		/// Initializes a named argument bound to a specific parameter.
		/// </summary>
		/// <param name="parameter">The bound parameter.</param>
		/// <param name="argument">The resolved argument expression.</param>
		/// <param name="member">The resolved callable member that owns <paramref name="parameter"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="parameter"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// A <see langword="null"/> <paramref name="argument"/> raises <see cref="NullReferenceException"/>
		/// instead, because the base initializer reads <c>argument.Type</c> before the null check runs.
		/// </remarks>
		public NamedArgumentResolveResult(IParameter parameter, ResolveResult argument, IParameterizedMember member = null)
			: base(argument.Type)
		{
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter));
			if (argument == null)
				throw new ArgumentNullException(nameof(argument));
			this.Member = member;
			this.Parameter = parameter;
			this.ParameterName = parameter.Name;
			this.Argument = argument;
		}

		/// <summary>
		/// Initializes a named argument when only the source parameter name is known.
		/// </summary>
		/// <param name="parameterName">The argument name written in source.</param>
		/// <param name="argument">The resolved argument expression.</param>
		/// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// A <see langword="null"/> <paramref name="argument"/> raises <see cref="NullReferenceException"/>
		/// instead, because the base initializer reads <c>argument.Type</c> before the null check runs.
		/// </remarks>
		public NamedArgumentResolveResult(string parameterName, ResolveResult argument)
			: base(argument.Type)
		{
			if (parameterName == null)
				throw new ArgumentNullException(nameof(parameterName));
			if (argument == null)
				throw new ArgumentNullException(nameof(argument));
			this.ParameterName = parameterName;
			this.Argument = argument;
		}
	}
}

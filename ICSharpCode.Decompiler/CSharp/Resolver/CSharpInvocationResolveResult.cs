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
using System.Linq;

using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Resolver
{
	/// <summary>
	/// Represents the result of resolving a method, constructor, or indexer invocation with C#-specific overload details.
	/// </summary>
	/// <remarks>
	/// <see cref="InvocationResolveResult"/> captures the target member and argument list; this derived type additionally records
	/// overload-resolution diagnostics and argument-to-parameter mapping information required by C# transforms.
	/// </remarks>
	public class CSharpInvocationResolveResult : InvocationResolveResult
	{
		/// <summary>
		/// Gets overload-resolution diagnostics attached to this invocation.
		/// </summary>
		public readonly OverloadResolutionErrors OverloadResolutionErrors;

		/// <summary>
		/// Gets a value indicating whether this invocation uses extension-method syntax.
		/// </summary>
		public readonly bool IsExtensionMethodInvocation;

		/// <summary>
		/// Gets a value indicating whether this invocation is a delegate call written without an explicit <c>.Invoke()</c>.
		/// </summary>
		public readonly bool IsDelegateInvocation;

		/// <summary>
		/// Gets a value indicating whether a <c>params</c> parameter is consumed in expanded form.
		/// </summary>
		public readonly bool IsExpandedForm;

		readonly IReadOnlyList<int> argumentToParameterMap;

		/// <summary>
		/// Initializes a C# invocation resolve result.
		/// </summary>
		/// <param name="targetResult">The invocation target expression.</param>
		/// <param name="member">The selected callable member.</param>
		/// <param name="arguments">The arguments supplied at the call site, in source order.</param>
		/// <param name="overloadResolutionErrors">The overload-resolution state for this invocation.</param>
		/// <param name="isExtensionMethodInvocation"><see langword="true"/> when extension method syntax was used.</param>
		/// <param name="isExpandedForm"><see langword="true"/> when the final <c>params</c> parameter is expanded from individual arguments.</param>
		/// <param name="isDelegateInvocation"><see langword="true"/> when the invocation calls a delegate instance.</param>
		/// <param name="argumentToParameterMap">
		/// Optional mapping from argument indices to parameter indices. Unmapped arguments are represented by <c>-1</c>.
		/// </param>
		/// <param name="initializerStatements">Optional object/collection initializer statements associated with this invocation result.</param>
		/// <param name="returnTypeOverride">Optional return type override used by specific resolver paths.</param>
		public CSharpInvocationResolveResult(
			ResolveResult targetResult, IParameterizedMember member,
			IList<ResolveResult> arguments,
			OverloadResolutionErrors overloadResolutionErrors = OverloadResolutionErrors.None,
			bool isExtensionMethodInvocation = false,
			bool isExpandedForm = false,
			bool isDelegateInvocation = false,
			IReadOnlyList<int> argumentToParameterMap = null,
			IList<ResolveResult> initializerStatements = null,
			IType returnTypeOverride = null
		)
			: base(targetResult, member, arguments, initializerStatements, returnTypeOverride)
		{
			this.OverloadResolutionErrors = overloadResolutionErrors;
			this.IsExtensionMethodInvocation = isExtensionMethodInvocation;
			this.IsExpandedForm = isExpandedForm;
			this.IsDelegateInvocation = isDelegateInvocation;
			this.argumentToParameterMap = argumentToParameterMap;
		}

		/// <summary>
		/// Gets a value indicating whether this invocation is considered erroneous by overload resolution.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <see cref="OverloadResolutionErrors"/> is not <see cref="OverloadResolutionErrors.None"/>;
		/// otherwise, <see langword="false"/>.
		/// </value>
		public override bool IsError {
			get { return this.OverloadResolutionErrors != OverloadResolutionErrors.None; }
		}

		/// <summary>
		/// Returns the argument-to-parameter index mapping for this invocation.
		/// </summary>
		/// <returns>
		/// A read-only index map where each argument index maps to a parameter index, or <c>-1</c> when an argument could not be mapped.
		/// The method returns <see langword="null"/> when no explicit mapping was captured.
		/// </returns>
		public IReadOnlyList<int> GetArgumentToParameterMap()
		{
			return argumentToParameterMap;
		}

		/// <summary>
		/// Produces the effective argument list aligned to <see cref="InvocationResolveResult.Member"/> parameters.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This materializes C# call semantics by expanding <c>params</c> arguments into an array creation,
		/// unwrapping named arguments to positional values, and injecting constants for omitted optional parameters.
		/// </para>
		/// <para>
		/// Parameters that still cannot be satisfied are filled with <see cref="ErrorResolveResult.UnknownError"/>.
		/// </para>
		/// </remarks>
		/// <returns>
		/// A positional argument list with one entry per parameter of <see cref="InvocationResolveResult.Member"/>.
		/// </returns>
		public override IList<ResolveResult> GetArgumentsForCall()
		{
			ResolveResult[] results = new ResolveResult[Member.Parameters.Count];
			List<ResolveResult> paramsArguments = IsExpandedForm ? new List<ResolveResult>() : null;
			// map arguments to parameters:
			for (int i = 0; i < Arguments.Count; i++)
			{
				int mappedTo;
				if (argumentToParameterMap != null)
					mappedTo = argumentToParameterMap[i];
				else
					mappedTo = IsExpandedForm ? Math.Min(i, results.Length - 1) : i;

				if (mappedTo >= 0 && mappedTo < results.Length)
				{
					if (IsExpandedForm && mappedTo == results.Length - 1)
					{
						paramsArguments.Add(Arguments[i]);
					}
					else
					{
						var narr = Arguments[i] as NamedArgumentResolveResult;
						if (narr != null)
							results[mappedTo] = narr.Argument;
						else
							results[mappedTo] = Arguments[i];
					}
				}
			}
			if (IsExpandedForm)
			{
				IType arrayType = Member.Parameters.Last().Type;
				IType int32 = Member.Compilation.FindType(KnownTypeCode.Int32);
				ResolveResult[] sizeArguments = { new ConstantResolveResult(int32, paramsArguments.Count) };
				results[results.Length - 1] = new ArrayCreateResolveResult(arrayType, sizeArguments, paramsArguments);
			}

			for (int i = 0; i < results.Length; i++)
			{
				if (results[i] == null)
				{
					if (Member.Parameters[i].IsOptional)
					{
						results[i] = new ConstantResolveResult(Member.Parameters[i].Type, Member.Parameters[i].GetConstantValue());
					}
					else
					{
						results[i] = ErrorResolveResult.UnknownError;
					}
				}
			}

			return results;
		}
	}
}

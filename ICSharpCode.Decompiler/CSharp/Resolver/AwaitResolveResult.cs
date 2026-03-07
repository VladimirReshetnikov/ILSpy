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

using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Resolver
{
	/// <summary>
	/// Represents the semantic decomposition of an <c>await</c> expression.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This node records the operations required by the awaitable pattern: <c>GetAwaiter()</c>,
	/// awaiter completion probing, continuation registration, and <c>GetResult()</c> extraction.
	/// </para>
	/// <para>
	/// See the C# specification section on await expressions for the required members on the awaiter type
	/// and the observable lowering behavior.
	/// </para>
	/// </remarks>
	public class AwaitResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the resolved <c>GetAwaiter()</c> invocation.
		/// </summary>
		/// <remarks>
		/// This can be an <see cref="InvocationResolveResult"/> for statically bound awaitables or a
		/// <see cref="DynamicInvocationResolveResult"/> when awaiting a dynamic expression.
		/// </remarks>
		public readonly ResolveResult GetAwaiterInvocation;

		/// <summary>
		/// Gets the resolved awaiter type.
		/// </summary>
		/// <value>
		/// This field is never <see langword="null"/>. It may be <see cref="SpecialType.UnknownType"/> when lookup is incomplete,
		/// or <see cref="SpecialType.Dynamic"/> for dynamic await.
		/// </value>
		public readonly IType AwaiterType;

		/// <summary>
		/// Gets the awaiter <c>IsCompleted</c> property resolved for this await expression.
		/// </summary>
		/// <value>
		/// <see langword="null"/> when the property cannot be resolved, does not satisfy required shape,
		/// or when the await operation is dynamic.
		/// </value>
		public readonly IProperty IsCompletedProperty;

		/// <summary>
		/// Gets the awaiter continuation-registration method.
		/// </summary>
		/// <value>
		/// <see langword="null"/> when no matching continuation method is resolved or when the await operation is dynamic.
		/// The resolved method can be either <c>OnCompleted</c> or <c>UnsafeOnCompleted</c>.
		/// </value>
		public readonly IMethod OnCompletedMethod;

		/// <summary>
		/// Gets the awaiter <c>GetResult()</c> method.
		/// </summary>
		/// <value>
		/// <see langword="null"/> when the method cannot be resolved or when the await operation is dynamic.
		/// </value>
		public readonly IMethod GetResultMethod;

		/// <summary>
		/// Initializes an <see cref="AwaitResolveResult"/>.
		/// </summary>
		/// <param name="resultType">The type produced by the await expression.</param>
		/// <param name="getAwaiterInvocation">The resolved invocation of <c>GetAwaiter()</c>.</param>
		/// <param name="awaiterType">The resolved awaiter type.</param>
		/// <param name="isCompletedProperty">The resolved <c>IsCompleted</c> property, if available.</param>
		/// <param name="onCompletedMethod">The resolved continuation registration method, if available.</param>
		/// <param name="getResultMethod">The resolved <c>GetResult</c> method, if available.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="awaiterType"/> or <paramref name="getAwaiterInvocation"/> is <see langword="null"/>.
		/// </exception>
		public AwaitResolveResult(IType resultType, ResolveResult getAwaiterInvocation, IType awaiterType, IProperty isCompletedProperty, IMethod onCompletedMethod, IMethod getResultMethod)
			: base(resultType)
		{
			if (awaiterType == null)
				throw new ArgumentNullException(nameof(awaiterType));
			if (getAwaiterInvocation == null)
				throw new ArgumentNullException(nameof(getAwaiterInvocation));
			this.GetAwaiterInvocation = getAwaiterInvocation;
			this.AwaiterType = awaiterType;
			this.IsCompletedProperty = isCompletedProperty;
			this.OnCompletedMethod = onCompletedMethod;
			this.GetResultMethod = getResultMethod;
		}

		/// <summary>
		/// Gets a value indicating whether the await expression has unresolved required awaiter members.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <see cref="GetAwaiterInvocation"/> is already an error, or when non-dynamic await
		/// is missing <see cref="IsCompletedProperty"/>, <see cref="OnCompletedMethod"/>, or <see cref="GetResultMethod"/>;
		/// otherwise, <see langword="false"/>.
		/// </value>
		public override bool IsError {
			get { return this.GetAwaiterInvocation.IsError || (AwaiterType.Kind != TypeKind.Dynamic && (this.IsCompletedProperty == null || this.OnCompletedMethod == null || this.GetResultMethod == null)); }
		}

		/// <summary>
		/// Returns child semantic results used by this await node.
		/// </summary>
		/// <returns>A single-item sequence containing <see cref="GetAwaiterInvocation"/>.</returns>
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return new[] { GetAwaiterInvocation };
		}
	}
}

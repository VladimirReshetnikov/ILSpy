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
	/// Represents the resolved enumeration pattern used by a <c>foreach</c> statement.
	/// </summary>
	/// <remarks>
	/// This result records the binder-selected <c>GetEnumerator</c>/<c>MoveNext</c>/<c>Current</c> members and the inferred element type,
	/// allowing the decompiler to preserve language-level <c>foreach</c> semantics even when the IL pattern is rewritten.
	/// </remarks>
	public class ForEachResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the semantic tree for the call to GetEnumerator.
		/// </summary>
		public readonly ResolveResult GetEnumeratorCall;

		/// <summary>
		/// Gets the collection type that was analyzed when resolving the enumeration pattern.
		/// </summary>
		public readonly IType CollectionType;

		/// <summary>
		/// Gets the enumerator type returned by the selected <c>GetEnumerator</c> call.
		/// </summary>
		public readonly IType EnumeratorType;

		/// <summary>
		/// Gets the element type.
		/// This is the type that would be inferred for an implicitly-typed element variable.
		/// For explicitly-typed element variables, this type may differ from <c>ElementVariable.Type</c>.
		/// </summary>
		public readonly IType ElementType;

		/// <summary>
		/// Gets the Current property on the IEnumerator.
		/// Returns null if the property is not found.
		/// </summary>
		public readonly IProperty CurrentProperty;

		/// <summary>
		/// Gets the MoveNext() method on the IEnumerator.
		/// Returns null if the method is not found.
		/// </summary>
		public readonly IMethod MoveNextMethod;

		/// <summary>
		/// Initializes a resolved foreach semantic node.
		/// </summary>
		/// <param name="getEnumeratorCall">The semantic tree for the selected <c>GetEnumerator</c> invocation.</param>
		/// <param name="collectionType">The collection type participating in enumeration.</param>
		/// <param name="enumeratorType">The enumerator type produced by <paramref name="getEnumeratorCall"/>.</param>
		/// <param name="elementType">The inferred element type used for implicit iteration variables.</param>
		/// <param name="currentProperty">The resolved <c>Current</c> property, or <see langword="null"/> when unavailable.</param>
		/// <param name="moveNextMethod">The resolved <c>MoveNext</c> method, or <see langword="null"/> when unavailable.</param>
		/// <param name="voidType">The semantic type used for statement-like results (typically the compilation's <c>void</c> type).</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="getEnumeratorCall"/>, <paramref name="collectionType"/>, <paramref name="enumeratorType"/>, or
		/// <paramref name="elementType"/> is <see langword="null"/>.
		/// </exception>
		public ForEachResolveResult(ResolveResult getEnumeratorCall, IType collectionType, IType enumeratorType, IType elementType, IProperty currentProperty, IMethod moveNextMethod, IType voidType)
			: base(voidType)
		{
			if (getEnumeratorCall == null)
				throw new ArgumentNullException(nameof(getEnumeratorCall));
			if (collectionType == null)
				throw new ArgumentNullException(nameof(collectionType));
			if (enumeratorType == null)
				throw new ArgumentNullException(nameof(enumeratorType));
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			this.GetEnumeratorCall = getEnumeratorCall;
			this.CollectionType = collectionType;
			this.EnumeratorType = enumeratorType;
			this.ElementType = elementType;
			this.CurrentProperty = currentProperty;
			this.MoveNextMethod = moveNextMethod;
		}
	}
}

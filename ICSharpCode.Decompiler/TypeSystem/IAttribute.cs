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

#nullable enable

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a decoded custom attribute instance attached to a type-system symbol.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This abstraction is intentionally higher-level than raw ECMA-335 blobs. Implementations expose constructor and
	/// argument values as resolved <see cref="IType"/>-based structures so decompiler transforms and semantic analysis can
	/// inspect attributes without reparsing metadata signatures.
	/// </para>
	/// <para>
	/// <see cref="HasDecodeErrors"/> allows callers to distinguish "attribute exists but payload could not be decoded"
	/// from "attribute is absent". This is important when operating on malformed or partially-resolved inputs.
	/// </para>
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public interface IAttribute
	{
		/// <summary>
		/// Gets the resolved attribute type.
		/// </summary>
		/// <value>
		/// The concrete type that defines the attribute instance (for example <c>System.ObsoleteAttribute</c>).
		/// </value>
		IType AttributeType { get; }

		/// <summary>
		/// Gets the constructor that was matched for the attribute payload.
		/// </summary>
		/// <value>
		/// The resolved constructor symbol, or <see langword="null"/> when constructor resolution failed even though
		/// attribute metadata was present.
		/// </value>
		IMethod? Constructor { get; }

		/// <summary>
		/// Gets whether payload decoding encountered metadata/signature errors.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when fixed or named arguments could not be decoded reliably; otherwise <see langword="false"/>.
		/// </value>
		bool HasDecodeErrors { get; }

		/// <summary>
		/// Gets the positional constructor arguments in declaration order.
		/// </summary>
		/// <value>
		/// A decoded immutable array that corresponds to constructor parameters from left to right.
		/// </value>
		ImmutableArray<CustomAttributeTypedArgument<IType>> FixedArguments { get; }

		/// <summary>
		/// Gets the named property/field assignments applied after constructor invocation.
		/// </summary>
		/// <value>
		/// A decoded immutable array of named assignments; each entry identifies whether it targets a field or property.
		/// </value>
		ImmutableArray<CustomAttributeNamedArgument<IType>> NamedArguments { get; }
	}
}

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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a symbol that stores or exposes a typed value, such as locals, parameters, and fields.
	/// </summary>
	/// <remarks>
	/// This abstraction unifies several metadata-backed symbol kinds behind one contract used by semantic analysis and
	/// decompilation transforms. Implementations may represent mutable runtime storage locations or compile-time constants.
	/// </remarks>
	public interface IVariable : ISymbol
	{
		/// <summary>
		/// Gets the name of the variable.
		/// </summary>
		new string Name { get; }

		/// <summary>
		/// Gets the type of the variable.
		/// </summary>
		IType Type { get; }

		/// <summary>
		/// Gets a value indicating whether the variable has a compile-time constant value.
		/// </summary>
		/// <value>
		/// <see langword="true"/> for C# <c>const</c>-like symbols; otherwise <see langword="false"/>.
		/// </value>
		bool IsConst { get; }

		/// <summary>
		/// Gets the compile-time constant or default value associated with this variable.
		/// </summary>
		/// <param name="throwOnInvalidMetadata">
		/// <see langword="true"/> to surface metadata decoding failures as exceptions; <see langword="false"/> to tolerate
		/// malformed metadata and return <see langword="null"/> when a value cannot be decoded.
		/// </param>
		/// <returns>
		/// The constant value for constant variables, or the default value for optional parameters;
		/// otherwise <see langword="null"/>.
		/// </returns>
		object? GetConstantValue(bool throwOnInvalidMetadata = false);
	}
}

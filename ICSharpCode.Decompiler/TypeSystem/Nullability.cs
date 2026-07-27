// Copyright (c) 2019 Daniel Grunwald
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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Describes nullability state as encoded in metadata and consumed by the decompiler type system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This enum mirrors the three-state model used by nullable-reference metadata:
	/// unknown/oblivious, explicitly non-null, and explicitly nullable.
	/// </para>
	/// <para>
	/// The values are intentionally stable because they are persisted in caches and propagated through
	/// multiple decoding stages (metadata readers, type-system wrappers, and AST transforms).
	/// </para>
	/// </remarks>
	public enum Nullability : byte
	{
		/// <summary>
		/// No explicit nullability annotation is available for the symbol.
		/// </summary>
		/// <remarks>
		/// This commonly appears for assemblies compiled before nullable reference types or in contexts
		/// where nullability is suppressed.
		/// </remarks>
		Oblivious = 0,

		/// <summary>
		/// The symbol is annotated as non-nullable.
		/// </summary>
		NotNullable = 1,

		/// <summary>
		/// The symbol is annotated as nullable.
		/// </summary>
		Nullable = 2
	}
}

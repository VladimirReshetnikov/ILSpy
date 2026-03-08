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

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics
{
	/// <summary>
	/// Represents an explicit semantic resolution error.
	/// </summary>
	/// <remarks>
	/// Some failure states are encoded by other resolve-result subclasses (for example invalid conversions). Use
	/// <see cref="ResolveResult.IsError"/> to detect errors in a subtype-agnostic way.
	/// </remarks>
	/// <seealso cref="ResolveResult.IsError"/>.
	public class ErrorResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets a shared error instance whose <see cref="ResolveResult.Type"/> is <see cref="SpecialType.UnknownType"/>.
		/// </summary>
		public static readonly ErrorResolveResult UnknownError = new ErrorResolveResult(SpecialType.UnknownType);

		/// <summary>
		/// Initializes an error result with the specified fallback expression type.
		/// </summary>
		/// <param name="type">The type exposed to downstream consumers despite the error.</param>
		public ErrorResolveResult(IType type) : base(type)
		{
		}

		/// <summary>
		/// Initializes an error result with a diagnostic message and source location.
		/// </summary>
		/// <param name="type">The type exposed to downstream consumers despite the error.</param>
		/// <param name="message">A resolver diagnostic message, or <see langword="null"/> when no text is available.</param>
		/// <param name="location">The source location associated with the diagnostic.</param>
		public ErrorResolveResult(IType type, string message, TextLocation location) : base(type)
		{
			this.Message = message;
			this.Location = location;
		}

		public override bool IsError {
			get { return true; }
		}

		/// <summary>
		/// Gets the resolver diagnostic text, or <see langword="null"/> when no message was captured.
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// Gets the source location associated with <see cref="Message"/>.
		/// </summary>
		public TextLocation Location { get; private set; }
	}
}

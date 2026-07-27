// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a failure while resolving a metadata reference to a type or member symbol.
	/// </summary>
	/// <remarks>
	/// This exception is used when reference resolution fails after metadata has been parsed successfully,
	/// for example when an expected target assembly/type/member cannot be found in the current resolution context.
	/// </remarks>
	[Serializable]
	public class ReferenceResolvingException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolvingException"/> class.
		/// </summary>
		public ReferenceResolvingException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolvingException"/> class.
		/// </summary>
		/// <param name="message">Message describing the resolution failure.</param>
		public ReferenceResolvingException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolvingException"/> class.
		/// </summary>
		/// <param name="message">Message describing the resolution failure.</param>
		/// <param name="inner">Underlying exception that caused this resolution failure.</param>
		public ReferenceResolvingException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolvingException"/> class.
		/// </summary>
		/// <param name="info">Serialized exception payload.</param>
		/// <param name="context">Serialization context.</param>
		protected ReferenceResolvingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
	}
}

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
using System.Runtime.Serialization;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents an error while parsing a reflection name.
	/// </summary>
	/// <remarks>
	/// The <see cref="Position"/> property points at the parser offset where invalid syntax was detected,
	/// which allows callers to produce diagnostics that highlight the failing token in the original text.
	/// </remarks>
	[Serializable]
	public class ReflectionNameParseException : Exception
	{
		int position;

		/// <summary>
		/// Gets the zero-based character position at which parsing failed.
		/// </summary>
		public int Position {
			get { return position; }
		}

		/// <summary>
		/// Initializes a new instance with the specified failure position.
		/// </summary>
		/// <param name="position">Zero-based index into the parsed reflection name.</param>
		public ReflectionNameParseException(int position)
		{
			this.position = position;
		}

		/// <summary>
		/// Initializes a new instance with the specified failure position and error message.
		/// </summary>
		/// <param name="position">Zero-based index into the parsed reflection name.</param>
		/// <param name="message">Human-readable parse error details.</param>
		public ReflectionNameParseException(int position, string message) : base(message)
		{
			this.position = position;
		}

		/// <summary>
		/// Initializes a new instance with the specified failure position, error message, and inner exception.
		/// </summary>
		/// <param name="position">Zero-based index into the parsed reflection name.</param>
		/// <param name="message">Human-readable parse error details.</param>
		/// <param name="innerException">Underlying exception that triggered the parse failure.</param>
		public ReflectionNameParseException(int position, string message, Exception innerException) : base(message, innerException)
		{
			this.position = position;
		}

		// This constructor is needed for serialization.
		/// <summary>
		/// Recreates a serialized <see cref="ReflectionNameParseException"/> instance.
		/// </summary>
		/// <param name="info">Serialization payload.</param>
		/// <param name="context">Serialization context.</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> is <see langword="null"/>.</exception>
		protected ReflectionNameParseException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			position = info.GetInt32("position");
		}

		/// <summary>
		/// Serializes the current exception, including <see cref="Position"/>.
		/// </summary>
		/// <param name="info">Destination serialization payload.</param>
		/// <param name="context">Serialization context.</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> is <see langword="null"/>.</exception>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("position", position);
		}
	}
}

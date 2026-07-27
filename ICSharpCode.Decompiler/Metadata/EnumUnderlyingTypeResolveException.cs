// Copyright (c) 2018 Siegfried Pammer
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
using System.Runtime.Serialization;
using System.Text;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Represents a failure to determine the underlying integral type of an enum from metadata.
	/// </summary>
	/// <remarks>
	/// This exception is intentionally caught in several metadata and analyzer paths so ILSpy can continue
	/// processing incomplete or malformed metadata blobs without aborting the whole decompilation flow.
	/// </remarks>
	[Serializable]
	public class EnumUnderlyingTypeResolveException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EnumUnderlyingTypeResolveException"/> class.
		/// </summary>
		public EnumUnderlyingTypeResolveException() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumUnderlyingTypeResolveException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public EnumUnderlyingTypeResolveException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumUnderlyingTypeResolveException"/> class with a specified error message and inner exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that caused the current exception.</param>
		public EnumUnderlyingTypeResolveException(string message, Exception inner) : base(message, inner) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="EnumUnderlyingTypeResolveException"/> class with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination.</param>
		protected EnumUnderlyingTypeResolveException(
		  SerializationInfo info,
		  StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// Indicates that a file could be opened but does not expose managed metadata in a format ILSpy can consume.
	/// </summary>
	[Serializable]
	public class MetadataFileNotSupportedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MetadataFileNotSupportedException"/> class.
		/// </summary>
		public MetadataFileNotSupportedException() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MetadataFileNotSupportedException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public MetadataFileNotSupportedException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MetadataFileNotSupportedException"/> class with a specified error message and inner exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="inner">The exception that caused the current exception.</param>
		public MetadataFileNotSupportedException(string message, Exception inner) : base(message, inner) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MetadataFileNotSupportedException"/> class with serialized data.
		/// </summary>
		/// <param name="info">The object that holds the serialized object data.</param>
		/// <param name="context">The contextual information about the source or destination.</param>
		protected MetadataFileNotSupportedException(
		  SerializationInfo info,
		  StreamingContext context) : base(info, context) { }
	}
}

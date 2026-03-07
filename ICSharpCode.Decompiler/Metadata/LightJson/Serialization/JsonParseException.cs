// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace LightJson.Serialization
{
	using System;

	/// <summary>
	/// Represents a syntax error produced while parsing JSON text.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parser reports both a coarse-grained <see cref="Type"/> classification and a 0-based
	/// <see cref="Position"/> to make diagnostics deterministic for tooling.
	/// </para>
	/// <para>
	/// This exception type is thrown by the internal LightJson parser used by metadata loading paths,
	/// including <c>.deps.json</c> processing in the .NET runtime resolver.
	/// </para>
	/// </remarks>
	internal sealed class JsonParseException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonParseException"/> class.
		/// </summary>
		public JsonParseException()
			: base(GetDefaultMessage(ErrorType.Unknown))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonParseException"/> class with the given error type and position.
		/// </summary>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(ErrorType type, TextPosition position)
			: this(GetDefaultMessage(type), type, position)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonParseException"/> class with the given message, error type, and position.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(string message, ErrorType type, TextPosition position)
			: base(message)
		{
			this.Type = type;
			this.Position = position;
		}

		/// <summary>
		/// Categorizes parse failures raised by <see cref="JsonReader"/>.
		/// </summary>
		public enum ErrorType : int
		{
			/// <summary>
			/// Indicates that the cause of the error is unknown.
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// Indicates that the text ended before the message could be parsed.
			/// </summary>
			IncompleteMessage,

			/// <summary>
			/// Indicates that a JsonObject contains more than one key with the same name.
			/// </summary>
			DuplicateObjectKeys,

			/// <summary>
			/// Indicates that the parser encountered and invalid or unexpected character.
			/// </summary>
			InvalidOrUnexpectedCharacter,
		}

		/// <summary>
		/// Gets the 0-based line and column where parsing detected the failure.
		/// </summary>
		/// <value>The scanner position captured at the point the error was reported.</value>
		public TextPosition Position { get; private set; }

		/// <summary>
		/// Gets the parser-specific error category.
		/// </summary>
		/// <value>The classification used to drive fallback behavior or diagnostics.</value>
		public ErrorType Type { get; private set; }

		private static string GetDefaultMessage(ErrorType type)
		{
			switch (type)
			{
				case ErrorType.IncompleteMessage:
					return "The string ended before a value could be parsed.";

				case ErrorType.InvalidOrUnexpectedCharacter:
					return "The parser encountered an invalid or unexpected character.";

				case ErrorType.DuplicateObjectKeys:
					return "The parser encountered a JsonObject with duplicate keys.";

				default:
					return "An error occurred while parsing the JSON message.";
			}
		}
	}
}

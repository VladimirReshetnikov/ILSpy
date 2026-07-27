// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace LightJson.Serialization
{
	/// <summary>
	/// Represents a 0-based cursor location within JSON source text.
	/// </summary>
	/// <remarks>
	/// The parser updates this structure as characters are consumed. It is embedded in
	/// <see cref="JsonParseException"/> so callers can report precise diagnostics.
	/// </remarks>
	internal struct TextPosition
	{
		/// <summary>
		/// The column position, 0-based.
		/// </summary>
		public long Column;

		/// <summary>
		/// The line position, 0-based.
		/// </summary>
		public long Line;
	}
}

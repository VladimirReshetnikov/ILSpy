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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Utility helpers for converting raw metadata token integers into SRM handle values in a fault-tolerant way.
	/// </summary>
	public static class MetadataTokenHelpers
	{
		/// <summary>
		/// Attempts to decode a raw metadata token as an <see cref="EntityHandle"/>.
		/// </summary>
		/// <param name="metadataToken">Raw metadata token value read from IL or custom debug blobs.</param>
		/// <returns>
		/// The decoded handle when the token is valid and non-negative; otherwise <see langword="null"/>.
		/// </returns>
		public static EntityHandle? TryAsEntityHandle(int metadataToken)
		{
			// SRM would interpret negative token values as virtual tokens,
			// but that causes problems later on.
			if (metadataToken < 0)
				return null;
			try
			{
				return MetadataTokens.EntityHandle(metadataToken);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		/// <summary>
		/// Decodes a raw metadata token and falls back to a nil handle when the token cannot be represented.
		/// </summary>
		/// <param name="metadataToken">Raw metadata token value read from IL or custom debug blobs.</param>
		/// <returns>
		/// A valid decoded handle, or the nil entity handle when the token is negative or malformed.
		/// </returns>
		public static EntityHandle EntityHandleOrNil(int metadataToken)
		{
			// SRM would interpret negative token values as virtual tokens,
			// but that causes problems later on.
			if (metadataToken < 0)
				return MetadataTokens.EntityHandle(0);
			try
			{
				return MetadataTokens.EntityHandle(metadataToken);
			}
			catch (ArgumentException)
			{
				return MetadataTokens.EntityHandle(0);
			}
		}
	}
}

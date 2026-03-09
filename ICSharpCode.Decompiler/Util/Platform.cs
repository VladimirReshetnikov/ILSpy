#nullable enable
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

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Platform-specific code.
	/// </summary>
	public static class Platform
	{
		/// <summary>
		/// Gets the default comparer used for filesystem-style path and file-name comparisons.
		/// </summary>
		/// <remarks>
		/// <para>
		/// On Unix-like platforms this property returns <see cref="StringComparer.Ordinal"/> because file names are usually case-sensitive.
		/// </para>
		/// <para>
		/// On Windows it returns <see cref="StringComparer.OrdinalIgnoreCase"/> to match the typical case-insensitive file-system behavior used by
		/// project decompilation paths and lookup tables.
		/// </para>
		/// </remarks>
		public static StringComparer FileNameComparer {
			get {
				switch (Environment.OSVersion.Platform)
				{
					case PlatformID.Unix:
					case PlatformID.MacOSX:
						return StringComparer.Ordinal;
					default:
						return StringComparer.OrdinalIgnoreCase;
				}
			}
		}
	}
}

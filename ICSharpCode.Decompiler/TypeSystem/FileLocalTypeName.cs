// Copyright (c) 2026 Vladimir Reshetnikov
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

using System;
using System.Diagnostics.CodeAnalysis;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// A type declared with the C# 11 'file' modifier is visible only inside the file that
	/// declares it, so the compiler is free to emit several of them under one name. It keeps them
	/// apart by mangling the metadata name as
	/// <c>&lt;<i>file</i>&gt;F<i>hash</i>__<i>Name</i></c>, where <i>hash</i> identifies the
	/// declaring file and <i>Name</i> is what the source wrote.
	/// </summary>
	static class FileLocalTypeName
	{
		/// <summary>
		/// Splits the metadata name of a file-local type into the name the source declared and
		/// the hash identifying its file. Returns false for any other name.
		/// </summary>
		public static bool TryParse(string metadataName, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string? fileHash)
		{
			name = null;
			fileHash = null;
			if (metadataName == null || metadataName.Length < 5 || metadataName[0] != '<')
				return false;
			int closing = metadataName.IndexOf('>');
			// The hash follows an 'F' directly after the file part, and at least one hash
			// character has to separate it from the '__' that introduces the name.
			if (closing < 1 || closing + 2 >= metadataName.Length || metadataName[closing + 1] != 'F')
				return false;
			int hashStart = closing + 2;
			int i = hashStart;
			while (i < metadataName.Length && IsHexDigit(metadataName[i]))
				i++;
			if (i == hashStart || i + 2 > metadataName.Length)
				return false;
			if (metadataName[i] != '_' || metadataName[i + 1] != '_')
				return false;
			int nameStart = i + 2;
			if (nameStart >= metadataName.Length)
				return false;
			name = metadataName.Substring(nameStart);
			// The file part travels with the hash: two types are in the same file only if both
			// agree, and the hash alone is not documented to be collision-free across names.
			fileHash = metadataName.Substring(0, i);
			return true;
		}

		/// <summary>
		/// Returns the name the source declared, or <paramref name="metadataName"/> unchanged
		/// when it is not the mangled name of a file-local type.
		/// </summary>
		public static string Unmangle(string metadataName)
		{
			return TryParse(metadataName, out var name, out _) ? name : metadataName;
		}

		static bool IsHexDigit(char c)
		{
			return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
		}
	}
}

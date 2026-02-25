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

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// Version-DisplayName pair used in UI scenarios, for example ILSpy's language version dropdown.
	/// </summary>
	public class LanguageVersion
	{
		/// <summary>
		/// Gets the language-version token understood by <c>DecompilerSettings.SetLanguageVersion(...)</c>
		/// (for example <c>CSharp13_0</c>).
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Gets a UI-friendly label shown in version pickers.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Creates a language version descriptor for menus and persisted language settings.
		/// </summary>
		/// <param name="version">The underlying decompiler language-version token.</param>
		/// <param name="name">Optional display label. When <see langword="null"/>, <paramref name="version"/> is used.</param>
		public LanguageVersion(string version, string? name = null)
		{
			Version = version ?? "";
			DisplayName = name ?? Version.ToString();
		}

		public override string ToString()
		{
			return $"[LanguageVersion DisplayName={DisplayName}, Version={Version}]";
		}
	}
}

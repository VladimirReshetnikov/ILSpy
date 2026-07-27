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
using System.Xml.Linq;

namespace ICSharpCode.ILSpyX.Settings
{
	/// <summary>
	/// Abstraction over ILSpy's XML settings document.
	/// Implementations expose individual top-level sections and provide a synchronized update pipeline.
	/// </summary>
	public interface ISettingsProvider
	{
		/// <summary>
		/// Gets a top-level section by name.
		/// </summary>
		/// <param name="section">Qualified section name under the root <c>ILSpy</c> element.</param>
		/// <returns>
		/// The stored section element when present; otherwise an empty element with the requested name.
		/// </returns>
		XElement this[XName section] { get; }

		/// <summary>
		/// Applies a mutation to the settings root and persists the result.
		/// </summary>
		/// <param name="action">Callback that updates the root element before it is saved.</param>
		void Update(Action<XElement> action);

		/// <summary>
		/// Saves or replaces an entire top-level section.
		/// </summary>
		/// <param name="section">Section element to write under the root <c>ILSpy</c> node.</param>
		void SaveSettings(XElement section);
	}
}

// Copyright (c) 2022 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.ILSpyX.Settings
{
	/// <summary>
	/// <see cref="ISettingsFilePathProvider"/> implementation that always returns a caller-supplied path.
	/// </summary>
	public class DefaultSettingsFilePathProvider : ISettingsFilePathProvider
	{
		private readonly string _providedPath;

		/// <summary>
		/// Creates a path provider that points to a specific settings file.
		/// </summary>
		/// <param name="providedPath">Path that should be returned from <see cref="GetSettingsFilePath"/>.</param>
		public DefaultSettingsFilePathProvider(string providedPath)
		{
			_providedPath = providedPath;
		}

		/// <summary>
		/// Returns the settings file path supplied when this instance was created.
		/// </summary>
		/// <returns>The unchanged path value provided to the constructor.</returns>
		public string GetSettingsFilePath()
		{
			return _providedPath;
		}
	}
}

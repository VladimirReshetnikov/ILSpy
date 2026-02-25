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

using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Stores accumulated diagnostics for a single unresolved assembly reference.
	/// </summary>
	public sealed class UnresolvedAssemblyNameReference
	{
		/// <summary>
		/// Gets the full display name of the referenced assembly.
		/// </summary>
		public string FullName { get; }

		/// <summary>
		/// Gets whether <see cref="Messages"/> contains at least one error-level diagnostic.
		/// </summary>
		public bool HasErrors => Messages.Any(m => m.Item1 == MessageKind.Error);

		/// <summary>
		/// Gets the ordered diagnostic messages recorded while trying to resolve this reference.
		/// </summary>
		public List<(MessageKind, string)> Messages { get; } = new List<(MessageKind, string)>();

		/// <summary>
		/// Initializes a new entry for the specified referenced assembly.
		/// </summary>
		/// <param name="fullName">Full display name of the referenced assembly.</param>
		public UnresolvedAssemblyNameReference(string fullName)
		{
			this.FullName = fullName;
		}
	}

	/// <summary>
	/// Classifies the severity of a reference-resolution diagnostic message.
	/// </summary>
	public enum MessageKind { Error, Warning, Info }
}

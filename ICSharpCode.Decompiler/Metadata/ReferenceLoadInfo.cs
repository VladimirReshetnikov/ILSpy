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
using System.Linq;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Thread-safe accumulator for reference-resolution diagnostics keyed by assembly full name.
	/// </summary>
	/// <remarks>
	/// This type is used while probing assembly references from multiple resolution paths
	/// (assembly list, framework packs, and explicit file probes) so the UI can later render
	/// a per-reference load log.
	/// </remarks>
	public class ReferenceLoadInfo
	{
		readonly Dictionary<string, UnresolvedAssemblyNameReference> loadedAssemblyReferences = new Dictionary<string, UnresolvedAssemblyNameReference>();

		/// <summary>
		/// Appends a diagnostic message for the specified assembly reference.
		/// </summary>
		/// <param name="fullName">Assembly full name used as the aggregation key.</param>
		/// <param name="kind">Severity of the diagnostic message.</param>
		/// <param name="message">Diagnostic text describing the resolution outcome.</param>
		public void AddMessage(string fullName, MessageKind kind, string message)
		{
			lock (loadedAssemblyReferences)
			{
				if (!loadedAssemblyReferences.TryGetValue(fullName, out var referenceInfo))
				{
					referenceInfo = new UnresolvedAssemblyNameReference(fullName);
					loadedAssemblyReferences.Add(fullName, referenceInfo);
				}
				referenceInfo.Messages.Add((kind, message));
			}
		}

		/// <summary>
		/// Appends a diagnostic message only when both the severity and the text differ from the most
		/// recently stored message for the same assembly.
		/// </summary>
		/// <param name="fullName">Assembly full name used as the aggregation key.</param>
		/// <param name="kind">Severity of the diagnostic message.</param>
		/// <param name="message">Diagnostic text describing the resolution outcome.</param>
		public void AddMessageOnce(string fullName, MessageKind kind, string message)
		{
			lock (loadedAssemblyReferences)
			{
				if (!loadedAssemblyReferences.TryGetValue(fullName, out var referenceInfo))
				{
					referenceInfo = new UnresolvedAssemblyNameReference(fullName);
					loadedAssemblyReferences.Add(fullName, referenceInfo);
					referenceInfo.Messages.Add((kind, message));
				}
				else
				{
					var lastMsg = referenceInfo.Messages.LastOrDefault();
					if (kind != lastMsg.Item1 && message != lastMsg.Item2)
						referenceInfo.Messages.Add((kind, message));
				}
			}
		}

		/// <summary>
		/// Looks up aggregated diagnostics for one assembly reference.
		/// </summary>
		/// <param name="fullName">Assembly full name used as the aggregation key.</param>
		/// <param name="info">When this method returns <c>true</c>, receives the matching entry.</param>
		/// <returns><c>true</c> if an entry exists for <paramref name="fullName"/>; otherwise <c>false</c>.</returns>
		public bool TryGetInfo(string fullName, out UnresolvedAssemblyNameReference info)
		{
			lock (loadedAssemblyReferences)
			{
				return loadedAssemblyReferences.TryGetValue(fullName, out info);
			}
		}

		/// <summary>
		/// Gets a point-in-time list of all tracked reference diagnostics. The list itself is a copy, but the
		/// entries in it are the live objects this instance keeps appending messages to.
		/// </summary>
		public IReadOnlyList<UnresolvedAssemblyNameReference> Entries {
			get {
				lock (loadedAssemblyReferences)
				{
					return loadedAssemblyReferences.Values.ToList();
				}
			}
		}

		/// <summary>
		/// Gets whether any tracked reference contains at least one <see cref="MessageKind.Error"/> message.
		/// </summary>
		public bool HasErrors {
			get {
				lock (loadedAssemblyReferences)
				{
					return loadedAssemblyReferences.Any(i => i.Value.HasErrors);
				}
			}
		}
	}
}

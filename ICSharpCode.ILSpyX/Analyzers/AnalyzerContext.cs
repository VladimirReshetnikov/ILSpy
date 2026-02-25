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
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Threading;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Abstractions;

namespace ICSharpCode.ILSpyX.Analyzers
{
	/// <summary>
	/// Provides additional context for analyzers.
	/// </summary>
	public class AnalyzerContext
	{
		/// <summary>
		/// Gets the active assembly-list session that determines which assemblies are in analysis scope.
		/// </summary>
		public required AssemblyList AssemblyList { get; init; }

		/// <summary>
		/// CancellationToken. Currently Analyzers do not support cancellation from the UI, but it should be checked nonetheless.
		/// </summary>
		public CancellationToken CancellationToken { get; init; }

		/// <summary>
		/// Currently used language.
		/// </summary>
		public required ILanguage Language { get; init; }

		/// <summary>
		/// Allows the analyzer to control whether the tree nodes will be sorted.
		/// Must be set within <see cref="IAnalyzer.Analyze(ISymbol, AnalyzerContext)"/>
		/// before the results are enumerated.
		/// </summary>
		public bool SortResults { get; set; }

		/// <summary>
		/// Tries to read the IL method body for a metadata-backed method symbol.
		/// </summary>
		/// <param name="method">Method to inspect.</param>
		/// <returns>
		/// A decoded method body when the symbol has a valid metadata token and readable body data;
		/// otherwise <see langword="null"/>.
		/// </returns>
		public MethodBodyBlock? GetMethodBody(IMethod method)
		{
			if (!method.HasBody || method.MetadataToken.IsNil || method.ParentModule?.MetadataFile == null)
				return null;
			var module = method.ParentModule.MetadataFile;
			var md = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
			try
			{
				return module.GetMethodBody(md.RelativeVirtualAddress);
			}
			catch (BadImageFormatException)
			{
				return null;
			}
		}

		/// <summary>
		/// Creates an analyzer scope rooted at the specified symbol.
		/// </summary>
		/// <param name="entity">The symbol that defines the assembly/module search boundary.</param>
		/// <returns>A scope object used by built-in analyzers to enumerate candidate symbols.</returns>
		public AnalyzerScope GetScopeOf(IEntity entity)
		{
			return new AnalyzerScope(AssemblyList, entity);
		}

		readonly ConcurrentDictionary<MetadataFile, DecompilerTypeSystem> typeSystemCache = new();

		/// <summary>
		/// Gets a cached <see cref="DecompilerTypeSystem"/> for a metadata module, creating one on first use.
		/// </summary>
		/// <param name="module">Metadata module that should be wrapped in a decompiler type system.</param>
		/// <returns>The cached or newly created type-system instance for <paramref name="module"/>.</returns>
		public DecompilerTypeSystem GetOrCreateTypeSystem(MetadataFile module)
		{
			return typeSystemCache.GetOrAdd(module, m => new DecompilerTypeSystem(m, m.GetAssemblyResolver()));
		}
	}
}

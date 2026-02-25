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
using System.Threading;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Holds per-run state shared across a single decompilation pipeline execution.
	/// </summary>
	internal class DecompileRun
	{
		/// <summary>
		/// Gets symbols that should be treated as defined for conditional compilation output.
		/// </summary>
		public HashSet<string> DefinedSymbols { get; } = new HashSet<string>();

		/// <summary>
		/// Gets or sets namespaces required by the current decompilation target.
		/// </summary>
		public HashSet<string> Namespaces { get; set; }

		/// <summary>
		/// Gets or sets the cancellation token used by long-running transforms.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Gets the decompiler settings used for this run.
		/// </summary>
		public DecompilerSettings Settings { get; }

		/// <summary>
		/// Gets or sets the documentation provider used to resolve XML documentation text.
		/// </summary>
		public IDocumentationProvider DocumentationProvider { get; set; }

		/// <summary>
		/// Gets a cache of synthesized record helper decompilers keyed by type definition.
		/// </summary>
		public Dictionary<ITypeDefinition, RecordDecompiler> RecordDecompilers { get; } = new Dictionary<ITypeDefinition, RecordDecompiler>();

		/// <summary>
		/// Gets a cache that tracks whether a type hierarchy was fully resolved.
		/// </summary>
		public Dictionary<ITypeDefinition, bool> TypeHierarchyIsKnown { get; } = new();

		/// <summary>
		/// Gets the root using scope used for type/name resolution during this run.
		/// </summary>
		public CSharp.TypeSystem.UsingScope UsingScope { get; }

		/// <summary>
		/// Initializes a new <see cref="DecompileRun"/> instance.
		/// </summary>
		/// <param name="settings">Decompiler options to apply for this run.</param>
		/// <param name="usingScope">Resolved using scope used by the C# resolver.</param>
		public DecompileRun(DecompilerSettings settings, CSharp.TypeSystem.UsingScope usingScope)
		{
			this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			this.UsingScope = usingScope ?? throw new ArgumentNullException(nameof(usingScope));
		}
	}

	enum EnumValueDisplayMode
	{
		None,
		All,
		AllHex,
		FirstOnly
	}
}

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

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyX.Analyzers
{
	/// <summary>
	/// Base interface for all analyzers. You can register an analyzer for any <see cref="ISymbol"/> by implementing
	/// this interface and adding an <see cref="ExportAnalyzerAttribute"/>.
	/// </summary>
	public interface IAnalyzer
	{
		/// <summary>
		/// Determines whether this analyzer should be offered for the specified symbol.
		/// </summary>
		/// <param name="symbol">Symbol currently selected in the analyzer UI, or <see langword="null"/> when none is.</param>
		/// <returns>
		/// <see langword="true"/> when the analyzer can process <paramref name="symbol"/>; otherwise <see langword="false"/>.
		/// Implementations must tolerate a <see langword="null"/> argument and return <see langword="false"/> for it.
		/// </returns>
		bool Show(ISymbol? symbol);

		/// <summary>
		/// Produces analyzer results for the selected symbol.
		/// </summary>
		/// <param name="analyzedSymbol">Root symbol being analyzed.</param>
		/// <param name="context">Execution context with scope, language, and cancellation information.</param>
		/// <returns>A (possibly lazy) sequence of symbols to display as analyzer results.</returns>
		IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context);
	}

	/// <summary>
	/// Metadata exposed by <see cref="ExportAnalyzerAttribute"/> and consumed when composing analyzer menu entries.
	/// </summary>
	public interface IAnalyzerMetadata
	{
		/// <summary>
		/// Gets the header shown in the analyzer root node.
		/// </summary>
		string Header { get; }

		/// <summary>
		/// Gets the sort key used to order analyzers in the context menu.
		/// </summary>
		int Order { get; }
	}
}

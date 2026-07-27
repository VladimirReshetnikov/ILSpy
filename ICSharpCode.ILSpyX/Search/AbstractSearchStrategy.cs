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
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX.Abstractions;

namespace ICSharpCode.ILSpyX.Search
{
	/// <summary>
	/// Selects the domain searched by a strategy.
	/// </summary>
	public enum SearchMode
	{
		/// <summary>Search types and members together.</summary>
		TypeAndMember,
		/// <summary>Search only type definitions.</summary>
		Type,
		/// <summary>Search members using <see cref="MemberSearchKind"/> to refine the target.</summary>
		Member,
		/// <summary>Search only method members.</summary>
		Method,
		/// <summary>Search only field members.</summary>
		Field,
		/// <summary>Search only property members.</summary>
		Property,
		/// <summary>Search only event members.</summary>
		Event,
		/// <summary>Search literal values inside member bodies.</summary>
		Literal,
		/// <summary>Search entities by metadata token value.</summary>
		Token,
		/// <summary>Search manifest and embedded resources.</summary>
		Resource,
		/// <summary>Search assembly-level metadata.</summary>
		Assembly,
		/// <summary>Search namespace names.</summary>
		Namespace
	}

	/// <summary>
	/// Captures immutable options and collaborators used to execute a search pass.
	/// </summary>
	public struct SearchRequest
	{
		/// <summary>Decompiler settings snapshot used when a strategy needs language-specific formatting details.</summary>
		public DecompilerSettings DecompilerSettings;
		/// <summary>Factory used by resource-oriented strategies to build tree nodes for recursive traversal.</summary>
		public ITreeNodeFactory TreeNodeFactory;
		/// <summary>Factory that projects matched objects into concrete <see cref="SearchResult"/> instances.</summary>
		public ISearchResultFactory SearchResultFactory;
		/// <summary>Primary domain to search.</summary>
		public SearchMode Mode;
		/// <summary>Assembly metadata field selected when <see cref="Mode"/> is <see cref="SearchMode.Assembly"/>.</summary>
		public AssemblySearchKind AssemblySearchKind;
		/// <summary>Member category selected when <see cref="Mode"/> is <see cref="SearchMode.Member"/>.</summary>
		public MemberSearchKind MemberSearchKind;
		/// <summary>Parsed keyword tokens from the query (operators such as <c>+</c>, <c>-</c>, <c>=</c>, and <c>~</c> are preserved).</summary>
		public string[] Keywords;
		/// <summary>Regular expression filter parsed from the query, or <see langword="null"/> when keyword matching is used.</summary>
		public Regex? RegEx;
		/// <summary>Indicates that name matching should include fully qualified names, not only simple member/type names.</summary>
		public bool FullNameSearch;
		/// <summary>Indicates that generic arity and arguments should be omitted before matching plain-text keywords.</summary>
		public bool OmitGenerics;
		// When set, CheckVisibility bypasses the api-visibility filter so private /
		// compiler-generated entities (state-machines, display classes, anonymous
		// closures, anything with `<...>` segments in its metadata name) become
		// findable. Set by the query parser when the user types `<` or `>` in
		// their search term: those characters are characteristically present in
		// compiler-generated names and rare in everyday API names, so they're a
		// reliable signal that the user wants the visibility filter relaxed.
		public bool IncludePrivateApi;
		/// <summary>Optional namespace substring restriction from the <c>innamespace:</c> prefix.</summary>
		public string InNamespace;
		/// <summary>Optional assembly-name restriction from the <c>inassembly:</c> prefix.</summary>
		public string InAssembly;
	}

	/// <summary>
	/// Base class that provides term matching and result queueing for concrete search strategies.
	/// </summary>
	public abstract class AbstractSearchStrategy
	{
		protected readonly string[] searchTerm;
		protected readonly Regex? regex;
		protected readonly bool fullNameSearch;
		protected readonly bool omitGenerics;
		protected readonly SearchRequest searchRequest;
		private readonly IProducerConsumerCollection<SearchResult> resultQueue;

		/// <summary>
		/// Initializes a strategy with a parsed <see cref="SearchRequest"/> and destination result queue.
		/// </summary>
		/// <param name="request">The search configuration and shared service references.</param>
		/// <param name="resultQueue">The concurrent queue that receives discovered matches.</param>
		protected AbstractSearchStrategy(SearchRequest request, IProducerConsumerCollection<SearchResult> resultQueue)
		{
			this.resultQueue = resultQueue;
			this.searchTerm = request.Keywords;
			this.regex = request.RegEx;
			this.searchRequest = request;
			this.fullNameSearch = request.FullNameSearch;
			this.omitGenerics = request.OmitGenerics;
		}

		/// <summary>
		/// Searches a metadata module and reports matches through the result queue.
		/// </summary>
		/// <param name="module">The module to search.</param>
		/// <param name="cancellationToken">Token used to cancel the search early.</param>
		public abstract void Search(MetadataFile module, CancellationToken cancellationToken);

		/// <summary>
		/// Evaluates whether a candidate name satisfies configured keyword or regex filters.
		/// </summary>
		/// <param name="name">The candidate text to match.</param>
		/// <returns><see langword="true"/> when the candidate passes all filters; otherwise, <see langword="false"/>.</returns>
		protected virtual bool IsMatch(string name)
		{
			if (regex != null)
			{
				return regex.IsMatch(name);
			}

			for (int i = 0; i < searchTerm.Length; ++i)
			{
				// How to handle overlapping matches?
				var term = searchTerm[i];
				if (string.IsNullOrEmpty(term))
					continue;
				string text = name;
				switch (term[0])
				{
					case '+': // must contain
						term = term.Substring(1);
						goto default;
					case '-': // should not contain
						if (term.Length > 1 && text.IndexOf(term.Substring(1), StringComparison.OrdinalIgnoreCase) >= 0)
							return false;
						break;
					case '=': // exact match
					{
						var equalCompareLength = text.IndexOf('`');
						if (equalCompareLength == -1)
							equalCompareLength = text.Length;

						if (term.Length > 1 && String.Compare(term, 1, text, 0, Math.Max(term.Length, equalCompareLength),
							StringComparison.OrdinalIgnoreCase) != 0)
							return false;
					}
					break;
					case '~':
						if (term.Length > 1 && !IsNoncontiguousMatch(text.ToLower(), term.Substring(1).ToLower()))
							return false;
						break;
					default:
						if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
							return false;
						break;
				}
			}
			return true;
		}

		bool IsNoncontiguousMatch(string text, string searchTerm)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
			{
				return false;
			}
			var textLength = text.Length;
			if (searchTerm.Length > textLength)
			{
				return false;
			}
			var i = 0;
			for (int searchIndex = 0; searchIndex < searchTerm.Length;)
			{
				while (i != textLength)
				{
					if (text[i] == searchTerm[searchIndex])
					{
						// Check if all characters in searchTerm have been matched
						if (searchTerm.Length == ++searchIndex)
							return true;
						i++;
						break;
					}
					i++;
				}
				if (i == textLength)
					return false;
			}
			return false;
		}

		/// <summary>
		/// Enqueues a discovered search result.
		/// </summary>
		/// <param name="result">The result to publish.</param>
		protected void OnFoundResult(SearchResult result)
		{
			resultQueue.TryAdd(result);
		}
	}
}

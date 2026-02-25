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
using System.Collections.Generic;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Abstractions;

namespace ICSharpCode.ILSpyX.Search
{
	/// <summary>
	/// Produces strongly typed <see cref="SearchResult"/> instances for specific search domains.
	/// </summary>
	public interface ISearchResultFactory
	{
		/// <summary>
		/// Creates a result entry for a type or member match.
		/// </summary>
		/// <param name="entity">The matched entity from the decompiler type system.</param>
		/// <returns>A populated <see cref="MemberSearchResult"/> for the supplied <paramref name="entity"/>.</returns>
		MemberSearchResult Create(IEntity entity);

		/// <summary>
		/// Creates a result entry for a matched resource.
		/// </summary>
		/// <param name="module">The metadata file that owns the resource.</param>
		/// <param name="resource">The matched resource.</param>
		/// <param name="node">Tree node representing the resource itself.</param>
		/// <param name="parent">Tree node representing the owning resource container.</param>
		/// <returns>A populated <see cref="ResourceSearchResult"/> for the supplied <paramref name="resource"/>.</returns>
		ResourceSearchResult Create(MetadataFile module, Resource resource, ITreeNode node, ITreeNode parent);

		/// <summary>
		/// Creates a result entry for an assembly-level match.
		/// </summary>
		/// <param name="module">The matched assembly or module file.</param>
		/// <returns>A populated <see cref="AssemblySearchResult"/> for the supplied <paramref name="module"/>.</returns>
		AssemblySearchResult Create(MetadataFile module);

		/// <summary>
		/// Creates a result entry for a namespace match.
		/// </summary>
		/// <param name="module">The metadata file that contributes the namespace.</param>
		/// <param name="namespace">The matched namespace.</param>
		/// <returns>A populated <see cref="NamespaceSearchResult"/> for the supplied namespace.</returns>
		NamespaceSearchResult Create(MetadataFile module, INamespace @namespace);
	}

	/// <summary>
	/// Base model used by the search UI to display and rank matches.
	/// </summary>
	public class SearchResult
	{
		/// <summary>
		/// Compares results by <see cref="Name"/> using ordinal string comparison.
		/// </summary>
		public static readonly IComparer<SearchResult> ComparerByName = new SearchResultNameComparer();

		/// <summary>
		/// Compares results by descending <see cref="Fitness"/> so the best match is shown first.
		/// </summary>
		public static readonly IComparer<SearchResult> ComparerByFitness = new SearchResultFitnessComparer();

		/// <summary>
		/// Gets the object used for navigation when the result is activated.
		/// </summary>
		public virtual object? Reference => null;

		/// <summary>
		/// Gets or sets the relevance score used for result ordering.
		/// Higher values indicate a better match.
		/// </summary>
		public float Fitness { get; set; }

		/// <summary>
		/// Gets or sets the primary label shown for the result.
		/// </summary>
		public required string Name { get; set; }

		/// <summary>
		/// Gets or sets the contextual location text (for example containing type or file path).
		/// </summary>
		public required string Location { get; set; }

		/// <summary>
		/// Gets or sets the assembly display name shown in the result list.
		/// </summary>
		public required string Assembly { get; set; }

		/// <summary>
		/// Gets or sets optional tooltip content displayed for this result.
		/// </summary>
		public object? ToolTip { get; set; }

		/// <summary>
		/// Gets or sets the icon shown next to <see cref="Name"/>.
		/// </summary>
		public required object Image { get; set; }

		/// <summary>
		/// Gets or sets the icon shown next to <see cref="Location"/>.
		/// </summary>
		public required object LocationImage { get; set; }

		/// <summary>
		/// Gets or sets the icon shown next to <see cref="Assembly"/>.
		/// </summary>
		public required object AssemblyImage { get; set; }

		public override string ToString()
		{
			return Name;
		}

		class SearchResultNameComparer : IComparer<SearchResult>
		{
			public int Compare(SearchResult? x, SearchResult? y)
			{
				return StringComparer.Ordinal.Compare(x?.Name ?? "", y?.Name ?? "");
			}
		}

		class SearchResultFitnessComparer : IComparer<SearchResult>
		{
			public int Compare(SearchResult? x, SearchResult? y)
			{
				//elements with higher Fitness come first
				return Comparer<float>.Default.Compare(y?.Fitness ?? 0, x?.Fitness ?? 0);
			}
		}
	}

	/// <summary>
	/// Search result that represents a matched metadata entity.
	/// </summary>
	public class MemberSearchResult : SearchResult
	{
#nullable disable
		/// <summary>
		/// Gets or sets the matched entity used for navigation.
		/// </summary>
		public IEntity Member { get; set; }

		/// <inheritdoc cref="SearchResult.Reference"/>
		public override object Reference => Member;
#nullable enable
	}

	/// <summary>
	/// Search result that represents a matched embedded resource.
	/// </summary>
	public class ResourceSearchResult : SearchResult
	{
#nullable disable
		/// <summary>
		/// Gets or sets the matched resource.
		/// </summary>
		public Resource Resource { get; set; }
#nullable enable

		/// <inheritdoc cref="SearchResult.Reference"/>
		public override object Reference => ValueTuple.Create(Resource, Name);
	}

	/// <summary>
	/// Search result that represents an assembly or module-level match.
	/// </summary>
	public class AssemblySearchResult : SearchResult
	{
#nullable disable
		/// <summary>
		/// Gets or sets the matched module.
		/// </summary>
		public MetadataFile Module { get; set; }

		/// <inheritdoc cref="SearchResult.Reference"/>
		public override object Reference => Module;
#nullable enable
	}

	/// <summary>
	/// Search result that represents a matched namespace.
	/// </summary>
	public class NamespaceSearchResult : SearchResult
	{
#nullable disable
		/// <summary>
		/// Gets or sets the matched namespace.
		/// </summary>
		public INamespace Namespace { get; set; }

		/// <inheritdoc cref="SearchResult.Reference"/>
		public override object Reference => Namespace;
#nullable enable
	}
}

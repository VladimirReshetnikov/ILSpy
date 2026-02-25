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
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX.Abstractions;

namespace ICSharpCode.ILSpyX.Search
{
	/// <summary>
	/// Searches embedded resources by traversing the resource tree built for a module.
	/// </summary>
	public class ResourceSearchStrategy : AbstractSearchStrategy
	{
		protected readonly bool searchInside;
		protected readonly ApiVisibility apiVisibility;
		protected readonly ITreeNodeFactory treeNodeFactory;

		/// <summary>
		/// Creates a strategy that matches resource names while honoring API visibility rules.
		/// </summary>
		/// <param name="apiVisibility">Visibility filter applied to manifest resources.</param>
		/// <param name="request">Search configuration and shared collaborators.</param>
		/// <param name="resultQueue">Queue that receives discovered results.</param>
		public ResourceSearchStrategy(ApiVisibility apiVisibility, SearchRequest request, IProducerConsumerCollection<SearchResult> resultQueue)
			: base(request, resultQueue)
		{
			this.treeNodeFactory = request.TreeNodeFactory;
			this.apiVisibility = apiVisibility;
			this.searchInside = true;
		}

		/// <summary>
		/// Determines whether a resource passes the configured <see cref="ApiVisibility"/> filter.
		/// </summary>
		/// <param name="resource">The resource to evaluate.</param>
		/// <returns><see langword="true"/> if the resource should be searched; otherwise, <see langword="false"/>.</returns>
		protected bool CheckVisibility(Resource resource)
		{
			if (apiVisibility == ApiVisibility.All)
				return true;

			if (apiVisibility == ApiVisibility.PublicOnly && (resource.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private)
				return false;

			return true;
		}

		/// <summary>
		/// Searches the module's resources and nested resource children for matching names.
		/// </summary>
		/// <param name="module">The module whose resources are searched.</param>
		/// <param name="cancellationToken">Token used to cancel the search.</param>
		public override void Search(MetadataFile module, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var resourcesNode = treeNodeFactory.CreateResourcesList(module);

			foreach (Resource resource in module.Resources)
				Search(module, resource, resourcesNode, treeNodeFactory.Create(resource), cancellationToken);
		}

		void Search(MetadataFile module, Resource resource, ITreeNode parent, ITreeNode node, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (node is IResourcesFileTreeNode treeNode)
			{
				if (!CheckVisibility(treeNode.Resource))
					return;
				resource = treeNode.Resource;
			}

			if (node.Text is string s && IsMatch(s))
				OnFoundResult(module, resource, node, parent);

			if (!searchInside)
				return;

			node.EnsureLazyChildren();
			foreach (var child in node.Children)
				Search(module, resource, node, child, cancellationToken);
		}

		void OnFoundResult(MetadataFile module, Resource resource, ITreeNode node, ITreeNode parent)
		{
			OnFoundResult(searchRequest.SearchResultFactory.Create(module, resource, node, parent));
		}
	}
}

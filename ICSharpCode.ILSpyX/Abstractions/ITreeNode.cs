// Copyright (c) 2022 Siegfried Pammer
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

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.ILSpyX.Abstractions
{
	/// <summary>
	/// Describes a tree node used by ILSpyX search abstractions.
	/// </summary>
	public interface ITreeNode
	{
		/// <summary>
		/// Gets the display text shown for this node.
		/// </summary>
		object? Text { get; }

		/// <summary>
		/// Gets the icon associated with this node.
		/// </summary>
		object? Icon { get; }

		/// <summary>
		/// Gets child nodes, materializing them lazily after <see cref="EnsureLazyChildren"/> is called.
		/// </summary>
		IEnumerable<ITreeNode> Children { get; }

		/// <summary>
		/// Ensures that deferred child creation has run so <see cref="Children"/> can be enumerated.
		/// </summary>
		void EnsureLazyChildren();
	}

	/// <summary>
	/// Represents a tree node that corresponds to a concrete resource file entry.
	/// </summary>
	public interface IResourcesFileTreeNode : ITreeNode
	{
		/// <summary>
		/// Gets the resource represented by this node.
		/// </summary>
		Resource Resource { get; }
	}

	/// <summary>
	/// Creates abstraction-level tree nodes used by search strategies.
	/// </summary>
	public interface ITreeNodeFactory
	{
		/// <summary>
		/// Creates the synthetic root node that contains all resources for a module.
		/// </summary>
		/// <param name="module">The module whose resource container node should be created.</param>
		/// <returns>A tree node that acts as parent for per-resource nodes.</returns>
		ITreeNode CreateResourcesList(MetadataFile module);

		/// <summary>
		/// Creates a tree node for an individual resource.
		/// </summary>
		/// <param name="resource">The resource to wrap in a tree node.</param>
		/// <returns>A tree node representing <paramref name="resource"/>.</returns>
		ITreeNode Create(Resource resource);
	}
}

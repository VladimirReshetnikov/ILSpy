// Copyright (c) 2014 Daniel Grunwald
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
#nullable enable

using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Maintains disjoint sets and supports near-constant-time union/find operations.
	/// </summary>
	/// <typeparam name="T">Element type used as the logical identity of each set member.</typeparam>
	/// <remarks>
	/// <para>
	/// This implementation combines path compression and union-by-rank. It is used by IL transforms to group related instructions/variables without
	/// repeatedly traversing entire equivalence classes.
	/// </para>
	/// <para>
	/// Elements are added lazily: calling <see cref="Find"/> or <see cref="Merge"/> on an unseen value implicitly creates a singleton set for it.
	/// </para>
	/// </remarks>
	public class UnionFind<T> where T : notnull
	{
		Dictionary<T, Node> mapping;

		class Node
		{
			public int rank;
			public Node parent;
			public T value;

			internal Node(T value)
			{
				this.value = value;
				this.parent = this;
			}
		}

		/// <summary>
		/// Initializes an empty disjoint-set structure.
		/// </summary>
		public UnionFind()
		{
			mapping = new Dictionary<T, Node>();
		}

		Node GetNode(T element)
		{
			if (!mapping.TryGetValue(element, out Node? node))
			{
				node = new Node(element);
				node.parent = node;
				mapping.Add(element, node);
			}
			return node;
		}

		/// <summary>
		/// Finds the representative element of the set containing <paramref name="element"/>.
		/// </summary>
		/// <param name="element">Element whose set representative should be returned.</param>
		/// <returns>The canonical representative value for <paramref name="element"/>'s set.</returns>
		/// <remarks>
		/// This operation applies path compression, flattening traversed parent links to speed up subsequent lookups.
		/// </remarks>
		public T Find(T element)
		{
			return FindRoot(GetNode(element)).value;
		}

		Node FindRoot(Node node)
		{
			if (node.parent != node)
				node.parent = FindRoot(node.parent);
			return node.parent;
		}

		/// <summary>
		/// Unions the sets containing <paramref name="a"/> and <paramref name="b"/>.
		/// </summary>
		/// <param name="a">First element.</param>
		/// <param name="b">Second element.</param>
		/// <remarks>
		/// If both elements are already in the same set, this method does nothing. Otherwise it attaches the lower-rank root under the higher-rank root,
		/// incrementing rank when both roots have equal rank.
		/// </remarks>
		public void Merge(T a, T b)
		{
			var rootA = FindRoot(GetNode(a));
			var rootB = FindRoot(GetNode(b));
			if (rootA == rootB)
				return;
			if (rootA.rank < rootB.rank)
				rootA.parent = rootB;
			else if (rootA.rank > rootB.rank)
				rootB.parent = rootA;
			else
			{
				rootB.parent = rootA;
				rootA.rank++;
			}
		}
	}
}

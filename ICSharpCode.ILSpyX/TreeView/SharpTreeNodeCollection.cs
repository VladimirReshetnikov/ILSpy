// Copyright (c) 2020 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

#nullable disable

namespace ICSharpCode.ILSpyX.TreeView
{
	/// <summary>
	/// Collection that validates that inserted nodes do not have another parent.
	/// </summary>
	public sealed class SharpTreeNodeCollection : IList<SharpTreeNode>, INotifyCollectionChanged
	{
		readonly SharpTreeNode parent;
		List<SharpTreeNode> list = new List<SharpTreeNode>();
		bool isRaisingEvent;

		/// <summary>
		/// Initializes a new collection for a specific parent node.
		/// </summary>
		/// <param name="parent">The owning parent node that receives child-change callbacks.</param>
		public SharpTreeNodeCollection(SharpTreeNode parent)
		{
			this.parent = parent;
		}

		/// <summary>
		/// Occurs after the collection mutates and the parent processed the change.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			Debug.Assert(!isRaisingEvent);
			isRaisingEvent = true;
			try
			{
				parent.OnChildrenChanged(e);
				CollectionChanged?.Invoke(this, e);
			}
			finally
			{
				isRaisingEvent = false;
			}
		}

		void ThrowOnReentrancy()
		{
			if (isRaisingEvent)
				throw new InvalidOperationException();
		}

		void ThrowIfValueIsNullOrHasParent(SharpTreeNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (node.modelParent != null)
				throw new ArgumentException("The node already has a parent", "node");
		}

		/// <summary>
		/// Gets or sets the child node at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Zero-based child index.</param>
		/// <returns>The child node at the specified index.</returns>
		/// <exception cref="ArgumentNullException">A <see langword="null"/> node is assigned.</exception>
		/// <exception cref="ArgumentException">The assigned node already belongs to another parent.</exception>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public SharpTreeNode this[int index] {
			get {
				return list[index];
			}
			set {
				ThrowOnReentrancy();
				var oldItem = list[index];
				if (oldItem == value)
					return;
				ThrowIfValueIsNullOrHasParent(value);
				list[index] = value;
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
			}
		}

		/// <summary>
		/// Gets the number of child nodes.
		/// </summary>
		public int Count {
			get { return list.Count; }
		}

		bool ICollection<SharpTreeNode>.IsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Returns the index of <paramref name="node"/> if it is a direct child of the owner.
		/// </summary>
		/// <param name="node">The node to locate.</param>
		/// <returns>The zero-based index when found; otherwise <c>-1</c>.</returns>
		public int IndexOf(SharpTreeNode node)
		{
			if (node == null || node.modelParent != parent)
				return -1;
			else
				return list.IndexOf(node);
		}

		/// <summary>
		/// Inserts a child node at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Insertion index.</param>
		/// <param name="node">Node to insert.</param>
		/// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="node"/> already has a parent.</exception>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void Insert(int index, SharpTreeNode node)
		{
			ThrowOnReentrancy();
			ThrowIfValueIsNullOrHasParent(node);
			list.Insert(index, node);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index));
		}

		/// <summary>
		/// Inserts multiple child nodes starting at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Insertion index.</param>
		/// <param name="nodes">Nodes to insert in order.</param>
		/// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <see langword="null"/> or contains <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Any inserted node already has a parent.</exception>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void InsertRange(int index, IEnumerable<SharpTreeNode> nodes)
		{
			if (nodes == null)
				throw new ArgumentNullException("nodes");
			ThrowOnReentrancy();
			List<SharpTreeNode> newNodes = nodes.ToList();
			if (newNodes.Count == 0)
				return;
			foreach (SharpTreeNode node in newNodes)
			{
				ThrowIfValueIsNullOrHasParent(node);
			}
			list.InsertRange(index, newNodes);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newNodes, index));
		}

		/// <summary>
		/// Removes the child node at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">Zero-based child index.</param>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void RemoveAt(int index)
		{
			ThrowOnReentrancy();
			var oldItem = list[index];
			list.RemoveAt(index);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
		}

		/// <summary>
		/// Removes a contiguous range of child nodes.
		/// </summary>
		/// <param name="index">Index of the first node to remove.</param>
		/// <param name="count">Number of nodes to remove.</param>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void RemoveRange(int index, int count)
		{
			ThrowOnReentrancy();
			if (count == 0)
				return;
			var oldItems = list.GetRange(index, count);
			list.RemoveRange(index, count);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index));
		}

		/// <summary>
		/// Appends a child node.
		/// </summary>
		/// <param name="node">Node to append.</param>
		/// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="node"/> already has a parent.</exception>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void Add(SharpTreeNode node)
		{
			ThrowOnReentrancy();
			ThrowIfValueIsNullOrHasParent(node);
			list.Add(node);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, list.Count - 1));
		}

		/// <summary>
		/// Appends multiple child nodes.
		/// </summary>
		/// <param name="nodes">Nodes to append in order.</param>
		public void AddRange(IEnumerable<SharpTreeNode> nodes)
		{
			InsertRange(this.Count, nodes);
		}

		/// <summary>
		/// Removes all children from the parent node.
		/// </summary>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void Clear()
		{
			ThrowOnReentrancy();
			var oldList = list;
			list = new List<SharpTreeNode>();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldList, 0));
		}

		/// <summary>
		/// Determines whether <paramref name="node"/> is a direct child of the owner.
		/// </summary>
		/// <param name="node">Node to locate.</param>
		/// <returns><see langword="true"/> if present; otherwise <see langword="false"/>.</returns>
		public bool Contains(SharpTreeNode node)
		{
			return IndexOf(node) >= 0;
		}

		/// <summary>
		/// Copies child nodes to an array.
		/// </summary>
		/// <param name="array">Destination array.</param>
		/// <param name="arrayIndex">Starting index in <paramref name="array"/>.</param>
		public void CopyTo(SharpTreeNode[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the first matching child node.
		/// </summary>
		/// <param name="item">Node to remove.</param>
		/// <returns><see langword="true"/> if a node was removed; otherwise <see langword="false"/>.</returns>
		public bool Remove(SharpTreeNode item)
		{
			int pos = IndexOf(item);
			if (pos >= 0)
			{
				RemoveAt(pos);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Returns an enumerator over the current child list.
		/// </summary>
		/// <returns>An enumerator of child nodes.</returns>
		public IEnumerator<SharpTreeNode> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}

		/// <summary>
		/// Removes all child nodes that match <paramref name="match"/>.
		/// </summary>
		/// <param name="match">Predicate that returns <see langword="true"/> for nodes to remove.</param>
		/// <exception cref="ArgumentNullException"><paramref name="match"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">The collection is being modified reentrantly from a change callback.</exception>
		public void RemoveAll(Predicate<SharpTreeNode> match)
		{
			if (match == null)
				throw new ArgumentNullException("match");
			ThrowOnReentrancy();
			int firstToRemove = 0;
			for (int i = 0; i < list.Count; i++)
			{
				bool removeNode;
				isRaisingEvent = true;
				try
				{
					removeNode = match(list[i]);
				}
				finally
				{
					isRaisingEvent = false;
				}
				if (!removeNode)
				{
					if (firstToRemove < i)
					{
						RemoveRange(firstToRemove, i - firstToRemove);
						i = firstToRemove - 1;
					}
					else
					{
						firstToRemove = i + 1;
					}
					Debug.Assert(firstToRemove == i + 1);
				}
			}
			if (firstToRemove < list.Count)
			{
				RemoveRange(firstToRemove, list.Count - firstToRemove);
			}
		}
	}
}

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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

#nullable disable

namespace ICSharpCode.ILSpyX.TreeView
{
	/// <summary>
	/// Exposes the visible projection of a <see cref="SharpTreeNode"/> hierarchy as an indexable list.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The tree model remains authoritative; this type only mirrors currently visible nodes and forwards
	/// add/remove notifications to consumers such as UI item controls.
	/// </para>
	/// <para>
	/// The constructor binds the flattener to the list-root used by <see cref="SharpTreeNode"/>'s internal
	/// AVL-based visible-node index. Call <see cref="Stop"/> when replacing the model root.
	/// </para>
	/// </remarks>
	public sealed class TreeFlattener : IList, INotifyCollectionChanged
	{
		/// <summary>
		/// The root node of the flat list tree.
		/// Tjis is not necessarily the root of the model!
		/// </summary>
		internal SharpTreeNode root;
		readonly bool includeRoot;
		readonly object syncRoot = new object();

		/// <summary>
		/// Initializes a new flattener for the tree rooted at <paramref name="modelRoot"/>.
		/// </summary>
		/// <param name="modelRoot">Any node within the tree to flatten.</param>
		/// <param name="includeRoot"><see langword="true"/> to expose the root node itself as list item 0; otherwise only descendants are listed.</param>
		public TreeFlattener(SharpTreeNode modelRoot, bool includeRoot)
		{
			this.root = modelRoot;
			while (root.listParent != null)
				root = root.listParent;
			root.treeFlattener = this;
			this.includeRoot = includeRoot;
		}

		/// <summary>
		/// Occurs when the visible-node projection changes.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raises <see cref="CollectionChanged"/> with a pre-built event payload.
		/// </summary>
		/// <param name="e">The collection change event arguments.</param>
		public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Publishes add notifications for newly visible nodes.
		/// </summary>
		/// <param name="index">Visible index of the first inserted node in root-inclusive coordinates.</param>
		/// <param name="nodes">Sequence of visible nodes inserted contiguously.</param>
		public void NodesInserted(int index, IEnumerable<SharpTreeNode> nodes)
		{
			if (!includeRoot)
				index--;
			foreach (SharpTreeNode node in nodes)
			{
				RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index++));
			}
		}

		/// <summary>
		/// Publishes remove notifications for nodes that are no longer visible.
		/// </summary>
		/// <param name="index">Visible index at which removal starts in root-inclusive coordinates.</param>
		/// <param name="nodes">Sequence of visible nodes removed from the projection.</param>
		public void NodesRemoved(int index, IEnumerable<SharpTreeNode> nodes)
		{
			if (!includeRoot)
				index--;
			foreach (SharpTreeNode node in nodes)
			{
				RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, index));
			}
		}

		/// <summary>
		/// Detaches this flattener from the underlying tree.
		/// </summary>
		public void Stop()
		{
			Debug.Assert(root.treeFlattener == this);
			root.treeFlattener = null;
		}

		/// <summary>
		/// Gets the visible node at the specified list index.
		/// </summary>
		/// <param name="index">Zero-based visible index in this projection.</param>
		/// <returns>The <see cref="SharpTreeNode"/> at <paramref name="index"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the range of visible nodes.</exception>
		public object this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException();
				return SharpTreeNode.GetNodeByVisibleIndex(root, includeRoot ? index : index + 1);
			}
			set {
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the number of visible nodes in this projection.
		/// </summary>
		public int Count {
			get {
				return includeRoot ? root.GetTotalListLength() : root.GetTotalListLength() - 1;
			}
		}

		/// <summary>
		/// Returns the visible index of <paramref name="item"/>.
		/// </summary>
		/// <param name="item">The candidate node.</param>
		/// <returns>The zero-based index when <paramref name="item"/> is visible in this projection; otherwise <c>-1</c>.</returns>
		public int IndexOf(object item)
		{
			SharpTreeNode node = item as SharpTreeNode;
			if (node != null && node.IsVisible && node.GetListRoot() == root)
			{
				if (includeRoot)
					return SharpTreeNode.GetVisibleIndexForNode(node);
				else
					return SharpTreeNode.GetVisibleIndexForNode(node) - 1;
			}
			else
			{
				return -1;
			}
		}

		bool IList.IsReadOnly {
			get { return true; }
		}

		bool IList.IsFixedSize {
			get { return false; }
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get {
				return syncRoot;
			}
		}

		void IList.Insert(int index, object item)
		{
			throw new NotSupportedException();
		}

		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		int IList.Add(object item)
		{
			throw new NotSupportedException();
		}

		void IList.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Determines whether the specified object is currently visible in this projection.
		/// </summary>
		/// <param name="item">The object to locate.</param>
		/// <returns><see langword="true"/> if <paramref name="item"/> is visible; otherwise <see langword="false"/>.</returns>
		public bool Contains(object item)
		{
			return IndexOf(item) >= 0;
		}

		/// <summary>
		/// Copies the visible nodes into <paramref name="array"/> beginning at <paramref name="arrayIndex"/>.
		/// </summary>
		/// <param name="array">Destination array.</param>
		/// <param name="arrayIndex">Starting index in <paramref name="array"/>.</param>
		public void CopyTo(Array array, int arrayIndex)
		{
			foreach (object item in this)
				array.SetValue(item, arrayIndex++);
		}

		void IList.Remove(object item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns an enumerator over the visible nodes in display order.
		/// </summary>
		/// <returns>An enumerator that yields <see cref="SharpTreeNode"/> instances.</returns>
		public IEnumerator GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++)
			{
				yield return this[i];
			}
		}
	}
}

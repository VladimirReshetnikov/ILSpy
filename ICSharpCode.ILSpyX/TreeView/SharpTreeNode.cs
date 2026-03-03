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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpyX.TreeView
{
	/// <summary>
	/// Represents a node in the cross-platform ILSpy tree model.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The node is UI-agnostic and surfaces state used by both WPF and cross-platform hosts
	/// (selection, expansion, drag/drop, and lazy-loading semantics).
	/// </para>
	/// <para>
	/// Visible-node indexing is maintained by an internal flat-list structure implemented in another
	/// partial declaration. Consumers usually interact through <see cref="Children"/>, <see cref="IsExpanded"/>,
	/// and traversal helpers such as <see cref="Descendants"/>.
	/// </para>
	/// </remarks>
	public partial class SharpTreeNode : INotifyPropertyChanged
	{
		/// <summary>
		/// Installs the image provider used by tree nodes to resolve icon keys.
		/// </summary>
		/// <param name="provider">The provider instance used by subsequent icon lookups.</param>
		[AllowNull]
		protected static ITreeNodeImagesProvider ImagesProvider { get; private set; }
		/// <summary>
		/// Sets the global image provider used by all <see cref="SharpTreeNode"/> instances.
		/// </summary>
		/// <param name="provider">Provider that maps icon identifiers to platform images.</param>
		public static void SetImagesProvider(ITreeNodeImagesProvider provider) => ImagesProvider = provider;

		SharpTreeNodeCollection? modelChildren;
		internal SharpTreeNode? modelParent;
		bool isVisible = true;

		void UpdateIsVisible(bool parentIsVisible, bool updateFlattener)
		{
			bool newIsVisible = parentIsVisible && !isHidden;
			if (isVisible != newIsVisible)
			{
				isVisible = newIsVisible;

				// invalidate the augmented data
				SharpTreeNode node = this;
				while (node != null && node.totalListLength >= 0)
				{
					node.totalListLength = -1;
					node = node.listParent;
				}
				// Remember the removed nodes:
				List<SharpTreeNode>? removedNodes = null;
				if (updateFlattener && !newIsVisible)
				{
					removedNodes = VisibleDescendantsAndSelf().ToList();
				}
				// also update the model children:
				UpdateChildIsVisible(false);

				// Validate our invariants:
				if (updateFlattener)
					CheckRootInvariants();

				// Tell the flattener about the removed nodes:
				if (removedNodes != null)
				{
					var flattener = GetListRoot().treeFlattener;
					if (flattener != null)
					{
						flattener.NodesRemoved(GetVisibleIndexForNode(this), removedNodes);
						foreach (var n in removedNodes)
							n.OnIsVisibleChanged();
					}
				}
				// Tell the flattener about the new nodes:
				if (updateFlattener && newIsVisible)
				{
					var flattener = GetListRoot().treeFlattener;
					if (flattener != null)
					{
						flattener.NodesInserted(GetVisibleIndexForNode(this), VisibleDescendantsAndSelf());
						foreach (var n in VisibleDescendantsAndSelf())
							n.OnIsVisibleChanged();
					}
				}
			}
		}

		protected virtual void OnIsVisibleChanged() { }

		void UpdateChildIsVisible(bool updateFlattener)
		{
			if (modelChildren != null && modelChildren.Count > 0)
			{
				bool showChildren = isVisible && isExpanded;
				foreach (SharpTreeNode child in modelChildren)
				{
					child.UpdateIsVisible(showChildren, updateFlattener);
				}
			}
		}

		#region Main

		/// <summary>
		/// Initializes a new node with no parent and no children.
		/// </summary>
		public SharpTreeNode()
		{
		}

		/// <summary>
		/// Gets the mutable child collection.
		/// </summary>
		public SharpTreeNodeCollection Children {
			get {
				if (modelChildren == null)
					modelChildren = new SharpTreeNodeCollection(this);
				return modelChildren;
			}
		}

		/// <summary>
		/// Gets the current model parent, or <see langword="null"/> for the root.
		/// </summary>
		public SharpTreeNode? Parent {
			get { return modelParent; }
		}

		/// <summary>
		/// Gets display text for the node.
		/// </summary>
		public virtual object? Text {
			get { return null; }
		}

		/// <summary>
		/// Gets navigation text used by keyboard search and accessibility fallbacks.
		/// </summary>
		public virtual object? NavigationText {
			get { return Text; }
		}

		/// <summary>
		/// Gets the icon payload rendered by the host.
		/// </summary>
		public virtual object? Icon {
			get { return null; }
		}

		/// <summary>
		/// Gets optional tooltip content.
		/// </summary>
		public virtual object? ToolTip {
			get { return null; }
		}

		/// <summary>
		/// Gets optional background brush object interpreted by the host UI.
		/// </summary>
		public virtual object? Background {
			get { return null; }
		}

		/// <summary>
		/// Gets optional foreground brush object interpreted by the host UI.
		/// </summary>
		public virtual object? Foreground {
			get { return null; }
		}

		/// <summary>
		/// Gets the nesting depth, where root is <c>0</c>.
		/// </summary>
		public int Level {
			get { return Parent != null ? Parent.Level + 1 : 0; }
		}

		/// <summary>
		/// Gets whether this node has no parent.
		/// </summary>
		public bool IsRoot {
			get { return Parent == null; }
		}

		bool isHidden;

		/// <summary>
		/// Gets or sets whether this node is hidden from visible traversal and flattening.
		/// </summary>
		public bool IsHidden {
			get { return isHidden; }
			set {
				if (isHidden != value)
				{
					isHidden = value;
					if (modelParent != null)
						UpdateIsVisible(modelParent.isVisible && modelParent.isExpanded, true);
					RaisePropertyChanged(nameof(IsHidden));
					if (Parent != null)
						Parent.RaisePropertyChanged(nameof(ShowExpander));
				}
			}
		}

		/// <summary>
		/// Return true when this node is not hidden and when all parent nodes are expanded and not hidden.
		/// </summary>
		public bool IsVisible {
			get { return isVisible; }
		}

		bool isSelected;

		/// <summary>
		/// Gets or sets whether the node is selected in the host control.
		/// </summary>
		public bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected != value)
				{
					isSelected = value;
					RaisePropertyChanged(nameof(IsSelected));
				}
			}
		}

		#endregion

		#region OnParentChanged / OnChildrenChanged
		/// <summary>
		/// Called after the node's parent reference changes.
		/// </summary>
		public virtual void OnParentChanged()
		{ }

		/// <summary>
		/// Called whenever children are added, removed, replaced, or reset.
		/// </summary>
		/// <param name="e">Collection change details from <see cref="Children"/>.</param>
		public virtual void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (SharpTreeNode node in e.OldItems)
				{
					Debug.Assert(node.modelParent == this);
					node.modelParent = null;
					node.OnParentChanged();
					Debug.WriteLine("Removing {0} from {1}", node, this);
					SharpTreeNode removeEnd = node;
					while (removeEnd.modelChildren != null && removeEnd.modelChildren.Count > 0)
						removeEnd = removeEnd.modelChildren.Last();

					List<SharpTreeNode>? removedNodes = null;
					int visibleIndexOfRemoval = 0;
					if (node.isVisible)
					{
						visibleIndexOfRemoval = GetVisibleIndexForNode(node);
						removedNodes = node.VisibleDescendantsAndSelf().ToList();
					}

					RemoveNodes(node, removeEnd);

					if (removedNodes != null)
					{
						var flattener = GetListRoot().treeFlattener;
						if (flattener != null)
						{
							flattener.NodesRemoved(visibleIndexOfRemoval, removedNodes);
						}
					}
				}
			}
			if (e.NewItems != null)
			{
				SharpTreeNode? insertionPos;
				if (e.NewStartingIndex == 0)
					insertionPos = null;
				else
					insertionPos = modelChildren?[e.NewStartingIndex - 1];

				foreach (SharpTreeNode node in e.NewItems)
				{
					Debug.Assert(node.modelParent == null);
					node.modelParent = this;
					node.OnParentChanged();
					node.UpdateIsVisible(isVisible && isExpanded, false);
					//Debug.WriteLine("Inserting {0} after {1}", node, insertionPos);

					while (insertionPos != null && insertionPos.modelChildren != null && insertionPos.modelChildren.Count > 0)
					{
						insertionPos = insertionPos.modelChildren.Last();
					}
					InsertNodeAfter(insertionPos ?? this, node);

					insertionPos = node;
					if (node.isVisible)
					{
						var flattener = GetListRoot().treeFlattener;
						if (flattener != null)
						{
							flattener.NodesInserted(GetVisibleIndexForNode(node), node.VisibleDescendantsAndSelf());
						}
					}
				}
			}

			RaisePropertyChanged(nameof(ShowExpander));
			RaiseIsLastChangedIfNeeded(e);
		}
		#endregion

		#region Expanding / LazyLoading

		/// <summary>
		/// Gets icon content used while <see cref="IsExpanded"/> is <see langword="true"/>.
		/// </summary>
		public virtual object? ExpandedIcon {
			get { return Icon; }
		}

		/// <summary>
		/// Gets whether an expander should be shown for this node.
		/// </summary>
		public virtual bool ShowExpander {
			get { return LazyLoading || Children.Any(c => !c.isHidden); }
		}

		bool isExpanded;

		/// <summary>
		/// Gets or sets whether children are currently expanded.
		/// </summary>
		public bool IsExpanded {
			get { return isExpanded; }
			set {
				if (isExpanded != value)
				{
					isExpanded = value;
					if (isExpanded)
					{
						EnsureLazyChildren();
						OnExpanding();
					}
					else
					{
						OnCollapsing();
					}
					UpdateChildIsVisible(true);
					RaisePropertyChanged(nameof(IsExpanded));
				}
			}
		}

		protected virtual void OnExpanding() { }
		protected virtual void OnCollapsing() { }

		bool lazyLoading;

		/// <summary>
		/// Gets or sets whether children are loaded on first expansion.
		/// </summary>
		public bool LazyLoading {
			get { return lazyLoading; }
			set {
				lazyLoading = value;
				if (lazyLoading)
				{
					IsExpanded = false;
					if (canExpandRecursively)
					{
						canExpandRecursively = false;
						RaisePropertyChanged(nameof(CanExpandRecursively));
					}
				}
				RaisePropertyChanged(nameof(LazyLoading));
				RaisePropertyChanged(nameof(ShowExpander));
			}
		}

		bool canExpandRecursively = true;

		/// <summary>
		/// Gets whether this node can participate in recursive expand operations.
		/// </summary>
		public virtual bool CanExpandRecursively {
			get { return canExpandRecursively; }
		}

		/// <summary>
		/// Gets whether the host should render an icon column for this node.
		/// </summary>
		public virtual bool ShowIcon {
			get { return Icon != null; }
		}

		protected virtual void LoadChildren()
		{
			throw new NotSupportedException(GetType().Name + " does not support lazy loading");
		}

		/// <summary>
		/// Ensures children are loaded when lazy loading is enabled.
		/// </summary>
		public void EnsureLazyChildren()
		{
			if (LazyLoading)
			{
				LazyLoading = false;
				LoadChildren();
			}
		}

		#endregion

		#region Ancestors / Descendants

		/// <summary>
		/// Enumerates all descendants in pre-order.
		/// </summary>
		/// <returns>A pre-order sequence of descendants.</returns>
		public IEnumerable<SharpTreeNode> Descendants()
		{
			return TreeTraversal.PreOrder(this.Children, n => n.Children);
		}

		/// <summary>
		/// Enumerates this node and all descendants in pre-order.
		/// </summary>
		/// <returns>A pre-order sequence including this node.</returns>
		public IEnumerable<SharpTreeNode> DescendantsAndSelf()
		{
			return TreeTraversal.PreOrder(this, n => n.Children);
		}

		internal IEnumerable<SharpTreeNode> VisibleDescendants()
		{
			return TreeTraversal.PreOrder(this.Children.Where(c => c.isVisible), n => n.Children.Where(c => c.isVisible));
		}

		/// <summary>
		/// Enumerates visible descendants in pre-order.
		/// </summary>
		/// <returns>A pre-order sequence of currently visible descendants.</returns>
		public IEnumerable<SharpTreeNode> VisibleDescendantsAndSelf()
		{
			return TreeTraversal.PreOrder(this, n => n.Children.Where(c => c.isVisible));
		}

		/// <summary>
		/// Enumerates ancestors, starting at the parent and moving upward.
		/// </summary>
		/// <returns>An upward sequence that excludes this node.</returns>
		public IEnumerable<SharpTreeNode> Ancestors()
		{
			for (SharpTreeNode? n = this.Parent; n != null; n = n.Parent)
				yield return n;
		}

		/// <summary>
		/// Enumerates this node followed by its ancestors.
		/// </summary>
		/// <returns>An upward sequence including this node.</returns>
		public IEnumerable<SharpTreeNode> AncestorsAndSelf()
		{
			for (SharpTreeNode? n = this; n != null; n = n.Parent)
				yield return n;
		}

		#endregion

		#region Editing

		/// <summary>
		/// Gets whether inline rename/edit is supported.
		/// </summary>
		public virtual bool IsEditable {
			get { return false; }
		}

		bool isEditing;

		/// <summary>
		/// Gets or sets whether the host is currently editing this node.
		/// </summary>
		public bool IsEditing {
			get { return isEditing; }
			set {
				if (isEditing != value)
				{
					isEditing = value;
					RaisePropertyChanged(nameof(IsEditing));
				}
			}
		}

		/// <summary>
		/// Loads the initial text for inline editing.
		/// </summary>
		/// <returns>The editable text, or <see langword="null"/> to keep the editor empty.</returns>
		public virtual string? LoadEditText()
		{
			return null;
		}

		/// <summary>
		/// Commits inline edit text.
		/// </summary>
		/// <param name="value">The edited value entered by the user.</param>
		/// <returns><see langword="true"/> if the edit was accepted; otherwise <see langword="false"/>.</returns>
		public virtual bool SaveEditText(string value)
		{
			return true;
		}

		#endregion

		#region Checkboxes

		/// <summary>
		/// Gets whether this node exposes a checkbox.
		/// </summary>
		public virtual bool IsCheckable {
			get { return false; }
		}

		bool? isChecked;

		/// <summary>
		/// Gets or sets the checkbox state.
		/// </summary>
		/// <remarks>
		/// Setting this value propagates to checkable descendants and recomputes tri-state values for checkable ancestors.
		/// </remarks>
		public bool? IsChecked {
			get { return isChecked; }
			set {
				SetIsChecked(value, true);
			}
		}

		void SetIsChecked(bool? value, bool update)
		{
			if (isChecked != value)
			{
				isChecked = value;

				if (update)
				{
					if (IsChecked != null)
					{
						foreach (var child in Descendants())
						{
							if (child.IsCheckable)
							{
								child.SetIsChecked(IsChecked, false);
							}
						}
					}

					foreach (var parent in Ancestors())
					{
						if (parent.IsCheckable)
						{
							if (!parent.TryValueForIsChecked(true))
							{
								if (!parent.TryValueForIsChecked(false))
								{
									parent.SetIsChecked(null, false);
								}
							}
						}
					}
				}

				RaisePropertyChanged(nameof(IsChecked));
			}
		}

		bool TryValueForIsChecked(bool? value)
		{
			if (Children.Where(n => n.IsCheckable).All(n => n.IsChecked == value))
			{
				SetIsChecked(value, false);
				return true;
			}
			return false;
		}

		#endregion

		#region Cut / Copy / Paste / Delete

		/// <summary>
		/// Gets whether the node is in a cut state.
		/// </summary>
		public bool IsCut { get { return false; } }
		/*
			static List<SharpTreeNode> cuttedNodes = new List<SharpTreeNode>();
			static IDataObject cuttedData;
			static EventHandler requerySuggestedHandler; // for weak event
	
			static void StartCuttedDataWatcher()
			{
				requerySuggestedHandler = new EventHandler(CommandManager_RequerySuggested);
				CommandManager.RequerySuggested += requerySuggestedHandler;
			}
	
			static void CommandManager_RequerySuggested(object sender, EventArgs e)
			{
				if (cuttedData != null && !Clipboard.IsCurrent(cuttedData)) {
					ClearCuttedData();
				}
			}
	
			static void ClearCuttedData()
			{
				foreach (var node in cuttedNodes) {
					node.IsCut = false;
				}
				cuttedNodes.Clear();
				cuttedData = null;
			}
	
			//static public IEnumerable<SharpTreeNode> PurifyNodes(IEnumerable<SharpTreeNode> nodes)
			//{
			//    var list = nodes.ToList();
			//    var array = list.ToArray();
			//    foreach (var node1 in array) {
			//        foreach (var node2 in array) {
			//            if (node1.Descendants().Contains(node2)) {
			//                list.Remove(node2);
			//            }
			//        }
			//    }
			//    return list;
			//}
	
			bool isCut;
	
			public bool IsCut
			{
				get { return isCut; }
				private set
				{
					isCut = value;
					RaisePropertyChanged("IsCut");
				}
			}
	
			internal bool InternalCanCut()
			{
				return InternalCanCopy() && InternalCanDelete();
			}
	
			internal void InternalCut()
			{
				ClearCuttedData();
				cuttedData = Copy(ActiveNodesArray);
				Clipboard.SetDataObject(cuttedData);
	
				foreach (var node in ActiveNodes) {
					node.IsCut = true;
					cuttedNodes.Add(node);
				}
			}
	
			internal bool InternalCanCopy()
			{
				return CanCopy(ActiveNodesArray);
			}
	
			internal void InternalCopy()
			{
				Clipboard.SetDataObject(Copy(ActiveNodesArray));
			}
	
			internal bool InternalCanPaste()
			{
				return CanPaste(Clipboard.GetDataObject());
			}
	
			internal void InternalPaste()
			{
				Paste(Clipboard.GetDataObject());
	
				if (cuttedData != null) {
					DeleteCore(cuttedNodes.ToArray());
					ClearCuttedData();
				}
			}
		 */

		/// <summary>
		/// Gets whether this node can be deleted.
		/// </summary>
		/// <returns><see langword="true"/> when <see cref="Delete"/> is supported for this node.</returns>
		public virtual bool CanDelete()
		{
			return false;
		}

		/// <summary>
		/// Deletes this node using host-facing delete semantics.
		/// </summary>
		/// <exception cref="NotSupportedException">The node type does not support deletion.</exception>
		public virtual void Delete()
		{
			throw new NotSupportedException(GetType().Name + " does not support deletion");
		}

		/// <summary>
		/// Deletes this node without additional host orchestration.
		/// </summary>
		/// <exception cref="NotSupportedException">The node type does not support deletion.</exception>
		public virtual void DeleteCore()
		{
			throw new NotSupportedException(GetType().Name + " does not support deletion");
		}

		/// <summary>
		/// Creates a platform data object for copy/drag operations.
		/// </summary>
		/// <param name="nodes">Nodes included in the payload.</param>
		/// <returns>A platform-specific data object.</returns>
		/// <exception cref="NotSupportedException">The node type does not support copy/drag operations.</exception>
		public virtual IPlatformDataObject Copy(SharpTreeNode[] nodes)
		{
			throw new NotSupportedException(GetType().Name + " does not support copy/paste or drag'n'drop");
		}

		/*
			public virtual bool CanCopy(SharpTreeNode[] nodes)
			{
				return false;
			}
	
			public virtual bool CanPaste(IDataObject data)
			{
				return false;
			}
	
			public virtual void Paste(IDataObject data)
			{
				EnsureLazyChildren();
				Drop(data, Children.Count, DropEffect.Copy);
			}
		 */
		#endregion

		#region Drag and Drop
		/// <summary>
		/// Gets whether the specified nodes can be dragged from this node context.
		/// </summary>
		/// <param name="nodes">The selected nodes considered for dragging.</param>
		/// <returns><see langword="true"/> if dragging is allowed; otherwise <see langword="false"/>.</returns>
		public virtual bool CanDrag(SharpTreeNode[] nodes)
		{
			return false;
		}

		/// <summary>
		/// Starts a drag operation for <paramref name="nodes"/>.
		/// </summary>
		/// <param name="dragSource">UI source object that initiated the drag.</param>
		/// <param name="nodes">Nodes included in the drag payload.</param>
		/// <param name="dragdropManager">Platform drag/drop manager.</param>
		public virtual void StartDrag(object dragSource, SharpTreeNode[] nodes, IPlatformDragDrop dragdropManager)
		{
			XPlatDragDropEffects effects = XPlatDragDropEffects.All;
			if (!nodes.All(n => n.CanDelete()))
				effects &= ~XPlatDragDropEffects.Move;

			XPlatDragDropEffects result = dragdropManager.DoDragDrop(dragSource, Copy(nodes), effects);
			if (result == XPlatDragDropEffects.Move)
			{
				foreach (SharpTreeNode node in nodes)
					node.DeleteCore();
			}
		}

		/// <summary>
		/// Determines whether a drag payload can be dropped at the specified child index.
		/// </summary>
		/// <param name="e">Platform drag event arguments.</param>
		/// <param name="index">Target insertion index under this node.</param>
		/// <returns><see langword="true"/> if a drop is allowed; otherwise <see langword="false"/>.</returns>
		public virtual bool CanDrop(IPlatformDragEventArgs e, int index)
		{
			return false;
		}

		/// <summary>
		/// Performs a drop operation after lazy children are materialized when required.
		/// </summary>
		/// <param name="e">Platform drag event arguments.</param>
		/// <param name="index">Target insertion index under this node.</param>
		public void InternalDrop(IPlatformDragEventArgs e, int index)
		{
			if (LazyLoading)
			{
				EnsureLazyChildren();
				index = Children.Count;
			}

			Drop(e, index);
		}

		/// <summary>
		/// Applies a drop payload at the specified child index.
		/// </summary>
		/// <param name="e">Platform drag event arguments.</param>
		/// <param name="index">Target insertion index under this node.</param>
		/// <exception cref="NotSupportedException">The node type does not support dropping.</exception>
		public virtual void Drop(IPlatformDragEventArgs e, int index)
		{
			throw new NotSupportedException(GetType().Name + " does not support Drop()");
		}
		#endregion

		#region IsLast (for TreeView lines)

		/// <summary>
		/// Gets whether this node is the last child within its parent.
		/// </summary>
		public bool IsLast {
			get {
				return Parent == null
					|| Parent.Children.Count == 0
					|| Parent.Children[^1] == this;
			}
		}

		void RaiseIsLastChangedIfNeeded(NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewStartingIndex == Children.Count - 1)
					{
						if (Children.Count > 1)
						{
							Children[Children.Count - 2].RaisePropertyChanged(nameof(IsLast));
						}
						Children[Children.Count - 1].RaisePropertyChanged(nameof(IsLast));
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					if (e.OldStartingIndex == Children.Count)
					{
						if (Children.Count > 0)
						{
							Children[Children.Count - 1].RaisePropertyChanged(nameof(IsLast));
						}
					}
					break;
			}
		}

		#endregion

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Occurs when one of this node's public properties changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/> for <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The property name.</param>
		public void RaisePropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}

		#endregion

		/// <summary>
		/// Gets called when the item is double-clicked.
		/// </summary>
		/// <param name="e">Platform event data for the activation gesture, forwarded by the tree host.</param>
		public virtual void ActivateItem(IPlatformRoutedEventArgs e)
		{
		}

		/// <summary>
		/// Gets called when the item is clicked with the middle mouse button.
		/// </summary>
		/// <param name="e">Platform event data for the secondary activation gesture.</param>
		public virtual void ActivateItemSecondary(IPlatformRoutedEventArgs e)
		{
		}

		/// <summary>
		/// Returns a text representation used by keyboard navigation and debug views.
		/// </summary>
		/// <returns><see cref="Text"/> converted to string, or <see cref="string.Empty"/> when no text is available.</returns>
		public override string? ToString()
		{
			// used for keyboard navigation
			object? text = this.Text;
			return text != null ? text.ToString() : string.Empty;
		}
	}
}

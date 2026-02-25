using System.Windows;

using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpy.Controls.TreeView
{
	/// <summary>
	/// WPF drag-and-drop bridge for <see cref="IPlatformDragDrop"/>.
	/// </summary>
	public class WpfWindowsDragDropManager : IPlatformDragDrop
	{
		/// <inheritdoc/>
		public XPlatDragDropEffects DoDragDrop(object dragSource, IPlatformDataObject data, XPlatDragDropEffects allowedEffects)
		{
			return (XPlatDragDropEffects)DragDrop.DoDragDrop(dragSource as DependencyObject, data.UnderlyingDataObject, (DragDropEffects)allowedEffects);
		}
	}
}

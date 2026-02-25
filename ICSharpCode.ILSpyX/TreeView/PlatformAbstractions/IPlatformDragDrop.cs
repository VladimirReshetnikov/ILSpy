namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Abstraction over platform drag-and-drop entry points.
	/// </summary>
	public interface IPlatformDragDrop
	{
		/// <summary>
		/// Starts a drag operation for tree nodes and returns the final effect selected by the drop target.
		/// </summary>
		/// <param name="dragSource">UI element that initiates the drag operation.</param>
		/// <param name="data">Data payload exposed to drop targets.</param>
		/// <param name="allowedEffects">Effects that the source permits the target to choose from.</param>
		/// <returns>The effect that was applied after the drag operation completed.</returns>
		XPlatDragDropEffects DoDragDrop(object dragSource, IPlatformDataObject data, XPlatDragDropEffects allowedEffects);
	}
}

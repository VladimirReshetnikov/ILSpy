namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Platform-neutral drag event data passed into tree node drag/drop handlers.
	/// </summary>
	public interface IPlatformDragEventArgs
	{
		/// <summary>
		/// Gets or sets the effect currently advertised for the drag operation.
		/// </summary>
		XPlatDragDropEffects Effects { get; set; }

		/// <summary>
		/// Gets the payload offered by the drag source.
		/// </summary>
		IPlatformDataObject Data { get; }
	}
}

namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Platform-neutral event data for routed UI events used by tree nodes.
	/// </summary>
	public interface IPlatformRoutedEventArgs
	{
		/// <summary>
		/// Gets or sets whether the routed event has been handled.
		/// </summary>
		bool Handled { get; set; }
	}
}

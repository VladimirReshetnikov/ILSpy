namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Provides image resources required by <see cref="TreeView.SharpTreeNode"/> implementations.
	/// </summary>
	public interface ITreeNodeImagesProvider
	{
		/// <summary>
		/// Gets the image source for assembly nodes.
		/// </summary>
		object Assembly { get; }
	}
}

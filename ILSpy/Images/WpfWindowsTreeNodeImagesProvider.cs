using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Supplies WPF image resources for tree node icons.
	/// </summary>
	public class WpfWindowsTreeNodeImagesProvider : ITreeNodeImagesProvider
	{
		/// <inheritdoc/>
		public object Assembly => Images.Assembly;
	}
}

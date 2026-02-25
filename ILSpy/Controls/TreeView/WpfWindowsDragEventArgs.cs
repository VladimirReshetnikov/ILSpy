using System.Windows;

using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpy.Controls.TreeView
{
	/// <summary>
	/// Adapter that exposes WPF <see cref="DragEventArgs"/> through <see cref="IPlatformDragEventArgs"/>.
	/// </summary>
	public class WpfWindowsDragEventArgs : IPlatformDragEventArgs
	{
		private readonly DragEventArgs _eventArgs;

		/// <summary>
		/// Initializes a new wrapper around WPF drag event args.
		/// </summary>
		/// <param name="eventArgs">WPF drag event arguments to adapt.</param>
		public WpfWindowsDragEventArgs(DragEventArgs eventArgs)
		{
			_eventArgs = eventArgs;
		}

		/// <inheritdoc/>
		public XPlatDragDropEffects Effects { get => (XPlatDragDropEffects)_eventArgs.Effects; set => _eventArgs.Effects = (DragDropEffects)value; }

		/// <inheritdoc/>
		public IPlatformDataObject Data => new WpfWindowsDataObject(_eventArgs.Data);
	}
}

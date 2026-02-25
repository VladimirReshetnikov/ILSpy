using System.Windows;

using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpy.Controls.TreeView
{
	/// <summary>
	/// Adapter that exposes WPF <see cref="RoutedEventArgs"/> through <see cref="IPlatformRoutedEventArgs"/>.
	/// </summary>
	public class WpfWindowsRoutedEventArgs : IPlatformRoutedEventArgs
	{
		private readonly RoutedEventArgs _eventArgs;

		/// <summary>
		/// Initializes a new wrapper around WPF routed event args.
		/// </summary>
		/// <param name="eventArgs">WPF routed event arguments to adapt.</param>
		public WpfWindowsRoutedEventArgs(RoutedEventArgs eventArgs)
		{
			_eventArgs = eventArgs;
		}

		/// <inheritdoc/>
		public bool Handled { get => _eventArgs.Handled; set => _eventArgs.Handled = value; }
	}
}

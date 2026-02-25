using System.Windows;

using ICSharpCode.ILSpyX.TreeView.PlatformAbstractions;

namespace ICSharpCode.ILSpy.Controls.TreeView
{
	/// <summary>
	/// WPF implementation of <see cref="IPlatformDataObject"/> backed by <see cref="IDataObject"/>.
	/// </summary>
	public sealed class WpfWindowsDataObject : IPlatformDataObject
	{
		private readonly IDataObject _dataObject;

		/// <summary>
		/// Initializes a new wrapper around a WPF data object.
		/// </summary>
		/// <param name="dataObject">Underlying WPF data object instance.</param>
		public WpfWindowsDataObject(IDataObject dataObject)
		{
			_dataObject = dataObject;
		}

		/// <inheritdoc/>
		public object GetData(string format)
		{
			return _dataObject.GetData(format);
		}

		/// <inheritdoc/>
		public bool GetDataPresent(string format)
		{
			return _dataObject.GetDataPresent(format);
		}

		/// <inheritdoc/>
		public void SetData(string format, object data)
		{
			_dataObject.SetData(format, data);
		}

		/// <inheritdoc/>
		object IPlatformDataObject.UnderlyingDataObject => _dataObject;
	}
}

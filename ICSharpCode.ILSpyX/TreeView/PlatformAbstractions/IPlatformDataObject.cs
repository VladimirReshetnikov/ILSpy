namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Cross-platform wrapper for clipboard and drag payloads used by <see cref="TreeView.SharpTreeNode"/>.
	/// </summary>
	public interface IPlatformDataObject
	{
		/// <summary>
		/// Determines whether a payload with the given data format exists.
		/// </summary>
		/// <param name="format">Format identifier understood by the platform data object (for example a MIME type or custom key).</param>
		/// <returns><see langword="true"/> when the format exists; otherwise <see langword="false"/>.</returns>
		bool GetDataPresent(string format);

		/// <summary>
		/// Gets the payload for a specific data format.
		/// </summary>
		/// <param name="format">Format identifier understood by the platform data object.</param>
		/// <returns>The payload value stored under <paramref name="format"/>.</returns>
		object GetData(string format);

		/// <summary>
		/// Stores payload data under a specific format identifier.
		/// </summary>
		/// <param name="format">Format identifier understood by the platform data object.</param>
		/// <param name="data">Payload to associate with <paramref name="format"/>.</param>
		void SetData(string format, object data);

		/// <summary>
		/// Gets the native platform data-object instance used by the current UI framework.
		/// </summary>
		object UnderlyingDataObject { get; }
	}
}

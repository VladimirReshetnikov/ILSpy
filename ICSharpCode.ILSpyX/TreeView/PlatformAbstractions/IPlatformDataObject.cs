// Copyright (c) 2024 Christoph Wille
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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

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

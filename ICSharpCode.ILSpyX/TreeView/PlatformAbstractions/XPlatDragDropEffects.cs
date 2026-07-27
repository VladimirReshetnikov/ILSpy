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

using System;

namespace ICSharpCode.ILSpyX.TreeView.PlatformAbstractions
{
	/// <summary>
	/// Platform-neutral drag/drop effect flags mirrored from desktop UI frameworks.
	/// </summary>
	/// <remarks>
	/// Numeric values intentionally match WPF <c>DragDropEffects</c> so hosts can cast between the two enums.
	/// </remarks>
	[Flags]
	public enum XPlatDragDropEffects
	{
		/// <summary>
		/// Scrolling is about to start or is currently occurring in the drop target.
		/// </summary>
		Scroll = int.MinValue,
		/// <summary>
		/// Combination flag that allows copy, move, and scroll effects.
		/// </summary>
		All = -2147483645,
		/// <summary>
		/// The drop target does not accept the data.
		/// </summary>
		None = 0,
		/// <summary>
		/// The data is copied to the drop target.
		/// </summary>
		Copy = 1,
		/// <summary>
		/// The data from the drag source is moved to the drop target.
		/// </summary>
		Move = 2,
		/// <summary>
		/// The data from the drag source is linked to the drop target.
		/// </summary>
		Link = 4
	}
}

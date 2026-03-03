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

// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
#nullable enable

using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Provides a thread-local reentrancy guard for recursive call paths.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The guard tracks active object identities in a thread-static stack. This prevents stack-overflow-prone recursion in type-system lookups while
	/// avoiding cross-thread interference.
	/// </para>
	/// <para>
	/// Typical usage is <c>using (var busy = BusyManager.Enter(this)) { if (!busy.Success) return ...; ... }</c>.
	/// </para>
	/// </remarks>
	public static class BusyManager
	{
		/// <summary>
		/// Represents the result of <see cref="Enter(object?)"/> and releases the busy marker when disposed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// A successful lock keeps a reference to the thread-local active-object stack. Calling
		/// <see cref="Dispose"/> removes the most recently pushed entry, restoring the prior reentrancy state.
		/// </para>
		/// <para>
		/// A failed lock is represented by <see cref="Failed"/> and performs no work on disposal.
		/// </para>
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible",
										 Justification = "Should always be used with 'var'")]
		public struct BusyLock : IDisposable
		{
			/// <summary>
			/// Gets a sentinel lock indicating that <see cref="Enter(object?)"/> detected reentrancy.
			/// </summary>
			public static readonly BusyLock Failed = new BusyLock(null);

			readonly List<object?>? objectList;

			internal BusyLock(List<object?>? objectList)
			{
				this.objectList = objectList;
			}

			/// <summary>
			/// Gets whether <see cref="Enter(object?)"/> succeeded and this instance owns an active busy marker.
			/// </summary>
			public bool Success {
				get { return objectList != null; }
			}

			/// <summary>
			/// Releases the current busy marker for the active thread.
			/// </summary>
			/// <remarks>
			/// If this instance equals <see cref="Failed"/>, this method is a no-op.
			/// </remarks>
			public void Dispose()
			{
				if (objectList != null)
				{
					objectList.RemoveAt(objectList.Count - 1);
				}
			}
		}

		[ThreadStatic] static List<object?>? _activeObjects;

		/// <summary>
		/// Attempts to enter a reentrancy guard for <paramref name="obj"/> on the current thread.
		/// </summary>
		/// <param name="obj">Object identity used to detect recursive entry.</param>
		/// <returns>
		/// A <see cref="BusyLock"/> with <see cref="BusyLock.Success"/> set to <see langword="true"/> when the object was not already active on this thread;
		/// otherwise <see cref="BusyLock.Failed"/>.
		/// </returns>
		/// <remarks>
		/// The guard is thread-affine because the active stack is stored in a thread-static list. This prevents false positives across threads while still
		/// breaking recursive cycles within one call chain, which is how the type-system recursion checks use this helper.
		/// </remarks>
		public static BusyLock Enter(object? obj)
		{
			List<object?>? activeObjects = _activeObjects;
			if (activeObjects == null)
				activeObjects = _activeObjects = new List<object?>();
			for (int i = 0; i < activeObjects.Count; i++)
			{
				if (activeObjects[i] == obj)
					return BusyLock.Failed;
			}
			activeObjects.Add(obj);
			return new BusyLock(activeObjects);
		}
	}
}

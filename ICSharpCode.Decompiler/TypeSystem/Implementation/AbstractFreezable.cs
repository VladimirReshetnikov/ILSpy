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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Helper methods for implementing the <see cref="IFreezable"/> immutability pattern used by the type system.
	/// </summary>
	/// <remarks>
	/// The decompiler publishes many symbol objects to caches that are read concurrently. Callers build mutable
	/// object graphs first, then transition them into frozen read-only structures before publication.
	/// </remarks>
	public static class FreezableHelper
	{
		/// <summary>
		/// Throws when <paramref name="freezable"/> is already frozen.
		/// </summary>
		/// <param name="freezable">The instance to validate.</param>
		/// <exception cref="InvalidOperationException">
		/// <paramref name="freezable"/> has already been frozen and can no longer be mutated.
		/// </exception>
		public static void ThrowIfFrozen(IFreezable freezable)
		{
			if (freezable.IsFrozen)
				throw new InvalidOperationException("Cannot mutate frozen " + freezable.GetType().Name);
		}

		/// <summary>
		/// Freezes each element in <paramref name="list"/> (when applicable), then returns a frozen list wrapper.
		/// </summary>
		/// <typeparam name="T">Element type contained in the list.</typeparam>
		/// <param name="list">The list to freeze, or <see langword="null"/>.</param>
		/// <returns>A read-only list representation suitable for post-freeze publication.</returns>
		public static IList<T> FreezeListAndElements<T>(IList<T> list)
		{
			if (list != null)
			{
				foreach (T item in list)
					Freeze(item);
			}
			return FreezeList(list);
		}

		/// <summary>
		/// Produces a read-only representation of <paramref name="list"/>.
		/// </summary>
		/// <typeparam name="T">Element type contained in the list.</typeparam>
		/// <param name="list">The source list, which may be <see langword="null"/> or empty.</param>
		/// <returns>
		/// <see cref="EmptyList{T}.Instance"/> for <see langword="null"/> or empty input;
		/// the original instance if <paramref name="list"/> is already read-only;
		/// otherwise a copied <see cref="ReadOnlyCollection{T}"/>.
		/// </returns>
		public static IList<T> FreezeList<T>(IList<T> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<T>.Instance;
			if (list.IsReadOnly)
			{
				// If the list is already read-only, return it directly.
				// This is important, otherwise we might undo the effects of interning.
				return list;
			}
			else
			{
				return new ReadOnlyCollection<T>(list.ToArray());
			}
		}

		/// <summary>
		/// Calls <see cref="IFreezable.Freeze"/> when <paramref name="item"/> implements <see cref="IFreezable"/>.
		/// </summary>
		/// <param name="item">Object that may participate in the freeze protocol.</param>
		public static void Freeze(object item)
		{
			IFreezable f = item as IFreezable;
			if (f != null)
				f.Freeze();
		}

		/// <summary>
		/// Freezes <paramref name="item"/> and returns the same instance for fluent initialization pipelines.
		/// </summary>
		/// <typeparam name="T">Freezable type.</typeparam>
		/// <param name="item">The instance to freeze.</param>
		/// <returns><paramref name="item"/> after it has been frozen.</returns>
		public static T FreezeAndReturn<T>(T item) where T : IFreezable
		{
			item.Freeze();
			return item;
		}

		/// <summary>
		/// If the item is not frozen, this method creates and returns a frozen clone.
		/// If the item is already frozen, it is returned without creating a clone.
		/// </summary>
		public static T GetFrozenClone<T>(T item) where T : IFreezable, ICloneable
		{
			if (!item.IsFrozen)
			{
				item = (T)item.Clone();
				item.Freeze();
			}
			return item;
		}
	}

	/// <summary>
	/// Base class for <see cref="IFreezable"/> objects that transition once from mutable to immutable state.
	/// </summary>
	/// <remarks>
	/// Derived types implement <see cref="FreezeInternal"/> to recursively freeze child objects and normalize
	/// collections. After <see cref="Freeze"/> completes, instances are expected to be read-only and safe for
	/// concurrent reads.
	/// </remarks>
	[Serializable]
	public abstract class AbstractFreezable : IFreezable
	{
		bool isFrozen;

		/// <summary>
		/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
		/// </summary>
		public bool IsFrozen {
			get { return isFrozen; }
		}

		/// <summary>
		/// Freezes this instance.
		/// </summary>
		public void Freeze()
		{
			if (!isFrozen)
			{
				FreezeInternal();
				isFrozen = true;
			}
		}

		/// <summary>
		/// Performs type-specific freeze work before the frozen flag is committed.
		/// </summary>
		/// <remarks>
		/// Implementations should freeze nested state and convert mutable collections into read-only equivalents.
		/// This method is invoked at most once per instance.
		/// </remarks>
		protected virtual void FreezeInternal()
		{
		}
	}
}

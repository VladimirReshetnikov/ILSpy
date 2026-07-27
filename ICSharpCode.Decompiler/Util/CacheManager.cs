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
using System.Collections.Concurrent;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Caches values for a single compilation, shared among all threads working with it, keyed by object identity.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The underlying dictionary uses <see cref="ReferenceComparer"/>, so keys are compared by reference identity rather than value equality.
	/// This allows individual compilation components to use private sentinel objects as collision-free cache keys.
	/// </para>
	/// <para>
	/// This type is thread-safe.
	/// </para>
	/// </remarks>
	public sealed class CacheManager
	{
		readonly ConcurrentDictionary<object, object> sharedDict = new ConcurrentDictionary<object, object>(ReferenceComparer.Instance);
		// There used to be a thread-local dictionary here, but I removed it as it was causing memory
		// leaks in some use cases.

		/// <summary>
		/// Gets the cached value associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">Identity key used for lookup.</param>
		/// <returns>The cached value, or <see langword="null"/> when no entry exists for <paramref name="key"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
		public object? GetShared(object key)
		{
			object? value;
			sharedDict.TryGetValue(key, out value);
			return value;
		}

		/// <summary>
		/// Gets the value associated with <paramref name="key"/>, or computes a value and atomically publishes it.
		/// </summary>
		/// <param name="key">Identity key used for lookup.</param>
		/// <param name="valueFactory">Factory invoked when the key is not yet present.</param>
		/// <returns>The existing or newly created value associated with <paramref name="key"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="valueFactory"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/> may invoke <paramref name="valueFactory"/> more than once
		/// under contention; only one produced value is published.
		/// </remarks>
		public object GetOrAddShared(object key, Func<object, object> valueFactory)
		{
			return sharedDict.GetOrAdd(key, valueFactory);
		}

		/// <summary>
		/// Gets the value associated with <paramref name="key"/>, or stores <paramref name="value"/> when the key is not present.
		/// </summary>
		/// <param name="key">Identity key used for lookup.</param>
		/// <param name="value">Value to publish if no entry exists yet.</param>
		/// <returns>The existing or newly stored value associated with <paramref name="key"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
		public object GetOrAddShared(object key, object value)
		{
			return sharedDict.GetOrAdd(key, value);
		}

		/// <summary>
		/// Sets the value associated with <paramref name="key"/>, replacing any previous value.
		/// </summary>
		/// <param name="key">Identity key used for lookup.</param>
		/// <param name="value">Value to store.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
		public void SetShared(object key, object value)
		{
			sharedDict[key] = value;
		}
	}
}

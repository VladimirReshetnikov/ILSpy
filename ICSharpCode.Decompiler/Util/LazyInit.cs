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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Provides allocation-free helpers for lock-free lazy initialization of reference fields.
	/// </summary>
	/// <remarks>
	/// These helpers are used throughout metadata and resolver code paths where repeated reads are common and per-access locking would be too expensive.
	/// </remarks>
	public static class LazyInit
	{
		/// <summary>
		/// Performs a volatile read of a reference field.
		/// </summary>
		/// <typeparam name="T">Reference type stored in <paramref name="location"/>.</typeparam>
		/// <param name="location">Field to read using acquire semantics.</param>
		/// <returns>The current value stored in <paramref name="location"/>.</returns>
		public static T VolatileRead<T>(ref T location) where T : class?
		{
			return Volatile.Read(ref location);
		}

		/// <summary>
		/// Atomically sets <paramref name="target"/> to <paramref name="newValue"/> when it is currently <see langword="null"/>.
		/// </summary>
		/// <typeparam name="T">Reference type of the lazily initialized field.</typeparam>
		/// <param name="target">Field to initialize.</param>
		/// <param name="newValue">Candidate value to publish if <paramref name="target"/> is currently <see langword="null"/>.</param>
		/// <returns>
		/// The existing value in <paramref name="target"/> when one was already published; otherwise <paramref name="newValue"/>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method does not compute values. Callers usually construct <paramref name="newValue"/> first and then race to publish it,
		/// accepting that discarded candidates might be allocated.
		/// </para>
		/// <para>
		/// Publication uses <see cref="Interlocked.CompareExchange(ref T, T, T)"/> so subsequent volatile reads observe a fully initialized object.
		/// </para>
		/// </remarks>
		[return: NotNullIfNotNull("newValue")]
		public static T? GetOrSet<T>(ref T? target, T? newValue) where T : class
		{
			T? oldValue = Interlocked.CompareExchange(ref target, newValue, null);
			return oldValue ?? newValue;
		}
	}
}

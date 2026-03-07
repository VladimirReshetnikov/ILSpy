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
using System.Threading;

namespace ICSharpCode.Decompiler.Util
{
	/// <summary>
	/// Invokes a callback exactly once when disposed.
	/// </summary>
	/// <remarks>
	/// This helper is used to model lightweight scope-exit actions without allocating a full custom disposable type for each call site.
	/// </remarks>
	public sealed class CallbackOnDispose : IDisposable
	{
		Action? action;

		/// <summary>
		/// Initializes a new instance that executes <paramref name="action"/> on disposal.
		/// </summary>
		/// <param name="action">Callback to execute when disposal first occurs.</param>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
		public CallbackOnDispose(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			this.action = action;
		}

		/// <summary>
		/// Executes the callback if it has not already run.
		/// </summary>
		/// <remarks>
		/// Thread-safe: concurrent calls race through <see cref="Interlocked.Exchange{T}(ref T, T)"/> and at most one caller receives
		/// the original delegate.
		/// </remarks>
		public void Dispose()
		{
			Action? a = Interlocked.Exchange(ref action, null);
			if (a != null)
			{
				a();
			}
		}
	}
}

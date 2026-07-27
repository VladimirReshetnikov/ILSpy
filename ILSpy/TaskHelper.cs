// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Provides task-composition helpers used by the UI layer to chain background work
	/// back onto the WPF synchronization context.
	/// </summary>
	public static class TaskHelper
	{
		/// <summary>
		/// A pre-completed non-generic task.
		/// </summary>
		public static readonly Task CompletedTask = FromResult<object>(null);

		/// <summary>
		/// Creates a completed task containing <paramref name="result"/>.
		/// </summary>
		/// <typeparam name="T">The result type.</typeparam>
		/// <param name="result">The value to expose through the returned task.</param>
		/// <returns>A task in the <see cref="TaskStatus.RanToCompletion"/> state.</returns>
		public static Task<T> FromResult<T>(T result)
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
			tcs.SetResult(result);
			return tcs.Task;
		}

		/// <summary>
		/// Creates a faulted task that propagates <paramref name="ex"/>.
		/// </summary>
		/// <typeparam name="T">The result type.</typeparam>
		/// <param name="ex">The exception to attach to the returned task.</param>
		/// <returns>A task in the <see cref="TaskStatus.Faulted"/> state.</returns>
		public static Task<T> FromException<T>(Exception ex)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(ex);
			return tcs.Task;
		}

		/// <summary>
		/// Creates a canceled task.
		/// </summary>
		/// <typeparam name="T">The result type.</typeparam>
		/// <returns>A task in the <see cref="TaskStatus.Canceled"/> state.</returns>
		public static Task<T> FromCancellation<T>()
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetCanceled();
			return tcs.Task;
		}

		/// <summary>
		/// Sets the result of the TaskCompletionSource based on the result of the finished task.
		/// </summary>
		/// <typeparam name="T">The task result type.</typeparam>
		/// <param name="tcs">The completion source to update.</param>
		/// <param name="task">The already-completed task whose status should be mirrored.</param>
		public static void SetFromTask<T>(this TaskCompletionSource<T> tcs, Task<T> task)
		{
			switch (task.Status)
			{
				case TaskStatus.RanToCompletion:
					tcs.SetResult(task.Result);
					break;
				case TaskStatus.Canceled:
					tcs.SetCanceled();
					break;
				case TaskStatus.Faulted:
					tcs.SetException(task.Exception.InnerExceptions);
					break;
				default:
					throw new InvalidOperationException("The input task must have already finished");
			}
		}

		/// <summary>
		/// Sets the result of the TaskCompletionSource based on the result of the finished task.
		/// </summary>
		/// <param name="tcs">The completion source to update.</param>
		/// <param name="task">The already-completed task whose status should be mirrored.</param>
		public static void SetFromTask(this TaskCompletionSource<object> tcs, Task task)
		{
			switch (task.Status)
			{
				case TaskStatus.RanToCompletion:
					tcs.SetResult(null);
					break;
				case TaskStatus.Canceled:
					tcs.SetCanceled();
					break;
				case TaskStatus.Faulted:
					tcs.SetException(task.Exception.InnerExceptions);
					break;
				default:
					throw new InvalidOperationException("The input task must have already finished");
			}
		}

		public static Task Then<T>(this Task<T> task, Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			return task.ContinueWith(t => action(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task<U> Then<T, U>(this Task<T> task, Func<T, U> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			return task.ContinueWith(t => func(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task Then<T>(this Task<T> task, Func<T, Task> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException(nameof(asyncFunc));
			return task.ContinueWith(t => asyncFunc(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task<U> Then<T, U>(this Task<T> task, Func<T, Task<U>> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException(nameof(asyncFunc));
			return task.ContinueWith(t => asyncFunc(t.Result), CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task Then(this Task task, Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			return task.ContinueWith(t => {
				t.Wait();
				action();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task<U> Then<U>(this Task task, Func<U> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));
			return task.ContinueWith(t => {
				t.Wait();
				return func();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public static Task Then(this Task task, Func<Task> asyncAction)
		{
			if (asyncAction == null)
				throw new ArgumentNullException(nameof(asyncAction));
			return task.ContinueWith(t => {
				t.Wait();
				return asyncAction();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		public static Task<U> Then<U>(this Task task, Func<Task<U>> asyncFunc)
		{
			if (asyncFunc == null)
				throw new ArgumentNullException(nameof(asyncFunc));
			return task.ContinueWith(t => {
				t.Wait();
				return asyncFunc();
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext()).Unwrap();
		}

		/// <summary>
		/// If the input task fails, calls the action to handle the error.
		/// </summary>
		/// <typeparam name="TException">The exception type that should be handled.</typeparam>
		/// <param name="task">The task whose failure should be observed.</param>
		/// <param name="action">The handler invoked when the task fails with <typeparamref name="TException"/>.</param>
		/// <returns>
		/// Returns a task that finishes successfully when error handling has completed.
		/// If the input task ran successfully, the returned task completes successfully.
		/// If the input task was cancelled, the returned task is cancelled as well.
		/// </returns>
		public static Task Catch<TException>(this Task task, Action<TException> action) where TException : Exception
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			return task.ContinueWith(t => {
				if (t.IsFaulted)
				{
					Exception ex = t.Exception;
					while (ex is AggregateException)
						ex = ex.InnerException;
					if (ex is TException)
						action((TException)ex);
					else
						throw t.Exception;
				}
			}, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
		}

		/// <summary>
		/// Ignore exceptions thrown by the task.
		/// </summary>
		/// <param name="task">The task to observe and intentionally ignore.</param>
		public static void IgnoreExceptions(this Task task)
		{
		}

		/// <summary>
		/// Handle exceptions by displaying the error message in the text view.
		/// </summary>
		/// <param name="task">The task whose unhandled exceptions should be reported to the UI.</param>
		public static void HandleExceptions(this Task task)
		{
			task.Catch<Exception>(exception => App.Current.Dispatcher.BeginInvoke(new Action(delegate {
				AvalonEditTextOutput output = new();
				output.Write(exception.ToString());
				App.ExportProvider.GetExportedValue<DockWorkspace>().ShowText(output);
			}))).IgnoreExceptions();
		}
	}
}

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class AsyncUsing
	{
		internal class AsyncDisposableClass : IAsyncDisposable
		{
			public ValueTask DisposeAsync()
			{
				throw new NotImplementedException();
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		internal struct AsyncDisposableStruct : IAsyncDisposable
		{
			public ValueTask DisposeAsync()
			{
				throw new NotImplementedException();
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = 1)]
		internal struct PatternAsyncDisposableStruct
		{
			public ValueTask DisposeAsync()
			{
				throw new NotImplementedException();
			}
		}

		public static async void TestAsyncUsing(IAsyncDisposable disposable)
		{
			await using (disposable)
			{
				Console.WriteLine("Hello");
			}
		}

		public static async void TestAsyncUsingClass()
		{
			await using (AsyncDisposableClass test = new AsyncDisposableClass())
			{
				Use(test);
			}
		}

		public static async void TestAsyncUsingStruct()
		{
			await using (AsyncDisposableStruct asyncDisposableStruct = default(AsyncDisposableStruct))
			{
				Use(asyncDisposableStruct);
			}
		}

		public static async void TestAsyncUsingNullableStruct()
		{
			await using (AsyncDisposableStruct? asyncDisposableStruct = new AsyncDisposableStruct?(default(AsyncDisposableStruct)))
			{
				Use(asyncDisposableStruct);
			}
		}

		public static async void TestAsyncUsingPatternStruct()
		{
			await using (default(PatternAsyncDisposableStruct))
			{
				Console.WriteLine("Hello");
			}
		}

		public static async void TestAsyncUsingConfigured(IAsyncDisposable disposable)
		{
			await using (disposable.ConfigureAwait(continueOnCapturedContext: false))
			{
				Console.WriteLine("Hello");
			}
		}

		private static void Use(IAsyncDisposable test)
		{
			throw new NotImplementedException();
		}

	}
}

// Copyright (c) 2026 Vladimir Reshetnikov
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
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public struct Padded
	{
		public int A;

		public long B;
	}

	public class UnsafeAsync
	{
		public async Task<int> SizeofInAsyncBody()
		{
			await Task.Yield();
			int result;
			unsafe
			{
				result = sizeof(Padded);
			}
			await Task.Yield();
			return result;
		}

		public Task SyncMethodWithAsyncLocalFunction()
		{
#if OPT
			unsafe
			{
				Console.WriteLine(sizeof(Padded));
			}
#else
			int value;
			unsafe
			{
				value = sizeof(Padded);
			}
			Console.WriteLine(value);
#endif
			return Loop();
			static async Task Loop()
			{
				await Task.Yield();
			}
		}

		public async Task<int> SizeofInLambdaArgToAwaitedCall()
		{
			return await Invoke(() => {
				unsafe
				{
					return sizeof(Padded);
				}
			});
		}

		private static Task<T> Invoke<T>(Func<T> f)
		{
			return Task.FromResult(f());
		}
	}
}

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

using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class LocalFunctionScopesInLoops
	{
		// The fetch loop assigns a variable that the local function captures, but the local
		// function's only call site lies in the second loop. The display struct's capture scope
		// must therefore enclose both loops; anchoring it at the first captured-variable store
		// would trap the declaration inside the fetch loop, unreachable from the call site.
		public async Task<int> ReadAsync()
		{
			int arrayLength;
			while (!TryReadArrayHeader(out arrayLength))
			{
				await FetchMoreBytesAsync();
			}
			int i = 0;
			int total = 0;
			for (; i < arrayLength; i++)
			{
				int batch = NextBatchSize();
				await ConsumeAsync(batch);
				total += batch;
			}
			return total;
			int NextBatchSize()
			{
				return arrayLength - i;
			}
		}

		// A local function declared and used entirely within a while(true) loop is declared once,
		// at the end of the loop body, after the loop's trailing 'continue;' has been removed.
		public int Sum(int limit)
		{
			int num = 0;
			while (true)
			{
				int x = num + 1;
				if (x > limit)
				{
					break;
				}
				num = Grow();
				int Grow()
				{
					return x * 2;
				}
			}
			return num;
		}

		private bool TryReadArrayHeader(out int arrayLength)
		{
			arrayLength = 0;
			return true;
		}

		private Task FetchMoreBytesAsync()
		{
			return Task.CompletedTask;
		}

		private Task ConsumeAsync(int batch)
		{
			return Task.CompletedTask;
		}
	}
}

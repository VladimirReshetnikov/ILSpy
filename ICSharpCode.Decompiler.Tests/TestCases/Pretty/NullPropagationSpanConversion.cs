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

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class NullPropagationSpanConversion
	{
		internal class Registration
		{
			public int Total { get; init; }
		}

		// The receiver of TotalLength reaches it through the implicit span conversion from
		// string[]; C# re-applies that conversion at the call site, so the chain is written
		// without naming it.
		private static int ChainThroughSpanConversion(string[][] rows, int i)
		{
			return (rows?[i]?.TotalLength()).GetValueOrDefault();
		}

		private static int ChainWithFallback(string[][] rows, int i)
		{
			return rows?[i]?.TotalLength() ?? 7;
		}

		// The same chain as the value of an init-only member: it has to stay a single
		// expression, otherwise the member cannot be set inside the object initializer.
		private static Registration InitOnlyMemberFromChain(string[][] rows, int i)
		{
			return new Registration {
				Total = (rows?[i]?.TotalLength() ?? 7)
			};
		}
	}
	internal static class SpanReceiverExtensions
	{
		public static int TotalLength(this ReadOnlySpan<string> values)
		{
			return values.Length;
		}
	}
}

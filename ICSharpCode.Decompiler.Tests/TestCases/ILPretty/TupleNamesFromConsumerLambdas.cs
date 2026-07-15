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

using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class TupleNamesFromConsumerLambdas
	{
		public static IEnumerable<T> PassThrough<T>(this IEnumerable<T> source)
		{
			return source;
		}

		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
		{
			return null;
		}

		public static int Direct(IEnumerable<(int, int)> input)
		{
			IEnumerable<(int left, int right)> source = input.PassThrough();
			return source.Select(((int left, int right) pair) => pair.left).First() + source.Count();
		}

		public static int Chain(IEnumerable<(int, int)> input)
		{
			IEnumerable<(int left, int right)> source = input.PassThrough();
			return (from pair in source.PassThrough()
				select pair.right).First() + source.Count();
		}

		public static int NullableChain(IEnumerable<(int, int)?> input)
		{
			IEnumerable<(int left, int right)?> source = input.PassThrough();
			return (from pair in source.WhereNotNull<(int left, int right)>()
				select pair.left).First() + source.Count();
		}

		public static int Conflict(IEnumerable<(int, int)> input)
		{
			IEnumerable<(int, int)> source = input.PassThrough();
			return source.Select<(int, int), int>(((int left, int right) pair) => pair.left).First() + source.Select<(int, int), int>(((int x, int y) pair) => pair.y).First();
		}

		public static (int left, int right)[] ProjectionQuery(IEnumerable<(int, int)> input)
		{
			IEnumerable<(int, int)> source = input.PassThrough();
			source.Count();
			return (from pair in source
				from result in new int[1] { pair.Item1 }
				select (left: pair.Item1, right: result)).ToArray();
		}
	}
}

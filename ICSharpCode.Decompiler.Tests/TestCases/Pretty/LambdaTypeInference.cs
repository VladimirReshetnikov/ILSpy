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

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class LambdaTypeInference
	{
		internal abstract class ResultBase
		{
		}

		internal sealed class ResultA : ResultBase
		{
		}

		internal sealed class ResultB : ResultBase
		{
		}

		public static IEnumerable<ResultBase> TargetTypedConditional(IEnumerable<int> source, bool flag)
		{
#if EXPECTED_OUTPUT
#if OPT
			return source.Select<int, ResultBase>(delegate (int item) {
				if (item < 0)
				{
					return new ResultA();
				}
				return (!flag) ? new ResultB() : new ResultA();
			});
#else
			return source.Select<int, ResultBase>((int item) => (item < 0) ? new ResultA() : (flag ? new ResultA() : new ResultB()));
#endif
#else
			return source.Select<int, ResultBase>(delegate (int item) {
				if (item < 0)
				{
					return new ResultA();
				}
				return flag ? new ResultA() : new ResultB();
			});
#endif
		}
	}
}

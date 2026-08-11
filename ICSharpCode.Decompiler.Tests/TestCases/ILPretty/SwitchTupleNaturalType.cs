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

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class SwitchTupleNaturalType
	{
		public static double Sum(int kind, double first, double second)
		{
			(double, double, double) obj = kind switch {
				0 => (first, second, 0.0),
				1 => (second, first, 0.0),
				2 => (0.0, first, second),
				3 => (0.0, second, first),
				4 => (second, 0.0, first),
				5 => (first, 0.0, second),
				_ => (0.0, 0.0, 0.0),
			};
			double item = obj.Item1;
			double item2 = obj.Item2;
			double item3 = obj.Item3;
			item++;
			item2++;
			item3++;
			return item + item2 + item3;
		}
	}
}

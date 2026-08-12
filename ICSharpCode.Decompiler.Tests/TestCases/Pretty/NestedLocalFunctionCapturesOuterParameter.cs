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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public static class NestedLocalFunctionCapturesOuterParameter
	{
		public static async IAsyncEnumerable<string> Convert(string value)
		{
			string prefix = value.Substring(0, 0);
			await Task.Yield();
#if OPT && EXPECTED_OUTPUT
			string[][] array = Array.ConvertAll(new string[1] { value }, (string original) => ConvertSet(new string[1] { "flavor" }, original));
			yield return array[0][0];
#else
			string[][] converted = Array.ConvertAll(new string[1] { value }, (string original) => ConvertSet(new string[1] { "flavor" }, original));
			yield return converted[0][0];
#endif
			string[] ConvertSet(string[] actions, string original)
			{
				return Array.ConvertAll(actions, (string action) => ConvertAction(action));
				string ConvertAction(string action)
				{
					if (action == "nested")
					{
						return ConvertSet(new string[1] { "!" }, original)[0];
					}
					if (action == "flavor")
					{
						return ConvertFlavors(new string[1] { "!" })[0];
					}
					return prefix + original + action;
				}
				string[] ConvertFlavors(string[] flavors)
				{
					return Array.ConvertAll(flavors, (string action) => ConvertAction(action));
				}
			}
		}
	}
}

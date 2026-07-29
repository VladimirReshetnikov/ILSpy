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

using ICSharpCode.Decompiler.Tests.TestCases.Pretty.ExtensionMethodDelegateHelpers;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class ExtensionMethodDelegate
	{
		public Func<int> GetSimpleDelegate(string value)
		{
			return value.GetLength;
		}

		public Func<short, bool> GetGenericDelegate(List<short> roles)
		{
#if !ROSLYN2 || ROSLYN3
			return ((IReadOnlyCollection<short>)roles).Contains;
#else
			// Casting the receiver is enough to name this method group unambiguously for every
			// other compiler; against Roslyn 2.10 the reference stays ambiguous, so the search
			// for a minimal form goes one step further and spells the type argument out.
			return ((IReadOnlyCollection<short>)roles).Contains<short>;
#endif
		}
	}
}
namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty.ExtensionMethodDelegateHelpers
{
	internal static class DelegateExtensions
	{
		public static int GetLength(this string value)
		{
			return value.Length;
		}

		public static bool Contains<T>(this IReadOnlyCollection<T> collection, T item)
		{
			return false;
		}
	}
}

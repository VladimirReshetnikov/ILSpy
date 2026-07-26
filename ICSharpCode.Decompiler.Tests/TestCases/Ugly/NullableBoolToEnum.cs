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

namespace ICSharpCode.Decompiler.Tests.TestCases.Ugly
{
	public static class NullableBoolToEnum
	{
		public enum Compliance
		{
			DeclaredTrue,
			DeclaredFalse,
			InheritedTrue,
			InheritedFalse,
			ImpliedFalse
		}

		private static readonly Dictionary<int, Compliance> Cache = new Dictionary<int, Compliance>();

		private static bool? GetDeclared(out string location)
		{
			location = null;
			return null;
		}

		private static bool IsTrue(Compliance c)
		{
			return c == Compliance.DeclaredTrue;
		}

		public static Compliance Get(int kind)
		{
			if (Cache.TryGetValue(kind, out var value))
			{
				return value;
			}
			bool? declared = GetDeclared(out var _);
			// A lifted bool decides the value, and the rest of the arms are enum constants. The stack
			// type cannot tell the two apart, which is how the lifted value ends up on the left of a
			// '??' whose fallback is a number -- a conversion C# does not have.
			if (declared.HasValue)
			{
				value = (declared.GetValueOrDefault() ? Compliance.DeclaredTrue : Compliance.DeclaredFalse);
			}
			else if (kind == 2)
			{
				value = Compliance.ImpliedFalse;
			}
			else
			{
				value = (IsTrue(Get(kind + 1)) ? Compliance.InheritedTrue : Compliance.InheritedFalse);
			}
			if (kind != 2 && kind != 3)
			{
				return value;
			}
			Cache[kind] = value;
			return value;
		}
	}
}

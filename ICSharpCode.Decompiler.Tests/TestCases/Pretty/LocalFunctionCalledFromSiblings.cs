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

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public static class LocalFunctionCalledFromSiblings
	{
		public static readonly List<string> All;

		public static readonly HashSet<string> NoEffect;

		static LocalFunctionCalledFromSiblings()
		{
			List<string> builder = new List<string>();
			HashSet<string> noEffect = new HashSet<string>();
			AddGeneral("a", noEffect: true);
			AddProject(1);
			AddRude("c");
			All = builder;
			NoEffect = noEffect;
			void Add(string id, bool isNoEffect)
			{
				builder.Add(id);
				if (isNoEffect)
				{
					noEffect.Add(id);
				}
			}
			void AddGeneral(string id, bool noEffect = false)
			{
				Add(id, noEffect);
			}
			void AddProject(int kind)
			{
				Add(kind.ToString(), isNoEffect: false);
			}
			void AddRude(string id)
			{
				Add(id, isNoEffect: true);
			}
		}
	}
}

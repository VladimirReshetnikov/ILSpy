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
using System.IO;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class EnhancedUsingVariableCollision
	{
		// Two out-argument locals with the same callee-derived name live in sibling scopes: one in
		// an if block, one in the trailing using statement. The trailing using is printed as a
		// braceless using declaration, which flattens its contents into the method's declaration
		// space, so the two same-named declarations collide across nested scopes (CS0136). An
		// optimized build stores both locals in one slot; the reconstruction must then merge them
		// into a single declaration hoisted to the shared scope.
		public void Test(bool condition)
		{
#if OPT
			int msgLen;
			if (condition)
			{
				Pack(out msgLen);
				Console.WriteLine(msgLen);
			}
			using MemoryStream memoryStream = new MemoryStream();
			Pack(out msgLen);
			memoryStream.WriteByte((byte)msgLen);
#else
			if (condition)
			{
				Pack(out var msgLen);
				Console.WriteLine(msgLen);
			}
			using MemoryStream memoryStream = new MemoryStream();
			Pack(out var msgLen2);
			memoryStream.WriteByte((byte)msgLen2);
#endif
		}

		private static void Pack(out int msgLen)
		{
			msgLen = 0;
		}
	}
}

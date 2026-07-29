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

using System.Runtime.InteropServices;

using ICSharpCode.Decompiler.Tests.TestCases.Pretty.ChainShadowLib;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	// An extension method in an ENCLOSING namespace of the consumer. C# name lookup reaches it
	// before any using directive, so from inside a nested namespace an unqualified
	// 'token.Trimmed()' binds here - not to the method the IL calls.
	public class ChainShadowBlock
	{
		public ChainShadowToken Token => default(ChainShadowToken);
	}
	internal static class ChainShadowGenericExtensions
	{
		public static ChainShadowToken Trimmed<T>(this T t) where T : struct
		{
			return default(ChainShadowToken);
		}
	}
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ChainShadowToken
	{
		public int Width => 3;
	}
}
namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty.ChainShadowInner
{
	public class NullPropagationExtensionShadowing
	{
		// The called extension cannot be written in infix form here: the enclosing namespace's
		// Trimmed<T> would capture it. With extension syntax unavailable, the null-conditional
		// chain must not be introduced either - 'Trimmed(b?.Token)' would type the argument
		// ChainShadowToken? and, worse, no longer skip the call for a null receiver. The
		// explicit conditional is the only faithful form.
		public ChainShadowToken Get(ChainShadowBlock b, ChainShadowToken fallback)
		{
			return ((b != null) ? new ChainShadowToken?(ChainShadowTokenExtensions.Trimmed(b.Token)) : ((ChainShadowToken?)null)) ?? fallback;
		}
	}
}
namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty.ChainShadowLib
{
	public static class ChainShadowTokenExtensions
	{
		public static ChainShadowToken Trimmed(this ChainShadowToken t)
		{
			return t;
		}
	}
}

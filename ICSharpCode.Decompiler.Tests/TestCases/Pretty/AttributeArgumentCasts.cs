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
using System.Runtime.InteropServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	// ClassInterfaceAttribute declares both a ClassInterfaceType and a short constructor; the
	// bare literal 0 binds to either (CS0121), so the cast to short is required.
	[ClassInterface((short)0)]
	internal class EnumSiblingConflict
	{
	}

	// A sibling int constructor would win for a bare literal, so the cast to short is required
	// to keep the recorded overload.
	[ShortOrInt((short)5)]
	internal class IntSiblingConflict
	{
	}

	// The attribute type has a single small-integer constructor, so the bare literal is
	// unambiguous and must stay uncast.
	[ShortOnly(0)]
	internal class NoConflict
	{
	}

	[AttributeUsage(AttributeTargets.All)]
	internal class ShortOnlyAttribute : Attribute
	{
		public ShortOnlyAttribute(short value)
		{
		}
	}

	[AttributeUsage(AttributeTargets.All)]
	internal class ShortOrIntAttribute : Attribute
	{
		public ShortOrIntAttribute(short value)
		{
		}

		public ShortOrIntAttribute(int value)
		{
		}
	}
}

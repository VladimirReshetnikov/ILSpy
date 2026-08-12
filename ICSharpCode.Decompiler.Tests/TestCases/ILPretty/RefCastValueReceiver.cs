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

using System.Runtime.CompilerServices;

using External;

#if !EXPECTED_OUTPUT
namespace External
{
	public readonly struct ValueMatch
	{
		public string Path => "path";
	}
}
#endif

public static class RefCastValueReceiver
{
	public static string ReadValue(ValueMatch value)
	{
		return value.Path;
	}

	public static string ReadIn(in ValueMatch value)
	{
		return value.Path;
	}

	public static int ReadSame(ref TargetReceiver value)
	{
		return value.Value;
	}

	public static int ReadReinterpreted(ref SourceReceiver value)
	{
		return Unsafe.As<SourceReceiver, TargetReceiver>(ref value).Value;
	}
}
public struct SourceReceiver
{
	public int Value;
}
public struct TargetReceiver
{
	private int _value;

	public int Value => _value;
}

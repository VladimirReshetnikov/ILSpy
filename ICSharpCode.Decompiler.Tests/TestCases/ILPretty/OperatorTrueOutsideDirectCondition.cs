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

public static class Conditions
{
	public static bool IsTrue(TruthValue value)
	{
		return value ? true : false;
	}

	public static bool IsOrdinaryTrue(int value)
	{
		return OrdinaryMethods.op_True(value);
	}

	public static void RunWhenTrue(TruthValue value)
	{
		if (value)
		{
			Console.WriteLine("true");
		}
	}

	public static void RunWhenNotTrue(TruthValue value)
	{
		if (!(value ? true : false))
		{
			Console.WriteLine("not true");
		}
	}

	public static void RunWhenNotTrueAndEnabled(TruthValue value, bool enabled)
	{
		if (!(value ? true : false) && enabled)
		{
			Console.WriteLine("not true and enabled");
		}
	}
}
public static class OrdinaryMethods
{
	public static bool op_True(int value)
	{
		return value > 0;
	}
}
public struct TruthValue(int state)
{
	private readonly int state = state;

	public static bool operator true(TruthValue value)
	{
		return value.state == 1;
	}

	public static bool operator false(TruthValue value)
	{
		return value.state == 0;
	}
}

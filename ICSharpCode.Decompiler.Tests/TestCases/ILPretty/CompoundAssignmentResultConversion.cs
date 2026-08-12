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
using System.Runtime.InteropServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion
{
	public class BaseNode
	{
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Bumper
	{
		public static Bumper operator ++(Bumper value)
		{
			return value;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Counter
	{
		public static Counter operator ++(Counter value)
		{
			return value;
		}

		[SpecialName]
		public void op_IncrementAssignment()
		{
		}
	}

	public class Node : BaseNode
	{
		public static BaseNode operator +(Node left, Node right)
		{
			return null;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Point
	{
		public static Vector operator -(Point left, Point right)
		{
			return default(Vector);
		}

		public static Point operator +(Point left, Point right)
		{
			return default(Point);
		}
	}

	public static class Screen
	{
		public static Counter Total;

		public static Bumper Value;

		public static void BumpTotal()
		{
			Counter total = Total;
			Total = Counter.op_Increment(total);
		}

		public static void BumpValue()
		{
			Value++;
		}

		public static Point Convert(Point relativePosition, Point offset)
		{
			relativePosition = (Point)(relativePosition - offset);
			return relativePosition;
		}

		public static Point ConvertChecked(Point relativePosition, Point offset)
		{
			relativePosition = checked((Point)(relativePosition - offset));
			return relativePosition;
		}

		public static Point Add(Point relativePosition, Point offset)
		{
			relativePosition += offset;
			return relativePosition;
		}

		public static Node Combine(Node node, Node other)
		{
			node = (Node)(node + other);
			return node;
		}

		public static Tally Accumulate(Tally tally, Tally other)
		{
			tally = tally + other;
			return tally;
		}

		public static short AddSmall(short value, short offset)
		{
			value += offset;
			return value;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Tally
	{
		public static Tally operator +(Tally left, Tally right)
		{
			return default(Tally);
		}

		[SpecialName]
		public void op_AdditionAssignment(Tally other)
		{
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector
	{
		public static explicit operator Point(Vector value)
		{
			return default(Point);
		}

		public static explicit operator checked Point(Vector value)
		{
			return default(Point);
		}
	}
}

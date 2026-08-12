using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class ScopedRefStructLocal
	{
		public static int Confined(bool c, int n)
		{
			scoped Span<byte> span;
			if (c)
			{
				Console.WriteLine("side effect");
				span = stackalloc byte[16];
			}
			else
			{
				span = stackalloc byte[8];
			}
			span[0] = (byte)n;
			return span[0] + span.Length;
		}

		public static Span<byte> Escaping(bool c, int n)
		{
			Span<byte> result;
			if (c)
			{
				Console.WriteLine("side effect");
				result = stackalloc byte[16];
			}
			else
			{
				result = stackalloc byte[8];
			}
			result[0] = (byte)n;
			return result;
		}

		public static int DerivedFromSliceAndHelper(bool c, int n)
		{
			Span<byte> b = stackalloc byte[16];
			scoped Span<byte> span;
			if (c)
			{
				Console.WriteLine("side effect");
				span = b.Slice(1, 4);
			}
			else
			{
				span = Wrap(b);
			}
			span[0] = (byte)n;
			return span.Length;
		}

		public static int DiscardedRefStructSink(bool c, int n)
		{
			Span<byte> span = stackalloc byte[16];
			scoped Span<byte> s;
			if (c)
			{
				Console.WriteLine("side effect");
				s = span.Slice(1, 4);
			}
			else
			{
				s = span;
			}
			Echo(s);
			return n;
		}

		public static int CopiedIntoConfinedLocal(bool c, int n)
		{
			Span<byte> span = stackalloc byte[16];
			scoped Span<byte> span2;
			if (c)
			{
				Console.WriteLine("side effect");
				span2 = span.Slice(1, 4);
			}
			else
			{
				span2 = span;
			}
			ReadOnlySpan<byte> s = span2;
			return Count(s) + s.Length;
		}

		public static int WideArraySpanStaysBare(bool c, byte[] array, int n)
		{
			Span<byte> span;
			if (c)
			{
				Console.WriteLine("side effect");
				span = array.AsSpan(1);
			}
			else
			{
				span = array;
			}
			span[0] = (byte)n;
			return span.Length;
		}

		public static int SwapInitializedSpans(int n)
		{
			scoped Span<int> span = default(Span<int>);
			Span<int> span2 = stackalloc int[16];
			Span<int> span3 = stackalloc int[16];
			span2[0] = n;
			span3[0] = n + 1;
			span = span3;
			span3 = span2;
			span2 = span;
			return span2[0] + span3[0];
		}

		private static Span<byte> Wrap(Span<byte> b)
		{
			return b.Slice(0, b.Length);
		}

		private static Span<byte> Echo(Span<byte> s)
		{
			return s;
		}

		private static int Count(ReadOnlySpan<byte> s)
		{
			return s.Length;
		}
	}
}

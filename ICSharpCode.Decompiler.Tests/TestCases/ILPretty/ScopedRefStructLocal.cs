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
	}
}

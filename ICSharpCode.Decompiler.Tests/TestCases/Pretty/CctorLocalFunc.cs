using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal static class CctorLocalFunc
	{
		public static event Action E1;

		public static event Action E2;

		public static event Action E3;

		static CctorLocalFunc()
		{
			int n = 42;
			E1 += () => {
				Use(n);
			};
			E2 += () => {
				Use(n);
			};
			E3 += () => {
				Use(n);
			};
			static void Use(int x)
			{
				Console.WriteLine(x);
			}
		}
	}
}

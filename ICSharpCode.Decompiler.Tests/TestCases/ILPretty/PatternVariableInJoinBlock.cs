using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class PatternVariableInJoinBlock
	{
		public static void Test(object o, bool outer, bool cond1, bool cond2)
		{
			string text;
			bool flag;
			if (outer)
			{
				text = o as string;
				if (text != null)
				{
					if (text.Length > 5)
					{
						if (!cond1)
						{
							goto IL_001e;
						}
					}
					else if (!cond2)
					{
						goto IL_001e;
					}
					flag = true;
					goto IL_0024;
				}
				Console.WriteLine("not string");
			}
			goto IL_0046;
			IL_0046:
			Console.WriteLine("done");
			return;
			IL_001e:
			flag = false;
			goto IL_0024;
			IL_0024:
			if (flag)
			{
				Console.WriteLine(text.Length);
			}
			Console.WriteLine(text);
			goto IL_0046;
		}
	}
}

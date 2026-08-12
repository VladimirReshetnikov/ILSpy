using System;
using System.Collections.Generic;

using Microsoft.VisualBasic.CompilerServices;

public class VBYieldInTryCatch
{
	private sealed class ThrowingSequence
	{
		private readonly string label;

		private readonly bool throwBeforeFirstValue;

		private readonly bool throwAfterFirstValue;

		public ThrowingSequence(string label, bool throwBeforeFirstValue, bool throwAfterFirstValue)
		{
			this.label = label;
			this.throwBeforeFirstValue = throwBeforeFirstValue;
			this.throwAfterFirstValue = throwAfterFirstValue;
		}

		public ThrowingEnumerator GetEnumerator()
		{
			Console.WriteLine(label + ":get-enumerator");
			if (throwBeforeFirstValue)
			{
				throw new InvalidOperationException("before");
			}
			return new ThrowingEnumerator(label, throwAfterFirstValue);
		}
	}

	private struct ThrowingEnumerator
	{
		private readonly string label;

		private readonly bool throwAfterFirstValue;

		private int position;

		public int Current => position;

		public ThrowingEnumerator(string label, bool throwAfterFirstValue)
		{
			this = default(ThrowingEnumerator);
			this.label = label;
			this.throwAfterFirstValue = throwAfterFirstValue;
			position = 0;
		}

		public bool MoveNext()
		{
			checked
			{
				position++;
				Console.WriteLine(label + ":move-next:" + Conversions.ToString(position));
				if (throwAfterFirstValue && position == 2)
				{
					throw new InvalidOperationException("after");
				}
				return position <= 2;
			}
		}
	}

	private static IEnumerable<int> Enumerate(string label, ThrowingSequence sequence)
	{
		ThrowingEnumerator throwingEnumerator = default(ThrowingEnumerator);
		bool flag = false;
		bool flag2 = false;
		while (!flag2)
		{
			bool flag3 = false;
			int num = default(int);
			try
			{
				if (!flag)
				{
					throwingEnumerator = sequence.GetEnumerator();
					flag = true;
				}
				if (throwingEnumerator.MoveNext())
				{
					int item = throwingEnumerator.Current;
					num = TraceValue(label, item);
					flag3 = true;
				}
				else
				{
					flag2 = true;
				}
			}
			catch (InvalidOperationException ex)
			{
				ProjectData.SetProjectError(ex);
				InvalidOperationException ex2 = ex;
				Console.WriteLine(label + ":catch:" + ex2.Message);
				ProjectData.ClearProjectError();
				flag2 = true;
			}
			if (flag3)
			{
				yield return num;
			}
		}
	}

	private static int TraceValue(string label, int value)
	{
		Console.WriteLine(label + ":yield:" + Conversions.ToString(value));
		return value;
	}

	private static void Run(string label, ThrowingSequence sequence)
	{
		Console.WriteLine(label + ":start");
		foreach (int item in Enumerate(label, sequence))
		{
			Console.WriteLine(label + ":consumer:" + Conversions.ToString(item));
		}
		Console.WriteLine(label + ":end");
	}

	[STAThread]
	public static void Main()
	{
		Run("before", new ThrowingSequence("before", throwBeforeFirstValue: true, throwAfterFirstValue: false));
		Run("after", new ThrowingSequence("after", throwBeforeFirstValue: false, throwAfterFirstValue: true));
		Run("complete", new ThrowingSequence("complete", throwBeforeFirstValue: false, throwAfterFirstValue: false));
	}
}

using System;
using System.Collections.Generic;

public class VBClosures
{
	public static List<Func<int>> PerIteration(IEnumerable<int> items, int factor)
	{
		List<Func<int>> list = new List<Func<int>>();
		checked
		{
			foreach (int item in items)
			{
				int num = item * factor;
				list.Add(() => num + factor);
			}
			return list;
		}
	}

	public static Func<int, int> SingleCapture(int factor)
	{
		return (int n) => checked(n * factor);
	}
}

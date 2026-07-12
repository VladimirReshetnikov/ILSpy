using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public readonly struct Item(string name, in Point point)
	{
		public readonly string Name = name;

		public readonly Point Point = point;
	}
	public readonly struct Point
	{
		public readonly int X;

		public readonly int Y;
	}
	public class QueryExpressionInParameter
	{
		public static Item[] ExtractItems(IEnumerable<string> groups, Func<string, IEnumerable<Point>> values)
		{
			return (from g in groups
				from p in values(g)
				select new Item(g, p)).ToArray();
		}
	}
}

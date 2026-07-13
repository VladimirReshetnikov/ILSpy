using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class OrderByThenByRangeVariable
	{
		public class Category
		{
			public int OrderNum;
		}

		public class Record
		{
			public Category Group;

			public Category Language;

			public string Name;
		}

		public IEnumerable<string> Sort(IEnumerable<Record> records)
		{
			return from g in records
				orderby g.Group.OrderNum, g.Language.OrderNum
				select g.Name;
		}
	}
}

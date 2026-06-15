using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class ExpressionTreeDeinlinedInArrayInitializer
{
	public IReadOnlyList<object> ItemsA { get; }

	public IReadOnlyList<object> ItemsB { get; }

	public ExpressionTreeDeinlinedInArrayInitializer()
	{
		Populator populator = new Populator();
		ItemsA = new object[1] { populator.Add((KeyA x) => x.FlagA, v: true) };
		populator = new Populator();
		ItemsB = new object[1] { populator.Add((KeyB x) => x.FlagB, v: false) };
	}
}
public class KeyA
{
	public bool FlagA;
}
public class KeyB
{
	public bool FlagB;
}
public class Populator
{
	public Populator Add<TKey>(Expression<Func<TKey, bool>> e, bool v)
	{
		return this;
	}
}

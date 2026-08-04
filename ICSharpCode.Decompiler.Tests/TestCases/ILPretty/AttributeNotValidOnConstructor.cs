using System;

[OnlyClassAndMethod]
public class Holder
{
	public Holder(int seed)
	{
	}

	[OnlyClassAndMethod]
	public void Run()
	{
	}
}
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class OnlyClassAndMethodAttribute : Attribute
{
}

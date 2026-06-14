using System;

public class BaseType
{
	public BaseType()
	{
	}

	public BaseType(string s)
	{
	}

	public BaseType(int mode)
	{
	}
}
public class DoubleUseTemporary : BaseType
{
	public DoubleUseTemporary(Holder h)
		: base((h.Name != null) ? h.Name : "fallback")
	{
	}
}
public class FieldInitsBeforeBaseCall : BaseType
{
	public readonly bool Flag;

	public readonly int Value;

	public FieldInitsBeforeBaseCall(int seed)
	{
		int num = seed + 1;
		Flag = num > 0 && num < 1000;
		Value = num;
	}
}
public class GuardBeforeBaseCall : BaseType
{
	public GuardBeforeBaseCall(int mode)
		: base(mode)
	{
		if (mode < 0)
		{
			throw new ArgumentOutOfRangeException("mode");
		}
	}
}
public class Holder
{
	public string Name;
}
public class SingleUseTemporary : BaseType
{
	public SingleUseTemporary(Holder h)
		: base(h.Name)
	{
	}
}

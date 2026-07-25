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

	public BaseType(Func<int> factory)
	{
	}
}
public class Closure
{
	public int X;

	public int Invoke()
	{
		return X;
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
		Flag = (num > 0) & (num < 1000);
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
public class InitializedObjectTemporary : BaseType
{
	public int Copy;

	public InitializedObjectTemporary(int x)
		: base(new Closure {
			X = x
		}.Invoke)
	{
		Copy = x;
	}
}
public class SingleUseTemporary : BaseType
{
	public SingleUseTemporary(Holder h)
		: base(h.Name)
	{
	}
}

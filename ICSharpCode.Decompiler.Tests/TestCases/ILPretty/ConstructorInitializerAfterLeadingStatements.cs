using System;

public class BaseType
{
	public BaseType()
	{
	}

	public BaseType(string s)
	{
	}

	public BaseType(string label, int mode)
	{
	}

	public BaseType(int mode)
	{
	}

	public BaseType(Func<int> factory)
	{
	}

	public BaseType(in ReadonlyConstructorInfo info)
	{
	}
}
public class BranchAssignedPatternArgument : BaseType
{
	public BranchAssignedPatternArgument(ConstructorArgumentOwner owner)
		: base("branch", (owner is SpecialConstructorArgumentOwner specialConstructorArgumentOwner) ? specialConstructorArgumentOwner.Value : (owner.Parent switch {
			var parent => ((parent != null && parent.Enabled) ? parent.Value : 0) + owner.Value,
		}))
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
public class ConstructorArgumentOwner
{
	public bool Enabled;

	public ConstructorArgumentOwner Parent;

	public int Value;
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
public class OverloadSensitiveBaseType
{
	public OverloadSensitiveBaseType(ReadonlyConstructorInfo info)
	{
	}

	public OverloadSensitiveBaseType(in ReadonlyConstructorInfo info)
	{
	}
}
public class OverloadSensitiveReadonlyStructTemporaryBaseConstructor : OverloadSensitiveBaseType
{
	public OverloadSensitiveReadonlyStructTemporaryBaseConstructor(int value)
	{
		ReadonlyConstructorInfo info = new ReadonlyConstructorInfo {
			Value = value
		};
		base._002Ector(in info);
	}
}
public struct ReadonlyConstructorInfo
{
	public int Value;
}
public class ReadonlyStructLvalueBaseConstructor : BaseType
{
	public ReadonlyStructLvalueBaseConstructor(in ReadonlyConstructorInfo info)
		: base(in info)
	{
	}
}
public class ReadonlyStructTemporaryBaseConstructor : BaseType
{
	public ReadonlyStructTemporaryBaseConstructor(int value)
		: base(new ReadonlyConstructorInfo {
			Value = Compute(value)
		})
	{
	}

	private static int Compute(int value)
	{
		return value + 1;
	}
}
public class SharedFallbackConstructorArgument : BaseType
{
	public SharedFallbackConstructorArgument(bool first, bool second)
		: base(first ? ((!second) ? 3 : 1) : ((!second) ? 3 : 2))
	{
	}
}
public class SingleUseTemporary : BaseType
{
	public SingleUseTemporary(Holder h)
		: base(h.Name)
	{
	}
}
public class SpecialConstructorArgumentOwner : ConstructorArgumentOwner
{
}

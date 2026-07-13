using System;

public class Base
{
	public object Value;
}
public class C
{
	public static Func<Derived, Base> MakeIdentity()
	{
		return (Derived e) => e;
	}

	public static Func<Derived, object> MakeReader()
	{
		return (Derived b) => b.Value;
	}
}
public class Derived : Base
{
}

using System;

[AttributeUsage(AttributeTargets.All)]
public sealed class AnywhereAttribute : Attribute
{
}
public class Holder
{
	public int Plain;
	[Anywhere]
	public int Kept;
	public int Number => 0;
	public event EventHandler Fired {
		add {
		}
		remove {
		}
	}
	public int GetValue(int arg)
	{
		return 0;
	}
	public static Holder operator +(Holder a, Holder b)
	{
		return null;
	}
}
[AttributeUsage(AttributeTargets.Class)]
public sealed class OnlyClassAttribute : Attribute
{
}
public enum Season
{
	Spring,
	[Anywhere]
	Summer
}

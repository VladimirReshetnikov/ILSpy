using System;

public class Callee
{
	public static void Load(ref Guid key, out string result)
	{
		result = "value";
	}
}
public class Caller
{
	public static string Call(ref Guid key)
	{
		Callee.Load(ref key, out string result);
		return result;
	}
}
namespace System
{
	public struct Guid
	{
		public int Value;
	}
}

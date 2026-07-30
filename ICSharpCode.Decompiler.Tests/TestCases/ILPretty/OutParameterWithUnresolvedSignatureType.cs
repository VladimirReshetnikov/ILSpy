using System;
using System.Runtime.CompilerServices;

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
		// The reinterpretation is expected: the two System.Guid definitions really are distinct
		// types here. What matters is 'out', which is lost when the call resolves to a fake method.
		Callee.Load(ref System.Runtime.CompilerServices.Unsafe.As<Guid, Guid>(ref key), out var result);
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

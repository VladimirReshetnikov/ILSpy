using System.Runtime.InteropServices;

public enum Flavor
{
	None,
	Some
}
public class OptionalDefaults
{
	public void OutStruct([Optional] out OptionalHandle h)
	{
		h = default(OptionalHandle);
	}
	public void ByValueStruct(OptionalHandle h = default(OptionalHandle))
	{
	}
	public void OutReference([Optional][DefaultParameterValue(null)] out string s)
	{
		s = null;
	}
	public void ByValueInt(int i = 3)
	{
	}
	public void RefEnum([Optional][DefaultParameterValue(Flavor.None)] ref Flavor f)
	{
	}
}
public struct OptionalHandle
{
	public int Value;
}

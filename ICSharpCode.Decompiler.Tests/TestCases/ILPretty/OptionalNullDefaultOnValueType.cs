using System.Runtime.InteropServices;

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
}
public struct OptionalHandle
{
	public int Value;
}

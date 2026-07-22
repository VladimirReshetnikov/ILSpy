using System.Runtime.InteropServices;

public static class Caller
{
	public static string CallConstrained()
	{
		RefLike refLike = default(RefLike);
		return refLike.ToString();
	}
}
[StructLayout(LayoutKind.Sequential, Size = 1)]
public ref struct RefLike
{
	public new string ToString()
	{
		return "RefLike";
	}
}

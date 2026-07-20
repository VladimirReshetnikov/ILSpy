using System.Collections.Generic;

public class C
{
	public void M(string a, int[] rules = null)
	{
	}

	public void M(string a, IEnumerable<string> predefined = null, int[] rules = null)
	{
	}

	public void Test()
	{
		M("x", null, null);
	}
}

using System.Collections.Generic;

public class HelperBase
{
	public string Tag;
	public int[] Data;
	public bool Flag;

	public HelperBase(string tag, int[] data, bool flag)
	{
		Tag = tag;
		Data = data;
		Flag = flag;
	}
}
public class HelperDerived : HelperBase
{
	public HelperDerived(int[] items, string tag)
		: base(tag, ILSpyHelper_ComputeData(items), flag: true)
	{
	}

	// ILSpy synthesized this method: the argument it returns is computed by statements, which a constructor initializer cannot contain.
	private static int[] ILSpyHelper_ComputeData(int[] items)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < items.Length; i++)
		{
			list.Add(items[i]);
		}
		for (int j = 0; j < 8; j += 2)
		{
			list.Add(j);
		}
		return list.ToArray();
	}
}
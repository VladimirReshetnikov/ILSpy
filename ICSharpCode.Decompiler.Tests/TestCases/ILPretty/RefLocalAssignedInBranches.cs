public struct Entry
{
	public int Value;
}
public static class Lookup
{
	public static Entry First;

	public static Entry Second;

	public static void Note()
	{
	}

	public static int Read(bool flag)
	{
		Entry reference_placeholder = default(Entry);
		ref Entry reference = ref reference_placeholder;
		if (flag)
		{
			Note();
			reference = ref First;
		}
		else
		{
			Note();
			reference = ref Second;
		}
		return reference.Value + reference.Value;
	}
}

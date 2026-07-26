using System.Runtime.CompilerServices;

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
		scoped ref Entry reference = ref System.Runtime.CompilerServices.Unsafe.NullRef<Entry>();
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

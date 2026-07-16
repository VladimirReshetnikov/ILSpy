public class Repro
{
	private class Hidden
	{
	}

	public static void Run()
	{
		DoWork(null);
		static void DoWork(Hidden value)
		{
		}
	}
}

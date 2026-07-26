using System;

public static class Probe
{
	public static void Run(Exception e)
	{
		Inner(e, 1);
		static void Inner(Exception e, int _)
		{
			string message = e.Message;
		}
	}
}

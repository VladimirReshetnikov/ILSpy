using System;

public class C
{
	public static void M()
	{
		Action a = default(Action);
		a = () => {
			a();
		};
		a();
	}
}

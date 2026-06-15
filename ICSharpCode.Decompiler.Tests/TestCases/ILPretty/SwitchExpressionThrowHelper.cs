using System;

public static class C
{
	public static string Describe(object o)
	{
		if (!(o is string))
		{
			if (!(o is Exception))
			{
				throw new InvalidOperationException();
			}
			return "exception";
		}
		return "string";
	}
}

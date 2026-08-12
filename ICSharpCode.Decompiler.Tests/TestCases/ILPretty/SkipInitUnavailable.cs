using System;

public class SkipInitUnavailable
{
	public static void Loop(int n)
	{
		int value = default(int);
		for (int i = 0; i < n; i++)
		{
			Console.Write(value);
			value = i;
		}
	}
	public static T LoopGeneric<T>(T value, int count)
	{
		T result = default(T);
		for (int i = 0; i < count; i++)
		{
			result = value;
		}
		return result;
	}
	public static int LoopRef(int value, int count)
	{
		int result = default(int);
		int reference_placeholder = default(int);
		ref int reference = ref reference_placeholder;
		for (int i = 0; i < count; i++)
		{
			result = reference;
			reference = ref value;
		}
		return result;
	}
}

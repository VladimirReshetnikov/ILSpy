using System;

public class UninitializedPointer
{
	public unsafe static void Loop(int n, byte* source)
	{
		byte* ptr = default(byte*);
		for (int i = 0; i < n; i++)
		{
			Console.Write(*ptr);
			ptr = source;
		}
	}
}

using System;

public class StackAllocTarget
{
	public unsafe static void Fill(int flag)
	{
		nint num = 0;
		if (flag != 0)
		{
			int* ptr = stackalloc int[4];
			num = (nint)ptr;
			*(int*)num = 7;
		}
		Console.Write(*(int*)num);
	}
}

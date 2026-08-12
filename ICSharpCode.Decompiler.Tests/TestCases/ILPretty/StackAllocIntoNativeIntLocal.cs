using System;

public struct EventData
{
	public ulong DataPointer;

	public uint Size;
}

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

	public unsafe static void FillEventData(int count)
	{
		EventData* ptr = stackalloc EventData[count];
		byte* ptr2 = (byte*)ptr;
		EventData* ptr3 = (EventData*)ptr2;
		ptr3->Size = 7u;
	}
}

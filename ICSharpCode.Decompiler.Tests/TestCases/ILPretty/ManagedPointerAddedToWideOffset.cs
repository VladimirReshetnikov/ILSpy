using System;
using System.Runtime.CompilerServices;

public static class Offsets
{
	public static int Base;

	public static ref byte AdvanceByWideOffset(ref byte start)
	{
		return ref System.Runtime.CompilerServices.Unsafe.Add(elementOffset: Base, source: ref start);
	}

	public unsafe static ref byte AdvanceByReference(ref byte start, ref byte offset)
	{
		return ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref start, (IntPtr)(nint)System.Runtime.CompilerServices.Unsafe.AsPointer(ref offset));
	}
}

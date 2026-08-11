using System;

public static class ModernRefSafetyScopedInParameter
{
	public static ReadOnlySpan<byte> GetSpan(in ModernSequence sequence)
	{
		return sequence.Data;
	}
}

public struct ModernSequence
{
	public byte[] Data;
}

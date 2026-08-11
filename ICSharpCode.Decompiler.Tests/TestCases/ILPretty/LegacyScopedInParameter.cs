using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class LegacyScopedInParameter
{
	public static LegacySequence? NullableSequence => null;

	public static ReadOnlySpan<byte> GetSpan(scoped in LegacySequence sequence)
	{
		if (sequence.HasData)
		{
			return sequence.Memory.Span;
		}
		return Copy(in sequence);
	}

	private static byte[] Copy(in LegacySequence sequence)
	{
		return new byte[0];
	}

	private static ReadOnlySpan<byte> GetNullableSpan(scoped in LegacySequence? sequence)
	{
		if (!sequence.HasValue)
		{
			return default(ReadOnlySpan<byte>);
		}
		return GetSpan(sequence.Value);
	}

	private static LegacySequence Create()
	{
		return new LegacySequence(new byte[1]);
	}

	public static ReadOnlySpan<byte> FromTemporary()
	{
		return GetSpan(Create());
	}

	public static ReadOnlySpan<byte> FromNullableProperty()
	{
		return GetNullableSpan(NullableSequence);
	}

	public static ReadOnlySpan<byte> ExplicitlyUnscoped([UnscopedRef] in LegacySequence sequence)
	{
		return sequence.Memory.Span;
	}

	public static ReadOnlySpan<LegacySequence> StorageEscapes(in LegacySequence sequence)
	{
		return MemoryMarshal.CreateReadOnlySpan(in LegacyUnsafe.AsRef(in sequence), 1);
	}
}

public struct LegacySequence(byte[] data)
{
	private byte[] data = data;

	public bool HasData => data != null;

	public ReadOnlyMemory<byte> Memory => data;
}

public static class LegacyUnsafe
{
	public static ref T AsRef<T>([UnscopedRef] in T source)
	{
		return ref Unsafe.AsRef(in source);
	}
}

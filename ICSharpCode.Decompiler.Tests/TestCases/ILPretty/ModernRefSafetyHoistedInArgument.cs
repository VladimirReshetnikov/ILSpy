using System;

public static class StringExtensions
{
	public static ReadOnlySpan<char> SliceOffAfterLast(this in ReadOnlySpan<char> span, in char delimiter)
	{
		return span.Slice(0);
	}

	public static void Walk(string text)
	{
		ReadOnlySpan<char> span = MemoryExtensions.AsSpan(text).SliceOffAfterLast('.');
		char delimiter = '.';
		while (span.Length > 0)
		{
			span = span.SliceOffAfterLast(delimiter);
		}
	}
}

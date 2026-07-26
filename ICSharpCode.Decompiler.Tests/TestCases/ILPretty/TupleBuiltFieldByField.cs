using System;

public static class Builder
{
	public static (string, int, string) Complete(string s)
	{
		return (s, 7, s);
	}

	public static (string, int, string) Partial(string s)
	{
		return new ValueTuple<string, int, string> {
			Item1 = s,
			Item3 = s
		};
	}
}
namespace System
{
	[Serializable]
	public struct ValueTuple<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
	{
		public T1 Item1 = item1;

		public T2 Item2 = item2;

		public T3 Item3 = item3;
	}
}

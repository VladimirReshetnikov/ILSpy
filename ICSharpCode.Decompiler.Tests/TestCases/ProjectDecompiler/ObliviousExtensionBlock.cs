namespace ExtensionBlockTest;

public static class Extensions
{
	extension(string value)
	{
		public int Length => value.Length;
	}

	extension<T>(T value)
	{
		public T Identity => value;
	}

	extension((string Text, int Count) value)
	{
		public int TextLength => value.Text.Length;
	}
}

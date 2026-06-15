namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	file static class FileLocalHelper
	{
		public static int Twice(int value)
		{
			return value * 2;
		}
	}

	file class FileLocalState
	{
		public int Value;
	}

	internal class FileLocalConsumer
	{
		public int UseHelper(int value)
		{
			return FileLocalHelper.Twice(value) + 1;
		}

		public int UseState(int value)
		{
			FileLocalState fileLocalState = new FileLocalState();
			fileLocalState.Value = value;
			return fileLocalState.Value + fileLocalState.Value;
		}
	}
}

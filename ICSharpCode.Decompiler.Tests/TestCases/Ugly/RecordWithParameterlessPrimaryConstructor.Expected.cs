namespace ICSharpCode.Decompiler.Tests.TestCases.Ugly;

public record RecordWithParameterlessPrimaryConstructor
{
	public int TabSize { get; init; } = 4;

	public RecordWithParameterlessPrimaryConstructor()
	{
	}

	public RecordWithParameterlessPrimaryConstructor(int tabSize)
		: this()
	{
		TabSize = tabSize;
	}
}

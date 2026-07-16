namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class FieldKeywordCtorBackingFieldWrite<T>
	{
		public readonly string Fallback;

		private string m_ExternallyWritten;

		public string Text => field ?? Fallback;

		public string ExternallyWritten => this.m_ExternallyWritten ?? Fallback;

		public FieldKeywordCtorBackingFieldWrite(string customText, string fallback)
		{
			Fallback = fallback;
			Text = customText;
		}

		public void SetExternallyWritten(string value)
		{
			this.m_ExternallyWritten = value;
		}
	}
}

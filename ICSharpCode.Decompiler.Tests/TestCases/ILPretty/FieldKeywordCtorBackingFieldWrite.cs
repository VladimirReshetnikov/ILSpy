namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class FieldKeywordCtorBackingFieldWrite<T>
	{
		public readonly string Fallback;

		private string m_Text;

		public string Text => this.m_Text ?? Fallback;

		public FieldKeywordCtorBackingFieldWrite(string customText, string fallback)
		{
			Fallback = fallback;
			this.m_Text = customText;
		}
	}
}

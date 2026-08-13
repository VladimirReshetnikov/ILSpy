using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class FieldKeywordCtorBackingFieldWrite
	{
		[CompilerGenerated]
		private string ExternallyWritten__BackingField;

		public readonly string Fallback;

		[field: AllowNull]
		public string Text {
			[CompilerGenerated]
			get {
				return field ?? Fallback;
			}
		}

		public string ExternallyWritten => ExternallyWritten__BackingField ?? Fallback;

		public FieldKeywordCtorBackingFieldWrite(string customText, string fallback)
		{
			Fallback = fallback;
			Text = customText;
		}

		public void SetExternallyWritten(string value)
		{
			ExternallyWritten__BackingField = value;
		}
	}

	public class FieldKeywordCtorBackingFieldWriteGeneric<T>
	{
		public readonly string Fallback;

		[field: AllowNull]
		public string Text => field ?? Fallback;

		public FieldKeywordCtorBackingFieldWriteGeneric(string customText, string fallback)
		{
			Fallback = fallback;
			Text = customText;
		}
	}
}

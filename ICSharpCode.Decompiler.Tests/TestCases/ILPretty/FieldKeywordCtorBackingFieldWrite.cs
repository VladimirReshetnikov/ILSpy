using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class FieldKeywordCtorBackingFieldWrite
	{
		[AllowNull]
		[CompilerGenerated]
		private readonly string _003CText_003Ek__BackingField;

		[CompilerGenerated]
		private string _003CExternallyWritten_003Ek__BackingField;

		public readonly string Fallback;

		public string Text {
			[CompilerGenerated]
			get {
				return _003CText_003Ek__BackingField ?? Fallback;
			}
		}

		public string ExternallyWritten => _003CExternallyWritten_003Ek__BackingField ?? Fallback;

		public FieldKeywordCtorBackingFieldWrite(string customText, string fallback)
		{
			Fallback = fallback;
			this._003CText_003Ek__BackingField = customText;
		}

		public void SetExternallyWritten(string value)
		{
			_003CExternallyWritten_003Ek__BackingField = value;
		}
	}

	public class FieldKeywordCtorBackingFieldWriteGeneric<T>
	{
		public readonly string Fallback;

		[AllowNull]
		private readonly string m_Text;

		public string Text => this.m_Text ?? Fallback;

		public FieldKeywordCtorBackingFieldWriteGeneric(string customText, string fallback)
		{
			Fallback = fallback;
			this.m_Text = customText;
		}
	}
}

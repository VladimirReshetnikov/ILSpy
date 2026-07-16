namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class FieldKeyword
	{
		internal class PrimaryConstructorParameter(string field)
		{
			public string Value => @field;
		}

		public string ChangeDetectingSetter {
			get {
				return field;
			}
			set {
				if (field != value)
				{
					field = value;
					OnChanged();
				}
			}
		}

		public int LazyGetterOnly {
			get {
				if (field == 0)
				{
					field = Compute();
				}
				return field;
			}
		}

		public string WithInitializer {
			get {
				return field;
			}
			set {
				field = value.Trim();
			}
		} = string.Empty;

		public string TrivialGetterCustomSetter {
			get {
				return field;
			}
			set {
				field = value ?? "";
			}
		}

		private void OnChanged()
		{
		}

		private int Compute()
		{
			return 42;
		}
	}
}

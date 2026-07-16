namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class FieldKeyword
	{
		internal class ConstructorAssignedGetterOnlyProperty
		{
			private readonly string fallback;

			public string Text => field ?? fallback;

			public ConstructorAssignedGetterOnlyProperty(string text, string fallback)
			{
				this.fallback = fallback;
				Text = text ?? this.fallback;
			}
		}

		internal class ConstructorParameterNameCollision
		{
			private readonly string fallback;

			public string Text => field ?? fallback;

			public ConstructorParameterNameCollision(string Text, string fallback)
			{
				this.fallback = fallback;
				this.Text = Text ?? this.fallback;
			}
		}

		internal static class StaticConstructorAssignedGetterOnlyProperty
		{
			private static readonly string fallback;

			public static string Text => field ?? fallback;

			static StaticConstructorAssignedGetterOnlyProperty()
			{
				fallback = string.Empty;
				Text = fallback;
			}
		}

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

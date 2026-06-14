namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class StructWithExpression
	{
		internal struct InitOnlyStruct
		{
			public bool MultiLine { get; init; }

			public int QuoteCount { get; init; }
		}

		internal struct MutableStruct
		{
			public int A { get; set; }

			public int B { get; set; }
		}

		private InitOnlyStruct field;

		public StructWithExpression(InitOnlyStruct field)
		{
			this.field = field;
		}

		// A 'with' on a non-record value type that mutates init-only members must be
		// recovered as a 'with' expression; assigning the init-only setters loosely
		// would not compile (CS8852).
		public InitOnlyStruct FromParameter(InitOnlyStruct p, int n)
		{
			return p with {
				MultiLine = false,
				QuoteCount = n
			};
		}

		// The copied value can also originate from a field load (ldobj).
		public InitOnlyStruct FromField(int n)
		{
			return field with {
				QuoteCount = n
			};
		}

		// A plain mutable-struct copy followed by ordinary setter assignments is not a
		// 'with' expression and must stay as separate statements.
		public MutableStruct Mutable(MutableStruct src, int n)
		{
			MutableStruct result = src;
			result.A = n;
			result.B = 5;
			return result;
		}
	}
}

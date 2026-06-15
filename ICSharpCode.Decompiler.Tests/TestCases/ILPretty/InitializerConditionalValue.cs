namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue
{
	public class EntryOptions
	{
		private bool _decompose;

		private bool _remove;

		private int _emphasis;

		public bool Decompose {
			get {
				return _decompose;
			}
			init {
				_decompose = value;
			}
		}

		public bool Remove {
			get {
				return _remove;
			}
			init {
				_remove = value;
			}
		}

		public int Emphasis {
			get {
				return _emphasis;
			}
			init {
				_emphasis = value;
			}
		}
	}
	public static class Repro
	{
		public static EntryOptions Build(object expression, bool condition, int emphasis)
		{
			return new EntryOptions {
				Decompose = true,
				Remove = true,
				Emphasis = (((condition || expression != null) ? true : false) ? emphasis : 2)
			};
		}
	}
}

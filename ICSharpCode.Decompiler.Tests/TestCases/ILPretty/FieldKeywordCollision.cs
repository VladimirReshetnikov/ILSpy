using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	internal class FieldKeywordCollision
	{
		public int field;

		[CompilerGenerated]
		private int _003CProp_003Ek__BackingField;

		public int Prop {
			[CompilerGenerated]
			get {
				return _003CProp_003Ek__BackingField + field;
			}
		}
	}
}

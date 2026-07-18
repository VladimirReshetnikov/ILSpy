using System.Diagnostics;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class AutoPropertyBackingFieldDebuggerBrowsable
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		[field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string Key { get; }

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		[field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Adjusted {
			get {
				return field + 1;
			}
			set {
				field = value;
			}
		}

		public AutoPropertyBackingFieldDebuggerBrowsable(string key)
		{
			Key = key;
		}
	}

	public class AutoPropertyBackingFieldDebuggerBrowsableGeneric<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		[field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value { get; }

		public AutoPropertyBackingFieldDebuggerBrowsableGeneric(T value)
		{
			Value = value;
		}
	}
}

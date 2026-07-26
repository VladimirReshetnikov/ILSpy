using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Ugly;

public static class NullableBoolToEnum
{
	public enum Compliance
	{
		DeclaredTrue,
		DeclaredFalse,
		InheritedTrue,
		InheritedFalse,
		ImpliedFalse
	}

	private static readonly Dictionary<int, Compliance> Cache = new Dictionary<int, Compliance>();

	private static bool? GetDeclared(out string location)
	{
		location = null;
		return null;
	}

	private static bool IsTrue(Compliance c)
	{
		return c == Compliance.DeclaredTrue;
	}

	public static Compliance Get(int kind)
	{
		if (Cache.TryGetValue(kind, out var value))
		{
			return value;
		}
		bool? declared = GetDeclared(out var _);
		value = (declared.HasValue ? ((declared != true) ? Compliance.DeclaredFalse : Compliance.DeclaredTrue) : ((kind != 2) ? (IsTrue(Get(kind + 1)) ? Compliance.InheritedTrue : Compliance.InheritedFalse) : Compliance.ImpliedFalse));
		if (kind != 2 && kind != 3)
		{
			return value;
		}
		Cache[kind] = value;
		return value;
	}
}

using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public class ExtensionMethodOverloadResolution
	{
		public class Transactions
		{
			public object Execute(string name, Action action)
			{
				action();
				return null;
			}
		}

		public void CallSite(Transactions transactions)
		{
			ExtensionMethodOverloadResolutionHelpers.Execute(transactions, "name", () => new object());
		}
	}

	public static class ExtensionMethodOverloadResolutionHelpers
	{
		public static T Execute<T>(this ExtensionMethodOverloadResolution.Transactions transactions, string name, Func<T> func)
		{
			return func();
		}
	}
}

using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	internal static class ArrayExtensions
	{
		public static bool Contains<T>(this T[] source, T value)
		{
			return true;
		}
	}
	internal class Container
	{
		public string[] GetItems()
		{
			return null;
		}
	}
	internal static class Extensions
	{
		public static bool Contains<T>(this IEnumerable<T> source, T value)
		{
			return false;
		}

		public static int Count<T>(this IEnumerable<T> source)
		{
			return 0;
		}
	}
	internal class NullPropagationExtensionMethod
	{
		private Container container;

		public bool CallSiteWithoutExtensionSyntax(string value)
		{
			Container obj = container;
			return ((obj != null) ? new bool?(Extensions.Contains(obj.GetItems(), value)) : ((bool?)null)) == true;
		}

		public bool CallSiteWithoutExtensionSyntax2(string value)
		{
			Container obj = container;
			if (((obj != null) ? new bool?(Extensions.Contains(obj.GetItems(), value)) : ((bool?)null)) ?? false)
			{
				return true;
			}
			return false;
		}

		public int CallSiteWithExtensionSyntax()
		{
			return (container?.GetItems().Count()).GetValueOrDefault();
		}
	}
}

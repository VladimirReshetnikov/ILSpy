using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ICSharpCode.Decompiler.Tests.TestCases.Correctness
{
	internal class CollectionExpressions
	{
		private static int counter;

		private static int Next(string name)
		{
			counter++;
			Console.WriteLine("  " + name + " -> " + counter);
			return counter;
		}

		private static void Main()
		{
			Console.WriteLine("Interface:");
			Print(InterfaceTarget());
			Print(InterfaceTarget("a", "b"));
			Print(SingleElement("only"));

			Console.WriteLine("List:");
			Print(ListConstants());
			Print(ListValues("x", "y"));

			Console.WriteLine("ImmutableArray:");
			Print(ImmutableConstants());
			Print(ImmutableValues("p", "q"));

			Console.WriteLine("Span:");
			Console.WriteLine(SpanLocal(3, 4));
			Console.WriteLine(SpanArgument(5, 6));

			Console.WriteLine("Spreads:");
			Print(SpreadIntoArray([2, 3]));
			Print(SpreadTwice(["a", "b"], ["c"]));
			Print(SpreadIntoList([4, 5]));
			Print(SpreadIntoInterface([6]));
			Print(SpreadOfList([7, 8]));
			Print(SpreadOfImmutable([9]));
			Console.WriteLine(SpreadOfSpan([1, 2, 3]));

			Console.WriteLine("Evaluation order:");
			counter = 0;
			Console.WriteLine(OrderOfSideEffects());
			counter = 0;
			Console.WriteLine(OrderInsideList());
		}

		private static IReadOnlyList<int> InterfaceTarget()
		{
			return [1, 2, 3];
		}

		private static IEnumerable<string> InterfaceTarget(string a, string b)
		{
			return [a, b];
		}

		private static IReadOnlyList<string> SingleElement(string s)
		{
			return [s];
		}

		private static List<int> ListConstants()
		{
			return [1, 2, 3];
		}

		private static List<string> ListValues(string a, string b)
		{
			return [a, b];
		}

		private static ImmutableArray<int> ImmutableConstants()
		{
			return [1, 2, 3];
		}

		private static ImmutableArray<string> ImmutableValues(string a, string b)
		{
			return [a, b];
		}

		private static int SpanLocal(int a, int b)
		{
			Span<int> span = [a, b, 7];
			return span[0] + span[1] + span[2];
		}

		private static int SpanArgument(int a, int b)
		{
			return Sum([a, b, 7]);
		}

		private static int Sum(ReadOnlySpan<int> values)
		{
			int num = 0;
			for (int i = 0; i < values.Length; i++)
			{
				num += values[i];
			}
			return num;
		}

		private static int[] SpreadIntoArray(int[] source)
		{
			return [0, .. source, 9];
		}

		private static string[] SpreadTwice(string[] first, string[] second)
		{
			return [.. first, .. second];
		}

		private static List<int> SpreadIntoList(int[] source)
		{
			return [0, .. source];
		}

		private static IEnumerable<int> SpreadIntoInterface(int[] source)
		{
			return [.. source, 1];
		}

		private static int[] SpreadOfList(List<int> source)
		{
			return [1, .. source];
		}

		private static ImmutableArray<int> SpreadOfImmutable(int[] source)
		{
			return [.. source];
		}

		private static int SpreadOfSpan(ReadOnlySpan<int> source)
		{
			return Sum([1, .. source]);
		}

		private static int OrderOfSideEffects()
		{
			return Combine(Next("receiver"), [Next("first"), Next("second")]);
		}

		private static int Combine(int seed, ReadOnlySpan<int> values)
		{
			return seed * 100 + Sum(values);
		}

		private static int OrderInsideList()
		{
			List<int> list = [Next("a"), Next("b")];
			return list[0] * 10 + list[1];
		}

		private static void Print<T>(IEnumerable<T> values)
		{
			Console.WriteLine("  " + string.Join(", ", values));
		}

		private static void Print<T>(ImmutableArray<T> values)
		{
			Console.WriteLine("  " + string.Join(", ", values));
		}
	}
}

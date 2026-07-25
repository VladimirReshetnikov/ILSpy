using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class CollectionExpressions
	{
		public IReadOnlyList<int> ArrayWrapper()
		{
			return [1, 2, 3];
		}

		public IEnumerable<string> ArrayWrapperEnumerable(string a, string b)
		{
			return [a, b];
		}

		public IReadOnlyList<string> SingleElement(string s)
		{
			return [s];
		}

		public IReadOnlyCollection<T> SingleElementGeneric<T>(T item)
		{
			return [item];
		}

		public void InterfaceLocal(string a, string b)
		{
			IReadOnlyList<string> list = [a, b];
			Use(list);
		}

		public void InterfaceArgument(string a, string b)
		{
			Use([a, b]);
		}

		public ImmutableArray<int> ImmutableConstants()
		{
			return [1, 2, 3];
		}

		public ImmutableArray<string> Immutable(string a, string b)
		{
			return [a, b];
		}

		public List<int> ListConstants()
		{
			List<int> list = [1, 2, 3];
			return list;
		}

		public List<string> List(string a, string b)
		{
			List<string> list = [a, b];
			return list;
		}

		public int SpanLocal(int a, int b)
		{
			Span<int> span = [a, b];
			return span[0] + span[1];
		}

		public int SpanArgument(int a, int b)
		{
			return Sum([a, b, 7]);
		}

		public int Sum(ReadOnlySpan<int> values)
		{
			int num = 0;
			ReadOnlySpan<int> readOnlySpan = values;
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				int num2 = readOnlySpan[i];
				num += num2;
			}
			return num;
		}

		public int[] SpreadIntoArray(int[] source)
		{
			return [0, .. source, 9];
		}

		public string[] SpreadTwice(string[] first, string[] second)
		{
			return [.. first, .. second];
		}

		public List<int> SpreadIntoList(int[] source)
		{
			return [0, .. source];
		}

		public IEnumerable<int> SpreadIntoInterface(int[] source)
		{
			return [.. source, 1];
		}

		public int[] SpreadOfList(List<int> source)
		{
			return [1, .. source];
		}

		public int[] SpreadOfSpan(ReadOnlySpan<int> source)
		{
			return [1, .. source];
		}

		public ImmutableArray<int> SpreadIntoImmutable(int[] source)
		{
			return [.. source];
		}

		public int[] SpreadOfEnumerable(IEnumerable<int> source)
		{
#if EXPECTED_OUTPUT
			// An enumerable of unknown length is appended one element at a time, which is also what
			// hand-written code looks like, so the collection expression is not recovered.
			List<int> list = new List<int>();
			list.Add(1);
			list.AddRange(source);
			return list.ToArray();
#else
			return [1, .. source];
#endif
		}

		public void Use(IReadOnlyList<string> list)
		{
		}

		public int[] NotACollectionExpression()
		{
			return new int[3] { 1, 2, 3 };
		}
	}
}

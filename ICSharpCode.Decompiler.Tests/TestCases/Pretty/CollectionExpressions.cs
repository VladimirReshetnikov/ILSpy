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

		public void Use(IReadOnlyList<string> list)
		{
		}

		public int[] NotACollectionExpression()
		{
			return new int[3] { 1, 2, 3 };
		}
	}
}

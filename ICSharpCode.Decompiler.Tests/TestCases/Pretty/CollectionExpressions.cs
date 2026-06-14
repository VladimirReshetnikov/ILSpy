using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class CollectionExpressions
	{
		public IReadOnlyList<int> ArrayWrapper()
		{
#if EXPECTED_OUTPUT
			return new int[3] { 1, 2, 3 };
#else
			return [1, 2, 3];
#endif
		}

		public IEnumerable<string> ArrayWrapperEnumerable(string a, string b)
		{
#if EXPECTED_OUTPUT
			return new string[2] { a, b };
#else
			return [a, b];
#endif
		}

		public IReadOnlyList<string> SingleElement(string s)
		{
#if EXPECTED_OUTPUT
			return new string[1] { s };
#else
			return [s];
#endif
		}

		public IReadOnlyCollection<T> SingleElementGeneric<T>(T item)
		{
#if EXPECTED_OUTPUT
			return new T[1] { item };
#else
			return [item];
#endif
		}
	}
}

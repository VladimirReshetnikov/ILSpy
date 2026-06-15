using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
#if EXPECTED_OUTPUT
	// A captured primary-constructor parameter produces a '<name>P' backing field. When the
	// constructor also contains a field initializer the compiler lowered to statements (here a
	// collection-expression spread), the class cannot be printed as a primary constructor, so the
	// backing fields must be rendered as ordinary de-mangled fields rather than leaking their raw
	// names.
	public sealed class CombinedStrategy : IStrategy
	{
		private readonly HashSet<Type> allTypes;

		private IStrategy first;

		private IStrategy second;

		public CombinedStrategy(IStrategy first, IStrategy second)
		{
			this.first = first;
			this.second = second;
			HashSet<Type> hashSet = new HashSet<Type>();
			foreach (Type type in this.first.GetTypes())
			{
				hashSet.Add(type);
			}
			foreach (Type type2 in this.second.GetTypes())
			{
				hashSet.Add(type2);
			}
			allTypes = hashSet;
		}

		public IReadOnlyCollection<Type> GetTypes()
		{
			return allTypes;
		}

#if OPT
		public bool IsEnabled(Type t)
		{
			if (!this.first.IsEnabled(t))
			{
				return this.second.IsEnabled(t);
			}
			return true;
		}
#else
		public bool IsEnabled(Type t)
		{
			return this.first.IsEnabled(t) || this.second.IsEnabled(t);
		}
#endif
	}
#else
	public sealed class CombinedStrategy(IStrategy first, IStrategy second) : IStrategy
	{
		private readonly HashSet<Type> allTypes = [.. first.GetTypes(), .. second.GetTypes()];

		public IReadOnlyCollection<Type> GetTypes()
		{
			return allTypes;
		}

		public bool IsEnabled(Type t)
		{
			return first.IsEnabled(t) || second.IsEnabled(t);
		}
	}
#endif
	public interface IStrategy
	{
		IReadOnlyCollection<Type> GetTypes();

		bool IsEnabled(Type t);
	}
}

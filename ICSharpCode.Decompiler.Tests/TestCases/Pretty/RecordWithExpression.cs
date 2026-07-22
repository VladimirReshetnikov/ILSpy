using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class RecordWithExpression
	{
		public record Request
		{
			public IReadOnlyList<string> Context { get; init; }

			public string Name { get; init; }
		}

		// 'src with { M = value }' clones src first and computes the values afterwards. As long as
		// every value is a single expression, the clone call and the member assignments end up
		// adjacent, which is all it takes to recover the with-expression.
		public Request Rename(Request request, string name)
		{
			return request with {
				Name = name
			};
		}

		// A collection expression with a spread is built by a loop, so the statements computing the
		// value separate the clone call from the member assignment. Since cloning a record is a
		// plain memberwise copy, those statements can be decompiled ahead of the with-expression.
		// Leaving them where they are would assign an init-only member outside an initializer,
		// which does not compile (CS8852).
		public Request Append(Request request, string entry)
		{
#if EXPECTED_OUTPUT
			IReadOnlyList<string> context = request.Context;
			int num = 0;
			string[] array = new string[1 + context.Count];
			foreach (string item in context)
			{
				array[num] = item;
				num++;
			}
			array[num] = entry;
			return request with {
				Context = array
			};
#else
			return request with { Context = [.. request.Context, entry] };
#endif
		}

		// A member assigned a simple expression has to move along with the clone call when a later
		// member needs statements of its own, so that both stay in the same initializer.
		public Request AppendAndRename(Request request, string entry, string name)
		{
#if EXPECTED_OUTPUT
			IReadOnlyList<string> context = request.Context;
			int num = 0;
			string[] array = new string[1 + context.Count];
			foreach (string item in context)
			{
				array[num] = item;
				num++;
			}
			array[num] = entry;
			return request with {
				Name = name,
				Context = array
			};
#else
			return request with { Name = name, Context = [.. request.Context, entry] };
#endif
		}
	}
}

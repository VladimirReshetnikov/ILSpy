using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class RecordWithExpression
	{
		public enum InitializerMode
		{
			None,
			Enabled
		}

		public sealed class InitializerSource
		{
			public bool Enabled { get; }
		}

		public sealed class InitOnlyOptions
		{
			public int First { get; init; }

			public InitializerMode Mode { get; init; }

			public int Last { get; init; }

			public InitOnlyOptions(InitOnlyOptions old)
			{
				First = old.First;
				Mode = old.Mode;
				Last = old.Last;
			}
		}

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

		// A 'with' whose receiver statically has a generic type parameter type (constrained to a
		// record class) is lowered with generic conversions around the clone: the receiver is
		// boxed to the record class for the clone call and for each setter, and the clone result
		// is converted back to the type parameter with unbox.any. All of these are no-op
		// reference conversions, so the with-expression is recovered across them.
		public T RenameConstrained<T>(T request, string name) where T : Request
		{
			return request with {
				Name = name
			};
		}

		// A copy with no members changed still compiles to a call of the record's clone member,
		// whose name cannot be written in C#, so it has to come back as an empty with-expression.
		public Request Copy(Request request)
		{
			return request with { };
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

		// Both calls are potentially impure: construction, First, the nullable value and Last must
		// keep this order even when the compiler lowers the nullable access through a temporary.
		public InitOnlyOptions CopyWithNullableValue(InitOnlyOptions old, int first, int last)
		{
#if EXPECTED_OUTPUT
			InitOnlyOptions obj = new InitOnlyOptions(GetOldOptions(old)) {
				First = first,
				Mode = GetInitializerSource() switch {
					var initializerSource => (initializerSource != null && initializerSource.Enabled) ? InitializerMode.Enabled : InitializerMode.None,
				},
				Last = last
			};
			return obj;
#else
			return new InitOnlyOptions(GetOldOptions(old)) {
				First = first,
				Mode = ((GetInitializerSource()?.Enabled == true) ? InitializerMode.Enabled : InitializerMode.None),
				Last = last
			};
#endif
		}

		private InitializerSource GetInitializerSource()
		{
			return null;
		}

		private InitOnlyOptions GetOldOptions(InitOnlyOptions old)
		{
			return old;
		}
	}
}

using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.Tests.TestCases.Ugly;

public static class QueryWithUntranslatableClause
{
	public static IEnumerable<object> MembersMatching(IEnumerable<IEnumerable<object>> source, object other)
	{
		return source.SelectMany((IEnumerable<object> t) => t, (IEnumerable<object> t, object m) => new { t, m }).Where(_003C_003Eh__TransparentIdentifier0 => {
			object m = _003C_003Eh__TransparentIdentifier0.m;
			return (m is string || m is int) ? true : false;
		}).Where(_003C_003Eh__TransparentIdentifier0 => !_003C_003Eh__TransparentIdentifier0.m.Equals(other))
			.Select(_003C_003Eh__TransparentIdentifier0 => _003C_003Eh__TransparentIdentifier0.m);
	}
}

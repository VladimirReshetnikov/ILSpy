using System.IO;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class AsyncUsingVariableNaming
	{
		// Two using-declaration locals with the same source name live in disjoint sibling scopes
		// (legal C#). Their non-overlapping lifetimes let the compiler hoist both onto a single
		// state-machine field. Because the first branch exits, the decompiler reconstructs them as
		// one nested inside the other; the two variables must then receive distinct names, or the
		// nested declaration would reuse a name from an enclosing scope (CS0136).
		public async Task<int> ReturnsFromNestedUsing(bool flag)
		{
#if EXPECTED_OUTPUT
			await Task.Yield();
			if (flag)
			{
				using (MemoryStream stream = new MemoryStream())
				{
					await Task.Yield();
					return (int)stream.Length;
				}
			}
			using MemoryStream stream2 = new MemoryStream();
			await Task.Yield();
			return (int)stream2.Length + 1;
#else
			await Task.Yield();
			if (flag)
			{
				using MemoryStream stream = new MemoryStream();
				await Task.Yield();
				return (int)stream.Length;
			}
			{
				using MemoryStream stream = new MemoryStream();
				await Task.Yield();
				return (int)stream.Length + 1;
			}
#endif
		}
	}
}

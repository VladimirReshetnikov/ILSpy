#if EXPECTED_OUTPUT
using System.Runtime.CompilerServices;
#endif
public static class RefLocalUninitialized
{
	public static T LoopRef<T>(T value, int count)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out T result);
		scoped ref T reference = ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
		for (int i = 0; i < count; i++)
		{
			result = reference;
			reference = ref value;
		}
		return result;
	}
}

#if EXPECTED_OUTPUT
using System.Runtime.CompilerServices;
#endif
public ref struct ByRefSlot
{
	public int Value;
}
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
	public static int LoopRefLike(ByRefSlot value, int count)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int value2);
		ByRefSlot reference_placeholder = default(ByRefSlot);
		ref ByRefSlot reference = ref reference_placeholder;
		for (int i = 0; i < count; i++)
		{
			value2 = reference.Value;
			reference = ref value;
		}
		return value2;
	}
	public static ref int FindValue(Slot[] items, int count)
	{
		ref Slot reference = ref System.Runtime.CompilerServices.Unsafe.NullRef<Slot>();
		for (int i = 0; i < count; i++)
		{
			reference = ref items[i];
		}
		return ref reference.Value;
	}
}
public struct Slot
{
	public int Value;
}

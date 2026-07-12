public interface IFlagged
{
	int Flags { get; set; }
}
public static class RefLocalSlotSharing
{
	public static void SharedRefLocalSlot<T>(T context, bool a, bool b) where T : class, IFlagged
	{
		if (a)
		{
			ref T reference = ref context;
			ref T reference2 = ref reference;
			int flags = reference.Flags | 1;
			reference2.Flags = flags;
		}
		if (b)
		{
			ref T reference3 = ref context;
			ref T reference4 = ref reference3;
			int flags2 = reference3.Flags | 2;
			reference4.Flags = flags2;
		}
		ref T reference5 = ref context;
		ref T reference6 = ref reference5;
		int flags3 = reference5.Flags | 4;
		reference6.Flags = flags3;
	}
}

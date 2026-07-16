public static class CrossRootConsumer
{
	public static int Read(CrossRootOwner owner)
	{
		return owner.a2;
	}

	public static CrossRootMode ReadMode()
	{
		return CrossRootMode.CrossRootMode;
	}
}

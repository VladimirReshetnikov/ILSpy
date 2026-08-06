file class Holder
{
	private int count;

	public int Next()
	{
		return global::Support.Compute(count);
	}
}
file static class Support
{
	public static int Compute(int value)
	{
		return value;
	}
}
public class Consumer
{
	public int Use()
	{
		return new global::Holder().Next();
	}
}

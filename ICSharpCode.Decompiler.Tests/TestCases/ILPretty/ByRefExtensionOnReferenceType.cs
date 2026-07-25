public class Builder
{
}
public struct Counter
{
	public int Value;
}
public static class Extensions
{
	public static void ScrubClass(ref Builder b)
	{
	}
	public static void Bump(this ref Counter c)
	{
	}
	public static void Touch(this Builder b)
	{
	}
}

public class Consumer
{
	public string CallWithArgument(Symbolic s)
	{
		return s.get_Described(3);
	}
	public string CallWithDefault(Symbolic s)
	{
		return s.get_Described(0);
	}
	public string CallParameterless(Symbolic s)
	{
		return s.Name;
	}
}
public abstract class Symbolic
{
	protected internal abstract string get_Described(int depth = 0);
	protected internal virtual string Name => null;
}

public abstract class Base
{
	public virtual bool TryTransform(object expression, ref object reference)
	{
		reference = null;
		return false;
	}

	public virtual void Replace(object expression, out object reference)
	{
		reference = null;
	}
}
public class Derived : Base
{
	public override bool TryTransform(object expression, ref object reference)
	{
		reference = null;
		return false;
	}

	public override void Replace(object expression, out object reference)
	{
		reference = null;
	}
}

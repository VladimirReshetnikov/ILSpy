public abstract class BaseMethodProtectedInternal
{
	protected internal abstract int Compute();
}
public abstract class BaseWithProtectedInternal
{
	protected internal abstract int Value { get; }
}
public class DerivedNarrowsAccessor : BaseWithProtectedInternal
{
	protected internal override int Value => 0;
}
public class DerivedNarrowsMethod : BaseMethodProtectedInternal
{
	protected internal override int Compute()
	{
		return 0;
	}
}
public class NarrowerAccessorOnDeclaration
{
	public int Value {
		get {
			return 0;
		}
		protected set {
		}
	}
}

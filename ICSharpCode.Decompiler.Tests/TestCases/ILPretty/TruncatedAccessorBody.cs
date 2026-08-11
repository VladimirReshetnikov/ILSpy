public interface ICancellable
{
	bool IsCancellable { get; }
}
public class TestClass : ICancellable
{
	public bool IsCancellable => false;
	bool ICancellable.IsCancellable {
		get {
			_ = IsCancellable;
			/*Error: End of method reached without returning.*/;
		}
	}
}

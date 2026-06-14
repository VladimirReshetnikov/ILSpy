public interface IField
{
	bool IsField { get; }
}
public class Wrapper
{
	private object Member;

	private bool IsField {
		get {
			if (Member is IField field2)
			{
				return field2.IsField;
			}
			return false;
		}
	}
}

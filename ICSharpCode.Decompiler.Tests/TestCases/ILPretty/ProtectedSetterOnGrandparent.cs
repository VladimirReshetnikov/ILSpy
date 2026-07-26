public class EventArgsBase
{
	private string text;

	public virtual string Text {
		get {
			return text;
		}
		protected set {
			text = value;
		}
	}
}
public class MessageEventArgs : EventArgsBase
{
	public override string Text => base.Text;
}
public class ParameterEventArgs : MessageEventArgs
{
	public override string Text {
		get {
			if (((EventArgsBase)this).Text == null)
			{
				base.Text = "computed";
			}
			return ((EventArgsBase)this).Text;
		}
	}
}

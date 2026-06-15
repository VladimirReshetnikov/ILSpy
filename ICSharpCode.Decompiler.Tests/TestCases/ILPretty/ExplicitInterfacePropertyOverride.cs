using System;

public class C : IHasToken
{
	object IHasToken.Token => null;

	int IHasToken.Value {
		get {
			return 0 + 0;
		}
		set {
		}
	}

	event EventHandler IHasToken.Changed {
		add {
		}
		remove {
		}
	}
}
public interface IHasToken
{
	object Token { get; }

	int Value { get; set; }

	event EventHandler Changed;
}

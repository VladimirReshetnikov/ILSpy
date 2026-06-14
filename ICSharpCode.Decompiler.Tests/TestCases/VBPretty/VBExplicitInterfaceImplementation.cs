using System;

public interface IHasNameCountChanged
{
	string Name { get; }

	int Count { get; set; }

	event EventHandler Changed;
}
public class VBExplicitInterfaceImplementation : IHasNameCountChanged
{
	public string TheName => null;

	string IHasNameCountChanged.Name => this.TheName;

	public int TheCount { get; set; }

	int IHasNameCountChanged.Count {
		get {
			//ILSpy generated this explicit interface implementation from .override directive in TheCount
			return this.TheCount;
		}
		set {
			this.TheCount = value;
		}
	}

	public event EventHandler SomethingChanged;

	event EventHandler IHasNameCountChanged.Changed {
		add {
			//ILSpy generated this explicit interface implementation from .override directive in SomethingChanged
			this.SomethingChanged += value;
		}
		remove {
			this.SomethingChanged -= value;
		}
	}
}

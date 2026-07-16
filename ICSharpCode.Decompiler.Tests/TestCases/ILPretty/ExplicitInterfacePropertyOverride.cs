using System;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseWithProperty
{
	public abstract string Name { get; }
}
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
public class DerivedAccessor : BaseWithProperty
{
	public override string Name => "derived";
}
public class ForwardingC : IHasToken
{
	private int storage;

	private object ReadToken()
	{
		return null;
	}

	object IHasToken.Token => this.ReadToken();

	private ref int ReadReference()
	{
		return ref storage;
	}

	ref int IHasToken.Reference => ref this.ReadReference();

	private int ReadValue()
	{
		return 1;
	}

	int IHasToken.Value {
		get {
			//ILSpy generated this explicit interface implementation from .override directive in ReadValue
			return this.ReadValue();
		}
		set {
			this.WriteValue(value);
		}
	}

	private void WriteValue(int value)
	{
	}

	private string ReadItem(int index)
	{
		return null;
	}

	string IHasToken.this[int index] {
		get {
			//ILSpy generated this explicit interface implementation from .override directive in ReadItem
			return this.ReadItem(index);
		}
		set {
			this.WriteItem(index, value);
		}
	}

	private void WriteItem(int index, string value)
	{
	}

	private void Subscribe(EventHandler value)
	{
	}

	event EventHandler IHasToken.Changed {
		add {
			//ILSpy generated this explicit interface implementation from .override directive in Subscribe
			this.Subscribe(value);
		}
		remove {
			this.Unsubscribe(value);
		}
	}

	private void Unsubscribe(EventHandler value)
	{
	}
}
public class GenericContainer<T> where T : class
{
	public struct Nested : IEnumerator<T>, IDisposable, IEnumerator
	{
		private T value;

		public T Current => value;

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}

		public void Dispose()
		{
		}

		public Nested(T value)
		{
			this.value = value;
		}
	}
}
public interface IHasToken
{
	object Token { get; }

	ref int Reference { get; }

	int Value { get; set; }

	string this[int index] { get; set; }

	event EventHandler Changed;
}
public interface IImplicitAccessors
{
	int Number { get; set; }
}
public class ImplicitAccessors : IImplicitAccessors
{
	private int value;

	public int get_Number()
	{
		return value;
	}

	int IImplicitAccessors.Number {
		get {
			return this.get_Number();
		}
		set {
			this.set_Number(value);
		}
	}

	public void set_Number(int value)
	{
		this.value = value;
	}
}
public interface IOrdinaryAccessorNames
{
	int get_Number();

	void set_Number(int value);
}
public class PropertyAccessors : IOrdinaryAccessorNames
{
	private int value;

	public int Number {
		get {
			return value;
		}
		set {
			this.value = value;
		}
	}

	int IOrdinaryAccessorNames.get_Number()
	{
		return this.Number;
	}

	void IOrdinaryAccessorNames.set_Number(int value)
	{
		this.Number = value;
	}
}

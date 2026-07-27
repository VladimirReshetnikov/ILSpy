using System.Runtime.InteropServices;

public class ComConsumer : IComBase
{
	public string get_Value()
	{
		return null;
	}

	string IComBase.Value => this.get_Value();

	public string get_TemplatePath(string projectType)
	{
		return null;
	}
}
[ComImport]
public interface IComBase
{
	string Value { get; }

	// C# has no syntax for parameterized property 'TemplatePath'.
	string get_TemplatePath(string projectType);
}
[ComImport]
public interface IComDerived : IComBase
{
	string get_Value();

	string get_TemplatePath(string projectType);
}
public interface IOrdinaryBase
{
	int get_Flag();
}
public interface IOverridingDerived : IPlainBase
{
	int ReadNumber()
	{
		return 1;
	}

	int IPlainBase.Number {
		get {
			//ILSpy generated this explicit interface implementation from .override directive in ReadNumber
			return this.ReadNumber();
		}
		set {
			this.WriteNumber(value);
		}
	}

	void WriteNumber(int value)
	{
	}
}
public interface IPlainBase
{
	int Number { get; set; }
}
public interface IPlainDerived : IPlainBase
{
	int get_Number();

	void set_Number(int value);
}
public interface IPropertyDerived : IOrdinaryBase
{
	int Flag { get; }
}

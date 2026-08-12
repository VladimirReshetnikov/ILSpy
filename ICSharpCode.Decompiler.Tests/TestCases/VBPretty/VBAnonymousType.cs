using System.Diagnostics;
using System.Runtime.CompilerServices;
#if LEGACY_VBC
using System.Text;
#endif

using Microsoft.VisualBasic.CompilerServices;

[StandardModule]
internal static class Program
{
	public static string MakeAnonymous(int value, string name)
	{
		VB_AnonymousType_0<int, string> obj = new VB_AnonymousType_0<int, string>(value, name);
		return obj.Name + obj.Value;
	}

	public static string MakeKeyedAnonymous(int id, string text)
	{
		var anon = new {
			Id = id,
			Text = text
		};
		return anon.Text + anon.Id;
	}
}
// A VB anonymous type. Its properties are settable and only those declared 'Key'
// take part in Equals and GetHashCode, so it cannot be written as a C# anonymous
// type and is declared here instead.
[CompilerGenerated]
[DebuggerDisplay("Value={Value}, Name={Name}")]
internal sealed class VB_AnonymousType_0<T0, T1>
{
#if !OPT && !LEGACY_VBC
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
	private T0 _Value;

#if !OPT && !LEGACY_VBC
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
#endif
	private T1 _Name;

	public T0 Value {
		get {
			return _Value;
		}
		set {
			_Value = value;
		}
	}

	public T1 Name {
		get {
			return _Name;
		}
		set {
			_Name = value;
		}
	}

#if !OPT && !LEGACY_VBC
	[DebuggerHidden]
#endif
	public VB_AnonymousType_0(T0 Value, T1 Name)
	{
		_Value = Value;
		_Name = Name;
	}

#if !OPT && !LEGACY_VBC
	[DebuggerHidden]
#endif
	public override string ToString()
	{
#if LEGACY_VBC
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{ ");
		stringBuilder.AppendFormat("{0} = {1}, ", "Value", _Value);
		stringBuilder.AppendFormat("{0} = {1} ", "Name", _Name);
		stringBuilder.Append("}");
		return stringBuilder.ToString();
#else
		return string.Format(null, "{{ Value = {0}, Name = {1} }}", new object[2] { _Value, _Name });
#endif
	}
}

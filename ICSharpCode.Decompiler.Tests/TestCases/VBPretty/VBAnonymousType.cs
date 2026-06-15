using Microsoft.VisualBasic.CompilerServices;

[StandardModule]
internal static class Program
{
	public static string MakeAnonymous(int value, string name)
	{
		var anon = new {
			Value = value,
			Name = name
		};
		return anon.Name + anon.Value;
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

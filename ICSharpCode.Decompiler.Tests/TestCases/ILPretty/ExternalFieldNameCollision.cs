using System.Reflection;

public class ExternalFieldConsumer
{
	public static int Read()
	{
		return (int)typeof(ExternalFieldLibrary).GetField("Value", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
	}
}

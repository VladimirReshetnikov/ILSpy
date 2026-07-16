using System.Reflection;

public class ExternalFieldConsumer
{
	public static int Read()
	{
		return (int)typeof(ExternalFieldLibrary).GetField("Value", (BindingFlags)58).GetValue(null);
	}
}

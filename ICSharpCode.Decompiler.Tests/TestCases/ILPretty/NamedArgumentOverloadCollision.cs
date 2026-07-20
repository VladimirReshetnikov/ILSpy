public static class ArrayUtil
{
	public static T[] Add<T>(T element, T[] array)
	{
		return array;
	}

	public static T[] Add<T>(T[] array, T element)
	{
		return array;
	}
}
public class Consumer
{
	public static string[] GetArray()
	{
		return null;
	}

	public static string GetElement()
	{
		return null;
	}

	public static string[] Wrap(int tag, string[] arr)
	{
		return arr;
	}

	public static string[] Test()
	{
		string element = GetElement();
		return Wrap(1, ArrayUtil.Add(GetArray(), element));
	}
}

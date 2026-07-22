using System.Collections.Generic;

public class Wrapper<T>
{
	public void CallMissing(Dictionary<string, T> dict, T item)
	{
		dict.AddMissing(item);
	}

	public T FetchMissing(Dictionary<string, T> dict)
	{
		return dict.GetMissing();
	}
}

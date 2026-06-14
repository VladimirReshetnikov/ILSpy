public class OutParameterDefiniteAssignment
{
	public bool EarlyReturnLeavesUnassigned(string text, out int value)
	{
		value = default(int);
		if (text == null)
		{
			return false;
		}
		value = text.Length;
		return true;
	}

	public void AssignedOnEveryPath(bool flag, out string result)
	{
		if (flag)
		{
			result = "yes";
		}
		else
		{
			result = "no";
		}
	}
}

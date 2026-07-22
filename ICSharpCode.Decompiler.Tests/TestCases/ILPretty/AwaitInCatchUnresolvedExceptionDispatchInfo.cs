using System.Threading.Tasks;

public static class AwaitInCatch
{
	public static async Task Run(Task work, Task cleanup)
	{
		try
		{
			await work;
		}
		catch
		{
			await cleanup;
			throw;
		}
	}
}

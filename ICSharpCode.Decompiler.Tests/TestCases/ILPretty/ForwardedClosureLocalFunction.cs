using System;

public class ConsoleLogger : ILogger
{
	public void Error(string message)
	{
		Console.WriteLine(message);
	}
}
public interface ILogger
{
	void Error(string message);
}
public class Repro
{
	public Repro(ILogger logger)
	{
		Console.WriteLine((long)new Func<int, IntPtr>(WndProc)(1));
		unsafe IntPtr WndProc(int msg)
		{
			void* hwnd = null;
			string def = null;
			switch (msg)
			{
			case 1:
				OnCreate();
				break;
			case 2:
				OnDestroy();
				break;
			}
			return (nint)hwnd;
			unsafe void OnCreate()
			{
				if (def != null)
				{
					logger.Error("already created");
				}
				else
				{
					def = "created";
					Register(hwnd, logger);
				}
			}
			void OnDestroy()
			{
				if (def == null)
				{
					logger.Error("not created");
				}
				else
				{
					def = null;
				}
			}
		}
	}

	private unsafe static void Register(void* hwnd, ILogger logger)
	{
		logger.Error("register");
	}

	public static void Main()
	{
		new Repro(new ConsoleLogger());
	}
}

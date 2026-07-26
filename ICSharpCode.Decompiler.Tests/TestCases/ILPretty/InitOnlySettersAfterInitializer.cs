public static class Factory
{
	public static Options Create(bool flag)
	{
		Options options = new Options {
			First = "a",
			Second = "b",
			Third = (flag ? 1 : 2),
			Fourth = 3
		};
		return options;
	}
}
public class Options
{
	public string First {
		get {
			return null;
		}
		init {
		}
	}

	public string Second {
		get {
			return null;
		}
		init {
		}
	}

	public int Third {
		get {
			return 0;
		}
		init {
		}
	}

	public int Fourth {
		get {
			return 0;
		}
		init {
		}
	}
}

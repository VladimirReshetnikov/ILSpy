namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class RequiredMembers
	{
		internal class Data
		{
			public required string Y;

			public required int X { get; set; }
		}

		private static void Use(Data d)
		{
		}

		private static Data CreateThenReassign(int x, string y)
		{
#if OPT
			Data obj = new Data {
				X = x,
				Y = y
			};
			obj.X = x;
			return obj;
#else
			Data data = new Data {
				X = x,
				Y = y
			};
			data.X = x;
			return data;
#endif
		}

		private static Data Create()
		{
#if OPT
			Data obj = new Data {
				X = 5,
				Y = "hi"
			};
			Use(obj);
			return obj;
#else
			Data data = new Data {
				X = 5,
				Y = "hi"
			};
			Use(data);
			return data;
#endif
		}
	}
}

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class StackAllocConditionalPointer
	{
		public unsafe static void Consume(char* p)
		{
		}

		public unsafe static char* Heap(int n)
		{
			return null;
		}

		public unsafe static void M(bool cond, int cchLength)
		{
			char* p;
			if (cond)
			{
				char* ptr = stackalloc char[cchLength + 1];
				p = ptr;
			}
			else
			{
				p = Heap(cchLength);
			}
			Consume(p);
		}

		public unsafe static void DefaultInitialized(int cchLength)
		{
			char* ptr = null;
			char* ptr2 = stackalloc char[cchLength + 1];
			ptr = ptr2;
			Consume(ptr);
		}
	}
}

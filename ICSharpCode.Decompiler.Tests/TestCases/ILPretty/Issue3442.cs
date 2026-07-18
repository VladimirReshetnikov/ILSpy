namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Issue3442
{
	public class Class : Interface
	{
		private void M<T>(int value = 42) where T : Interface
		{
		}

		void Interface.M<T>(int value)
		{
			//ILSpy generated this explicit interface implementation from .override directive in M
			this.M<T>(value);
		}

		private (int result, int count) NamedTuple((int left, int right) pair)
		{
			return pair;
		}

		(int, int) Interface.NamedTuple((int, int) pair)
		{
			//ILSpy generated this explicit interface implementation from .override directive in NamedTuple
			return this.NamedTuple(pair);
		}
	}
	public interface Interface
	{
		void M<T>(int value = 42) where T : Interface;

		(int result, int count) NamedTuple((int left, int right) pair);
	}
}

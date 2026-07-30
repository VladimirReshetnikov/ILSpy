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

		private (int result, int count) NamedTuple(params (int left, int right)[] pairs)
		{
			return pairs[0];
		}

		(int result, int count) Interface.NamedTuple((int left, int right)[] pairs)
		{
			//ILSpy generated this explicit interface implementation from .override directive in NamedTuple
			return this.NamedTuple(pairs);
		}
	}
	public interface Interface
	{
		void M<T>(int value = 42) where T : Interface;

		(int result, int count) NamedTuple(params (int left, int right)[] pairs);
	}
}

using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal interface IProp<T>
	{
		T Value { get; set; }
		void Advise(Action<int, T> handler);
	}

	internal static class LocalFunctionInLambdaCapture
	{
		public static void Bind<T>(IProp<T> property, T initial)
		{
			property.Advise(delegate (int lt, T v) {
				property.Value = Pick();
				T Pick()
				{
					if (v != null)
					{
						return v;
					}
					return initial;
				}
			});
		}
	}
}

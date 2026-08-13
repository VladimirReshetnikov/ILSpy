using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class Cookie2
	{
		public void Execute(Action a)
		{
			a();
		}
	}
	internal interface IProp2<T>
	{
		T Value { get; set; }
		void Advise(Action<int, T> handler);
	}
	internal enum Propagation2
	{
		None,
		Save,
		Initial
	}
	internal static class TwoHandlersLocalFunc
	{
		public static void BindWithCondition<T>(IProp2<T> target, IProp2<T> source, T initial, Func<T, bool> acceptT, Func<T, bool> acceptS, Cookie2 cookie, Propagation2 prop)
		{
			target.Advise((int lt, T v) => {
				cookie.Execute(() => {
					T val = WhenInvalid(target, source, initial);
					source.Value = ((acceptT == null || acceptT(v)) ? target.Value : val);
				});
			});
			source.Advise((int lt, T v) => {
				cookie.Execute(() => {
					T val = WhenInvalid2(source, target, initial);
					target.Value = ((acceptS == null || acceptS(v)) ? source.Value : val);
				});
			});
			T WhenInvalid(IProp2<T> s, IProp2<T> t, T def)
			{
				T value = t.Value;
				if (prop == Propagation2.Save)
				{
					value = s.Value;
				}
				return value;
			}
			T WhenInvalid2(IProp2<T> s, IProp2<T> t, T def)
			{
				T value = t.Value;
				if (prop == Propagation2.Save)
				{
					value = s.Value;
				}
				return value;
			}
		}
	}
}

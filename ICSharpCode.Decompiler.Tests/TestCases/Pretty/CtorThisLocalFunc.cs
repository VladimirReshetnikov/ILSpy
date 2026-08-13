using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class CtorThisLocalFunc
	{
		private int count;

		private string last;

		public CtorThisLocalFunc(IEventSource3 added, IEventSource3 removed)
		{
			added.Advise(() => {
				Record("added");
			});
			removed.Advise(() => {
				Record("removed");
			});
			void Record(string kind)
			{
				count++;
				last = kind;
			}
		}
	}
	internal interface IEventSource3
	{
		void Advise(Action handler);
	}
}

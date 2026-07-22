using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Tests.TestCases.Pretty.ExtensionGetAwaiter_Helper;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public class ExtensionGetAwaiter
	{
		public async Task SwitchToScheduler()
		{
			await TaskScheduler.Default;
			Console.WriteLine("After");
		}

		public async Task<int> SwitchToSchedulerAndReturn()
		{
			await TaskScheduler.Default;
			return 42;
		}
	}
}
namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty.ExtensionGetAwaiter_Helper
{
	public class SchedulerAwaiter : INotifyCompletion
	{
		public bool IsCompleted => false;

		public void OnCompleted(Action continuation)
		{
		}

		public void GetResult()
		{
		}
	}

	public static class TaskSchedulerAwaitExtensions
	{
		public static SchedulerAwaiter GetAwaiter(this TaskScheduler scheduler)
		{
			return new SchedulerAwaiter();
		}
	}
}

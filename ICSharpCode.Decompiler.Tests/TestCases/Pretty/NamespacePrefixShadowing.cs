using System.Windows.Threading;

using Windows.Win32;

namespace Microsoft.VisualStudio.Threading
{
	internal static class Consumer
	{
		private static int PInvoke => 0;

		internal static void Invoke(Dispatcher dispatcher)
		{
			global::Windows.Win32.PInvoke.Invoke();
		}
	}
}

namespace Windows.Win32
{
	internal static class PInvoke
	{
		internal static void Invoke()
		{
		}
	}
}

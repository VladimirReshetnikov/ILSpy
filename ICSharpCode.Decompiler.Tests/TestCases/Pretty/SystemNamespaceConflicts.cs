using System;
using System.Diagnostics;
using System.Threading;

using SystemNamespaceConflicts.Ole;

namespace SystemNamespaceConflicts
{
	public interface IDocumentSite
	{
		int SetSite(SystemNamespaceConflicts.Ole.IServiceProvider site);

		int GetGuid(out Guid guid);
	}

	internal class UsesConflictingName
	{
		public SystemNamespaceConflicts.Ole.IServiceProvider Resolve(SystemNamespaceConflicts.Ole.IServiceProvider site)
		{
			return site;
		}
	}

	// "ThreadState" is declared both in System.Threading (System.Threading.Thread.dll) and in
	// System.Diagnostics (System.Diagnostics.Process.dll). This file imports both namespaces,
	// but the module references only the former assembly, so the ambiguity is only visible when
	// the unreferenced assembly's namespace contribution is completed.
	internal class UsesConflictingThreadState
	{
		public Stopwatch Stopwatch;

		public Thread Thread;

		public System.Threading.ThreadState ThreadState;
	}
}
namespace SystemNamespaceConflicts.Ole
{
	public interface IServiceProvider
	{
		int QueryService(ref Guid service, ref Guid interfaceId);
	}
}

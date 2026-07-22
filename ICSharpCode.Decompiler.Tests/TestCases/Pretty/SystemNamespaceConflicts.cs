using System;

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
}
namespace SystemNamespaceConflicts.Ole
{
	public interface IServiceProvider
	{
		int QueryService(ref Guid service, ref Guid interfaceId);
	}
}

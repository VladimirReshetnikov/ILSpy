using System.Configuration.Assemblies;
using System.Reflection;

namespace AssemblyHashAlgorithmConflict
{
	internal sealed class UsesConflictingAssemblyHashAlgorithm
	{
		public AssemblyName AssemblyName;

		public System.Configuration.Assemblies.AssemblyHashAlgorithm HashAlgorithm;
	}
}

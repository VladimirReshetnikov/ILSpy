using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	[ComImport]
	[CompilerGenerated]
	[Guid("FD8B0517-71F4-49FB-AA30-D268DA076233")]
	[TypeIdentifier]
	public interface ISecretsHandler
	{
		IEnumerable<(string, string, object)> GetUserSecrets();

		void AddUserSecret((string, string, object) secret);

		(string name, string value) GetNamedPair();

		IEnumerable<(string, string, object)> GetExplicit();
	}
	public class SecretsHandlerWrapper : ISecretsHandler
	{
		public IEnumerable<(string, string, object)> GetUserSecrets()
		{
			return null;
		}

		public void AddUserSecret((string, string, object) secret)
		{
		}

		public (string name, string value) GetNamedPair()
		{
			return (name: null, value: null);
		}

		IEnumerable<(string, string, object)> ISecretsHandler.GetExplicit()
		{
			return null;
		}
	}
}

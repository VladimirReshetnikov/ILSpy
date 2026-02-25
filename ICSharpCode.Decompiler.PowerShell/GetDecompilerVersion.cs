using System;
using System.Management.Automation;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Returns the version of the ILSpy decompiler assembly loaded by this PowerShell module.
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "DecompilerVersion")]
	[OutputType(typeof(string))]
	public class GetDecompilerVersion : PSCmdlet
	{
		/// <summary>
		/// Writes the version of <c>ICSharpCode.Decompiler</c> currently loaded in this PowerShell session.
		/// </summary>
		protected override void ProcessRecord()
		{
			WriteObject(typeof(FullTypeName).Assembly.GetName().Version.ToString());
		}
	}
}

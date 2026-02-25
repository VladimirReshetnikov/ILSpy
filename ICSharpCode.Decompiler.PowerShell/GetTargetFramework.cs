using System.Management.Automation;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Reads and returns the target framework moniker declared by the decompiler's main module.
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "TargetFramework")]
	[OutputType(typeof(string))]
	public class GetTargetFramework : PSCmdlet
	{
		/// <summary>
		/// Gets or sets the decompiler session whose module metadata should be queried.
		/// </summary>
		[Parameter(Position = 0, Mandatory = true)]
		public CSharpDecompiler Decompiler { get; set; }

		/// <summary>
		/// Reads the module target framework moniker from metadata and writes it to the pipeline.
		/// </summary>
		protected override void ProcessRecord()
		{
			MetadataFile module = Decompiler.TypeSystem.MainModule.MetadataFile;
			WriteObject(module.Metadata.DetectTargetFrameworkId());
		}
	}
}

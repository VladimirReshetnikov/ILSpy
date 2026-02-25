using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Decompiles either an entire module or a single type and returns the generated C# source text.
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "DecompiledSource")]
	[OutputType(typeof(string))]
	public class GetDecompiledSourceCmdlet : PSCmdlet
	{
		/// <summary>
		/// Gets or sets the decompiler session that provides metadata and decompilation settings.
		/// </summary>
		[Parameter(Position = 0, Mandatory = true)]
		public CSharpDecompiler Decompiler { get; set; }

		/// <summary>
		/// Gets or sets the full type name to decompile. Leave this value as <see langword="null"/>
		/// to decompile the whole module instead of a single type.
		/// </summary>
		[Parameter]
		public string TypeName { get; set; } = string.Empty;

		protected override void ProcessRecord()
		{
			try
			{
				StringWriter output = new StringWriter();
				if (TypeName == null)
				{
					output.Write(Decompiler.DecompileWholeModuleAsString());
				}
				else
				{
					var name = new FullTypeName(TypeName);
					output.Write(Decompiler.DecompileTypeAsString(name));
				}

				WriteObject(output.ToString());
			}
			catch (Exception e)
			{
				WriteVerbose(e.ToString());
				WriteError(new ErrorRecord(e, ErrorIds.DecompilationFailed, ErrorCategory.OperationStopped, null));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Returns type definitions from the main module that match the requested type-kind filter.
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "DecompiledTypes")]
	[OutputType(typeof(ITypeDefinition[]))]
	public class GetDecompiledTypesCmdlet : PSCmdlet
	{
		/// <summary>
		/// Gets or sets the decompiler session whose main module will be inspected.
		/// </summary>
		[Parameter(Position = 0, Mandatory = true)]
		public CSharpDecompiler Decompiler { get; set; }

		/// <summary>
		/// Gets or sets one or more type-kind selectors (for example <c>class</c>, <c>struct</c>, or compact forms such as <c>cis</c>).
		/// </summary>
		[Parameter(Mandatory = true)]
		public string[] Types { get; set; }

		/// <summary>
		/// Enumerates types from the main module and returns only definitions whose <see cref="TypeKind"/> matches <see cref="Types"/>.
		/// </summary>
		protected override void ProcessRecord()
		{
			HashSet<TypeKind> kinds = TypesParser.ParseSelection(Types);

			try
			{
				List<ITypeDefinition> output = new List<ITypeDefinition>();
				foreach (var type in Decompiler.TypeSystem.MainModule.TypeDefinitions)
				{
					if (!kinds.Contains(type.Kind))
						continue;
					output.Add(type);
				}

				WriteObject(output.ToArray());
			}
			catch (Exception e)
			{
				WriteVerbose(e.ToString());
				WriteError(new ErrorRecord(e, ErrorIds.DecompilationFailed, ErrorCategory.OperationStopped, null));
			}
		}
	}
}

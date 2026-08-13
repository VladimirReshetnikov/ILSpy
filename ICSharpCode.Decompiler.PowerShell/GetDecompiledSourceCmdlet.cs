// Copyright (c) 2017 Christoph Wille
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		/// Gets or sets the full type name to decompile. The default is <see cref="string.Empty"/>; the whole
		/// module is decompiled only when this value is explicitly set to <see langword="null"/>.
		/// </summary>
		[Parameter]
		public string TypeName { get; set; } = string.Empty;

		/// <summary>
		/// Decompiles either the whole module or <see cref="TypeName"/> and writes the generated C# text to the pipeline.
		/// </summary>
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
				this.WriteDecompilationErrors(Decompiler.Errors);
			}
			catch (Exception e)
			{
				WriteVerbose(e.ToString());
				WriteError(new ErrorRecord(e, ErrorIds.DecompilationFailed, ErrorCategory.OperationStopped, null));
			}
		}
	}
}

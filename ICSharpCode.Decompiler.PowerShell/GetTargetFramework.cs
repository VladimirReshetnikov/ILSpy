// Copyright (c) 2025 Snorri Beck Gislason
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

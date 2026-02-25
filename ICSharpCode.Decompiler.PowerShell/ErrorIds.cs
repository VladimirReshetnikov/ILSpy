using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Stable error identifiers used when cmdlets emit <see cref="System.Management.Automation.ErrorRecord"/> instances.
	/// </summary>
	public static class ErrorIds
	{
		/// <summary>
		/// Error identifier used when loading the input assembly or symbols fails.
		/// </summary>
		public static readonly string AssemblyLoadFailed = "1";

		/// <summary>
		/// Error identifier used when decompilation of the loaded assembly fails.
		/// </summary>
		public static readonly string DecompilationFailed = "2";
	}
}

using System;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Decompiles a loaded assembly into a full C# project on disk and streams progress updates
	/// back to the PowerShell host while files are being generated.
	/// </summary>
	[Cmdlet(VerbsCommon.Get, "DecompiledProject")]
	[OutputType(typeof(string))]
	public class GetDecompiledProjectCmdlet : PSCmdlet, IProgress<DecompilationProgress>
	{
		/// <summary>
		/// Gets or sets the decompiler session to use as the source of metadata and settings.
		/// </summary>
		[Parameter(Position = 0, Mandatory = true)]
		public CSharpDecompiler Decompiler { get; set; }

		/// <summary>
		/// Gets or sets the destination directory where the generated project files are written.
		/// The directory must already exist.
		/// </summary>
		[Parameter(Position = 1, Mandatory = true)]
		[Alias("PSPath", "OutputPath")]
		[ValidateNotNullOrEmpty]
		public string LiteralPath { get; set; }

		readonly object syncObject = new object();
		int completed;
		string fileName;
		ProgressRecord progress;

		/// <summary>
		/// Receives unit-level progress notifications from <see cref="WholeProjectDecompiler"/> and
		/// converts them into PowerShell progress records.
		/// </summary>
		/// <param name="value">The latest decompilation progress snapshot.</param>
		public void Report(DecompilationProgress value)
		{
			lock (syncObject)
			{
				completed++;
				progress = new ProgressRecord(1, "Decompiling " + fileName, $"Completed {completed} of {value.TotalUnits}: {value.Status}") {
					PercentComplete = (int)(completed * 100.0 / value.TotalUnits)
				};
			}
		}

		protected override void ProcessRecord()
		{
			string path = GetUnresolvedProviderPathFromPSPath(LiteralPath);
			if (!Directory.Exists(path))
			{
				WriteObject("Destination directory must exist prior to decompilation");
				return;
			}

			try
			{
				var task = Task.Run(() => DoDecompile(path));
				int timeout = 100;

				// Give the decompiler some time to spin up all threads
				Thread.Sleep(timeout);

				while (!task.IsCompleted)
				{
					ProgressRecord progress;
					lock (syncObject)
					{
						progress = this.progress;
						this.progress = null;
					}
					if (progress != null)
					{
						timeout = 100;
						WriteProgress(progress);
					}
					else
					{
						Thread.Sleep(timeout);
						timeout = Math.Min(1000, timeout * 2);
					}
				}

				task.Wait();

				WriteProgress(new ProgressRecord(1, "Decompiling " + fileName, "Decompilation finished") { RecordType = ProgressRecordType.Completed });
			}
			catch (Exception e)
			{
				WriteVerbose(e.ToString());
				WriteError(new ErrorRecord(e, ErrorIds.DecompilationFailed, ErrorCategory.OperationStopped, null));
			}
		}

		/// <summary>
		/// Executes project decompilation into <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The destination directory for the generated project.</param>
		private void DoDecompile(string path)
		{
			MetadataFile module = Decompiler.TypeSystem.MainModule.MetadataFile;
			var assemblyResolver = new UniversalAssemblyResolver(module.FileName, false, module.Metadata.DetectTargetFrameworkId());
			WholeProjectDecompiler decompiler = new WholeProjectDecompiler(assemblyResolver);
			decompiler.ProgressIndicator = this;
			fileName = module.FileName;
			completed = 0;
			decompiler.DecompileProject(module, path);
		}
	}
}

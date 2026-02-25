using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

using McMaster.Extensions.CommandLineUtils.Abstractions;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace ICSharpCode.ILSpyCmd
{
	[AttributeUsage(AttributeTargets.Class)]
	/// <summary>
	/// Enforces that <c>--project</c> is only used when an output directory is provided.
	/// </summary>
	public sealed class ProjectOptionRequiresOutputDirectoryValidationAttribute : ValidationAttribute
	{
		/// <summary>
		/// Initializes a new validation attribute instance.
		/// </summary>
		public ProjectOptionRequiresOutputDirectoryValidationAttribute()
		{
		}

		/// <summary>
		/// Validates that project decompilation mode has a writable output target.
		/// </summary>
		/// <param name="value">The object currently being validated.</param>
		/// <param name="context">Validation services and metadata for the current command invocation.</param>
		/// <returns>A validation error if <c>--project</c> is set without <c>--outputdir</c>; otherwise success.</returns>
		protected override ValidationResult IsValid(object value, ValidationContext context)
		{
			if (value is ILSpyCmdProgram obj)
			{
				if (obj.CreateCompilableProjectFlag && string.IsNullOrEmpty(obj.OutputDirectory))
				{
					return new ValidationResult("--project cannot be used unless --outputdir is also specified");
				}
			}
			return ValidationResult.Success;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	/// <summary>
	/// Validates that a file path option is either omitted or points to an existing file.
	/// Relative paths are resolved against the command working directory.
	/// </summary>
	public sealed class FileExistsOrNullAttribute : ValidationAttribute
	{
		/// <summary>
		/// Validates a single optional file path.
		/// </summary>
		/// <param name="value">The option value to validate.</param>
		/// <param name="validationContext">Validation context containing the command-line runtime services.</param>
		/// <returns><see cref="ValidationResult.Success"/> when the path is empty or the file exists; otherwise an error.</returns>
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			var path = value as string;
			if (string.IsNullOrEmpty(path))
			{
				return ValidationResult.Success;
			}

			if (!Path.IsPathRooted(path) && validationContext.GetService(typeof(CommandLineContext)) is CommandLineContext context)
			{
				path = Path.Combine(context.WorkingDirectory, path);
			}

			if (File.Exists(path))
			{
				return ValidationResult.Success;
			}

			return new ValidationResult($"File '{path}' does not exist!");
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	/// <summary>
	/// Validates that one or more file path arguments refer to existing files.
	/// Relative paths are resolved against the command working directory.
	/// </summary>
	public sealed class FilesExistAttribute : ValidationAttribute
	{
		/// <summary>
		/// Validates either a single path or a collection of paths.
		/// </summary>
		/// <param name="value">The value being validated (expected to be a <see cref="string"/> or <see cref="string[]"/>).</param>
		/// <param name="validationContext">Validation context containing the command-line runtime services.</param>
		/// <returns><see cref="ValidationResult.Success"/> when every path exists; otherwise the first encountered error.</returns>
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			switch (value)
			{
				case string path:
					return ValidatePath(path);
				case string[] paths:
				{
					foreach (string path in paths)
					{
						ValidationResult result = ValidatePath(path);
						if (result != ValidationResult.Success)
							return result;
					}
					return ValidationResult.Success;
				}
				default:
					return new ValidationResult($"File '{value}' does not exist!");
			}

			ValidationResult ValidatePath(string path)
			{
				if (!string.IsNullOrWhiteSpace(path))
				{
					if (!Path.IsPathRooted(path) && validationContext.GetService(typeof(CommandLineContext)) is CommandLineContext context)
					{
						path = Path.Combine(context.WorkingDirectory, path);
					}

					if (File.Exists(path))
					{
						return ValidationResult.Success;
					}
				}

				return new ValidationResult($"File '{path}' does not exist!");
			}
		}
	}
}

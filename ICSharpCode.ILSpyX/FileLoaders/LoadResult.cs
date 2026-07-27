// Copyright (c) 2024 Siegfried Pammer
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
using System.IO;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.ILSpyX.FileLoaders
{
	/// <summary>
	/// Represents the outcome produced by an <see cref="IFileLoader"/>.
	/// </summary>
	public sealed class LoadResult
	{
		/// <summary>
		/// Gets the loaded metadata image when the file was recognized as a metadata-bearing artifact.
		/// </summary>
		public MetadataFile? MetadataFile { get; init; }

		/// <summary>
		/// Gets the exception captured while attempting to load the file.
		/// </summary>
		public Exception? FileLoadException { get; init; }

		/// <summary>
		/// Gets the loaded package when the input file represents a bundle or archive.
		/// </summary>
		public LoadedPackage? Package { get; init; }

		/// <summary>
		/// Gets whether the loader produced a successful result.
		/// </summary>
		public bool IsSuccess => FileLoadException == null;
	}

	/// <summary>
	/// Carries contextual options used while probing and loading a file.
	/// </summary>
	/// <param name="ApplyWinRTProjections">
	/// <see langword="true"/> to apply Windows Runtime projections while reading metadata.
	/// </param>
	/// <param name="ParentBundle">
	/// The containing bundle assembly when loading entries from a bundle; otherwise <see langword="null"/>.
	/// </param>
	public record FileLoadContext(bool ApplyWinRTProjections, LoadedAssembly? ParentBundle);

	/// <summary>
	/// Provides format-specific loading logic used by <see cref="LoadedAssembly"/>.
	/// </summary>
	public interface IFileLoader
	{
		/// <summary>
		/// Attempts to load a file from the provided stream.
		/// </summary>
		/// <param name="fileName">Display name or source path used for diagnostics and metadata identity.</param>
		/// <param name="stream">Stream positioned at the beginning of the candidate file data.</param>
		/// <param name="context">Additional options and state for the current load operation.</param>
		/// <returns>
		/// A <see cref="LoadResult"/> when the loader recognized the format; otherwise <see langword="null"/>.
		/// </returns>
		Task<LoadResult?> Load(string fileName, Stream stream, FileLoadContext context);
	}
}

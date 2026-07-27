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

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.ILSpyX.FileLoaders
{
	/// <summary>
	/// Loads Portable Executable files that contain managed metadata.
	/// </summary>
	public sealed class PEFileLoader : IFileLoader
	{
		/// <summary>
		/// Probes for an <c>MZ</c> header and loads the stream as a PE image when it matches.
		/// </summary>
		/// <param name="fileName">Display name or source path of the file being loaded.</param>
		/// <param name="stream">Input stream containing the candidate file bytes.</param>
		/// <param name="context">Load options that influence metadata reader behavior.</param>
		/// <returns>
		/// A successful <see cref="LoadResult"/> for recognized PE files; otherwise <see langword="null"/>.
		/// </returns>
		public async Task<LoadResult?> Load(string fileName, Stream stream, FileLoadContext context)
		{
			if (stream.Length < 2 || stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
			{
				return null;
			}

			return await LoadPEFile(fileName, stream, context).ConfigureAwait(false);
		}

		/// <summary>
		/// Loads the supplied stream as a managed PE file.
		/// </summary>
		/// <param name="fileName">Display name or source path of the file being loaded.</param>
		/// <param name="stream">Input stream containing PE bytes.</param>
		/// <param name="context">Load options that influence metadata reader behavior.</param>
		/// <returns>A <see cref="LoadResult"/> containing the loaded <see cref="PEFile"/>.</returns>
		public static Task<LoadResult> LoadPEFile(string fileName, Stream stream, FileLoadContext context)
		{
			MetadataReaderOptions options = context.ApplyWinRTProjections
				? MetadataReaderOptions.ApplyWindowsRuntimeProjections
				: MetadataReaderOptions.None;
			stream.Position = 0;
			PEFile module = new PEFile(fileName, stream, PEStreamOptions.PrefetchEntireImage | PEStreamOptions.LeaveOpen, metadataOptions: options);
			return Task.FromResult(new LoadResult { MetadataFile = module });
		}
	}
}

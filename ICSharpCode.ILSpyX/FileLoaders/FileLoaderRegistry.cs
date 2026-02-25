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
using System.Collections.Generic;

namespace ICSharpCode.ILSpyX.FileLoaders
{
	/// <summary>
	/// Stores ordered <see cref="IFileLoader"/> registrations used to probe files.
	/// </summary>
	public sealed class FileLoaderRegistry
	{
		readonly List<IFileLoader> registeredLoaders = new List<IFileLoader>();

		/// <summary>
		/// Gets registered file loaders in probing order.
		/// </summary>
		public IReadOnlyList<IFileLoader> RegisteredLoaders => registeredLoaders;

		/// <summary>
		/// Adds a format loader to the end of the probing sequence.
		/// </summary>
		/// <param name="loader">The loader instance to register.</param>
		/// <exception cref="ArgumentNullException"><paramref name="loader"/> is <see langword="null"/>.</exception>
		public void Register(IFileLoader loader)
		{
			if (loader is null)
			{
				throw new ArgumentNullException(nameof(loader));
			}

			registeredLoaders.Add(loader);
		}

		/// <summary>
		/// Initializes the registry with ILSpy's default loader order.
		/// </summary>
		public FileLoaderRegistry()
		{
			Register(new XamarinCompressedFileLoader());
			Register(new WebCilFileLoader());
			Register(new MetadataFileLoader());
			Register(new BundleFileLoader()); // bundles are PE files with a special signature, prefer over normal PE files
			Register(new PEFileLoader()); // prefer PE format over archives, because ZIP has no fixed header
			Register(new ArchiveFileLoader());
		}
	}
}

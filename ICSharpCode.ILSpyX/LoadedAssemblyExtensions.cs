// Copyright (c) 2018 Siegfried Pammer
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

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// Extension helpers that map from <see cref="MetadataFile"/> to the owning <see cref="LoadedAssembly"/>
	/// so consumers can reuse ILSpy-managed resolver, debug-info, and type-system services.
	/// </summary>
	public static class LoadedAssemblyExtensions
	{
		/// <summary>
		/// This method creates a Cecil object model from a PEFile. It is intended as helper method for plugins.
		/// Note that this method is expensive and creates high memory pressure!
		/// Note that accessing the Cecil objects created by this method after an assembly has been unloaded by ILSpy
		/// might lead to <see cref="AccessViolationException"/> or similar.
		/// </summary>
		/// <param name="file">The PE file for which a Cecil module model should be created.</param>
		/// <remarks>Use only as last resort if there is something missing in the official ILSpy API.
		/// Consider creating an issue at https://github.com/icsharpcode/ILSpy/issues/new
		/// and discussing the problem with us.</remarks>
		public unsafe static Mono.Cecil.ModuleDefinition CreateCecilObjectModel(this PEFile file)
		{
			if (!file.Reader.IsEntireImageAvailable)
				throw new InvalidOperationException("Need full image to create Cecil object model!");
			var image = file.Reader.GetEntireImage();
			return Mono.Cecil.ModuleDefinition.ReadModule(new UnmanagedMemoryStream(image.Pointer, image.Length));
		}

		/// <summary>
		/// Gets the assembly resolver configured for the <see cref="LoadedAssembly"/> that owns <paramref name="file"/>.
		/// </summary>
		/// <param name="file">Metadata file associated with a loaded assembly.</param>
		/// <param name="loadOnDemand">Whether dependencies may be loaded lazily when first requested.</param>
		/// <returns>The assembly resolver used by ILSpy for <paramref name="file"/>.</returns>
		public static IAssemblyResolver GetAssemblyResolver(this MetadataFile file, bool loadOnDemand = true)
		{
			return GetLoadedAssembly(file).GetAssemblyResolver(loadOnDemand);
		}

		internal static IAssemblyResolver GetAssemblyResolver(this MetadataFile file, AssemblyListSnapshot snapshot, bool loadOnDemand = true)
		{
			return GetLoadedAssembly(file).GetAssemblyResolver(snapshot, loadOnDemand);
		}

		/// <summary>
		/// Gets debug-info services (PDB/source mappings) associated with the loaded assembly, if available.
		/// </summary>
		/// <param name="file">Metadata file associated with a loaded assembly.</param>
		/// <returns>A debug-info provider, or <see langword="null"/> when symbols are unavailable.</returns>
		public static IDebugInfoProvider? GetDebugInfoOrNull(this MetadataFile file)
		{
			return GetLoadedAssembly(file).GetDebugInfoOrNull();
		}

		/// <summary>
		/// Gets the default decompiler type system for the loaded assembly.
		/// </summary>
		/// <param name="file">Metadata file associated with a loaded assembly.</param>
		/// <returns>The default compilation model, or <see langword="null"/> if it could not be built.</returns>
		public static ICompilation? GetTypeSystemOrNull(this MetadataFile file)
		{
			return GetLoadedAssembly(file).GetTypeSystemOrNull();
		}

		/// <summary>
		/// Gets a decompiler type system configured with options derived from <paramref name="settings"/>.
		/// </summary>
		/// <param name="file">Metadata file associated with a loaded assembly.</param>
		/// <param name="settings">Decompiler settings that influence type-system construction options.</param>
		/// <returns>The configured compilation model, or <see langword="null"/> if it could not be built.</returns>
		public static ICompilation? GetTypeSystemWithDecompilerSettingsOrNull(this MetadataFile file, DecompilerSettings settings)
		{
			return GetLoadedAssembly(file).GetTypeSystemOrNull(DecompilerTypeSystem.GetOptions(settings));
		}

		/// <summary>
		/// Gets the <see cref="LoadedAssembly"/> instance currently associated with a metadata file.
		/// </summary>
		/// <param name="file">Metadata file to map.</param>
		/// <returns>The owning <see cref="LoadedAssembly"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="file"/> is not tracked by ILSpy's loaded-assembly map.</exception>
		public static LoadedAssembly GetLoadedAssembly(this MetadataFile file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			LoadedAssembly? loadedAssembly;
			lock (LoadedAssembly.loadedAssemblies)
			{
				if (!LoadedAssembly.loadedAssemblies.TryGetValue(file, out loadedAssembly))
					throw new ArgumentException("The specified file is not associated with a LoadedAssembly!");
			}
			return loadedAssembly;
		}
	}
}

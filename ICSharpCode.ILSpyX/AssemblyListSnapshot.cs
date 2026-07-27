// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;
using ICSharpCode.ILSpyX.Extensions;
using ICSharpCode.ILSpyX.FileLoaders;
using ICSharpCode.ILSpyX.Instrumentation;

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// Immutable snapshot of loaded assemblies with lazily-built lookup indexes used by resolver paths.
	/// </summary>
	class AssemblyListSnapshot
	{
		readonly ImmutableArray<LoadedAssembly> assemblies;
		Dictionary<string, MetadataFile>? asmLookupByFullName;
		Dictionary<string, MetadataFile>? asmLookupByShortName;
		Dictionary<string, List<(MetadataFile module, Version version)>>? asmLookupByShortNameGrouped;
		/// <summary>
		/// Gets the snapshot payload captured from the assembly list.
		/// </summary>
		public ImmutableArray<LoadedAssembly> Assemblies => assemblies;

		/// <summary>
		/// Captures a point-in-time set of loaded assemblies for deterministic resolver/search operations.
		/// </summary>
		/// <param name="assemblies">Assemblies included in this snapshot.</param>
		public AssemblyListSnapshot(ImmutableArray<LoadedAssembly> assemblies)
		{
			this.assemblies = assemblies;
		}

		/// <summary>
		/// Attempts to resolve an assembly reference against snapshot assemblies using TFM-scoped full-name or short-name matching.
		/// </summary>
		/// <param name="reference">The assembly reference to match.</param>
		/// <param name="tfm">Target framework moniker used to scope candidates.</param>
		/// <returns>The best matching loaded module, or <see langword="null"/> if none matches.</returns>
		public async Task<MetadataFile?> TryGetModuleAsync(IAssemblyReference reference, string tfm)
		{
			bool isWinRT = reference.IsWindowsRuntime;
			if (tfm.StartsWith(".NETFramework,Version=v4.", StringComparison.Ordinal))
			{
				tfm = ".NETFramework,Version=v4";
			}
			string key = tfm + ";" + (isWinRT ? reference.Name : reference.FullName);
			var lookup = LazyInit.VolatileRead(ref isWinRT ? ref asmLookupByShortName : ref asmLookupByFullName);
			if (lookup == null)
			{
				lookup = await CreateLoadedAssemblyLookupAsync(shortNames: isWinRT).ConfigureAwait(false);
				lookup = LazyInit.GetOrSet(ref isWinRT ? ref asmLookupByShortName : ref asmLookupByFullName, lookup);
			}
			if (lookup.TryGetValue(key, out MetadataFile? module))
				return module;
			return null;
		}

		/// <summary>
		/// Attempts fallback resolution by assembly short name and version ordering when an exact identity match is unavailable.
		/// </summary>
		/// <param name="reference">The unresolved assembly reference.</param>
		/// <returns>
		/// A module with the smallest version greater than or equal to <paramref name="reference"/>,
		/// or the highest available version if all candidates are older; otherwise <see langword="null"/>.
		/// </returns>
		public async Task<MetadataFile?> TryGetSimilarModuleAsync(IAssemblyReference reference)
		{
			var lookup = LazyInit.VolatileRead(ref asmLookupByShortNameGrouped);
			if (lookup == null)
			{
				lookup = await CreateLoadedAssemblyShortNameGroupLookupAsync().ConfigureAwait(false);
				lookup = LazyInit.GetOrSet(ref asmLookupByShortNameGrouped, lookup);
			}

			if (!lookup.TryGetValue(reference.Name, out var candidates))
				return null;
			return candidates.FirstOrDefault(c => c.version >= reference.Version).module ?? candidates.Last().module;
		}

		private async Task<Dictionary<string, MetadataFile>> CreateLoadedAssemblyLookupAsync(bool shortNames)
		{
			ILSpyXEventSource.Log.SnapshotLookupBuildStart(assemblies.Length);
			try
			{
				var result = new Dictionary<string, MetadataFile>(StringComparer.OrdinalIgnoreCase);
				foreach (LoadedAssembly loaded in assemblies)
				{
					try
					{
						var module = await loaded.GetMetadataFileOrNullAsync().ConfigureAwait(false);
						if (module == null)
							continue;
						var reader = module.Metadata;
						if (reader == null || !reader.IsAssembly)
							continue;
						string tfm = await loaded.GetTargetFrameworkIdAsync().ConfigureAwait(false);
						if (tfm.StartsWith(".NETFramework,Version=v4.", StringComparison.Ordinal))
						{
							tfm = ".NETFramework,Version=v4";
						}
						string key = tfm + ";"
							+ (shortNames ? module.Name : module.FullName);
						if (!result.ContainsKey(key))
						{
							result.Add(key, module);
						}
					}
					catch (BadImageFormatException)
					{
						continue;
					}
				}
				return result;
			}
			finally
			{
				ILSpyXEventSource.Log.SnapshotLookupBuildStop(assemblies.Length);
			}
		}

		private async Task<Dictionary<string, List<(MetadataFile module, Version version)>>> CreateLoadedAssemblyShortNameGroupLookupAsync()
		{
			ILSpyXEventSource.Log.SnapshotLookupBuildStart(assemblies.Length);
			try
			{
				var result = new Dictionary<string, List<(MetadataFile module, Version version)>>(StringComparer.OrdinalIgnoreCase);

				foreach (LoadedAssembly loaded in assemblies)
				{
					try
					{
						var module = await loaded.GetMetadataFileOrNullAsync().ConfigureAwait(false);
						var reader = module?.Metadata;
						if (reader == null || !reader.IsAssembly)
							continue;
						var asmDef = reader.GetAssemblyDefinition();
						var asmDefName = reader.GetString(asmDef.Name);

						var line = (module!, version: asmDef.Version);

						if (!result.TryGetValue(asmDefName, out var existing))
						{
							existing = new List<(MetadataFile module, Version version)>();
							result.Add(asmDefName, existing);
							existing.Add(line);
							continue;
						}

						int index = existing.BinarySearch(line.version, l => l.version);
						index = index < 0 ? ~index : index + 1;
						existing.Insert(index, line);
					}
					catch (BadImageFormatException)
					{
						continue;
					}
				}

				return result;
			}
			finally
			{
				ILSpyXEventSource.Log.SnapshotLookupBuildStop(assemblies.Length);
			}
		}

		/// <summary>
		/// Enumerates all loadable assemblies reachable from this snapshot, including descendants discovered inside packages/bundles.
		/// </summary>
		/// <returns>A flattened list of top-level and package-contained assemblies.</returns>
		public async Task<IList<LoadedAssembly>> GetAllAssembliesAsync()
		{
			var results = new List<LoadedAssembly>(assemblies.Length);

			foreach (var asm in assemblies)
			{
				LoadResult result;
				try
				{
					result = await asm.GetLoadResultAsync().ConfigureAwait(false);
				}
				catch
				{
					results.Add(asm);
					continue;
				}
				if (result.Package != null)
				{
					AddDescendants(result.Package.RootFolder);
				}
				else if (result.MetadataFile != null)
				{
					results.Add(asm);
				}
			}

			void AddDescendants(PackageFolder folder)
			{
				foreach (var subFolder in folder.Folders)
				{
					AddDescendants(subFolder);
				}

				foreach (var entry in folder.Entries)
				{
					if (!entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !entry.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
						continue;
					var asm = folder.ResolveFileName(entry.Name);
					if (asm == null)
						continue;
					results.Add(asm);
				}
			}

			return results;
		}
	}
}

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

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Documentation
{
	/// <summary>
	/// Locates and caches XML documentation providers for metadata modules.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Lookup prefers side-by-side XML files (including localized subdirectories) and falls back to known .NET Framework reference/runtime directories.
	/// </para>
	/// <para>
	/// Results are cached per <see cref="MetadataFile"/> instance, including negative lookups, so repeated documentation queries avoid repeated filesystem probing.
	/// </para>
	/// </remarks>
	public static class XmlDocLoader
	{
		static readonly Lazy<XmlDocumentationProvider> mscorlibDocumentation = new Lazy<XmlDocumentationProvider>(LoadMscorlibDocumentation);
		static readonly ConditionalWeakTable<MetadataFile, XmlDocumentationProvider> cache = new();

		static XmlDocumentationProvider LoadMscorlibDocumentation()
		{
			string xmlDocFile = FindXmlDocumentation("mscorlib.dll", TargetRuntime.Net_4_0)
				?? FindXmlDocumentation("mscorlib.dll", TargetRuntime.Net_2_0);
			if (xmlDocFile != null)
				return new XmlDocumentationProvider(xmlDocFile);
			else
				return null;
		}

		/// <summary>
		/// Gets lazily loaded documentation for <c>mscorlib.dll</c> from the best available framework profile.
		/// </summary>
		public static XmlDocumentationProvider MscorlibDocumentation {
			get { return mscorlibDocumentation.Value; }
		}

		/// <summary>
		/// Loads (or retrieves from cache) the XML documentation provider associated with a metadata module.
		/// </summary>
		/// <param name="module">Module whose assembly path and runtime are used to locate XML documentation.</param>
		/// <returns>A documentation provider for <paramref name="module"/>, or <see langword="null"/> when no suitable XML file is found.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="module"/> is <see langword="null"/>.</exception>
		public static XmlDocumentationProvider LoadDocumentation(MetadataFile module)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			lock (cache)
			{
				if (!cache.TryGetValue(module, out XmlDocumentationProvider xmlDoc))
				{
					string xmlDocFile = LookupLocalizedXmlDoc(module.FileName);
					if (xmlDocFile == null)
					{
						xmlDocFile = FindXmlDocumentation(Path.GetFileName(module.FileName), module.GetRuntime());
					}
					if (xmlDocFile != null)
					{
						xmlDoc = new XmlDocumentationProvider(xmlDocFile);
						cache.Add(module, xmlDoc);
					}
					else
					{
						cache.Add(module, null); // add missing documentation files as well
						xmlDoc = null;
					}
				}
				return xmlDoc;
			}
		}

		static readonly string referenceAssembliesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\\Framework");
		static readonly string frameworkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework");

		static string FindXmlDocumentation(string assemblyFileName, TargetRuntime runtime)
		{
			string fileName;
			switch (runtime)
			{
				case TargetRuntime.Net_1_0:
					fileName = LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v1.0.3705", assemblyFileName));
					break;
				case TargetRuntime.Net_1_1:
					fileName = LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v1.1.4322", assemblyFileName));
					break;
				case TargetRuntime.Net_2_0:
					fileName = LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, "v3.5", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v3.5\Profile\Client", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, "v3.0", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v2.0.50727", assemblyFileName));
					break;
				case TargetRuntime.Net_4_0:
				default:
					fileName = LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.8.1", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.8", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.7.2", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.7.1", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.7", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.6.2", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.6.1", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.6", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.5.2", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.5.1", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.5", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, @".NETFramework\v4.0", assemblyFileName))
						?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v4.0.30319", assemblyFileName));
					break;
			}
			return fileName;
		}

		/// <summary>
		/// Resolves the best XML documentation file path for an assembly path, honoring UI-culture-specific folders.
		/// </summary>
		/// <param name="fileName">Assembly file path whose extension is replaced with <c>.xml</c> during probing.</param>
		/// <returns>
		/// First existing candidate from culture-specific, language fallback, neutral, and English fallback locations;
		/// otherwise <see langword="null"/>.
		/// </returns>
		internal static string LookupLocalizedXmlDoc(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return null;

			string xmlFileName = Path.ChangeExtension(fileName, ".xml");

			CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			string localizedXmlDocFile = GetLocalizedName(xmlFileName, currentCulture.Name);
			string localizedXmlDocFallbackFile = GetLocalizedName(xmlFileName, currentCulture.TwoLetterISOLanguageName);

			//Debug.WriteLine("Try find XMLDoc @" + localizedXmlDocFile);
			if (File.Exists(localizedXmlDocFile))
			{
				return localizedXmlDocFile;
			}
			//Debug.WriteLine("Try find XMLDoc @" + localizedXmlDocFallbackFile);
			if (File.Exists(localizedXmlDocFallbackFile))
			{
				return localizedXmlDocFallbackFile;
			}
			//Debug.WriteLine("Try find XMLDoc @" + xmlFileName);
			if (File.Exists(xmlFileName))
			{
				return xmlFileName;
			}
			if (currentCulture.TwoLetterISOLanguageName != "en")
			{
				string englishXmlDocFile = GetLocalizedName(xmlFileName, "en");
				//Debug.WriteLine("Try find XMLDoc @" + englishXmlDocFile);
				if (File.Exists(englishXmlDocFile))
				{
					return englishXmlDocFile;
				}
			}
			return null;
		}

		private static string GetLocalizedName(string fileName, string language)
		{
			return Path.Combine(Path.GetDirectoryName(fileName), language, Path.GetFileName(fileName));
		}
	}
}

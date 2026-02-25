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

// #define STRESS

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Solution;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.Util;
using ICSharpCode.ILSpyX;

using ILCompiler.Reflection.ReadyToRun;

using TomsToolbox.Composition;

using MetadataReader = System.Reflection.Metadata.MetadataReader;

namespace ICSharpCode.ILSpy.ReadyToRun
{
#if STRESS
	class DummyOutput : ITextOutput
	{
		public string IndentationString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public void Indent()
		{
		}

		public void MarkFoldEnd()
		{
		}

		public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false, bool isDefinition = false)
		{
		}

		public void Unindent()
		{
		}

		public void Write(char ch)
		{
		}

		public void Write(string text)
		{
		}

		public void WriteLine()
		{
		}

		public void WriteLocalReference(string text, object reference, bool isDefinition = false)
		{
		}

		public void WriteReference(OpCodeInfo opCode, bool omitSuffix = false)
		{
		}

		public void WriteReference(PEFile module, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
		{
		}

		public void WriteReference(IType type, string text, bool isDefinition = false)
		{
		}

		public void WriteReference(IMember member, string text, bool isDefinition = false)
		{
		}

		public void WriteReference(MetadataFile metadata, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
		{
		}
	}
#endif

	/// <summary>
	/// ILSpy language backend that renders native ReadyToRun method bodies as annotated assembly text.
	/// </summary>
	[Export(typeof(Language))]
	[Shared]
	internal class ReadyToRunLanguage(SettingsService settingsService, IExportProvider exportProvider) : Language
	{
		private static readonly ConditionalWeakTable<MetadataFile, ReadyToRunReaderCacheEntry> readyToRunReaders = new ConditionalWeakTable<MetadataFile, ReadyToRunReaderCacheEntry>();

		/// <summary>
		/// Gets the display name shown in ILSpy's language selector.
		/// </summary>
		public override string Name => "ReadyToRun";

		/// <summary>
		/// Gets the file extension used for ReadyToRun text exports.
		/// </summary>
		public override string FileExtension {
			get { return ".asm"; }
		}

		/// <summary>
		/// Writes a semicolon-prefixed comment line using assembler-style syntax.
		/// </summary>
		/// <param name="output">Target text sink receiving the comment.</param>
		/// <param name="comment">Comment payload without the leading comment marker.</param>
		public override void WriteCommentLine(ITextOutput output, string comment)
		{
			output.WriteLine("; " + comment);
		}

		/// <summary>
		/// Emits assembly-level ReadyToRun metadata headers and then delegates standard tree decompilation to the base language.
		/// </summary>
		/// <param name="assembly">Loaded assembly selected for decompilation.</param>
		/// <param name="output">Destination for decompiled text.</param>
		/// <param name="options">Current decompilation options and cancellation state.</param>
		/// <returns>The project identifier returned by the base decompilation pipeline.</returns>
		public override ProjectId DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options)
		{
			PEFile module = assembly.GetMetadataFileAsync().GetAwaiter().GetResult() as PEFile;
			ReadyToRunReaderCacheEntry cacheEntry = GetReader(assembly, module);
			if (cacheEntry.readyToRunReader == null)
			{
				WriteCommentLine(output, cacheEntry.failureReason);
			}
			else
			{
				ReadyToRunReader reader = cacheEntry.readyToRunReader;
				WriteCommentLine(output, $"Machine                  : {reader.Machine}");
				WriteCommentLine(output, $"OperatingSystem          : {reader.OperatingSystem}");
				WriteCommentLine(output, $"CompilerIdentifier       : {reader.CompilerIdentifier}");
				if (reader.OwnerCompositeExecutable != null)
				{
					WriteCommentLine(output, $"OwnerCompositeExecutable : {reader.OwnerCompositeExecutable}");
				}
			}

			return base.DecompileAssembly(assembly, output, options);
		}

		/// <summary>
		/// Decompiles the selected managed method by locating corresponding ReadyToRun runtime functions and disassembling their native code.
		/// </summary>
		/// <param name="method">Method metadata used to locate ReadyToRun entries.</param>
		/// <param name="output">Destination for assembly output and annotations.</param>
		/// <param name="options">Decompilation options controlling cancellation and formatting behavior.</param>
		public override void DecompileMethod(IMethod method, ITextOutput output, DecompilationOptions options)
		{
			PEFile module = method.ParentModule.MetadataFile as PEFile;
			ReadyToRunReaderCacheEntry cacheEntry = GetReader(module.GetLoadedAssembly(), module);
			if (cacheEntry.readyToRunReader == null)
			{
				WriteCommentLine(output, cacheEntry.failureReason);
			}
			else
			{
				ReadyToRunReader reader = cacheEntry.readyToRunReader;
				int bitness = -1;
				if (reader.Machine == Machine.Amd64)
				{
					bitness = 64;
				}
				else
				{
					Debug.Assert(reader.Machine == Machine.I386);
					bitness = 32;
				}
				if (cacheEntry.methodMap == null)
				{
					IEnumerable<ReadyToRunMethod> readyToRunMethods = null;
					if (cacheEntry.compositeReadyToRunReader == null)
					{
						readyToRunMethods = reader.Methods;
					}
					else
					{
						readyToRunMethods = cacheEntry.compositeReadyToRunReader.Methods
							.Where(m => {
								MetadataReader mr = m.ComponentReader.MetadataReader;
								return string.Equals(mr.GetString(mr.GetAssemblyDefinition().Name), method.ParentModule.Name, StringComparison.OrdinalIgnoreCase);
							});
					}
					cacheEntry.methodMap = readyToRunMethods.ToList()
							.GroupBy(m => m.MethodHandle)
							.ToDictionary(g => g.Key, g => g.ToArray());
				}
				var displaySettings = settingsService.DisplaySettings;
				bool showMetadataTokens = displaySettings.ShowMetadataTokens;
				bool showMetadataTokensInBase10 = displaySettings.ShowMetadataTokensInBase10;
#if STRESS
				ITextOutput originalOutput = output;
				output = new DummyOutput();
				{
					foreach (var readyToRunMethod in reader.Methods)
					{
#else
				if (cacheEntry.methodMap.TryGetValue(method.MetadataToken, out var methods))
				{
					foreach (var readyToRunMethod in methods)
					{
#endif
						foreach (RuntimeFunction runtimeFunction in readyToRunMethod.RuntimeFunctions)
						{
							PEFile file = null;
							ReadyToRunReader disassemblingReader = null;
							if (cacheEntry.compositeReadyToRunReader == null)
							{
								disassemblingReader = reader;
								file = method.ParentModule.MetadataFile as PEFile;
							}
							else
							{
								disassemblingReader = cacheEntry.compositeReadyToRunReader;
								file = ((IlSpyAssemblyMetadata)readyToRunMethod.ComponentReader).Module;
							}

							new ReadyToRunDisassembler(output, disassemblingReader, runtimeFunction, settingsService).Disassemble(file, bitness, (ulong)runtimeFunction.StartAddress, showMetadataTokens, showMetadataTokensInBase10);
						}
					}
				}
#if STRESS
				output = originalOutput;
				output.WriteLine("Passed");
#endif
			}
		}

		/// <summary>
		/// Reuses IL language tooltip rendering for metadata entities shown while browsing ReadyToRun output.
		/// </summary>
		/// <param name="entity">Entity for which tooltip content is requested.</param>
		/// <returns>A rich-text tooltip produced by the IL language service.</returns>
		public override RichText GetRichTextTooltip(IEntity entity)
		{
			return exportProvider.GetExportedValue<LanguageService>().ILLanguage.GetRichTextTooltip(entity);
		}

		private ReadyToRunReaderCacheEntry GetReader(LoadedAssembly assembly, MetadataFile file)
		{
			ReadyToRunReaderCacheEntry result;
			lock (readyToRunReaders)
			{
				if (!readyToRunReaders.TryGetValue(file, out result))
				{
					result = new ReadyToRunReaderCacheEntry();
					try
					{
						if ((file is not PEFile module) || (module.Reader == null))
						{
							result.readyToRunReader = null;
							result.failureReason = "File is not a valid PE file.";
						}
						else
						{
							ReadOnlyMemory<byte> content = module.Reader.GetEntireImage().GetContent().AsMemory();
							result.readyToRunReader = new ReadyToRunReader(new ReadyToRunAssemblyResolver(assembly), new StandaloneAssemblyMetadata(module.Reader), module.Reader, module.FileName, content);
							if (result.readyToRunReader.Machine != Machine.Amd64 && result.readyToRunReader.Machine != Machine.I386)
							{
								result.failureReason = $"Architecture {result.readyToRunReader.Machine} is not currently supported.";
								result.readyToRunReader = null;
							}
							else if (result.readyToRunReader.OwnerCompositeExecutable != null)
							{
								string compositeModuleName = Path.GetFileNameWithoutExtension(result.readyToRunReader.OwnerCompositeExecutable);
								PEFile compositeFile = assembly.GetAssemblyResolver().ResolveModule(assembly.GetMetadataFileOrNull(), compositeModuleName) as PEFile;
								if (compositeFile == null)
								{
									result.readyToRunReader = null;
									result.failureReason = "Composite File is not a valid PE file.";
								}
								else
								{
									ReadOnlyMemory<byte> compositeContent = compositeFile.Reader.GetEntireImage().GetContent().AsMemory();
									result.compositeReadyToRunReader = new ReadyToRunReader(new ReadyToRunAssemblyResolver(assembly), compositeModuleName, compositeContent);
								}

							}
						}
					}
					catch (BadImageFormatException e)
					{
						result.failureReason = e.Message;
					}
					readyToRunReaders.Add(file, result);
				}
			}
			return result;
		}

		private class ReadyToRunAssemblyResolver : ILCompiler.Reflection.ReadyToRun.IAssemblyResolver
		{
			private LoadedAssembly loadedAssembly;
			private Decompiler.Metadata.IAssemblyResolver assemblyResolver;

			/// <summary>
			/// Creates a resolver that maps ReadyToRun assembly references through ILSpy's loaded-assembly resolution pipeline.
			/// </summary>
			/// <param name="loadedAssembly">Assembly context that provides metadata and module resolution services.</param>
			public ReadyToRunAssemblyResolver(LoadedAssembly loadedAssembly)
			{
				this.loadedAssembly = loadedAssembly;
				assemblyResolver = loadedAssembly.GetAssemblyResolver();
			}

			/// <summary>
			/// Resolves an assembly reference handle from metadata into ReadyToRun assembly metadata.
			/// </summary>
			/// <param name="metadataReader">Metadata reader that owns <paramref name="assemblyReferenceHandle"/>.</param>
			/// <param name="assemblyReferenceHandle">Handle identifying the referenced assembly.</param>
			/// <param name="parentFile">Parent file path supplied by the ReadyToRun reader (unused by this resolver).</param>
			/// <returns>Assembly metadata for the resolved module, or <see langword="null"/> when resolution fails.</returns>
			public IAssemblyMetadata FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
			{
				return GetAssemblyMetadata(assemblyResolver.Resolve(new Decompiler.Metadata.AssemblyReference(metadataReader, assemblyReferenceHandle)));
			}

			/// <summary>
			/// Resolves a module by simple file name within the current loaded-assembly context.
			/// </summary>
			/// <param name="simpleName">Simple module name to resolve.</param>
			/// <param name="parentFile">Parent file path supplied by the ReadyToRun reader (unused by this resolver).</param>
			/// <returns>Assembly metadata for the resolved module, or <see langword="null"/> when not found.</returns>
			public IAssemblyMetadata FindAssembly(string simpleName, string parentFile)
			{
				return GetAssemblyMetadata(assemblyResolver.ResolveModule(loadedAssembly.GetMetadataFileOrNull(), simpleName));
			}

			private IAssemblyMetadata GetAssemblyMetadata(MetadataFile module)
			{
				if (module is not PEFile peFile || peFile.Reader == null)
				{
					return null;
				}
				else
				{
					return new IlSpyAssemblyMetadata(peFile);
				}
			}
		}

		private class IlSpyAssemblyMetadata : StandaloneAssemblyMetadata
		{
			/// <summary>
			/// Gets the PE module wrapped by this ReadyToRun assembly metadata adapter.
			/// </summary>
			public PEFile Module { get; private set; }

			/// <summary>
			/// Initializes metadata adapter state for a module resolved by ILSpy.
			/// </summary>
			/// <param name="module">Resolved PE module that provides metadata and image reader access.</param>
			public IlSpyAssemblyMetadata(PEFile module) : base(module.Reader)
			{
				Module = module;
			}
		}

		private class ReadyToRunReaderCacheEntry
		{
			public ReadyToRunReader readyToRunReader;
			public ReadyToRunReader compositeReadyToRunReader;
			public string failureReason;
			public Dictionary<EntityHandle, ReadyToRunMethod[]> methodMap;
		}
	}
}

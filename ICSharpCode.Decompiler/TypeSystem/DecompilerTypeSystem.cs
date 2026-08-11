// Copyright (c) 2018 Daniel Grunwald
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
using System.Linq;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Instrumentation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

using static ICSharpCode.Decompiler.Metadata.MetadataExtensions;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Options that control how metadata is represented in the type system.
	/// </summary>
	[Flags]
	public enum TypeSystemOptions
	{
		/// <summary>
		/// No options enabled; stay as close to the metadata as possible.
		/// </summary>
		None = 0,
		/// <summary>
		/// [DynamicAttribute] is used to replace 'object' types with the 'dynamic' type.
		/// 
		/// If this option is not active, the 'dynamic' type is not used, and the attribute is preserved.
		/// </summary>
		Dynamic = 1,
		/// <summary>
		/// Tuple types are represented using the TupleType class.
		/// [TupleElementNames] is used to name the tuple elements.
		/// 
		/// If this option is not active, the tuples are represented using their underlying type, and the attribute is preserved.
		/// </summary>
		Tuple = 2,
		/// <summary>
		/// If this option is active, [ExtensionAttribute] is removed and methods are marked as IsExtensionMethod.
		/// Otherwise, the attribute is preserved but the methods are not marked.
		/// </summary>
		ExtensionMethods = 4,
		/// <summary>
		/// Only load the public API into the type system.
		/// </summary>
		OnlyPublicAPI = 8,
		/// <summary>
		/// Do not cache accessed entities.
		/// In a normal type system (without this option), every type or member definition has exactly one ITypeDefinition/IMember
		/// instance. This instance is kept alive until the whole type system can be garbage-collected.
		/// When this option is specified, the type system avoids these caches.
		/// This reduces the memory usage in many cases, but increases the number of allocations.
		/// Also, some code in the decompiler expects to be able to compare type/member definitions by reference equality,
		/// and thus will fail with uncached type systems.
		/// </summary>
		Uncached = 0x10,
		/// <summary>
		/// If this option is active, [DecimalConstantAttribute] is removed and constant values are transformed into simple decimal literals.
		/// </summary>
		DecimalConstants = 0x20,
		/// <summary>
		/// If this option is active, modopt and modreq types are preserved in the type system.
		/// 
		/// Note: the decompiler currently does not support handling modified types;
		/// activating this option may lead to incorrect decompilation or internal errors.
		/// </summary>
		KeepModifiers = 0x40,
		/// <summary>
		/// If this option is active, [IsReadOnlyAttribute] on parameters+structs is removed
		/// and parameters are marked as in, structs as readonly.
		/// Otherwise, the attribute is preserved but the parameters and structs are not marked.
		/// </summary>
		ReadOnlyStructsAndParameters = 0x80,
		/// <summary>
		/// If this option is active, [IsByRefLikeAttribute] is removed and structs are marked as ref.
		/// Otherwise, the attribute is preserved but the structs are not marked.
		/// </summary>
		RefStructs = 0x100,
		/// <summary>
		/// If this option is active, [IsUnmanagedAttribute] is removed from type parameters,
		/// and HasUnmanagedConstraint is set instead.
		/// </summary>
		UnmanagedConstraints = 0x200,
		/// <summary>
		/// If this option is active, [NullableAttribute] is removed and reference types with
		/// nullability annotations are used instead.
		/// </summary>
		NullabilityAnnotations = 0x400,
		/// <summary>
		/// If this option is active, [IsReadOnlyAttribute] on methods is removed
		/// and the method marked as ThisIsRefReadOnly.
		/// </summary>
		ReadOnlyMethods = 0x800,
		/// <summary>
		/// [NativeIntegerAttribute] is used to replace 'IntPtr' types with the 'nint' type.
		/// </summary>
		NativeIntegers = 0x1000,
		/// <summary>
		/// Allow function pointer types. If this option is not enabled, function pointers are
		/// replaced with the 'IntPtr' type.
		/// </summary>
		FunctionPointers = 0x2000,
		/// <summary>
		/// Allow C# 11 scoped annotation. If this option is not enabled, ScopedRefAttribute
		/// will be reported as custom attribute.
		/// </summary>
		ScopedRef = 0x4000,
		/// <summary>
		/// Replace 'IntPtr' types with the 'nint' type even in absence of [NativeIntegerAttribute].
		/// Note: DecompilerTypeSystem constructor removes this setting from the options if
		/// not targeting .NET 7 or later.
		/// </summary>
		NativeIntegersWithoutAttribute = 0x8000,
		/// <summary>
		/// If this option is active, [RequiresLocationAttribute] on parameters is removed
		/// and parameters are marked as ref readonly.
		/// Otherwise, the attribute is preserved but the parameters are not marked
		/// as if it was a ref parameter without any attributes.
		/// </summary>
		RefReadOnlyParameters = 0x10000,
		/// <summary>
		/// If this option is active, [ParamCollectionAttribute] on parameters is removed
		/// and parameters are marked as params.
		/// Otherwise, the attribute is preserved but the parameters are not marked
		/// as if it was a normal parameter without any attributes.
		/// </summary>
		ParamsCollections = 0x20000,
		/// <summary>
		/// If this option is active, span types (Span&lt;T&gt; and ReadOnlySpan&lt;T&gt;) are treated like
		/// built-in types and language rules of C# 14 and later are applied.
		/// </summary>
		FirstClassSpanTypes = 0x40000,
		/// <summary>
		/// If this option is active, extension member groups are detected, otherwise the compiler-generated nested classes are left as-is.
		/// </summary>
		ExtensionMembers = 0x80000,
		/// <summary>
		/// If this option is active, methods with the MethodImplAttribute(MethodImplOptions.Async) are treated as async methods.
		/// </summary>
		RuntimeAsync = 0x100000,
		/// <summary>
		/// Default settings: typical options for the decompiler, with all C# language features enabled.
		/// </summary>
		Default = Dynamic | Tuple | ExtensionMethods | DecimalConstants | ReadOnlyStructsAndParameters
			| RefStructs | UnmanagedConstraints | NullabilityAnnotations | ReadOnlyMethods
			| NativeIntegers | FunctionPointers | ScopedRef | NativeIntegersWithoutAttribute
			| RefReadOnlyParameters | ParamsCollections | FirstClassSpanTypes | ExtensionMembers
			| RuntimeAsync
	}

	/// <summary>
	/// Builds the decompiler-specific <see cref="ICompilation"/> graph from metadata files and resolver policy.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> The constructor and factory methods materialize a <see cref="MetadataModule"/> for the main
	/// input, resolve transitive references (including type-forwarding edges), and apply
	/// <see cref="TypeSystemOptions"/> so that metadata attributes are projected into high-level semantic constructs
	/// such as tuple names, nullable annotations, and native integer types.
	/// </para>
	/// <para>
	/// <b>Thread safety.</b> This type is thread-safe after initialization. The contained symbol model is designed for
	/// concurrent reads.
	/// </para>
	/// </remarks>
	public class DecompilerTypeSystem : SimpleCompilation, IDecompilerTypeSystem
	{
		/// <summary>
		/// Maps decompiler UI/settings flags to the corresponding type-system projection flags.
		/// </summary>
		/// <param name="settings">Decompiler feature settings to translate.</param>
		/// <returns>The set of <see cref="TypeSystemOptions"/> that implement the requested language projections.</returns>
		public static TypeSystemOptions GetOptions(DecompilerSettings settings)
		{
			var typeSystemOptions = TypeSystemOptions.None;
			if (settings.Dynamic)
				typeSystemOptions |= TypeSystemOptions.Dynamic;
			if (settings.TupleTypes)
				typeSystemOptions |= TypeSystemOptions.Tuple;
			if (settings.ExtensionMethods)
				typeSystemOptions |= TypeSystemOptions.ExtensionMethods;
			if (settings.DecimalConstants)
				typeSystemOptions |= TypeSystemOptions.DecimalConstants;
			if (settings.IntroduceRefModifiersOnStructs)
				typeSystemOptions |= TypeSystemOptions.RefStructs;
			if (settings.IntroduceReadonlyAndInModifiers)
				typeSystemOptions |= TypeSystemOptions.ReadOnlyStructsAndParameters;
			if (settings.IntroduceUnmanagedConstraint)
				typeSystemOptions |= TypeSystemOptions.UnmanagedConstraints;
			if (settings.NullableReferenceTypes)
				typeSystemOptions |= TypeSystemOptions.NullabilityAnnotations;
			if (settings.ReadOnlyMethods)
				typeSystemOptions |= TypeSystemOptions.ReadOnlyMethods;
			if (settings.NativeIntegers)
				typeSystemOptions |= TypeSystemOptions.NativeIntegers;
			if (settings.FunctionPointers)
				typeSystemOptions |= TypeSystemOptions.FunctionPointers;
			if (settings.ScopedRef)
				typeSystemOptions |= TypeSystemOptions.ScopedRef;
			if (settings.NumericIntPtr)
				typeSystemOptions |= TypeSystemOptions.NativeIntegersWithoutAttribute;
			if (settings.RefReadOnlyParameters)
				typeSystemOptions |= TypeSystemOptions.RefReadOnlyParameters;
			if (settings.ParamsCollections)
				typeSystemOptions |= TypeSystemOptions.ParamsCollections;
			if (settings.FirstClassSpanTypes)
				typeSystemOptions |= TypeSystemOptions.FirstClassSpanTypes;
			if (settings.ExtensionMembers)
				typeSystemOptions |= TypeSystemOptions.ExtensionMembers;
			if (settings.AsyncAwait)
				typeSystemOptions |= TypeSystemOptions.RuntimeAsync;
			return typeSystemOptions;
		}

		/// <summary>
		/// Asynchronously creates a type system using <see cref="TypeSystemOptions.Default"/>.
		/// </summary>
		/// <param name="mainModule">The primary PE module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		/// <returns>A task that completes with an initialized <see cref="DecompilerTypeSystem"/> instance.</returns>
		public static Task<DecompilerTypeSystem> CreateAsync(PEFile mainModule, IAssemblyResolver assemblyResolver)
		{
			return CreateAsync(mainModule, assemblyResolver, TypeSystemOptions.Default);
		}

		/// <summary>
		/// Asynchronously creates a type system using options derived from <paramref name="settings"/>.
		/// </summary>
		/// <param name="mainModule">The primary PE module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		/// <param name="settings">Decompiler settings used to compute type-system options.</param>
		/// <returns>A task that completes with an initialized <see cref="DecompilerTypeSystem"/> instance.</returns>
		public static Task<DecompilerTypeSystem> CreateAsync(PEFile mainModule, IAssemblyResolver assemblyResolver, DecompilerSettings settings)
		{
			return CreateAsync(mainModule, assemblyResolver, GetOptions(settings ?? throw new ArgumentNullException(nameof(settings))));
		}

		/// <summary>
		/// Asynchronously creates a type system using explicit projection options.
		/// </summary>
		/// <param name="mainModule">The primary PE module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		/// <param name="typeSystemOptions">Metadata-projection and semantic-model options.</param>
		/// <returns>A task that completes with an initialized <see cref="DecompilerTypeSystem"/> instance.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="mainModule"/> or <paramref name="assemblyResolver"/> is <see langword="null"/>.
		/// </exception>
		public static async Task<DecompilerTypeSystem> CreateAsync(PEFile mainModule, IAssemblyResolver assemblyResolver, TypeSystemOptions typeSystemOptions)
		{
			if (mainModule == null)
				throw new ArgumentNullException(nameof(mainModule));
			if (assemblyResolver == null)
				throw new ArgumentNullException(nameof(assemblyResolver));
			var ts = new DecompilerTypeSystem(typeSystemOptions);
			await ts.InitializeAsync(mainModule, assemblyResolver)
				.ConfigureAwait(false);
			return ts;
		}

		private MetadataModule mainModule;
		private TypeSystemOptions typeSystemOptions;

		private DecompilerTypeSystem(TypeSystemOptions typeSystemOptions)
		{
			this.typeSystemOptions = typeSystemOptions;
		}

		/// <summary>
		/// Initializes a type system synchronously using <see cref="TypeSystemOptions.Default"/>.
		/// </summary>
		/// <param name="mainModule">The primary metadata module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		public DecompilerTypeSystem(MetadataFile mainModule, IAssemblyResolver assemblyResolver)
			: this(mainModule, assemblyResolver, TypeSystemOptions.Default)
		{
		}

		/// <summary>
		/// Initializes a type system synchronously using options derived from <paramref name="settings"/>.
		/// </summary>
		/// <param name="mainModule">The primary metadata module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		/// <param name="settings">Decompiler settings used to compute type-system options.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <see langword="null"/>.</exception>
		public DecompilerTypeSystem(MetadataFile mainModule, IAssemblyResolver assemblyResolver, DecompilerSettings settings)
			: this(mainModule, assemblyResolver, GetOptions(settings ?? throw new ArgumentNullException(nameof(settings))))
		{
		}

		/// <summary>
		/// Initializes a type system synchronously using explicit projection options.
		/// </summary>
		/// <param name="mainModule">The primary metadata module to model.</param>
		/// <param name="assemblyResolver">Resolver used for assembly and module references.</param>
		/// <param name="typeSystemOptions">Metadata-projection and semantic-model options.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="mainModule"/> or <paramref name="assemblyResolver"/> is <see langword="null"/>.
		/// </exception>
		/// <remarks>
		/// This constructor blocks the calling thread until reference resolution and graph initialization complete.
		/// Use <see cref="CreateAsync(PEFile, IAssemblyResolver, TypeSystemOptions)"/> when asynchronous initialization is
		/// preferred.
		/// </remarks>
		public DecompilerTypeSystem(MetadataFile mainModule, IAssemblyResolver assemblyResolver, TypeSystemOptions typeSystemOptions)
			: this(typeSystemOptions)
		{
			if (mainModule == null)
				throw new ArgumentNullException(nameof(mainModule));
			if (assemblyResolver == null)
				throw new ArgumentNullException(nameof(assemblyResolver));
			InitializeAsync(mainModule, assemblyResolver).GetAwaiter().GetResult();
		}

		// Assemblies that are added to the compilation of a .NET Core / .NET 5 or newer module even
		// though nothing in its metadata references them. They hold compile-time-only types
		// (DllImport, MarshalAs, Unsafe, ...) that the decompiled C# has to be able to name.
		static readonly string[] implicitReferences = new[] {
			"System.Runtime.InteropServices",
			"System.Runtime.CompilerServices.Unsafe"
		};

		// Assemblies that complete the root "System" namespace: the reference packs spread it over
		// System.Runtime plus exactly this handful of assemblies, and a project generated from the
		// decompilation compiles against all of them, while the module's own reference closure usually
		// stops at System.Runtime. Since practically every decompiled file imports "System", a type
		// that is invisible here lets the C# output claim a short name is unambiguous when it is not:
		// an interface named IServiceProvider would be printed unqualified and then collide with
		// System.IServiceProvider (declared in System.ComponentModel.dll) in the generated project.
		//
		// The same reasoning extends to short names that the reference packs declare in two commonly
		// imported System sub-namespaces, with the two declarations in different assemblies: a file
		// importing both namespaces must qualify such a name, which the decompiler can only know when
		// both declaring assemblies are loaded. "ThreadState" is the known case - System.Threading
		// (System.Threading.Thread.dll, listed above) vs. System.Diagnostics
		// (System.Diagnostics.Process.dll).
		//
		// Unlike the implicit references above, these assemblies are loaded solely so that name
		// qualification checks can see their type names. They join the compilation as
		// name-lookup-only modules (MetadataModule.IsNameLookupOnly): the decompiled module does not
		// reference them, so their members - most importantly extension methods such as
		// System.MemoryExtensions.* - must not become overload candidates during resolution.
		static readonly string[] namespaceCompletionReferences = new[] {
			"System.ComponentModel",
			"System.ComponentModel.TypeConverter",
			"System.Console",
			"System.Diagnostics.Process",
			"System.Memory",
			"System.Threading.Thread"
		};

		static bool ReferencesType(MetadataFile module, string @namespace, string name)
		{
			var metadata = module.Metadata;
			foreach (var handle in metadata.TypeReferences)
			{
				var type = metadata.GetTypeReference(handle);
				if (metadata.StringComparer.Equals(type.Namespace, @namespace)
					&& metadata.StringComparer.Equals(type.Name, name))
				{
					return true;
				}
			}
			return false;
		}

		// A modern assembly that directly references WindowsBase is compiled through the WPF framework
		// reference even when it never names a PresentationFramework type. PresentationFramework still
		// contributes namespaces to C# lookup in that project: notably Microsoft.Windows, which can make
		// a metadata type such as global::Windows.Win32.PInvoke bind incorrectly when emitted without the
		// global alias. Load it only for WindowsBase consumers so unrelated decompilations do not pay for
		// the much larger Windows Desktop name surface.
		static readonly string[] windowsDesktopNamespaceCompletionReferences = new[] {
			"PresentationFramework"
		};

		sealed class NameLookupOnlyModuleReference : IModuleReference
		{
			readonly MetadataFile file;
			readonly TypeSystemOptions options;

			public NameLookupOnlyModuleReference(MetadataFile file, TypeSystemOptions options)
			{
				this.file = file;
				this.options = options;
			}

			IModule IModuleReference.Resolve(ITypeResolveContext context)
			{
				return new MetadataModule(context.Compilation, file, options, isNameLookupOnly: true);
			}
		}

		private async Task InitializeAsync(MetadataFile mainModule, IAssemblyResolver assemblyResolver)
		{
			DecompilerEventSource.Log.TypeSystemInitStart(mainModule.Name);
			int referencedAssembliesResolved = 0;
			try
			{
				referencedAssembliesResolved = await InitializeCoreAsync(mainModule, assemblyResolver).ConfigureAwait(false);
			}
			finally
			{
				DecompilerEventSource.Log.TypeSystemInitStop(mainModule.Name, referencedAssembliesResolved);
			}
		}

		/// <returns>The number of references in the final set passed to Init(): distinct
		/// resolved assemblies (same-name lower-version duplicates dropped) plus resolved
		/// non-assembly modules.</returns>
		private async Task<int> InitializeCoreAsync(MetadataFile mainModule, IAssemblyResolver assemblyResolver)
		{
			// Load referenced assemblies and type-forwarder references.
			// This is necessary to make .NET Core/PCL binaries work better.
			var referencedAssemblies = new List<MetadataFile>();
			var nameLookupOnlyAssemblyNames = new HashSet<string>();
			// Simple names of the assembly references actually declared in metadata (by the main
			// module or by type forwarders), whether or not they resolve. An assembly the module
			// declares a reference to is a real reference even when the declared reference failed
			// to resolve and the file was found via the implicit-reference fallback instead, so it
			// must never be demoted to a name-lookup-only module.
			var declaredAssemblyReferenceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var assemblyReferenceQueue = new Queue<(bool IsAssembly, MetadataFile MainModule, object Reference, Task<MetadataFile> ResolveTask)>();
			var comparer = KeyComparer.Create(((bool IsAssembly, MetadataFile MainModule, object Reference) reference) =>
				reference.IsAssembly ? "A:" + ((IAssemblyReference)reference.Reference).FullName :
									   "M:" + reference.Reference);
			var assemblyReferencesInQueue = new HashSet<(bool IsAssembly, MetadataFile Parent, object Reference)>(comparer);
			var mainMetadata = mainModule.Metadata;
			var tfm = mainModule.DetectTargetFrameworkId();
			var (identifier, version) = UniversalAssemblyResolver.ParseTargetFramework(tfm);
			foreach (var h in mainMetadata.GetModuleReferences())
			{
				try
				{
					var moduleRef = mainMetadata.GetModuleReference(h);
					var moduleName = mainMetadata.GetString(moduleRef.Name);
					foreach (var fileHandle in mainMetadata.AssemblyFiles)
					{
						var file = mainMetadata.GetAssemblyFile(fileHandle);
						if (mainMetadata.StringComparer.Equals(file.Name, moduleName) && file.ContainsMetadata)
						{
							AddToQueue(false, mainModule, moduleName);
							break;
						}
					}
				}
				catch (BadImageFormatException)
				{
				}
			}
			foreach (var refs in mainModule.AssemblyReferences)
			{
				declaredAssemblyReferenceNames.Add(refs.Name);
				AddToQueue(true, mainModule, refs);
			}
			while (assemblyReferenceQueue.Count > 0)
			{
				var asmRef = assemblyReferenceQueue.Dequeue();
				var asm = await asmRef.ResolveTask.ConfigureAwait(false);
				if (asm != null)
				{
					referencedAssemblies.Add(asm);
					var metadata = asm.Metadata;
					foreach (var h in metadata.ExportedTypes)
					{
						var exportedType = metadata.GetExportedType(h);
						switch (exportedType.Implementation.Kind)
						{
							case SRM.HandleKind.AssemblyReference:
								var forwarderRef = new AssemblyReference(asm, (SRM.AssemblyReferenceHandle)exportedType.Implementation);
								declaredAssemblyReferenceNames.Add(forwarderRef.Name);
								AddToQueue(true, asm, forwarderRef);
								break;
							case SRM.HandleKind.AssemblyFile:
								var file = metadata.GetAssemblyFile((SRM.AssemblyFileHandle)exportedType.Implementation);
								AddToQueue(false, asm, metadata.GetString(file.Name));
								break;
						}
					}
				}
				if (assemblyReferenceQueue.Count == 0)
				{
					// For .NET Core and .NET 5 and newer, we need to pull in implicit references which are not included in the metadata,
					// as they contain compile-time-only types, such as System.Runtime.InteropServices.dll (for DllImport, MarshalAs, etc.)
					switch (identifier)
					{
						case TargetFrameworkIdentifier.NETCoreApp:
						case TargetFrameworkIdentifier.NETStandard:
						case TargetFrameworkIdentifier.NET:
							var namespaceCompletionReferencesForModule = new List<string>(namespaceCompletionReferences);
							// The reference pack also contributes System.Reflection.AssemblyHashAlgorithm from
							// System.Reflection.Metadata. Load that name surface only when the input actually uses
							// the identically named System.Configuration.Assemblies enum from System.Runtime; this
							// keeps the common type-system initialization path free of an otherwise unused module.
							if (ReferencesType(mainModule, "System.Configuration.Assemblies", "AssemblyHashAlgorithm"))
							{
								namespaceCompletionReferencesForModule.Add("System.Reflection.Metadata");
							}
							if (declaredAssemblyReferenceNames.Contains("WindowsBase"))
							{
								namespaceCompletionReferencesForModule.AddRange(windowsDesktopNamespaceCompletionReferences);
							}
							foreach (var item in implicitReferences.Concat(namespaceCompletionReferencesForModule))
							{
								var existing = referencedAssemblies.FirstOrDefault(asm => asm.Name == item);
								if (existing == null)
								{
									AddToQueue(true, mainModule, AssemblyNameReference.Parse(item + ", Version=" + version.ToString(3) + ".0, Culture=neutral"));
									// Demote to name-lookup-only just those assemblies the module does not
									// declare a reference to; a declared-but-unresolved reference stays a
									// full reference when the fallback load finds it.
									if (namespaceCompletionReferencesForModule.Contains(item)
										&& !declaredAssemblyReferenceNames.Contains(item))
									{
										nameLookupOnlyAssemblyNames.Add(item);
									}
								}
							}
							break;
					}

				}
			}
			if (!(identifier == TargetFrameworkIdentifier.NET && version >= new Version(7, 0)))
			{
				typeSystemOptions &= ~TypeSystemOptions.NativeIntegersWithoutAttribute;
			}
			var mainModuleWithOptions = mainModule.WithOptions(typeSystemOptions);
			// create IModuleReferences for all references
			var referencedAssembliesWithOptions = new List<IModuleReference>(referencedAssemblies.Count);
			Dictionary<string, (Version version, int insertionIndex)> referenceAssemblyVersionMap = new();
			foreach (var file in referencedAssemblies)
			{
				// if the file is an assembly, we need to make sure to deduplicate all assemblies,
				// with the same name, but different version. We keep the highest version number.
				if (file.IsAssembly)
				{
					var newFileVersion = file.Metadata.GetAssemblyDefinition().Version;
					if (referenceAssemblyVersionMap.TryGetValue(file.Name, out var info))
					{
						if (newFileVersion >= info.version)
						{
							referencedAssembliesWithOptions[info.insertionIndex] = CreateModuleReference(file);
							referenceAssemblyVersionMap[file.Name] = (newFileVersion, info.insertionIndex);
						}
						continue;
					}
					else
					{
						referenceAssemblyVersionMap[file.Name] = (file.Metadata.GetAssemblyDefinition().Version, referencedAssembliesWithOptions.Count);
					}
				}
				referencedAssembliesWithOptions.Add(CreateModuleReference(file));
			}
			// Primitive types are necessary to avoid assertions in ILReader.
			// Other known types are necessary in order for transforms to work (e.g. Task<T> for async transform).
			// Figure out which known types are missing from our type system so far:
			var missingKnownTypes = KnownTypeReference.AllKnownTypes.Where(IsMissing).ToList();
			if (missingKnownTypes.Count > 0)
			{
				Init(mainModuleWithOptions, referencedAssembliesWithOptions.Concat(new[] { MinimalCorlib.CreateWithTypes(missingKnownTypes) }));
			}
			else
			{
				Init(mainModuleWithOptions, referencedAssembliesWithOptions);
			}
			this.mainModule = (MetadataModule)base.MainModule;
			return referencedAssembliesWithOptions.Count;

			IModuleReference CreateModuleReference(MetadataFile file)
			{
				if (nameLookupOnlyAssemblyNames.Contains(file.Name))
				{
					return new NameLookupOnlyModuleReference(file, typeSystemOptions);
				}
				return file.WithOptions(typeSystemOptions);
			}

			void AddToQueue(bool isAssembly, MetadataFile mainModule, object reference)
			{
				if (assemblyReferencesInQueue.Add((isAssembly, mainModule, reference)))
				{
					// Immediately start loading the referenced module as we add the entry to the queue.
					// This allows loading multiple modules in parallel.
					Task<MetadataFile> asm;
					if (isAssembly)
					{
						asm = assemblyResolver.ResolveAsync((IAssemblyReference)reference);
					}
					else
					{
						asm = assemblyResolver.ResolveModuleAsync(mainModule, (string)reference);
					}
					assemblyReferenceQueue.Enqueue((isAssembly, mainModule, reference, asm));
				}
			}

			bool IsMissing(KnownTypeReference knownType)
			{
				var name = knownType.TypeName;
				if (!mainModule.GetTypeDefinition(name).IsNil)
					return false;
				foreach (var file in referencedAssemblies)
				{
					if (!file.GetTypeDefinition(name).IsNil)
						return false;
				}
				return true;
			}
		}

		public new MetadataModule MainModule => mainModule;

		public override TypeSystemOptions TypeSystemOptions => typeSystemOptions;
	}
}

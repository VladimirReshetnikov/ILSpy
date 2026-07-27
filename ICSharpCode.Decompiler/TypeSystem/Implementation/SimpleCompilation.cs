// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Provides a baseline <see cref="ICompilation"/> implementation backed by a main module and a fixed reference set.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> <see cref="SimpleCompilation"/> resolves each supplied <see cref="IModuleReference"/> once during
	/// initialization and then serves immutable module lists for the lifetime of the instance.
	/// </para>
	/// <para>
	/// <b>Extensibility.</b> Derived classes can customize alias behavior and root-namespace composition by overriding
	/// <see cref="GetNamespaceForExternAlias"/> and <see cref="CreateRootNamespace"/>.
	/// </para>
	/// <para>
	/// <b>Notes for maintainers.</b> Several APIs throw <see cref="InvalidOperationException"/> when called before
	/// initialization so subclasses that use the protected constructor must invoke <see cref="Init"/> before exposing the
	/// instance.
	/// </para>
	/// </remarks>
	public class SimpleCompilation : ICompilation
	{
		readonly CacheManager cacheManager = new CacheManager();
		IModule mainModule;
		KnownTypeCache knownTypeCache;
		IReadOnlyList<IModule> assemblies;
		IReadOnlyList<IModule> referencedAssemblies;
		bool initialized;
		INamespace rootNamespace;

		/// <summary>
		/// Initializes a compilation from a main module reference and additional module references.
		/// </summary>
		/// <param name="mainAssembly">Reference that resolves to the primary module being decompiled.</param>
		/// <param name="assemblyReferences">References that should populate <see cref="ReferencedModules"/>.</param>
		public SimpleCompilation(IModuleReference mainAssembly, params IModuleReference[] assemblyReferences)
		{
			Init(mainAssembly, assemblyReferences);
		}

		/// <summary>
		/// Initializes a compilation from a main module reference and an enumerable of references.
		/// </summary>
		/// <param name="mainAssembly">Reference that resolves to the primary module being decompiled.</param>
		/// <param name="assemblyReferences">References that should populate <see cref="ReferencedModules"/>.</param>
		public SimpleCompilation(IModuleReference mainAssembly, IEnumerable<IModuleReference> assemblyReferences)
		{
			Init(mainAssembly, assemblyReferences);
		}

		/// <summary>
		/// Initializes a new instance for derived types that need to defer initialization.
		/// </summary>
		/// <remarks>
		/// Derived classes must call <see cref="Init(IModuleReference, IEnumerable{IModuleReference})"/> before exposing
		/// this instance to callers.
		/// </remarks>
		protected SimpleCompilation()
		{
		}

		/// <summary>
		/// Resolves module references and initializes the immutable module lists used by this compilation.
		/// </summary>
		/// <param name="mainAssembly">Reference for the primary module.</param>
		/// <param name="assemblyReferences">References for additional modules.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="mainAssembly"/> or <paramref name="assemblyReferences"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// A supplied reference cannot be resolved into a usable module.
		/// </exception>
		protected void Init(IModuleReference mainAssembly, IEnumerable<IModuleReference> assemblyReferences)
		{
			if (mainAssembly == null)
				throw new ArgumentNullException(nameof(mainAssembly));
			if (assemblyReferences == null)
				throw new ArgumentNullException(nameof(assemblyReferences));
			var context = new SimpleTypeResolveContext(this);
			this.mainModule = mainAssembly.Resolve(context);
			List<IModule> assemblies = new List<IModule>();
			assemblies.Add(this.mainModule);
			List<IModule> referencedAssemblies = new List<IModule>();
			foreach (var asmRef in assemblyReferences)
			{
				IModule asm;
				try
				{
					asm = asmRef.Resolve(context);
				}
				catch (InvalidOperationException)
				{
					throw new InvalidOperationException("Tried to initialize compilation with an invalid assembly reference. (Forgot to load the assembly reference ? - see CecilLoader)");
				}
				if (asm != null && !assemblies.Contains(asm))
					assemblies.Add(asm);
				if (asm != null && !referencedAssemblies.Contains(asm))
					referencedAssemblies.Add(asm);
			}
			this.assemblies = assemblies.AsReadOnly();
			this.referencedAssemblies = referencedAssemblies.AsReadOnly();
			this.knownTypeCache = new KnownTypeCache(this);
			this.initialized = true;
		}

		/// <inheritdoc/>
		public IModule MainModule {
			get {
				if (!initialized)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return mainModule;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<IModule> Modules {
			get {
				if (!initialized)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return assemblies;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<IModule> ReferencedModules {
			get {
				if (!initialized)
					throw new InvalidOperationException("Compilation isn't initialized yet");
				return referencedAssemblies;
			}
		}

		/// <inheritdoc/>
		public INamespace RootNamespace {
			get {
				INamespace ns = LazyInit.VolatileRead(ref this.rootNamespace);
				if (ns != null)
				{
					return ns;
				}
				else
				{
					if (!initialized)
						throw new InvalidOperationException("Compilation isn't initialized yet");
					return LazyInit.GetOrSet(ref this.rootNamespace, CreateRootNamespace());
				}
			}
		}

		/// <summary>
		/// Creates the global namespace view used for <see cref="RootNamespace"/>.
		/// </summary>
		/// <returns>
		/// A merged namespace containing the main module root and all referenced module roots.
		/// </returns>
		/// <remarks>
		/// Derived classes can override this method to alter global namespace composition, for example to add extern-alias
		/// partitioning.
		/// </remarks>
		protected virtual INamespace CreateRootNamespace()
		{
			// SimpleCompilation does not support extern aliases; but derived classes might.
			// CreateRootNamespace() is virtual so that derived classes can change the global namespace.
			INamespace[] namespaces = new INamespace[referencedAssemblies.Count + 1];
			namespaces[0] = mainModule.RootNamespace;
			for (int i = 0; i < referencedAssemblies.Count; i++)
			{
				namespaces[i + 1] = referencedAssemblies[i].RootNamespace;
			}
			return new MergedNamespace(this, namespaces);
		}

		/// <inheritdoc/>
		public CacheManager CacheManager {
			get { return cacheManager; }
		}

		/// <inheritdoc/>
		public virtual INamespace GetNamespaceForExternAlias(string alias)
		{
			if (string.IsNullOrEmpty(alias))
				return this.RootNamespace;
			// SimpleCompilation does not support extern aliases; but derived classes might.
			return null;
		}

		/// <inheritdoc/>
		public IType FindType(KnownTypeCode typeCode)
		{
			return knownTypeCache.FindType(typeCode);
		}

		/// <inheritdoc/>
		public StringComparer NameComparer {
			get { return StringComparer.Ordinal; }
		}

		/// <inheritdoc/>
		public virtual TypeSystemOptions TypeSystemOptions => TypeSystemOptions.Default;

		public override string ToString()
		{
			return "[" + GetType().Name + " " + mainModule.AssemblyName + "]";
		}
	}
}

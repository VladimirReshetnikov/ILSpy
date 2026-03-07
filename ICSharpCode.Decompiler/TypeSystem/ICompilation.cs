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

#nullable enable

using System;
using System.Collections.Generic;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents a fully materialized semantic compilation graph used for type and member resolution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> A compilation groups the primary module being decompiled together with all modules that are
	/// reachable through reference resolution. Resolution APIs in the type system use this object as the identity root,
	/// so symbols from different <see cref="ICompilation"/> instances are intentionally not interchangeable.
	/// </para>
	/// <para>
	/// <b>Usage.</b> Most call sites obtain a compilation through <see cref="DecompilerTypeSystem"/> or through helper
	/// contexts such as <see cref="ITypeResolveContext"/>. Consumers typically use it to resolve known framework types,
	/// enumerate referenced modules, and access the merged root namespace used for cross-module lookup.
	/// </para>
	/// <para>
	/// <b>Notes for implementers.</b> Implementations are expected to preserve stable symbol identity semantics within
	/// one compilation instance. Several decompiler transforms rely on comparing definition objects by reference.
	/// </para>
	/// </remarks>
	public interface ICompilation
	{
		/// <summary>
		/// Gets the primary module that defines the entry point for this semantic graph.
		/// </summary>
		/// <value>
		/// The module being decompiled or analyzed. All items in <see cref="Modules"/> are reachable from this module via
		/// direct references, type forwarders, or resolver-provided augmentation.
		/// </value>
		IModule MainModule { get; }

		/// <summary>
		/// Gets all modules participating in this compilation.
		/// </summary>
		/// <remarks>
		/// The first entry is always <see cref="MainModule"/>. The remaining entries are resolver-dependent and typically
		/// represent transitive references used during type/member lookup.
		/// </remarks>
		IReadOnlyList<IModule> Modules { get; }

		/// <summary>
		/// Gets the modules referenced by <see cref="MainModule"/>.
		/// </summary>
		/// <value>
		/// The subset of <see cref="Modules"/> that excludes <see cref="MainModule"/>.
		/// </value>
		IReadOnlyList<IModule> ReferencedModules { get; }

		/// <summary>
		/// Gets the global namespace view used for unqualified and namespace-qualified type lookup.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The returned namespace is unnamed and represents a merged projection over all modules in
		/// <see cref="Modules"/>.
		/// </para>
		/// <para>
		/// This is unrelated to the C# project setting often called "root namespace".
		/// </para>
		/// </remarks>
		INamespace RootNamespace { get; }

		/// <summary>
		/// Gets the alias-specific root namespace used when resolving <c>extern alias</c> references.
		/// </summary>
		/// <param name="alias">
		/// The alias to resolve. <see langword="null"/> or an empty string requests the global root namespace.
		/// </param>
		/// <returns>
		/// The namespace projection for the alias, or <see langword="null"/> when this compilation does not expose the
		/// alias.
		/// </returns>
		/// <remarks>
		/// Implementations that do not support extern aliases should return <see cref="RootNamespace"/> for the global
		/// case and <see langword="null"/> otherwise.
		/// </remarks>
		INamespace? GetNamespaceForExternAlias(string? alias);

		/// <summary>
		/// Resolves a framework type by its well-known identifier.
		/// </summary>
		/// <param name="typeCode">The known type identifier to resolve.</param>
		/// <returns>
		/// The corresponding type in this compilation, or an unknown type when the requested framework type is unavailable
		/// in the loaded reference set.
		/// </returns>
		IType FindType(KnownTypeCode typeCode);

		/// <summary>
		/// Gets the comparer that defines identifier equality for namespace and type lookup.
		/// </summary>
		/// <value>
		/// The comparer used by APIs such as <see cref="INamespace.GetTypeDefinition"/>.
		/// </value>
		StringComparer NameComparer { get; }

		/// <summary>
		/// Gets the cache manager used to memoize type-system lookups and resolution results.
		/// </summary>
		CacheManager CacheManager { get; }

		/// <summary>
		/// Gets the metadata projection options that were used when this compilation was created.
		/// </summary>
		TypeSystemOptions TypeSystemOptions { get; }
	}

	/// <summary>
	/// Exposes the owning <see cref="ICompilation"/> for symbols and contexts that are bound to one compilation graph.
	/// </summary>
	public interface ICompilationProvider
	{
		/// <summary>
		/// Gets the parent compilation.
		/// </summary>
		/// <value>
		/// The non-null compilation that governs symbol identity and resolution behavior for this instance.
		/// </value>
		ICompilation Compilation { get; }
	}
}

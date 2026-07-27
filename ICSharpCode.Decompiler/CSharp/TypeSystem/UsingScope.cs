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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

#nullable enable

namespace ICSharpCode.Decompiler.CSharp.TypeSystem
{
	/// <summary>
	/// Represents the set of namespace imports and aliasing rules active at one point in C# name resolution.
	/// </summary>
	/// <remarks>
	/// Scopes form a parent chain that mirrors namespace nesting; the resolver walks that chain for
	/// namespace/type lookup and caches identifier results per scope for reuse.
	/// </remarks>
	public class UsingScope
	{
		readonly CSharpTypeResolveContext parentContext;

		internal readonly ConcurrentDictionary<string, ResolveResult> ResolveCache = new ConcurrentDictionary<string, ResolveResult>();
		internal List<List<IMethod>>? AllExtensionMethods;

		/// <summary>
		/// Initializes a using scope for a namespace declaration (or the compilation root namespace).
		/// </summary>
		/// <param name="context">Parent resolve context from which the enclosing scope chain is derived.</param>
		/// <param name="namespace">Namespace represented by this scope level.</param>
		/// <param name="usings">Namespaces imported directly into this scope level.</param>
		public UsingScope(CSharpTypeResolveContext context, INamespace @namespace, ImmutableArray<INamespace> usings)
		{
			this.parentContext = context ?? throw new ArgumentNullException(nameof(context));
			this.Usings = usings;
			this.Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
		}

		/// <summary>
		/// Gets the namespace represented by this scope level.
		/// </summary>
		public INamespace Namespace { get; }

		/// <summary>
		/// Gets the next outer using scope, or <see langword="null"/> for the outermost scope.
		/// </summary>
		public UsingScope Parent {
			get { return parentContext.CurrentUsingScope; }
		}

		/// <summary>
		/// Gets the namespaces imported directly by this scope.
		/// </summary>
		public ImmutableArray<INamespace> Usings { get; }

		/// <summary>
		/// Gets aliases declared in this scope.
		/// </summary>
		/// <remarks>
		/// The decompiler currently models only namespace imports here, so this list is empty.
		/// </remarks>
		public IReadOnlyList<KeyValuePair<string, ResolveResult>> UsingAliases => [];

		/// <summary>
		/// Gets extern aliases declared in this scope.
		/// </summary>
		/// <remarks>
		/// Extern aliases are not represented by this implementation, so this list is empty.
		/// </remarks>
		public IReadOnlyList<string> ExternAliases => [];

		/// <summary>
		/// Gets whether this scope declares an alias with the specified identifier.
		/// </summary>
		/// <param name="identifier">Alias name to test.</param>
		/// <returns><see langword="true"/> if an alias with <paramref name="identifier"/> exists; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// This implementation currently has no alias table and therefore always returns <see langword="false"/>.
		/// </remarks>
		public bool HasAlias(string identifier) => false;

		internal UsingScope WithNestedNamespace(string simpleName)
		{
			var ns = Namespace.GetChildNamespace(simpleName) ?? new DummyNamespace(Namespace, simpleName);
			return new UsingScope(
				parentContext.WithUsingScope(this),
				ns,
				[]);
		}

		sealed class DummyNamespace : INamespace
		{
			readonly INamespace parentNamespace;
			readonly string name;

			public DummyNamespace(INamespace parentNamespace, string name)
			{
				this.parentNamespace = parentNamespace;
				this.name = name;
			}

			string INamespace.ExternAlias => "";

			string INamespace.FullName {
				get { return NamespaceDeclaration.BuildQualifiedName(parentNamespace.FullName, name); }
			}

			public string Name {
				get { return name; }
			}

			SymbolKind ISymbol.SymbolKind {
				get { return SymbolKind.Namespace; }
			}

			INamespace INamespace.ParentNamespace {
				get { return parentNamespace; }
			}

			IEnumerable<INamespace> INamespace.ChildNamespaces {
				get { return EmptyList<INamespace>.Instance; }
			}

			IEnumerable<ITypeDefinition> INamespace.Types {
				get { return EmptyList<ITypeDefinition>.Instance; }
			}

			IEnumerable<IModule> INamespace.ContributingModules {
				get { return EmptyList<IModule>.Instance; }
			}

			ICompilation ICompilationProvider.Compilation {
				get { return parentNamespace.Compilation; }
			}

			INamespace? INamespace.GetChildNamespace(string name)
			{
				return null;
			}

			ITypeDefinition? INamespace.GetTypeDefinition(string name, int typeParameterCount)
			{
				return null;
			}
		}
	}
}

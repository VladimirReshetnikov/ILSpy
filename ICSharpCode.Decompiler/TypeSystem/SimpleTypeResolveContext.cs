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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Provides an immutable <see cref="ITypeResolveContext"/> implementation for compilation, module, type, and member scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> Each instance captures one snapshot of resolution scope. Methods that adjust scope return new
	/// instances and keep the original instance unchanged.
	/// </para>
	/// <para>
	/// <b>Usage.</b> This type is commonly used when resolving <see cref="ITypeReference"/> values outside a full
	/// resolver pipeline, for example in metadata loaders or helper utilities that need a lightweight context object.
	/// </para>
	/// </remarks>
	public class SimpleTypeResolveContext : ITypeResolveContext
	{
		readonly ICompilation compilation;
		readonly IModule currentModule;
		readonly ITypeDefinition currentTypeDefinition;
		readonly IMember currentMember;

		/// <summary>
		/// Creates a context that is anchored only to a compilation.
		/// </summary>
		/// <param name="compilation">The compilation used for symbol identity and known-type lookup.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="compilation"/> is <see langword="null"/>.</exception>
		public SimpleTypeResolveContext(ICompilation compilation)
		{
			if (compilation == null)
				throw new ArgumentNullException(nameof(compilation));
			this.compilation = compilation;
		}

		/// <summary>
		/// Creates a context anchored to a specific module.
		/// </summary>
		/// <param name="module">The current module scope.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="module"/> is <see langword="null"/>.</exception>
		public SimpleTypeResolveContext(IModule module)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			this.compilation = module.Compilation;
			this.currentModule = module;
		}

		/// <summary>
		/// Creates a context anchored to an entity and infers module/type/member scope from that entity.
		/// </summary>
		/// <param name="entity">The entity that supplies the current compilation and lexical scope.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <see langword="null"/>.</exception>
		public SimpleTypeResolveContext(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			this.compilation = entity.Compilation;
			this.currentModule = entity.ParentModule;
			this.currentTypeDefinition = (entity as ITypeDefinition) ?? entity.DeclaringTypeDefinition;
			this.currentMember = entity as IMember;
		}

		private SimpleTypeResolveContext(ICompilation compilation, IModule currentModule, ITypeDefinition currentTypeDefinition, IMember currentMember)
		{
			this.compilation = compilation;
			this.currentModule = currentModule;
			this.currentTypeDefinition = currentTypeDefinition;
			this.currentMember = currentMember;
		}

		/// <inheritdoc/>
		public ICompilation Compilation {
			get { return compilation; }
		}

		/// <inheritdoc/>
		public IModule CurrentModule {
			get { return currentModule; }
		}

		/// <inheritdoc/>
		public ITypeDefinition CurrentTypeDefinition {
			get { return currentTypeDefinition; }
		}

		/// <inheritdoc/>
		public IMember CurrentMember {
			get { return currentMember; }
		}

		/// <inheritdoc/>
		public ITypeResolveContext WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			return new SimpleTypeResolveContext(compilation, currentModule, typeDefinition, currentMember);
		}

		/// <inheritdoc/>
		public ITypeResolveContext WithCurrentMember(IMember member)
		{
			return new SimpleTypeResolveContext(compilation, currentModule, currentTypeDefinition, member);
		}
	}
}

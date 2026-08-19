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

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Captures the lexical type-resolution state used by the C# resolver while decompiling code.
	/// </summary>
	/// <remarks>
	/// Instances are immutable snapshots: methods prefixed with <c>With</c> return a new context
	/// with one component replaced, so resolver pipelines can branch safely without mutating shared state.
	/// </remarks>
	public sealed class CSharpTypeResolveContext : ITypeResolveContext
	{
		readonly IModule module;
		readonly UsingScope currentUsingScope;
		readonly ITypeDefinition currentTypeDefinition;
		readonly IMember currentMember;
		readonly string[] methodTypeParameterNames;

		/// <summary>
		/// Initializes a new resolve context rooted in a module, with optional scope and member anchors.
		/// </summary>
		/// <param name="module">Module that provides the compilation and global type system for lookups.</param>
		/// <param name="usingScope">Current using/namespace scope used for namespace and extension-method lookup.</param>
		/// <param name="typeDefinition">Current containing type, if resolution runs inside a type body.</param>
		/// <param name="member">Current containing member, if resolution runs inside a member body.</param>
		public CSharpTypeResolveContext(IModule module, UsingScope usingScope = null, ITypeDefinition typeDefinition = null, IMember member = null)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			this.module = module;
			this.currentUsingScope = usingScope;
			this.currentTypeDefinition = typeDefinition;
			this.currentMember = member;
		}

		private CSharpTypeResolveContext(IModule module, UsingScope usingScope, ITypeDefinition typeDefinition, IMember member, string[] methodTypeParameterNames)
		{
			this.module = module;
			this.currentUsingScope = usingScope;
			this.currentTypeDefinition = typeDefinition;
			this.currentMember = member;
			this.methodTypeParameterNames = methodTypeParameterNames;
		}

		/// <summary>
		/// Gets the active using scope.
		/// </summary>
		public UsingScope CurrentUsingScope {
			get { return currentUsingScope; }
		}

		/// <summary>
		/// Gets the compilation associated with <see cref="CurrentModule"/>.
		/// </summary>
		public ICompilation Compilation {
			get { return module.Compilation; }
		}

		/// <summary>
		/// Gets the module that acts as the root for metadata and type lookup.
		/// </summary>
		public IModule CurrentModule {
			get { return module; }
		}

		/// <summary>
		/// Gets the current containing type, or <see langword="null"/> when resolution is not in a type body.
		/// </summary>
		public ITypeDefinition CurrentTypeDefinition {
			get { return currentTypeDefinition; }
		}

		/// <summary>
		/// Gets the current containing member, or <see langword="null"/> when resolution is not in a member body.
		/// </summary>
		public IMember CurrentMember {
			get { return currentMember; }
		}

		/// <summary>
		/// Creates a copy of this context with a different current type.
		/// </summary>
		/// <param name="typeDefinition">Type to set as the active containing type.</param>
		/// <returns>A new context that keeps all other state from this instance.</returns>
		public CSharpTypeResolveContext WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			return new CSharpTypeResolveContext(module, currentUsingScope, typeDefinition, currentMember, methodTypeParameterNames);
		}

		ITypeResolveContext ITypeResolveContext.WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			return WithCurrentTypeDefinition(typeDefinition);
		}

		/// <summary>
		/// Creates a copy of this context with a different current member.
		/// </summary>
		/// <param name="member">Member to set as the active containing member.</param>
		/// <returns>A new context that keeps all other state from this instance.</returns>
		public CSharpTypeResolveContext WithCurrentMember(IMember member)
		{
			return new CSharpTypeResolveContext(module, currentUsingScope, currentTypeDefinition, member, methodTypeParameterNames);
		}

		ITypeResolveContext ITypeResolveContext.WithCurrentMember(IMember member)
		{
			return WithCurrentMember(member);
		}

		/// <summary>
		/// Creates a copy of this context with a different using scope.
		/// </summary>
		/// <param name="usingScope">Using scope to use for namespace and alias lookup.</param>
		/// <returns>A new context that keeps all other state from this instance.</returns>
		public CSharpTypeResolveContext WithUsingScope(UsingScope usingScope)
		{
			return new CSharpTypeResolveContext(module, usingScope, currentTypeDefinition, currentMember, methodTypeParameterNames);
		}
	}
}

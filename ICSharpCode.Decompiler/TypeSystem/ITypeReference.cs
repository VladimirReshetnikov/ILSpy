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

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents an unresolved reference that can be bound to an <see cref="IType"/> in a specific resolve context.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> Implementations capture type identity information independently from a concrete declaration.
	/// The final <see cref="IType"/> may differ depending on generic substitutions, current member/type scope, and
	/// compilation options supplied through <see cref="ITypeResolveContext"/>.
	/// </para>
	/// <para>
	/// <b>Usage.</b> Consumers should delay assumptions about type shape until <see cref="Resolve"/> completes.
	/// The same <see cref="ITypeReference"/> instance can resolve to different concrete type instances when invoked with
	/// different contexts.
	/// </para>
	/// </remarks>
	public interface ITypeReference
	{
		// Keep this interface simple: I decided against having GetMethods/GetEvents etc. here,
		// so that the Resolve step is never hidden from the consumer.

		// I decided against implementing IFreezable here: IUnresolvedTypeDefinition can be used as ITypeReference,
		// but when freezing the reference, one wouldn't expect the definition to freeze.

		/// <summary>
		/// Resolves this reference within the supplied semantic context.
		/// </summary>
		/// <param name="context">
		/// The resolve context that provides compilation identity and current scope (module/type/member) for generic and
		/// nested-name binding.
		/// </param>
		/// <returns>
		/// The resolved type. Returns an unknown type (<see cref="TypeKind.Unknown"/>) when binding fails.
		/// Never returns <see langword="null"/>.
		/// </returns>
		IType Resolve(ITypeResolveContext context);
	}

	/// <summary>
	/// Describes the ambient scope used by type and member reference resolution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Semantics.</b> A resolve context carries both a compilation root and optional scope anchors
	/// (<see cref="CurrentModule"/>, <see cref="CurrentTypeDefinition"/>, <see cref="CurrentMember"/>). Resolution
	/// logic uses these anchors to interpret generic parameters, nested types, and member-local constructs.
	/// </para>
	/// <para>
	/// <b>Usage.</b> Contexts are typically immutable snapshots; methods such as
	/// <see cref="WithCurrentTypeDefinition"/> and <see cref="WithCurrentMember"/> produce derived contexts for narrower
	/// scopes without mutating the original instance.
	/// </para>
	/// </remarks>
	public interface ITypeResolveContext : ICompilationProvider
	{
		/// <summary>
		/// Gets the module considered "current" for this resolution scope.
		/// </summary>
		/// <value>
		/// The current module, or <see langword="null"/> when the context is compilation-wide and not anchored to a module.
		/// </value>
		IModule? CurrentModule { get; }

		/// <summary>
		/// Gets the current declaring type used for nested type/member lookup.
		/// </summary>
		/// <value>
		/// The active type definition, or <see langword="null"/> when resolution is not occurring inside a type scope.
		/// </value>
		ITypeDefinition? CurrentTypeDefinition { get; }

		/// <summary>
		/// Gets the current member used for method type parameters and member-local symbol interpretation.
		/// </summary>
		/// <value>
		/// The active member, or <see langword="null"/> when resolution is not anchored to a specific member body.
		/// </value>
		IMember? CurrentMember { get; }

		/// <summary>
		/// Creates a context with the specified current type while keeping the same compilation and module scope.
		/// </summary>
		/// <param name="typeDefinition">The replacement current type, or <see langword="null"/> to clear type scope.</param>
		/// <returns>A context that reflects the requested type scope.</returns>
		ITypeResolveContext WithCurrentTypeDefinition(ITypeDefinition? typeDefinition);

		/// <summary>
		/// Creates a context with the specified current member while keeping the same compilation and type scope.
		/// </summary>
		/// <param name="member">The replacement current member, or <see langword="null"/> to clear member scope.</param>
		/// <returns>A context that reflects the requested member scope.</returns>
		ITypeResolveContext WithCurrentMember(IMember? member);
	}
}

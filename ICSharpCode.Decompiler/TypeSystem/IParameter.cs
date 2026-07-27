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
using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Describes how a parameter is passed at call sites and represented in signatures.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Ordering intentionally matches <see cref="CSharp.Syntax.FieldDirection"/> so the decompiler can map parser/AST
	/// modifiers to type-system values without translation tables.
	/// </para>
	/// <para>
	/// Values in this enum describe source-level calling semantics rather than raw metadata flags. For example,
	/// <see cref="In"/> and <see cref="RefReadOnly"/> are both represented by byref signatures in metadata, but they
	/// carry different language contracts and therefore remain distinct here.
	/// </para>
	/// </remarks>
	public enum ReferenceKind : byte
	{
		/// <summary>
		/// The parameter is passed by value (no <c>ref</c>/<c>out</c>/<c>in</c> modifier).
		/// </summary>
		None,
		/// <summary>
		/// The parameter is passed by reference and must be assigned by the callee before return.
		/// </summary>
		Out,
		/// <summary>
		/// The parameter is passed by reference and is both readable and writable by caller and callee.
		/// </summary>
		Ref,
		/// <summary>
		/// The parameter is passed by readonly reference using the C# <c>in</c> modifier.
		/// </summary>
		In,
		/// <summary>
		/// The parameter uses the C# <c>ref readonly</c> modifier.
		/// </summary>
		/// <remarks>
		/// This is represented differently from <see cref="In"/> because ILSpy preserves the exact source-level modifier
		/// set when that information is recoverable from metadata attributes.
		/// </remarks>
		RefReadOnly,
	}

	/// <summary>
	/// Carries C# lifetime annotations that can be attached to parameters.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The decompiler uses this structure to preserve source-level lifetime modifiers (currently <c>scoped</c>)
	/// separately from <see cref="ReferenceKind"/>. That separation is important because both concepts influence
	/// emitted syntax but describe different contracts.
	/// </para>
	/// <para>
	/// Legacy preview fields are kept for backward compatibility with older callers and metadata patterns.
	/// </para>
	/// </remarks>
	public struct LifetimeAnnotation
	{
		/// <summary>
		/// Gets or sets whether the parameter is annotated with C# <c>scoped</c> lifetime semantics.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <c>ScopedRefAttribute</c> metadata maps to a scoped parameter contract;
		/// otherwise <see langword="false"/>.
		/// </value>
		public bool ScopedRef {
#pragma warning disable 618
			get { return RefScoped; }
			set { RefScoped = value; }
#pragma warning restore 618
		}

		/// <summary>
		/// Legacy backing field for <see cref="ScopedRef"/>.
		/// </summary>
		/// <remarks>
		/// This field is obsolete and retained only for source compatibility. New code should use
		/// <see cref="ScopedRef"/>.
		/// </remarks>
		[Obsolete("Use ScopedRef property instead of directly accessing this field")]
		public bool RefScoped;

		/// <summary>
		/// Legacy preview field for an older C# lifetime annotation experiment.
		/// </summary>
		/// <remarks>
		/// Current C# language versions no longer use this form; ILSpy keeps the field so older callers can still
		/// deserialize or inspect historical data.
		/// </remarks>
		[Obsolete("C# 11 preview: \"ref scoped\" no longer supported")]
		public bool ValueScoped;
	}

	/// <summary>
	/// Represents a single parameter in a method, constructor, accessor, delegate, or function pointer signature.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This abstraction merges metadata facts (custom attributes, optional/default-value payloads, marshalling,
	/// byref flags) with language-level interpretation used by ILSpy's resolver and C# output pipeline.
	/// </para>
	/// <para>
	/// Implementations are expected to preserve signature order and to report values that are stable for the lifetime
	/// of the owning symbol.
	/// </para>
	/// </remarks>
	public interface IParameter : IVariable
	{
		/// <summary>
		/// Gets the attributes on this parameter.
		/// </summary>
		IEnumerable<IAttribute> GetAttributes();

		/// <summary>
		/// Gets the reference kind of this parameter.
		/// </summary>
		ReferenceKind ReferenceKind { get; }

		/// <summary>
		/// Gets additional lifetime annotations associated with this parameter.
		/// </summary>
		/// <value>
		/// A <see cref="LifetimeAnnotation"/> value that currently captures scoped-lifetime semantics.
		/// </value>
		/// <remarks>
		/// This value is independent from <see cref="ReferenceKind"/>. A parameter can be by-value or by-reference and
		/// still carry lifetime annotations when the language version and metadata support it.
		/// </remarks>
		LifetimeAnnotation Lifetime { get; }

		/// <summary>
		/// Gets whether this parameter is a C# 'params' parameter.
		/// </summary>
		bool IsParams { get; }

		/// <summary>
		/// Gets whether this parameter is optional.
		/// The default value is given by the <see cref="IVariable.GetConstantValue"/> function.
		/// </summary>
		bool IsOptional { get; }

		/// <summary>
		/// Gets whether this parameter has a constant value when presented in method signature.
		/// </summary>
		/// <remarks>
		/// This can only be <c>true</c> if the parameter is optional, and it's true for most
		/// optional parameters. However it is possible to compile a parameter without a default value,
		/// and some parameters handle their default values in a special way.
		///
		/// For example, <see cref="DecimalConstantAttribute"/> does not use normal constants,
		/// so when <see cref="DecompilerSettings.DecimalConstants"/> is <c>false</c>
		/// we expose <c>DecimalConstantAttribute</c> directly instead of a constant value.
		///
		/// On the call sites, though, we can still use the value inferred from the attribute.
		/// </remarks>
		bool HasConstantValueInSignature { get; }

		/// <summary>
		/// Gets the owner of this parameter.
		/// May return null; for example when parameters belong to lambdas or anonymous methods.
		/// </summary>
		IParameterizedMember? Owner { get; }
	}
}

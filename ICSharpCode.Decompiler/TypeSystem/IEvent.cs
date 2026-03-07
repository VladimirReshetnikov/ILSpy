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

using System.Diagnostics.CodeAnalysis;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Represents an event member, including optional accessor methods and event-level metadata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The type-system models events as first-class members and exposes accessor availability explicitly
	/// because metadata permits incomplete event accessor sets. For example, metadata may contain an event
	/// with only add/remove accessors, or with an additional raise accessor.
	/// </para>
	/// <para>
	/// Consumers should check <see cref="CanAdd"/>, <see cref="CanRemove"/>, and <see cref="CanInvoke"/>
	/// before reading the corresponding accessor property. Implementations guarantee that a <c>Can*</c>
	/// property returning <see langword="true"/> implies the related accessor is non-<see langword="null"/>.
	/// </para>
	/// </remarks>
	public interface IEvent : IMember
	{
		/// <summary>
		/// Gets whether an add accessor is available.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <see cref="AddAccessor"/> can be called; otherwise <see langword="false"/>.
		/// </value>
		[MemberNotNullWhen(true, nameof(AddAccessor))]
		bool CanAdd { get; }

		/// <summary>
		/// Gets whether a remove accessor is available.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <see cref="RemoveAccessor"/> can be called; otherwise <see langword="false"/>.
		/// </value>
		[MemberNotNullWhen(true, nameof(RemoveAccessor))]
		bool CanRemove { get; }

		/// <summary>
		/// Gets whether a raise/invoke accessor is available.
		/// </summary>
		/// <value>
		/// <see langword="true"/> when <see cref="InvokeAccessor"/> is present in metadata; otherwise <see langword="false"/>.
		/// </value>
		[MemberNotNullWhen(true, nameof(InvokeAccessor))]
		bool CanInvoke { get; }

		/// <summary>
		/// Gets the method used to add handlers to this event.
		/// </summary>
		/// <value>
		/// The add accessor when available; otherwise <see langword="null"/>.
		/// </value>
		IMethod? AddAccessor { get; }

		/// <summary>
		/// Gets the method used to remove handlers from this event.
		/// </summary>
		/// <value>
		/// The remove accessor when available; otherwise <see langword="null"/>.
		/// </value>
		IMethod? RemoveAccessor { get; }

		/// <summary>
		/// Gets the method used to raise this event.
		/// </summary>
		/// <value>
		/// The raise accessor when available; otherwise <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// C# source declarations typically do not emit a raise accessor, but it can appear in IL metadata.
		/// </remarks>
		IMethod? InvokeAccessor { get; }
	}
}

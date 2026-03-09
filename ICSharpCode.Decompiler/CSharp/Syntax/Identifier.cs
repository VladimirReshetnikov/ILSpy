// 
// Identifier.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
	/// <summary>
	/// Represents an identifier token in the C# syntax tree, including source span and verbatim-identifier state.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Most declaration and reference nodes store their textual name via an <see cref="Identifier"/> child in
	/// <see cref="Roles.Identifier"/>. Keeping the identifier as a separate token node allows transforms to preserve
	/// token-level information (for example <c>@</c>-escaping) without mutating parent-node shape.
	/// </para>
	/// <para>
	/// Instances can be frozen when attached to immutable trees; mutating members call <c>ThrowIfFrozen()</c>.
	/// </para>
	/// </remarks>
	public class Identifier : AstNode
	{
		/// <summary>
		/// Sentinel node that represents a missing identifier.
		/// </summary>
		public new static readonly Identifier Null = new NullIdentifier();
		sealed class NullIdentifier : Identifier
		{
			public override bool IsNull {
				get {
					return true;
				}
			}

			public override void AcceptVisitor(IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}

			public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}

			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}

			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}

		/// <summary>
		/// Gets the AST node classification.
		/// </summary>
		public override NodeType NodeType {
			get {
				return NodeType.Token;
			}
		}

		string name;
		/// <summary>
		/// Gets or sets the identifier text without a leading <c>@</c> marker.
		/// </summary>
		/// <exception cref="ArgumentNullException">
		/// Thrown when set to <see langword="null"/>.
		/// </exception>
		public string Name {
			get { return this.name; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				ThrowIfFrozen();
				this.name = value;
			}
		}

		TextLocation startLocation;
		/// <summary>
		/// Gets the source location of the first character that belongs to this identifier token.
		/// </summary>
		public override TextLocation StartLocation {
			get {
				return startLocation;
			}
		}

		/// <summary>
		/// Updates <see cref="StartLocation"/> for tokens inserted by output visitors.
		/// </summary>
		/// <param name="value">The source position to associate with this token.</param>
		internal void SetStartLocation(TextLocation value)
		{
			ThrowIfFrozen();
			this.startLocation = value;
		}

		const uint verbatimBit = 1u << AstNodeFlagsUsedBits;

		/// <summary>
		/// Gets or sets whether the identifier should be emitted with a leading <c>@</c> escape.
		/// </summary>
		/// <remarks>
		/// This flag affects formatting/output only; <see cref="Name"/> always stores the unescaped identifier text.
		/// </remarks>
		public bool IsVerbatim {
			get {
				return (flags & verbatimBit) != 0;
			}
			set {
				ThrowIfFrozen();
				if (value)
					flags |= verbatimBit;
				else
					flags &= ~verbatimBit;
			}
		}

		/// <summary>
		/// Gets the source location immediately after the identifier token.
		/// </summary>
		public override TextLocation EndLocation {
			get {
				return new TextLocation(StartLocation.Line, StartLocation.Column + (Name ?? "").Length + (IsVerbatim ? 1 : 0));
			}
		}

		Identifier()
		{
			this.name = string.Empty;
		}

		/// <summary>
		/// Initializes a new <see cref="Identifier"/> instance with explicit token text and source location.
		/// </summary>
		/// <param name="name">Identifier text without a leading <c>@</c>.</param>
		/// <param name="location">Start location of the identifier token.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is <see langword="null"/>.
		/// </exception>
		protected Identifier(string name, TextLocation location)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			this.Name = name;
			this.startLocation = location;
		}

		/// <summary>
		/// Creates an identifier at <see cref="TextLocation.Empty"/>.
		/// </summary>
		/// <param name="name">Identifier text, optionally including a leading <c>@</c>.</param>
		/// <returns>
		/// A new identifier node, or <see cref="Null"/> when <paramref name="name"/> is <see langword="null"/> or empty.
		/// </returns>
		public static Identifier Create(string name)
		{
			return Create(name, TextLocation.Empty);
		}

		/// <summary>
		/// Creates an identifier at a specific source location.
		/// </summary>
		/// <param name="name">Identifier text, optionally including a leading <c>@</c>.</param>
		/// <param name="location">Location of the first identifier character in source coordinates.</param>
		/// <returns>
		/// A new identifier node, or <see cref="Null"/> when <paramref name="name"/> is <see langword="null"/> or empty.
		/// </returns>
		/// <remarks>
		/// When <paramref name="name"/> starts with <c>@</c>, the returned node has <see cref="IsVerbatim"/> set to
		/// <see langword="true"/> and <paramref name="location"/> is shifted by one column so
		/// <see cref="StartLocation"/> still points at the first character of <see cref="Name"/>.
		/// </remarks>
		public static Identifier Create(string name, TextLocation location)
		{
			if (string.IsNullOrEmpty(name))
				return Identifier.Null;
			if (name[0] == '@')
				return new Identifier(name.Substring(1), new TextLocation(location.Line, location.Column + 1)) { IsVerbatim = true };
			else
				return new Identifier(name, location);
		}

		/// <summary>
		/// Creates an identifier while explicitly controlling verbatim escaping state.
		/// </summary>
		/// <param name="name">Identifier text without a leading <c>@</c>.</param>
		/// <param name="location">Location of the first identifier character.</param>
		/// <param name="isVerbatim"><see langword="true"/> to emit the identifier as a verbatim identifier.</param>
		/// <returns>
		/// A new identifier node, or <see cref="Null"/> when <paramref name="name"/> is <see langword="null"/> or empty.
		/// </returns>
		public static Identifier Create(string name, TextLocation location, bool isVerbatim)
		{
			if (string.IsNullOrEmpty(name))
				return Identifier.Null;

			if (isVerbatim)
				return new Identifier(name, location) { IsVerbatim = true };
			return new Identifier(name, location);
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitIdentifier(this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitIdentifier(this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIdentifier(this, data);
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			Identifier o = other as Identifier;
			return o != null && !o.IsNull && MatchString(this.Name, o.Name);
		}
	}
}

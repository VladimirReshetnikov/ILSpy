// Copyright (c) 2017 Siegfried Pammer
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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Represents a pinned local/temporary type modifier from IL metadata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Pinning is an implementation detail for local-variable signatures and is not directly expressible in C# type syntax.
	/// ILSpy carries it in the type graph so low-level IL analysis can preserve original metadata shape.
	/// </para>
	/// <para>
	/// Consumers that reason about semantic element types commonly strip this wrapper using modifier-skipping helpers.
	/// </para>
	/// </remarks>
	public sealed class PinnedType : TypeWithElementType
	{
		/// <summary>
		/// Creates a pinned wrapper around <paramref name="elementType"/>.
		/// </summary>
		/// <param name="elementType">The pinned element type.</param>
		public PinnedType(IType elementType)
			: base(elementType)
		{
		}

		public override string NameSuffix => " pinned";

		public override bool? IsReferenceType => elementType.IsReferenceType;
		public override bool IsByRefLike => elementType.IsByRefLike;

		public override TypeKind Kind => TypeKind.Other;

		public override IType VisitChildren(TypeVisitor visitor)
		{
			var newType = elementType.AcceptVisitor(visitor);
			if (newType == elementType)
				return this;
			return new PinnedType(newType);
		}
	}
}

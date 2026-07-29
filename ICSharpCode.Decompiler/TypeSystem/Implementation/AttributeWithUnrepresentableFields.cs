// Copyright (c) 2026 Vladimir Reshetnikov
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

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// An attribute reconstructed from metadata that held more than the attribute itself can express.
	/// The marshalling blob is the case in point: it carries a custom marshaler's GUID and native type
	/// name, and MarshalAsAttribute has no property for either, so the values would otherwise be read
	/// and thrown away. What was left over travels with the attribute so that whoever writes the
	/// declaration can say what the metadata held.
	/// </summary>
	public sealed class AttributeWithUnrepresentableFields : DefaultAttribute
	{
		/// <param name="attributeType">The type of the attribute being reconstructed.</param>
		/// <param name="fixedArguments">The constructor arguments the attribute is written with.</param>
		/// <param name="namedArguments">The property and field assignments the attribute is written with.</param>
		/// <param name="unrepresentable">
		/// A human-readable description of what the metadata held beyond those arguments, ready to be
		/// emitted as a comment next to the attribute.
		/// </param>
		public AttributeWithUnrepresentableFields(IType attributeType,
			ImmutableArray<CustomAttributeTypedArgument<IType>> fixedArguments,
			ImmutableArray<CustomAttributeNamedArgument<IType>> namedArguments,
			string unrepresentable)
			: base(attributeType, fixedArguments, namedArguments)
		{
			this.Unrepresentable = unrepresentable;
		}

		/// <summary>What the metadata held that the attribute cannot say.</summary>
		public string Unrepresentable { get; }
	}
}

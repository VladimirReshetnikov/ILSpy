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

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Represents an <see cref="IField"/> wrapper whose field type is projected through a substitution.
	/// </summary>
	/// <remarks>
	/// Constant values and field modifiers are forwarded from the definition member; only type-facing projections are
	/// specialized.
	/// </remarks>
	public class SpecializedField : SpecializedMember, IField
	{
		/// <summary>
		/// Creates a specialized field wrapper when substitution affects the field context.
		/// </summary>
		/// <param name="fieldDefinition">The unspecialized field definition.</param>
		/// <param name="substitution">Substitution to apply.</param>
		/// <returns>
		/// <paramref name="fieldDefinition"/> when no specialization is required; otherwise a specialized wrapper.
		/// </returns>
		internal static IField Create(IField fieldDefinition, TypeParameterSubstitution substitution)
		{
			if (TypeParameterSubstitution.Identity.Equals(substitution) || fieldDefinition.DeclaringType.TypeParameterCount == 0)
			{
				return fieldDefinition;
			}
			if (substitution.MethodTypeArguments != null && substitution.MethodTypeArguments.Count > 0)
				substitution = new TypeParameterSubstitution(substitution.ClassTypeArguments, EmptyList<IType>.Instance);
			return new SpecializedField(fieldDefinition, substitution);
		}

		readonly IField fieldDefinition;

		/// <summary>
		/// Initializes a specialized field wrapper.
		/// </summary>
		/// <param name="fieldDefinition">The wrapped field definition.</param>
		/// <param name="substitution">Substitution to apply when projecting field type.</param>
		public SpecializedField(IField fieldDefinition, TypeParameterSubstitution substitution)
			: base(fieldDefinition)
		{
			this.fieldDefinition = fieldDefinition;
			AddSubstitution(substitution);
		}

		public bool IsReadOnly => fieldDefinition.IsReadOnly;
		public bool ReturnTypeIsRefReadOnly => fieldDefinition.ReturnTypeIsRefReadOnly;
		public bool IsVolatile => fieldDefinition.IsVolatile;

		IType IVariable.Type {
			get { return this.ReturnType; }
		}

		public bool IsConst {
			get { return fieldDefinition.IsConst; }
		}

		public object GetConstantValue(bool throwOnInvalidMetadata)
		{
			return fieldDefinition.GetConstantValue(throwOnInvalidMetadata);
		}
	}
}

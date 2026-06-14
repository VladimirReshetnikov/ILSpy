// Copyright (c) 2024 ICSharpCode
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

using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Reconstructs C# 14 semi-auto-properties that use the 'field' contextual keyword.
	///
	/// A property whose accessors carry custom logic (so they were not folded to a trivial
	/// <c>{ get; set; }</c> auto-property) but reference only the compiler-generated
	/// <c>&lt;Name&gt;k__BackingField</c> is rewritten to use the <c>field</c> keyword in its
	/// accessors, and the explicit backing field is removed.
	///
	/// The reconstruction is applied only when nothing outside the property's own accessors
	/// touches the backing field. If it is read or written elsewhere (another method, a nested
	/// type, or a constructor assignment that was not turned into a field initializer), the
	/// original source could not have used the <c>field</c> keyword - the field would be
	/// unnameable - so the explicit field is kept.
	///
	/// Runs after <see cref="TransformFieldAndConstructorInitializers"/> so that a property
	/// initializer has already been moved onto the field declaration and is therefore not seen
	/// as an external use inside a constructor.
	/// </summary>
	public class IntroduceFieldKeyword : DepthFirstAstVisitor, IAstTransform
	{
		TransformContext context;

		public void Run(AstNode rootNode, TransformContext context)
		{
			if (!context.Settings.UseFieldKeyword || !context.Settings.AutomaticProperties)
				return;
			this.context = context;
			rootNode.AcceptVisitor(this);
		}

		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			base.VisitPropertyDeclaration(propertyDeclaration);
			TryIntroduceFieldKeyword(propertyDeclaration);
		}

		void TryIntroduceFieldKeyword(PropertyDeclaration propertyDeclaration)
		{
			if (propertyDeclaration.GetSymbol() is not IProperty property)
				return;
			ITypeDefinition declaringType = property.DeclaringTypeDefinition;
			if (declaringType == null)
				return;
			// The 'field' keyword lowers to the C# auto-property backing field <Name>k__BackingField.
			string backingFieldName = "<" + property.Name + ">k__BackingField";
			IField field = declaringType
				.GetFields(f => f.Name == backingFieldName && f.IsStatic == property.IsStatic, GetMemberOptions.IgnoreInheritedMembers)
				.FirstOrDefault();
			if (field == null || !field.IsCompilerGenerated())
				return;
			TypeDeclaration typeDeclaration = propertyDeclaration.Ancestors.OfType<TypeDeclaration>().FirstOrDefault();
			if (typeDeclaration == null)
				return;
			// Collect every reference to the backing field anywhere in the declaring type. All of them
			// must live inside this property's own accessor bodies; a reference anywhere else disqualifies
			// the property.
			var accessorReferences = new List<Expression>();
			foreach (AstNode node in typeDeclaration.Descendants)
			{
				if (node is not (IdentifierExpression or MemberReferenceExpression))
					continue;
				if (node.GetSymbol() is not IField referenced || referenced.MetadataToken != field.MetadataToken)
					continue;
				if (IsInsideOwnAccessor(node, propertyDeclaration))
					accessorReferences.Add((Expression)node);
				else
					return;
			}
			if (accessorReferences.Count == 0)
				return;
			// Introducing the contextual 'field' keyword shadows anything else named 'field' that is in
			// scope in an accessor: a type member, a local/parameter, a foreach/pattern/out/deconstruction
			// variable, or a primary-constructor parameter. Rewriting the backing-field references to a
			// bare 'field' would then silently re-bind those other references to the backing field, so
			// leave the property alone if any other 'field' name could collide.
			if (DeclaringTypeDeclaresFieldMember(declaringType) || AccessorReferencesFieldName(propertyDeclaration))
				return;

			foreach (Expression reference in accessorReferences)
			{
				var fieldKeyword = new IdentifierExpression("field");
				var mrr = reference.Annotation<MemberResolveResult>();
				if (mrr != null)
					fieldKeyword.AddAnnotation(mrr);
				reference.ReplaceWith(fieldKeyword);
			}
			if (!propertyDeclaration.Getter.IsNull)
				CSharpDecompiler.RemoveAttribute(propertyDeclaration.Getter, KnownAttribute.CompilerGenerated);
			if (!propertyDeclaration.Setter.IsNull)
				CSharpDecompiler.RemoveAttribute(propertyDeclaration.Setter, KnownAttribute.CompilerGenerated);

			FieldDeclaration fieldDeclaration = typeDeclaration.Members.OfType<FieldDeclaration>()
				.FirstOrDefault(fd => fd.Variables.Count == 1 && field.MetadataToken == (fd.GetSymbol() as IField)?.MetadataToken);
			if (fieldDeclaration != null)
			{
				VariableInitializer variable = fieldDeclaration.Variables.First();
				if (!variable.Initializer.IsNull)
					propertyDeclaration.Initializer = variable.Initializer.Detach();
				CSharpDecompiler.RemoveAttribute(fieldDeclaration, KnownAttribute.CompilerGenerated);
				CSharpDecompiler.RemoveAttribute(fieldDeclaration, KnownAttribute.DebuggerBrowsable);
				foreach (AttributeSection section in fieldDeclaration.Attributes.ToArray())
				{
					section.AttributeTarget = "field";
					propertyDeclaration.Attributes.Add(section.Detach());
				}
				fieldDeclaration.Remove();
			}
		}

		static bool IsInsideOwnAccessor(AstNode node, PropertyDeclaration property)
		{
			for (AstNode ancestor = node.Parent; ancestor != null && ancestor != property; ancestor = ancestor.Parent)
			{
				if (ancestor is Accessor accessor && accessor.Parent == property)
					return true;
			}
			return false;
		}

		static bool DeclaringTypeDeclaresFieldMember(ITypeDefinition declaringType)
		{
			foreach (IMember member in declaringType.Members)
			{
				if (member.Name == "field")
					return true;
			}
			foreach (ITypeDefinition nestedType in declaringType.NestedTypes)
			{
				if (nestedType.Name == "field")
					return true;
			}
			return false;
		}

		// Returns true if any identifier named 'field' (a declaration in any form, or a reference to a
		// member/parameter) already appears in an accessor body, where the contextual keyword would
		// capture it. This deliberately over-approximates (it also rejects a qualified this.field access,
		// which would be safe) because such collisions are rare and bailing only forgoes a reconstruction.
		static bool AccessorReferencesFieldName(PropertyDeclaration property)
		{
			foreach (Accessor accessor in new[] { property.Getter, property.Setter })
			{
				if (accessor.IsNull)
					continue;
				foreach (Identifier id in accessor.Descendants.OfType<Identifier>())
				{
					if (id.Name == "field")
						return true;
				}
			}
			return false;
		}
	}
}

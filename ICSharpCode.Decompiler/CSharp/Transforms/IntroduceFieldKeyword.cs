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

using System;
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
	/// The reconstruction is applied only when every backing-field reference can be represented
	/// through the property. References in the property's accessors become <c>field</c>, and a
	/// simple assignment in the matching constructor of a getter-only property becomes an
	/// assignment to the property. Other external reads or writes require an explicit field.
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
			if (context.Settings.GetMinimumRequiredVersion() < LanguageVersion.CSharp14_0)
				return;
			this.context = context;
			rootNode.AcceptVisitor(this);
		}

		public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			base.VisitPropertyDeclaration(propertyDeclaration);
			EscapeFieldKeywordReferences(propertyDeclaration);
			if (context.Settings.UseFieldKeyword && context.Settings.AutomaticProperties)
			{
				TryIntroduceFieldKeyword(propertyDeclaration);
			}
		}

		// In C# 14, a bare identifier named 'field' inside a property accessor binds to the
		// property's synthesized backing field. Preserve references that were already present in
		// the AST by escaping them before TryIntroduceFieldKeyword adds intentional bare keywords.
		static void EscapeFieldKeywordReferences(PropertyDeclaration propertyDeclaration)
		{
			foreach (var accessor in new[] { propertyDeclaration.Getter, propertyDeclaration.Setter })
			{
				if (accessor is null)
					continue;
				foreach (Identifier identifier in accessor.Descendants.OfType<Identifier>())
				{
					if (identifier.Name == "field")
					{
						identifier.IsVerbatim = true;
					}
				}
			}
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
			// Collect every reference to the backing field anywhere in the declaring type. Accessor
			// references can become 'field'. C# also permits a getter-only field-backed property to be
			// assigned in a matching constructor, just like a getter-only auto-property.
			var accessorReferences = new List<Expression>();
			var constructorAssignmentReferences = new List<Expression>();
			var constructorPropertyAssignmentReferences = new List<Expression>();
			var externalReferences = new List<Expression>();
			foreach (AstNode node in typeDeclaration.Descendants)
			{
				if (node is not (IdentifierExpression or MemberReferenceExpression))
					continue;
				var expression = (Expression)node;
				// PatternStatementTransform may already render a store to a compiler-generated
				// backing field as a getter-only property assignment. That spelling is valid if we
				// introduce 'field', but must be changed back to the explicit field whenever this
				// transform has to re-materialize it.
				if (node.GetSymbol() is IProperty referencedProperty
					&& referencedProperty.MetadataToken == property.MetadataToken
					&& referencedProperty.ParentModule == property.ParentModule
					&& IsRewritableConstructorAssignment(expression, propertyDeclaration, property))
				{
					constructorPropertyAssignmentReferences.Add(expression);
					continue;
				}
				// Match by metadata token plus declaring module. A token alone is only a per-module row
				// index, so an unrelated field in another assembly can share it and be mistaken for this
				// backing field, rewriting a cross-assembly reference to the wrong member. Comparing the
				// token (rather than IField identity) keeps a reference through a generic instantiation,
				// which is a specialized member distinct from the field definition, matching too.
				if (node.GetSymbol() is not IField referenced
					|| referenced.MetadataToken != field.MetadataToken
					|| referenced.ParentModule != field.ParentModule)
					continue;
				if (IsInsideOwnAccessor(node, propertyDeclaration))
					accessorReferences.Add(expression);
				else if (IsRewritableConstructorAssignment(expression, propertyDeclaration, property))
					constructorAssignmentReferences.Add(expression);
				else
					externalReferences.Add(expression);
			}
			if (externalReferences.Count > 0)
			{
				// The 'field' keyword does not apply. If the explicit backing field is hidden but its
				// references survive, they would dangle; re-materialize it as an ordinary field.
				RematerializeHiddenBackingField(typeDeclaration, field,
					accessorReferences.Concat(constructorAssignmentReferences)
						.Concat(constructorPropertyAssignmentReferences).Concat(externalReferences));
				return;
			}
			if (accessorReferences.Count == 0)
			{
				if (constructorAssignmentReferences.Count > 0)
				{
					RematerializeHiddenBackingField(typeDeclaration, field,
						constructorAssignmentReferences.Concat(constructorPropertyAssignmentReferences));
				}
				return;
			}
			// Roslyn always emits a mutable backing field for a field-backed property, even when
			// the property has only a getter. Replacing an initonly metadata field with the C# 14
			// contextual keyword would therefore silently drop the readonly flag. Keep an explicit
			// field in that case so its metadata and constructor-only assignment semantics survive.
			// An already-reconstructed auto-property has no accessor references and exits above;
			// its { get; } syntax correctly recreates an initonly backing field.
			if (field.IsReadOnly)
			{
				RematerializeHiddenBackingField(typeDeclaration, field,
					accessorReferences.Concat(constructorAssignmentReferences)
						.Concat(constructorPropertyAssignmentReferences));
				return;
			}
			// Introducing the contextual 'field' keyword shadows anything else named 'field' that is in
			// scope in an accessor: a type member, a local/parameter, a foreach/pattern/out/deconstruction
			// variable, or a primary-constructor parameter. Rewriting the backing-field references to a
			// bare 'field' would then silently re-bind those other references to the backing field, so
			// leave the property alone if any other 'field' name could collide.
			if (DeclaringTypeDeclaresFieldMember(declaringType) || AccessorReferencesFieldName(propertyDeclaration))
			{
				RematerializeHiddenBackingField(typeDeclaration, field,
					accessorReferences.Concat(constructorAssignmentReferences)
						.Concat(constructorPropertyAssignmentReferences));
				return;
			}

			foreach (Expression reference in accessorReferences)
			{
				var fieldKeyword = new IdentifierExpression("field");
				var mrr = reference.Annotation<MemberResolveResult>();
				if (mrr != null)
					fieldKeyword.AddAnnotation(mrr);
				reference.ReplaceWith(fieldKeyword);
			}
			foreach (Expression reference in constructorAssignmentReferences)
			{
				var mrr = reference.Annotation<MemberResolveResult>();
				reference.GetChild(Slots.Identifier)!.Name = property.Name;
				reference.RemoveAnnotations<MemberResolveResult>();
				reference.AddAnnotation(new MemberResolveResult(mrr?.TargetResult, property));
			}
			if (propertyDeclaration.Getter is not null)
				CSharpDecompiler.RemoveAttribute(propertyDeclaration.Getter, KnownAttribute.CompilerGenerated);
			if (propertyDeclaration.Setter is not null)
				CSharpDecompiler.RemoveAttribute(propertyDeclaration.Setter, KnownAttribute.CompilerGenerated);

			FieldDeclaration fieldDeclaration = typeDeclaration.Members.OfType<FieldDeclaration>()
				.FirstOrDefault(fd => fd.Variables.Count == 1 && fd.GetSymbol() is IField f
					&& f.MetadataToken == field.MetadataToken && f.ParentModule == field.ParentModule);
			// A hidden backing field does not always have an attached declaration in the AST. This can
			// happen for a specialized field of a generic type, but its custom attributes still belong
			// on the synthesized field that the C# compiler will recreate. Convert a temporary declaration
			// solely as an attribute carrier in that case.
			FieldDeclaration attributeSource = fieldDeclaration
				?? (FieldDeclaration)context.TypeSystemAstBuilder.ConvertEntity(field);
			if (fieldDeclaration != null)
			{
				VariableInitializer variable = fieldDeclaration.Variables.First();
				if (variable.Initializer is not null)
					propertyDeclaration.Initializer = variable.Initializer.Detach();
				fieldDeclaration.Remove();
			}
			CSharpDecompiler.RemoveAttribute(attributeSource, KnownAttribute.CompilerGenerated);
			CSharpDecompiler.RemoveAttribute(attributeSource, KnownAttribute.DebuggerBrowsable);
			foreach (AttributeSection section in attributeSource.Attributes.ToArray())
			{
				section.AttributeTarget = "field";
				propertyDeclaration.Attributes.Add(section.Detach());
			}
		}

		/// <summary>
		/// A compiler-generated property backing field is hidden from the member list on the assumption
		/// that an absorbing transform (the auto-property fold or the 'field' keyword) will remove its
		/// references. When neither applies - the accessor carries custom logic and the field is also
		/// written from outside the accessors - emit a hidden field as an ordinary field with a
		/// de-mangled name. If the field already has an explicit declaration, keep it but retarget any
		/// references that an earlier transform had rendered as property assignments. A de-mangled name
		/// equals the property name, so FixNameCollisions renames the private field afterwards.
		/// </summary>
		void RematerializeHiddenBackingField(TypeDeclaration typeDeclaration, IField field, IEnumerable<Expression> references)
		{
			var fieldDecl = typeDeclaration.Members.OfType<FieldDeclaration>()
				.FirstOrDefault(fd => fd.Variables.Count == 1 && fd.GetSymbol() is IField f
					&& f.MetadataToken == field.MetadataToken && f.ParentModule == field.ParentModule);
			string name;
			if (fieldDecl != null)
			{
				name = fieldDecl.Variables.Single().Name;
				// Existing field references already resolve. Only a reference that was rewritten to
				// the property (notably a constructor assignment) needs to be redirected.
				references = references.Where(reference => reference.GetSymbol() is not IField f
					|| f.MetadataToken != field.MetadataToken || f.ParentModule != field.ParentModule).ToArray();
			}
			else
			{
				// strip the leading '<' and trailing '>k__BackingField' of '<name>k__BackingField'
				const string suffix = ">k__BackingField";
				if (!field.Name.StartsWith("<", StringComparison.Ordinal) || !field.Name.EndsWith(suffix, StringComparison.Ordinal))
					return;
				name = field.Name.Substring(1, field.Name.Length - 1 - suffix.Length);

				fieldDecl = (FieldDeclaration)context.TypeSystemAstBuilder.ConvertEntity(field);
				fieldDecl.Variables.Single().Name = name;
				// the synthesized field is an ordinary field now, so drop attributes that only
				// make sense on the hidden compiler-generated backing field
				CSharpDecompiler.RemoveAttribute(fieldDecl, KnownAttribute.CompilerGenerated);
				CSharpDecompiler.RemoveAttribute(fieldDecl, KnownAttribute.DebuggerBrowsable);

				var lastField = typeDeclaration.Members.OfType<FieldDeclaration>().LastOrDefault();
				var firstMember = typeDeclaration.Members.FirstOrDefault();
				if (lastField != null)
					typeDeclaration.Members.InsertAfter(lastField, fieldDecl);
				else if (firstMember != null)
					typeDeclaration.Members.InsertBefore(firstMember, fieldDecl);
				else
					typeDeclaration.Members.Add(fieldDecl);
			}

			foreach (Expression reference in references)
			{
				var mrr = reference.Annotation<MemberResolveResult>();
				if (reference is IdentifierExpression && !field.IsStatic)
				{
					// an unqualified instance backing-field access may collide with a same-named
					// parameter or local, so qualify it explicitly with 'this'
					var replacement = new MemberReferenceExpression(new ThisReferenceExpression(), name)
						.CopyAnnotationsFrom(reference);
					if (mrr != null)
					{
						replacement.RemoveAnnotations<MemberResolveResult>();
						replacement.AddAnnotation(new MemberResolveResult(new ThisResolveResult(field.DeclaringType), field));
					}
					reference.ReplaceWith(replacement);
				}
				else
				{
					reference.GetChild(Slots.Identifier)!.Name = name;
					if (mrr != null)
					{
						reference.RemoveAnnotations<MemberResolveResult>();
						reference.AddAnnotation(new MemberResolveResult(mrr.TargetResult, field));
					}
				}
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

		static bool IsRewritableConstructorAssignment(Expression reference,
			PropertyDeclaration propertyDeclaration, IProperty property)
		{
			if (propertyDeclaration.Setter is not null
				|| reference.Parent is not AssignmentExpression {
					Operator: AssignmentOperatorType.Assign,
					Parent: ExpressionStatement
				} assignment
				|| assignment.Left != reference)
			{
				return false;
			}

			AstNode function = reference.Ancestors.FirstOrDefault(ancestor => ancestor is
				LambdaExpression or AnonymousMethodExpression or LocalFunctionDeclarationStatement or EntityDeclaration);
			if (function is not ConstructorDeclaration constructor
				|| constructor.GetSymbol() is not IMethod constructorMethod
				|| !constructorMethod.IsConstructor
				|| constructorMethod.IsStatic != property.IsStatic
				|| constructorMethod.DeclaringTypeDefinition != property.DeclaringTypeDefinition)
			{
				return false;
			}

			return reference is IdentifierExpression
				|| (!property.IsStatic && reference is MemberReferenceExpression { Target: ThisReferenceExpression });
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
			foreach (var accessor in new[] { property.Getter, property.Setter })
			{
				if (accessor is null)
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

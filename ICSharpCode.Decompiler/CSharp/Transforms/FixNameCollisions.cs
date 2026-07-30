// Copyright (c) 2016 Daniel Grunwald
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// Rename entities to solve name collisions that make the code uncompilable.
	/// </summary>
	/// <remarks>
	/// Renames private fields that collide with property/event names and type parameters whose
	/// metadata names collide with their containing type or method.
	/// </remarks>
	public class FixNameCollisions : IAstTransform
	{
		public void Run(AstNode rootNode, TransformContext context)
		{
			var renamedSymbols = new Dictionary<ISymbol, string>();
			var externalRenameCache = new Dictionary<ITypeDefinition, IReadOnlyDictionary<ISymbol, string>>();
			var typeDeclarations = rootNode.DescendantsAndSelf.OfType<TypeDeclaration>()
				.Select(declaration => (Declaration: declaration,
					Definition: (declaration.GetResolveResult() as TypeResolveResult)?.Type.GetDefinition()))
				.Where(item => item.Definition != null)
				.ToDictionary(item => item.Definition!, item => item.Declaration);
			foreach (var typeDecl in rootNode.DescendantsAndSelf.OfType<TypeDeclaration>())
			{
				var reservedNestedTypeNames = new HashSet<string>();
				if (typeDecl.GetResolveResult() is TypeResolveResult { Type: { } resolvedType }
					&& resolvedType.GetDefinition() is { } typeDefinition)
				{
					int outerTypeParameterCount = typeDefinition.DeclaringType?.TypeParameterCount ?? 0;
					var symbols = typeDefinition.TypeParameters.Skip(outerTypeParameterCount);
					RenameTypeParameters(typeDecl.TypeParameters, symbols, typeDecl.Constraints,
						new[] { typeDecl.Name }, typeDecl);
					var emittedNestedTypes = typeDecl.Members
						.Select(declaration => declaration.GetSymbol() as ITypeDefinition)
						.Where(definition => definition != null).ToHashSet();
					reservedNestedTypeNames = typeDefinition.NestedTypes
						.Where(nestedType => !emittedNestedTypes.Contains(nestedType))
						.Select(GetOutputTypeName).ToHashSet();
				}

				var typeParameterNames = typeDecl.TypeParameters.Select(p => p.Name).ToHashSet();
				var usedMemberNames = typeDecl.Members.SelectMany(GetMemberNames)
					.Append(typeDecl.Name).Concat(typeParameterNames).Concat(reservedNestedTypeNames).ToHashSet();
				foreach (EntityDeclaration member in typeDecl.Members)
				{
					if (member.GetChild(Slots.PrivateImplementationType) is not null
						|| member.GetSymbol() is IMethod method && (method.IsConstructor || method.IsDestructor))
					{
						continue;
					}
					if (member is FieldDeclaration or EventDeclaration)
					{
						if (member.GetSymbol() is not ISymbol variableSymbol)
							continue;
						var variables = member.GetChildren(Slots.Variable).OfType<VariableInitializer>().ToList();
						if (variables.Count != 1 || variables[0].Name != typeDecl.Name)
							continue;
						string newName = PickNumberedName(usedMemberNames, variables[0].Name);
						context.Step($"Rename member '{variables[0].Name}' to '{newName}'", member);
						variables[0].Name = newName;
						renamedSymbols[GetSymbolDefinition(variableSymbol)] = newName;
						usedMemberNames.Add(newName);
					}
					else if (member is not EnumMemberDeclaration
						&& member.Name == typeDecl.Name && member.GetSymbol() is ISymbol symbol)
					{
						string newName = PickNumberedName(usedMemberNames, member.Name);
						context.Step($"Rename member '{member.Name}' to '{newName}'", member);
						member.Name = newName;
						renamedSymbols[GetSymbolDefinition(symbol)] = newName;
						usedMemberNames.Add(newName);
					}
				}

				var memberNames = typeDecl.Members.Select(m => {
					var type = m.GetChild(Slots.PrivateImplementationType);
					return type is null ? m.Name : type + "." + m.Name;
				}).Concat(reservedNestedTypeNames).ToHashSet();
				// memberNames does not include fields or non-custom events because those
				// don't have a single name, but a list of VariableInitializers.
				foreach (var fieldDecl in typeDecl.Members.OfType<FieldDeclaration>())
				{
					if (fieldDecl.Variables.Count != 1)
						continue;
					string oldName = fieldDecl.Variables.Single().Name;
					ISymbol? symbol = fieldDecl.GetSymbol();
					if (memberNames.Contains(oldName) && symbol is IField { Accessibility: Accessibility.Private })
					{
						string newName = PickNewName(memberNames, oldName);
						context.Step($"Rename field '{oldName}' to '{newName}'", fieldDecl);
						fieldDecl.Variables.Single().Name = newName;
						renamedSymbols[GetSymbolDefinition(symbol)] = newName;
					}
				}

				var acceptedMembers = new List<(ISymbol Symbol, string Name)>();
				usedMemberNames = typeDecl.Members.SelectMany(GetMemberNames)
					.Append(typeDecl.Name).Concat(typeParameterNames).Concat(reservedNestedTypeNames).ToHashSet();
				var conflictCandidates = new List<(EntityDeclaration Member, ISymbol Symbol, string Name, bool NameIsPinned)>();
				foreach (EntityDeclaration member in typeDecl.Members)
				{
					if (!TryGetMember(member, out ISymbol? symbol, out string? name))
						continue;
					conflictCandidates.Add((member, symbol, name, IsInheritanceConstrained(symbol)));
				}
				// Members whose name is pinned by inheritance keep it and claim it first; overridable
				// members are also preferred, so a colliding virtual/non-virtual pair resolves by
				// renaming the non-virtual member. The ordering is stable, so unconstrained members
				// still resolve conflicts among themselves in declaration order.
				foreach (var (member, symbol, name, nameIsPinned) in conflictCandidates
					.OrderByDescending(candidate => candidate.NameIsPinned
						|| candidate.Symbol is IMember { IsOverridable: true }))
				{
					string acceptedName = name;
					bool conflicts = typeParameterNames.Contains(acceptedName) || reservedNestedTypeNames.Contains(acceptedName)
						|| acceptedMembers.Any(previous => previous.Name == acceptedName
						&& !CanShareName(previous.Symbol, symbol));
					if (conflicts && !nameIsPinned)
					{
						string newName = PickNumberedName(usedMemberNames, acceptedName);
						context.Step($"Rename conflicting member '{acceptedName}' to '{newName}'", member);
						RenameMember(member, newName);
						renamedSymbols[GetSymbolDefinition(symbol)] = newName;
						usedMemberNames.Add(newName);
						acceptedName = newName;
					}
					acceptedMembers.Add((symbol, acceptedName));
				}
			}

			foreach (var methodDecl in rootNode.DescendantsAndSelf.OfType<MethodDeclaration>())
			{
				if (methodDecl.GetSymbol() is not IMethod method)
					continue;
				// Local functions have no declared accessibility; their symbols still refer to generated metadata methods.
				if (methodDecl.Parent is TypeDeclaration)
					MakeSignatureTypesAccessible(methodDecl, method);
				var forbiddenNames = methodDecl.Ancestors.OfType<TypeDeclaration>().Select(t => t.Name)
					.Append(methodDecl.Name);
				RenameTypeParameters(methodDecl.TypeParameters, method.TypeParameters, methodDecl.Constraints,
					forbiddenNames, methodDecl);
			}

			foreach (var node in rootNode.DescendantsAndSelf)
			{
				// A delegate invocation carries the delegate type's Invoke as its symbol, but its target
				// names the delegate value rather than that method - nothing in source ever names it.
				// Rewriting the target would put a member where the delegate was and lose the call.
				if (node is InvocationExpression invocation
					&& invocation.GetSymbol() is { } invokedSymbol
					&& invokedSymbol is not IMethod { DeclaringType.Kind: TypeKind.Delegate }
					&& TryGetRenamedName(invokedSymbol, out string? invokedName)
					&& invocation.Target.GetChild(Slots.Identifier) is Identifier invokedIdentifier)
				{
					context.Step($"Rename invocation target to '{invokedName}'", invocation.Target);
					if (invocation.Target is IdentifierExpression && invokedSymbol is IMember invokedMember)
					{
						Expression receiver = invokedMember.IsStatic
							? new TypeReferenceExpression(context.TypeSystemAstBuilder.ConvertType(invokedMember.DeclaringType))
							: new ThisReferenceExpression();
						var typeArguments = invocation.Target.GetChildren(Slots.TypeArgument)
							.OfType<AstType>().Select(type => type.Detach()).ToArray();
						invocation.Target.ReplaceWith(new MemberReferenceExpression(receiver, invokedName, typeArguments)
							.CopyAnnotationsFrom(invocation.Target));
					}
					else
					{
						invokedIdentifier.Name = invokedName;
					}
				}
				if (node is NamedExpression namedExpression
					&& namedExpression.GetSymbol() is { } namedSymbol
					&& TryGetRenamedName(namedSymbol, out string? namedExpressionName))
				{
					context.Step($"Rename object initializer member to '{namedExpressionName}'", namedExpression);
					namedExpression.Name = namedExpressionName;
				}
				if (node is IdentifierExpression || node is MemberReferenceExpression)
				{
					ISymbol? symbol = node.GetSymbol();
					if (symbol != null && TryGetRenamedName(symbol, out string? newName))
					{
						// An IdentifierExpression / MemberReferenceExpression always carries its name identifier.
						context.Step($"Rename field reference to '{newName}'", node);
						node.GetChild(Slots.Identifier)!.Name = newName;
					}
				}
			}

			bool TryGetRenamedName(ISymbol symbol, [NotNullWhen(true)] out string? newName)
			{
				symbol = GetSymbolDefinition(symbol);
				if (renamedSymbols.TryGetValue(symbol, out newName))
					return true;
				// Members and nested types declared in another file of the same module may have been
				// renamed by that file's declaration pass; recompute those renames from the type system.
				ITypeDefinition? declaringType = symbol switch {
					IMember member => member.DeclaringTypeDefinition,
					ITypeDefinition typeDefinition => typeDefinition.DeclaringTypeDefinition,
					_ => null,
				};
				if (declaringType is null || declaringType.ParentModule != context.TypeSystem.MainModule)
					return false;
				var externalRenames = GetExternalRenames(declaringType);
				return externalRenames.TryGetValue(symbol, out newName);
			}

			IReadOnlyDictionary<ISymbol, string> GetExternalRenames(ITypeDefinition typeDefinition)
			{
				if (!externalRenameCache.TryGetValue(typeDefinition, out var renames))
				{
					renames = ComputeMemberRenames(typeDefinition);
					externalRenameCache.Add(typeDefinition, renames);
				}
				return renames;
			}

			string GetOutputTypeName(ITypeDefinition typeDefinition)
			{
				if (typeDefinition.DeclaringTypeDefinition is { } declaringType
					&& GetExternalRenames(declaringType).TryGetValue(typeDefinition, out string? renamedType))
				{
					return renamedType;
				}
				if (typeDefinition.DeclaringTypeDefinition is { } parentType
					&& typeDefinition.Name == GetOutputTypeName(parentType))
				{
					// A hidden compiler-generated nested type may be pulled back into the output by
					// another reconstructed member. It still needs the enclosing-type collision rename,
					// even though MemberIsHidden excluded it from the parent's normal declaration pass.
					return typeDefinition.Name + "2";
				}
				return typeDefinition.Name;
			}

			IReadOnlyDictionary<ISymbol, string> ComputeMemberRenames(ITypeDefinition typeDefinition)
			{
				string outputTypeName = GetOutputTypeName(typeDefinition);
				var symbols = typeDefinition.NestedTypes.Cast<ISymbol>()
					.Concat(typeDefinition.Fields.Concat<ISymbol>(typeDefinition.Properties)
						.Concat(typeDefinition.Events).Concat(typeDefinition.Methods)
						.Where(IsEmittedRenameCandidate))
					.Where(IsRenameCandidate).ToList();
				var names = symbols.ToDictionary(GetSymbolDefinition, symbol => symbol.Name);

				int outerTypeParameterCount = typeDefinition.DeclaringType?.TypeParameterCount ?? 0;
				var ownTypeParameters = typeDefinition.TypeParameters.Skip(outerTypeParameterCount).ToList();
				var usedTypeParameterNames = ownTypeParameters.Select(p => p.Name).Append(outputTypeName).ToHashSet();
				var finalTypeParameterNames = ownTypeParameters.Select(p => p.Name).ToList();
				for (int i = 0; i < finalTypeParameterNames.Count; i++)
				{
					if (finalTypeParameterNames[i] == outputTypeName)
					{
						finalTypeParameterNames[i] = PickNumberedName(usedTypeParameterNames, finalTypeParameterNames[i]);
						usedTypeParameterNames.Add(finalTypeParameterNames[i]);
					}
				}
				var typeParameterNames = finalTypeParameterNames.ToHashSet();

				var usedNames = names.Values.Append(outputTypeName).Concat(typeParameterNames).ToHashSet();
				foreach (ISymbol symbol in symbols.Where(symbol =>
					names[GetSymbolDefinition(symbol)] == outputTypeName
					&& !(typeDefinition.Kind == TypeKind.Enum && symbol is IField)))
				{
					ISymbol definition = GetSymbolDefinition(symbol);
					string newName = PickNumberedName(usedNames, names[definition]);
					names[definition] = newName;
					usedNames.Add(newName);
				}

				var nonVariableNames = symbols.Where(symbol => symbol is not IField and not IEvent)
					.Select(symbol => names[GetSymbolDefinition(symbol)]).ToHashSet();
				foreach (IField field in symbols.OfType<IField>().Where(field => field.Accessibility == Accessibility.Private))
				{
					ISymbol definition = GetSymbolDefinition(field);
					if (nonVariableNames.Contains(names[definition]))
						names[definition] = PickNewName(nonVariableNames, names[definition]);
				}

				var acceptedMembers = new List<(ISymbol Symbol, string Name)>();
				usedNames = names.Values.Append(outputTypeName).Concat(typeParameterNames).ToHashSet();
				// Mirrors the conflict pass over the emitted declarations: names pinned by inheritance
				// are claimed first and never renamed, so external references and the declaring file
				// agree on which colliding member had to move.
				foreach (var (symbol, nameIsPinned) in symbols
					.Select(symbol => (Symbol: symbol, NameIsPinned: IsInheritanceConstrained(symbol)))
					.OrderByDescending(candidate => candidate.NameIsPinned
						|| candidate.Symbol is IMember { IsOverridable: true }))
				{
					ISymbol definition = GetSymbolDefinition(symbol);
					string name = names[definition];
					bool conflicts = typeParameterNames.Contains(name)
						|| acceptedMembers.Any(previous => previous.Name == name && !CanShareName(previous.Symbol, symbol));
					if (conflicts && !nameIsPinned)
					{
						name = PickNumberedName(usedNames, name);
						names[definition] = name;
						usedNames.Add(name);
					}
					acceptedMembers.Add((symbol, name));
				}

				return names.Where(pair => pair.Key.Name != pair.Value)
					.ToDictionary(pair => pair.Key, pair => pair.Value);
			}

			bool IsEmittedRenameCandidate(ISymbol symbol)
			{
				return symbol is IEntity entity && !entity.MetadataToken.IsNil
					&& !CSharpDecompiler.MemberIsHidden(
						(entity.ParentModule as MetadataModule)?.MetadataFile, entity.MetadataToken, context.Settings)
					&& IsRenameCandidate(symbol);
			}

			static bool IsRenameCandidate(ISymbol symbol)
			{
				if (symbol is IMember { IsExplicitInterfaceImplementation: true })
					return false;
				if (symbol is IMethod { IsConstructor: true } or IMethod { IsDestructor: true } or IMethod { IsOperator: true })
					return false;
				// A property C# cannot declare is written as its accessor methods, so it has no name
				// of its own in the output to rename - and nothing collides with it either.
				if (symbol is IProperty parameterized && parameterized.IsParameterizedProperty())
					return false;
				return symbol is not IProperty { IsIndexer: true };
			}

			// Returns true if the member's name is pinned by an inheritance relationship: renaming an
			// override severs it from the virtual member it overrides (CS0115), and renaming a member
			// that implicitly implements an interface member leaves the interface unimplemented
			// (CS0535). A same-signature member of an implemented interface counts as an
			// implementation without verifying which sibling actually occupies the slot: skipping a
			// rename merely leaves a collision behind, while renaming a genuine implementation breaks
			// the type.
			static bool IsInheritanceConstrained(ISymbol symbol)
			{
				if (symbol is not IMember member)
					return false;
				if (member.IsOverride)
					return true;
				return InheritanceHelper.GetBaseMembers(member, includeImplementedInterfaces: true)
					.Any(baseMember => baseMember.DeclaringType.Kind == TypeKind.Interface);
			}

			foreach (var type in rootNode.DescendantsAndSelf.OfType<AstType>())
			{
				ISymbol? symbol = type.GetResolveResult() switch {
					TypeResolveResult { Type: ITypeParameter typeParameter } => typeParameter,
					TypeResolveResult typeResolveResult => typeResolveResult.Type.GetDefinition(),
					_ => null,
				};
				if (symbol != null && TryGetRenamedName(symbol, out string? newName)
					&& type.GetChild(Slots.Identifier) is Identifier identifier)
				{
					context.Step($"Rename type reference to '{newName}'", type);
					identifier.Name = newName;
				}
			}

			void RenameTypeParameters(
				IEnumerable<TypeParameterDeclaration> declarations,
				IEnumerable<ITypeParameter> symbols,
				IEnumerable<Constraint> constraints,
				IEnumerable<string> forbiddenNames,
				AstNode owner)
			{
				var usedNames = declarations.Select(d => d.Name).Concat(forbiddenNames).ToHashSet();
				var forbidden = forbiddenNames.ToHashSet();
				foreach (var (declaration, symbol) in declarations.Zip(symbols))
				{
					if (!forbidden.Contains(declaration.Name))
						continue;
					string oldName = declaration.Name;
					string newName = PickNumberedName(usedNames, oldName);
					context.Step($"Rename type parameter '{oldName}' to '{newName}'", owner);
					declaration.Name = newName;
					renamedSymbols[GetSymbolDefinition(symbol)] = newName;
					usedNames.Add(newName);
					foreach (Constraint constraint in constraints.Where(c => c.TypeParameter.Identifier == oldName))
						constraint.TypeParameter.Identifier = newName;
				}
			}

			static IEnumerable<string> GetMemberNames(EntityDeclaration member)
			{
				if (member is FieldDeclaration or EventDeclaration)
					return member.GetChildren(Slots.Variable).OfType<VariableInitializer>().Select(v => v.Name);
				return new[] { member.Name };
			}

			static ISymbol GetSymbolDefinition(ISymbol symbol)
			{
				return symbol is IMember member ? member.MemberDefinition : symbol;
			}

			static bool TryGetMember(EntityDeclaration member, [NotNullWhen(true)] out ISymbol? symbol, [NotNullWhen(true)] out string? name)
			{
				symbol = member.GetSymbol();
				name = null;
				if (symbol == null || member.GetChild(Slots.PrivateImplementationType) is not null)
					return false;
				if (symbol is IMethod { IsConstructor: true } or IMethod { IsDestructor: true } or IMethod { IsOperator: true })
					return false;
				if (member is IndexerDeclaration)
					return false;
				if (member is FieldDeclaration or EventDeclaration)
				{
					var variables = member.GetChildren(Slots.Variable).OfType<VariableInitializer>().ToList();
					if (variables.Count != 1)
						return false;
					name = variables[0].Name;
					return true;
				}
				name = member.Name;
				return !string.IsNullOrEmpty(name);
			}

			static void RenameMember(EntityDeclaration member, string newName)
			{
				if (member is FieldDeclaration or EventDeclaration)
					member.GetChildren(Slots.Variable).OfType<VariableInitializer>().Single().Name = newName;
				else
					member.Name = newName;
			}

			static bool CanShareName(ISymbol first, ISymbol second)
			{
				if (first is IMethod firstMethod && second is IMethod secondMethod)
					return !SignatureComparer.Ordinal.Equals(firstMethod, secondMethod);
				if (first is ITypeDefinition firstType && second is ITypeDefinition secondType)
					return firstType.TypeParameterCount != secondType.TypeParameterCount;
				return false;
			}

			void MakeSignatureTypesAccessible(MethodDeclaration methodDecl, IMethod method)
			{
				if (method.IsOverride || method.IsExplicitInterfaceImplementation
					|| method.DeclaringTypeDefinition?.Kind == TypeKind.Interface)
				{
					return;
				}

				// What matters is the accessibility the declaration is written with, which is not
				// always the one the metadata records: a compiler-generated helper can be assembly-
				// visible in metadata and still be emitted as private. Widening a signature type to
				// match the metadata would then loosen a declaration nothing has trouble reaching.
				Accessibility requiredAccessibility = AccessibilityFromModifiers(methodDecl.Modifiers)
					.Intersect(method.EffectiveAccessibility());
				MakeTypeAccessible(method.ReturnType, requiredAccessibility);
				foreach (IParameter parameter in method.Parameters)
					MakeTypeAccessible(parameter.Type, requiredAccessibility);
			}

			void MakeTypeAccessible(IType type, Accessibility requiredAccessibility)
			{
				if (type.GetDefinition() is { } definition
					&& !requiredAccessibility.LessThanOrEqual(definition.EffectiveAccessibility())
					&& typeDeclarations.TryGetValue(definition, out TypeDeclaration? declaration))
				{
					Accessibility newAccessibility = definition.Accessibility.Union(requiredAccessibility);
					if (definition.DeclaringTypeDefinition == null && newAccessibility != Accessibility.Public)
					{
						// A type at namespace level may only be public or internal (CS1527). Internal covers
						// a requirement that stays inside the assembly; one that reaches derived types, which
						// may live outside it, is only met by going public.
						newAccessibility = newAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal
							? Accessibility.Internal
							: Accessibility.Public;
					}
					context.Step($"Raise signature type accessibility to '{newAccessibility}'", declaration);
					declaration.Modifiers = declaration.Modifiers & ~Modifiers.VisibilityMask
						| TypeSystemAstBuilder.ModifierFromAccessibility(newAccessibility,
							context.Settings.IntroducePrivateProtectedAccessibility);
				}
				if (type is TypeWithElementType typeWithElementType)
				{
					MakeTypeAccessible(typeWithElementType.ElementType, requiredAccessibility);
					return;
				}
				if (type is TupleType tupleType)
				{
					foreach (IType elementType in tupleType.ElementTypes)
						MakeTypeAccessible(elementType, requiredAccessibility);
				}
				else if (type is FunctionPointerType functionPointerType)
				{
					MakeTypeAccessible(functionPointerType.ReturnType, requiredAccessibility);
					foreach (IType parameterType in functionPointerType.ParameterTypes)
						MakeTypeAccessible(parameterType, requiredAccessibility);
				}
				else
				{
					foreach (IType typeArgument in type.TypeArguments)
						MakeTypeAccessible(typeArgument, requiredAccessibility);
				}
			}
		}

		/// <summary>
		/// Reads back the accessibility a declaration is written with. This is the inverse of
		/// <see cref="TypeSystemAstBuilder.ModifierFromAccessibility"/>; a member carrying no
		/// visibility modifier at all is private, which is what C# defaults it to.
		/// </summary>
		static Accessibility AccessibilityFromModifiers(Modifiers modifiers)
		{
			switch (modifiers & Modifiers.VisibilityMask)
			{
				case Modifiers.Public:
					return Accessibility.Public;
				case Modifiers.Internal:
					return Accessibility.Internal;
				case Modifiers.Protected:
					return Accessibility.Protected;
				case Modifiers.Protected | Modifiers.Internal:
					return Accessibility.ProtectedOrInternal;
				case Modifiers.Private | Modifiers.Protected:
					return Accessibility.ProtectedAndInternal;
				default:
					return Accessibility.Private;
			}
		}

		/// <summary>
		/// Appends the lowest number that makes <paramref name="name"/> unused. Members and type
		/// parameters are both renamed this way: neither has a conventional prefix to fall back on,
		/// so the numbered suffix stays closest to the metadata name.
		/// </summary>
		static string PickNumberedName(ISet<string> usedNames, string name)
		{
			for (int num = 2; ; num++)
			{
				string newName = name + num;
				if (!usedNames.Contains(newName))
					return newName;
			}
		}

		/// <summary>
		/// Picks a free name for a private field, preferring the <c>m_</c> prefix that conventionally
		/// distinguishes a backing field from the property or event it collides with.
		/// </summary>
		static string PickNewName(ISet<string> memberNames, string name)
		{
			if (!memberNames.Contains("m_" + name))
				return "m_" + name;
			return PickNumberedName(memberNames, name);
		}
	}
}

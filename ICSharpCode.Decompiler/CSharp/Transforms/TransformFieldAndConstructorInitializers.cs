// Copyright (c) 2025 Siegfried Pammer
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.CSharp.Transforms
{
	/// <summary>
	/// This transform moves field initializers at the start of constructors to their respective field declarations
	/// and transforms this-/base-ctor calls in constructors to constructor initializers.
	/// </summary>
	public class TransformFieldAndConstructorInitializers : IAstTransform
	{
		/// <summary>
		/// Pattern for reference types:
		/// this..ctor(...);
		/// </summary>
		internal static readonly AstNode ThisCallClassPattern = new ExpressionStatement(
			new NamedNode("invocation", new InvocationExpression(
				new MemberReferenceExpression(
					new Choice {
						new NamedNode("target", new ThisReferenceExpression()),
						new NamedNode("target", new BaseReferenceExpression()),
						new CastExpression {
							Type = new AnyNode(),
							Expression = new Choice {
							new NamedNode("target", new ThisReferenceExpression()),
								new NamedNode("target", new BaseReferenceExpression()),
							}
						}
					}, ".ctor"),
				new Repeat(new AnyNode()))
			)
		);

		/// <summary>
		/// Pattern for value types:
		/// this = new TSelf(...);
		/// </summary>
		internal static readonly AstNode ThisCallStructPattern = new ExpressionStatement(
				new AssignmentExpression(
					new NamedNode("target", new ThisReferenceExpression()),
					new NamedNode("invocation", new ObjectCreateExpression(new AnyNode(), new Repeat(new AnyNode())))
				)
		);

		internal static bool IsGeneratedPrimaryConstructorBackingField(IField field)
		{
			var name = field.Name;
			return name.StartsWith("<", StringComparison.Ordinal)
				&& name.EndsWith(">P", StringComparison.Ordinal)
				&& field.IsCompilerGenerated();
		}

		enum InitializerKind
		{
			Static,
			Instance,
			Primary
		}

		class InitializerSequence
		{
			static readonly ExpressionStatement memberInitializerPattern = new() {
				Expression = new AssignmentExpression(
					new Choice {
							new NamedNode("fieldAccess", new MemberReferenceExpression {
								Target = new ThisReferenceExpression(),
								MemberName = Pattern.AnyString
							}),
							new NamedNode("fieldAccess", new IdentifierExpression(Pattern.AnyString))
					},
					new AnyNode("initializer")
				)
			};

			[AllowNull]
			public List<(Statement Statement, IMember Member, Expression Initializer, bool DependsOnConstructorBody, bool ReferencesInstanceMember)> Statements;

			public Dictionary<Statement, List<(Statement Statement, Expression Initializer)>>? StatementToOtherCtorsMap;

			public bool HasDuplicateAssignments { get; private set; }
			public bool IsUnsafe { get; private set; }
			public bool CoversFullBody { get; private set; }

			public static InitializerSequence? Analyze(ConstructorInitializerAnalyzer context, ConstructorDeclaration ctor, IMethod ctorMethod)
			{
				var sequence = new InitializerSequence() {
					Statements = [],
					IsUnsafe = ctor.HasModifier(Modifiers.Unsafe)
				};
				var initializedMembers = new HashSet<IMember>();
				var function = ctor.Annotation<ILFunction>();
				var isStruct = ctorMethod.DeclaringType.Kind == TypeKind.Struct;

				bool onlyMoveConstants = !context.context.Settings.AlwaysMoveInitializer
					&& !context.IsBeforeFieldInit && ctorMethod.IsStatic;
				bool skippedStmts = false;

				Statement? stmt;
				for (stmt = ctor.Body?.Statements.FirstOrDefault(); stmt != null; stmt = stmt.GetNextStatement())
				{
					var m = memberInitializerPattern.Match(stmt);
					if (!m.Success)
						break;
					var fieldAccess = m.Get<AstNode>("fieldAccess").Single();
					var initializer = m.Get<Expression>("initializer").Single();
					var member = (fieldAccess.GetSymbol() as IMember)?.MemberDefinition;
					if (member == null || !CanHaveInitializer(member, context))
						break;
					if (onlyMoveConstants && member is not IField { IsConst: true })
					{
						skippedStmts = true;
						continue;
					}
					if (!initializedMembers.Add(member))
						sequence.HasDuplicateAssignments = true;

					bool dependsOnBody = false;
					bool referencesInstanceMember = false;

					// 'this' is modeled as a parameter with a negative index. An initializer that reads
					// 'this' (i.e. another instance field/property/method of the same type) cannot be
					// turned into a field initializer (CS0236), even for a primary constructor, where
					// references to the primary constructor's own parameters (index >= 0) are otherwise
					// permitted.
					void InspectVariable(ILVariable v, bool isPrimaryConstructorBackingFieldTarget = false)
					{
						if (v.Function == function && v.Kind == VariableKind.Parameter)
						{
							dependsOnBody = true;
							if (v.Index < 0 && !isPrimaryConstructorBackingFieldTarget)
								referencesInstanceMember = true;
						}
					}

					static bool IsPrimaryConstructorBackingFieldTarget(ILInstruction variableInstruction, ILInstruction root)
					{
						for (ILInstruction? current = variableInstruction.Parent; current != null; current = current.Parent)
						{
							IField? field;
							ILInstruction? target;
							if ((current.MatchLdFld(out target, out field) || current.MatchLdFlda(out target, out field))
								&& IsGeneratedPrimaryConstructorBackingField(field)
								&& (target == variableInstruction || target.Descendants.Contains(variableInstruction)))
							{
								return true;
							}
							if (current == root)
								break;
						}
						return false;
					}

					foreach (var instruction in initializer.Annotations.OfType<ILInstruction>())
					{
						foreach (var inst in instruction.Descendants)
						{
							if (inst is IInstructionWithVariableOperand { Variable: var v })
							{
								InspectVariable(v, IsPrimaryConstructorBackingFieldTarget(inst, instruction));
							}
						}
					}
					// The initializer expression may have been rebuilt without IL-instruction annotations
					// (for example a constructor-parameter reference reduced to an identifier that carries
					// only a resolve result), so also scan the syntax tree for parameter references.
					foreach (var astNode in initializer.DescendantsAndSelf)
					{
						if (astNode.GetResolveResult() is ILVariableResolveResult { Variable: var av })
							InspectVariable(av);
					}

					sequence.Statements.Add((stmt, member, initializer, dependsOnBody, referencesInstanceMember));
				}

				if (!skippedStmts)
				{
					if (stmt == null)
					{
						sequence.CoversFullBody = true;
					}
					else
					{
						var m = isStruct
							? ThisCallStructPattern.Match(stmt)
							: ThisCallClassPattern.Match(stmt);
						if (m.Success)
						{
							sequence.CoversFullBody = stmt.GetNextStatement() == null;
						}
					}
				}

				return sequence;
			}

			private static bool CanHaveInitializer(IMember member, ConstructorInitializerAnalyzer context)
			{
				if (context.MemberToDeclaringSyntaxNodeMap == null)
					return false;
				if (!context.MemberToDeclaringSyntaxNodeMap.TryGetValue(member, out var declaringSyntaxNode))
					return true;
				return declaringSyntaxNode is FieldDeclaration
					or PropertyDeclaration { IsAutomaticProperty: true }
					or EventDeclaration;
			}

			public bool IsMatch(ConstructorDeclaration ctor)
			{
				if (ctor.Body is null)
					return false;
				var stmts = ctor.Body.Statements;
				var otherStmt = stmts.FirstOrDefault();
				foreach (var (stmt, member, initializer, _, _) in Statements)
				{
					var m = memberInitializerPattern.Match(otherStmt);
					if (!m.Success)
						return false;
					Debug.Assert(otherStmt != null); // because m.Success
					var fieldAccess = m.Get<AstNode>("fieldAccess").Single();
					var otherMember = (fieldAccess.GetSymbol() as IMember)?.MemberDefinition;
					if (!member.Equals(otherMember))
						return false;
					var otherInitializer = m.Get<Expression>("initializer").Single();
					if (!initializer.IsMatch(otherInitializer))
						return false;
					StatementToOtherCtorsMap ??= [];
					if (!StatementToOtherCtorsMap.TryGetValue(stmt, out var list))
					{
						list = [];
						StatementToOtherCtorsMap[stmt] = list;
					}
					list.Add((otherStmt, otherInitializer));
					otherStmt = otherStmt.GetNextStatement();
				}
				return true;
			}
		}

		class ConstructorInitializerAnalyzer
		{
			internal readonly TransformContext context;
			public readonly ITypeDefinition TypeDefinition;
			public readonly TypeDeclaration? TypeDeclaration;
			public readonly RecordDecompiler? RecordDecompiler;

			[AllowNull]
			public Dictionary<IMember, EntityDeclaration> MemberToDeclaringSyntaxNodeMap;

			public Dictionary<IParameter, (IProperty?, IField)>? PrimaryConstructorParameterToBackingStoreMap;
			public Dictionary<IField, ILVariable>? BackingFieldToPrimaryConstructorParameterVariableMap;

			public InitializerSequence? InstanceInitializers;
			public InitializerSequence? StaticInitializers;
			public InitializerSequence? PrimaryConstructorInitializers;

			public IMethod? StaticConstructor;
			public ConstructorDeclaration? StaticConstructorDecl;

			public IMethod? RecordCopyConstructor;
			public ConstructorDeclaration? RecordCopyConstructorDecl;

			public IMethod? PrimaryConstructor;
			public ConstructorDeclaration? PrimaryConstructorDecl;

			[AllowNull]
			public ConstructorDeclaration[] InstanceConstructors;

			public bool IsBeforeFieldInit {
				get {
					if (TypeDefinition?.MetadataToken.IsNil != false)
						return false;

					var metadata = context.TypeSystem.MainModule.MetadataFile.Metadata;
					var td = metadata.GetTypeDefinition((SRM.TypeDefinitionHandle)TypeDefinition.MetadataToken);

					return td.HasFlag(TypeAttributes.BeforeFieldInit);
				}
			}

			public ConstructorInitializerAnalyzer(TransformContext context, ITypeDefinition typeDefinition, TypeDeclaration? typeDeclaration)
			{
				this.context = context;
				TypeDefinition = typeDefinition;
				TypeDeclaration = typeDeclaration;
				RecordDecompiler = context.DecompileRun.RecordDecompilers.TryGetValue(typeDefinition, out var record) ? record : null;
			}

			public bool Analyze(IEnumerable<AstNode> members)
			{
				MemberToDeclaringSyntaxNodeMap = members
					.Select(m => (symbol: m.GetSymbol(), entity: (EntityDeclaration)m))
					.Where(_ => _.symbol is IMember)
					.ToDictionary(_ => ((IMember)_.symbol!).MemberDefinition ?? (IMember)_.symbol!, _ => _.entity);

				List<ConstructorDeclaration> constructorsNotChainedWithThis = [];
				List<ConstructorDeclaration> allCtors = [];

				// we use the row number of the first method in the metadata table as a heuristic
				// to determine the primary constructor in non-record structs.
				// This is not completely reliable, but works.
				var metadata = context.TypeSystem.MainModule.metadata;
				var typeDef = metadata.GetTypeDefinition((SRM.TypeDefinitionHandle)TypeDefinition.MetadataToken);
				var firstMethodRowNumber = MetadataTokens.GetRowNumber(typeDef.GetMethods().FirstOrDefault());

				foreach (var ctor in members.OfType<ConstructorDeclaration>())
				{
					var ctorMethod = (IMethod)ctor.GetSymbol()!;
					Debug.Assert(ctorMethod.IsConstructor);
					Debug.Assert(ctorMethod.MetadataToken.IsNil == false);

					int rowNumber = MetadataTokens.GetRowNumber(ctorMethod.MetadataToken);
					Debug.Assert(rowNumber > 0);

					if (ctorMethod.Equals(RecordDecompiler?.PrimaryConstructor))
					{
						Debug.Assert(PrimaryConstructorDecl == null);
						PrimaryConstructor = ctorMethod;
						PrimaryConstructorDecl = ctor;
						PrimaryConstructorParameterToBackingStoreMap = RecordDecompiler.GetParameterToBackingStoreMap();
						constructorsNotChainedWithThis.Add(ctor);
					}
					else if (ctorMethod.IsStatic)
					{
						Debug.Assert(StaticConstructorDecl == null);
						StaticConstructor = ctorMethod;
						StaticConstructorDecl = ctor;
					}
					else if (RecordDecompiler != null && RecordDecompiler.IsCopyConstructor(ctorMethod))
					{
						Debug.Assert(RecordCopyConstructorDecl == null);
						RecordCopyConstructor = ctorMethod;
						RecordCopyConstructorDecl = ctor;
					}
					else
					{
						// find this-ctor call; it is usually the first statement, but may be
						// preceded by field initializers, guard clauses, or inlinable temporaries.
						bool isStruct = ctorMethod.DeclaringType.Kind == TypeKind.Struct;
						var stmt = FindConstructorInitializerCall(ctor.Body, isStruct, out var m);

						allCtors.Add(ctor);

						if (stmt != null && m.Get<Expression>("target").Single() is ThisReferenceExpression)
							continue;

						constructorsNotChainedWithThis.Add(ctor);
					}
				}

				Accessibility expectedCtorAccessibility = TypeDefinition.IsAbstract ? Accessibility.Protected : Accessibility.Public;

				if (context.Settings.UsePrimaryConstructorSyntaxForNonRecordTypes
					&& RecordDecompiler == null && constructorsNotChainedWithThis.Count == 1)
				{
					Debug.Assert(PrimaryConstructor == null);

					// this constructor could be converted to a primary constructor
					var ctor = constructorsNotChainedWithThis[0];
					var ctorMethod = (IMethod)constructorsNotChainedWithThis[0].GetSymbol()!;

					var initializer = InitializerSequence.Analyze(this, ctor, ctorMethod);

					if (initializer is { CoversFullBody: true, HasDuplicateAssignments: false, Statements.Count: > 0 }
						&& ctorMethod.Accessibility == expectedCtorAccessibility)
					{
						bool transformToPrimaryConstructor = MetadataTokens.GetRowNumber(ctorMethod.MetadataToken) == firstMethodRowNumber;

						if (ctorMethod.Parameters.Count == 0)
						{
							transformToPrimaryConstructor = false;
						}

						// An initializer that reads another instance member (e.g. 'field2 = field1.Length;')
						// cannot be expressed as a field initializer, so the constructor body must be kept.
						// Converting such a constructor to a primary constructor would emit field initializers
						// that reference 'this', producing non-compilable C# (CS0236).
						if (initializer.Statements.Any(s => s.ReferencesInstanceMember))
						{
							transformToPrimaryConstructor = false;
						}

						foreach (var (stmt, member, expr, dependsOnBody, referencesInstanceMember) in initializer.Statements)
						{
							if (member is IField f && IsGeneratedPrimaryConstructorBackingField(f))
							{
								var variable = expr.Annotation<ILVariableResolveResult>()?.Variable;
								if (variable is { Kind: VariableKind.Parameter, Index: >= 0 and int index })
								{
									var p = ctorMethod.Parameters[index];
									PrimaryConstructorParameterToBackingStoreMap ??= [];
									BackingFieldToPrimaryConstructorParameterVariableMap ??= [];
									PrimaryConstructorParameterToBackingStoreMap[p] = (null, f);
									BackingFieldToPrimaryConstructorParameterVariableMap[f] = variable;
									transformToPrimaryConstructor = true;
								}
							}
						}

						if (context.Settings.ShowXmlDocumentation && context.DecompileRun.DocumentationProvider is { } provider)
						{
							var classDoc = provider.GetDocumentation(ctorMethod.DeclaringTypeDefinition);
							var ctorDoc = provider.GetDocumentation(ctorMethod);

							if (ctorDoc != null && ctorDoc != classDoc)
							{
								transformToPrimaryConstructor = false;
							}
						}

						if (transformToPrimaryConstructor)
						{
							PrimaryConstructorParameterToBackingStoreMap ??= [];
							PrimaryConstructorDecl = ctor;
							PrimaryConstructor = ctorMethod;
							PrimaryConstructorInitializers = initializer;
						}
					}
				}

				if (StaticConstructor != null)
				{
					StaticInitializers = InitializerSequence.Analyze(this, StaticConstructorDecl!, StaticConstructor);
				}

				if (PrimaryConstructor != null)
				{
					Debug.Assert(PrimaryConstructorDecl != null);

					// if there exists a primary constructor, all other constructors must call it
					Debug.Assert(constructorsNotChainedWithThis.Count == 1);

					PrimaryConstructorInitializers ??= InitializerSequence.Analyze(this, PrimaryConstructorDecl, PrimaryConstructor);
				}

				if (constructorsNotChainedWithThis.Count > 0)
				{
					if (TypeDefinition?.Kind != TypeKind.Struct || (context.Settings.StructDefaultConstructorsAndFieldInitializers && !TypeDefinition.IsRecord))
					{
						bool isPrimaryCtor = constructorsNotChainedWithThis[0] == PrimaryConstructorDecl;
						var sequence = isPrimaryCtor
							? PrimaryConstructorInitializers
							: InitializerSequence.Analyze(this, constructorsNotChainedWithThis[0], (IMethod)constructorsNotChainedWithThis[0].GetSymbol()!);

						if (sequence == null)
							return false;

						bool sequenceMatchesAllCtors = true;
						for (int i = 1; i < constructorsNotChainedWithThis.Count; i++)
						{
							if (!sequence.IsMatch(constructorsNotChainedWithThis[i]))
							{
								sequenceMatchesAllCtors = false;
								break;
							}
						}

						if (!sequenceMatchesAllCtors)
						{
							// The non-this-chained constructors disagree on their leading field
							// assignments, so there is no shared field-initializer sequence to extract.
							// A primary constructor must extract its initializers (its parameters drive
							// them), so bail; otherwise keep the assignments in the bodies but continue,
							// so the this(...)/base(...) chains still get lifted to initializers.
							if (isPrimaryCtor)
								return false;
						}
						else if (!isPrimaryCtor)
						{
							if (!sequence.Statements.Any(s => s.DependsOnConstructorBody))
								InstanceInitializers = sequence;
						}
					}
				}

				InstanceConstructors = allCtors.ToArray();

				return true;
			}

			public bool MoveConstructorInitializer(ConstructorDeclaration constructorDeclaration, IMethod ctorMethod)
			{
				if (constructorDeclaration.Body is null)
					return false;
				var isValueType = ctorMethod.DeclaringType.Kind == TypeKind.Struct;

				// value types may omit the constructor initializer completely
				if (constructorDeclaration.Body.Statements.FirstOrDefault() == null && isValueType)
				{
					return true;
				}

				// The chained this-/base-ctor call is normally the first body statement, but a few
				// IL shapes (e.g. obfuscator output) emit field initializers, guard clauses, or an
				// inlinable temporary before it. Look past such leading statements so the call is
				// still recognized; leaving it behind would print an uncompilable 'base..ctor(...)'.
				Statement? stmt = FindConstructorInitializerCall(constructorDeclaration.Body, isValueType, out var m);

				if (stmt == null)
					return isValueType;

				if (isValueType && stmt != constructorDeclaration.Body.Statements.FirstOrDefault())
				{
					// A value-type chain is an ordinary body statement ('this = new TSelf(...)'), which
					// is legal C# as it stands. Lifting it past the leading statements into a this(...)
					// initializer would run it before those statements (e.g. before an argument
					// null-guard), changing the observable behavior; keep the body form instead.
					return true;
				}

				AstNode invocation = m.Get<AstNode>("invocation").Single();
				if (invocation.GetSymbol() is not IMethod { IsConstructor: true } ctor)
					return false;

				// Any local read inside the call arguments is illegal once the call becomes an
				// initializer (the initializer runs before the body that declares the local), so
				// fold its declaration into the arguments. Bail if that cannot be done safely.
				if (stmt != constructorDeclaration.Body.Statements.FirstOrDefault()
					&& !TryFoldLeadingTemporariesIntoConstructorCall(constructorDeclaration.Body, stmt, invocation))
				{
					return false;
				}

				ConstructorInitializerType type = ctor.DeclaringTypeDefinition == ctorMethod.DeclaringTypeDefinition
					? ConstructorInitializerType.This
					: ConstructorInitializerType.Base;

				var ci = new ConstructorInitializer { ConstructorInitializerType = type };

				context.Step("Move constructor call to initializer", stmt);
				// Move arguments from invocation to initializer:
				invocation.GetChildren(Slots.Argument).MoveTo(ci.Arguments);
				// Add the initializer: (unless it is the default 'base()')
				if (!(ci.ConstructorInitializerType == ConstructorInitializerType.Base && ci.Arguments.Count == 0))
					constructorDeclaration.Initializer = ci.CopyAnnotationsFrom(invocation);

				// Remove the statement
				stmt.Remove();
				context.EndStep(constructorDeclaration.Initializer);

				return true;
			}

			/// <summary>
			/// Finds the chained this-/base-constructor call statement in a constructor body.
			/// The call is normally the first statement, but may be preceded by field initializers,
			/// guard clauses, or inlinable temporaries; the first statement matching the chained-call
			/// pattern is returned together with its match. Returns <c>null</c> if there is none.
			/// </summary>
			private static Statement? FindConstructorInitializerCall(BlockStatement? body, bool isValueType, out Match m)
			{
				for (Statement? stmt = body?.Statements.FirstOrDefault(); stmt != null; stmt = stmt.GetNextStatement())
				{
					m = isValueType
						? ThisCallStructPattern.Match(stmt)
						: ThisCallClassPattern.Match(stmt);
					if (m.Success)
						return stmt;
				}
				m = default;
				return null;
			}

			/// <summary>
			/// Folds the declarations of single-use (or side-effect-free) local temporaries that the
			/// chained-constructor call reads into the call's arguments and removes those declarations,
			/// so the arguments only reference parameters and fields once the call becomes an initializer.
			/// Returns <c>false</c> (bail) if a referenced local cannot be folded safely.
			/// </summary>
			private static bool TryFoldLeadingTemporariesIntoConstructorCall(BlockStatement body, Statement callStatement, AstNode invocation)
			{
				// Collect the local variables referenced inside the call arguments.
				var argumentUses = new Dictionary<ILVariable, List<IdentifierExpression>>();
				foreach (var argument in invocation.GetChildren(Slots.Argument))
				{
					foreach (var identifier in argument.DescendantsAndSelf.OfType<IdentifierExpression>())
					{
						var variable = identifier.GetILVariable();
						if (variable == null || variable.Kind == VariableKind.Parameter)
							continue;
						if (!argumentUses.TryGetValue(variable, out var uses))
						{
							uses = [];
							argumentUses[variable] = uses;
						}
						uses.Add(identifier);
					}
				}

				if (argumentUses.Count == 0)
				{
					// The call reads no locals; any preceding statements (field initializers, guard
					// clauses) keep running in the body and the call lifts to an initializer unchanged.
					return true;
				}

				foreach (var (variable, uses) in argumentUses)
				{
					// The declaration of a foldable temporary must precede the call.
					if (FindSingleDeclaratorDeclaration(body, callStatement, variable) is not { } declaration)
						return false;

					var initializer = declaration.Variables.Single().Initializer;
					if (initializer is null)
						return false;

					// The temporary may only be read by the call arguments; any other read would be
					// left referencing an undeclared local after the declaration is removed.
					int totalUses = body.DescendantsAndSelf
						.OfType<IdentifierExpression>()
						.Count(id => id.GetILVariable() == variable);
					if (totalUses != uses.Count)
					{
						if (TryFoldInitializedObjectTemporary(body, callStatement, invocation, variable, declaration, initializer))
							continue;
						// Element stores fold into the array creation they follow, which leaves the
						// temporary read only by the call and the ordinary fold below applicable.
						if (!TryFoldArrayElementStores(callStatement, variable, declaration, initializer))
							return false;
						totalUses = body.DescendantsAndSelf
							.OfType<IdentifierExpression>()
							.Count(id => id.GetILVariable() == variable);
						if (totalUses != uses.Count)
							return false;
					}

					// The initializer becomes part of the constructor initializer, which runs before
					// the body. It may therefore only reference parameters and fields, never another
					// body local that would be declared later.
					if (initializer.DescendantsAndSelf.OfType<IdentifierExpression>()
						.Any(id => id.GetILVariable() is { Kind: not VariableKind.Parameter }))
					{
						return false;
					}

					// A temporary read by more than one argument is inlined into each use even though
					// that re-evaluates the initializer. A constructor initializer cannot be preceded
					// by a local declaration, so once the call lifts there is no place to keep a single
					// evaluation; leaving the call in the body instead would emit an uncompilable
					// explicit '.ctor' invocation. This matches the source such IL typically comes from,
					// where the same expression was written in each argument position.
					foreach (var use in uses)
					{
						use.ReplaceWith(initializer.Clone());
					}
					declaration.Remove();
				}

				return true;
			}

			/// <summary>
			/// Folds element stores that follow an array creation back into the creation itself, so the
			/// array can travel into a constructor initializer. The compiler leaves an element behind
			/// whenever its value needs a temporary of its own; that temporary comes along, since it is
			/// declared immediately before the store that reads it.
			/// </summary>
			private static bool TryFoldArrayElementStores(Statement callStatement, ILVariable variable,
				VariableDeclarationStatement declaration, Expression initializer)
			{
				if (initializer is not ArrayCreateExpression { Initializer: { } arrayInitializer } arrayCreation || arrayInitializer.Elements.Count == 0)
					return false;
				var elements = arrayInitializer.Elements.ToArray();
				var absorbed = new List<(Statement Statement, int Index, Expression Value, Statement? HoistedValue)>();
				for (Statement? statement = declaration.GetNextStatement(); statement != null && statement != callStatement;
					statement = statement.GetNextStatement())
				{
					Statement? hoisted = null;
					Expression? hoistedValue = null;
					ILVariable? hoistedLocal = null;
					var candidate = statement;
					if (candidate is VariableDeclarationStatement { Variables: [{ Initializer: not null } declared] } hoistedDeclaration
						&& !hoistedDeclaration.Type.IsVar() && declared.GetILVariable() is { } hoistedVariable)
					{
						hoisted = candidate;
						hoistedValue = declared.Initializer;
						hoistedLocal = hoistedVariable;
						if (candidate.GetNextStatement() is not { } afterHoist || afterHoist == callStatement)
							return false;
						candidate = afterHoist;
						statement = candidate;
						if (!IsSoleUseOfHoistedValue(hoistedVariable, candidate))
							return false;
					}
					if (candidate is not ExpressionStatement {
						Expression: AssignmentExpression {
							Operator: AssignmentOperatorType.Assign,
							Left: IndexerExpression {
								Target: IdentifierExpression target,
								Arguments: [PrimitiveExpression { Value: int index }]
							},
							Right: var value
						}
					})
					{
						return false;
					}
					if (target.GetILVariable() != variable || index < 0 || index >= elements.Length)
						return false;
					// Only an element the compiler left at its default is up for grabs; anything else
					// would be a store the array creation already accounts for.
					if (elements[index] is not (DefaultValueExpression or NullReferenceExpression))
						return false;
					if (value.DescendantsAndSelf.OfType<IdentifierExpression>()
						.Any(identifier => identifier.GetILVariable() == variable))
					{
						return false;
					}
					absorbed.Add((candidate, index, value, hoisted));
					if (hoistedValue != null)
					{
						// The element value may name other things besides the temporary - parameters,
						// fields, the callee of a call - so pick out the one read of the temporary that
						// IsSoleUseOfHoistedValue established rather than the only identifier present.
						var hoistedUse = value.DescendantsAndSelf.OfType<IdentifierExpression>()
							.Single(identifier => identifier.GetILVariable() == hoistedLocal);
						hoistedUse.ReplaceWith(hoistedValue.Detach());
					}
				}

				if (absorbed.Count == 0)
					return false;
				foreach (var (assignment, index, value, hoisted) in absorbed)
				{
					value.Detach();
					elements[index].ReplaceWith(value);
					assignment.Remove();
					hoisted?.Remove();
				}
				return true;
			}

			static bool IsSoleUseOfHoistedValue(ILVariable hoisted, Statement store)
			{
				return store is ExpressionStatement { Expression: AssignmentExpression { Right: var value } }
					&& value.DescendantsAndSelf.OfType<IdentifierExpression>()
						.Count(identifier => identifier.GetILVariable() == hoisted) == 1;
			}

			private static bool TryFoldInitializedObjectTemporary(BlockStatement body, Statement callStatement,
				AstNode invocation, ILVariable variable, VariableDeclarationStatement declaration, Expression initializer)
			{
				if (initializer is not ObjectCreateExpression objectCreation)
					return false;

				var fieldInitializers = new Dictionary<IMember,
					(string Name, AstNode AnnotationSource, MemberReferenceExpression? SetupAccess, Expression Value, Statement? Statement)>();
				foreach (var namedExpression in objectCreation.Initializer?.Elements.OfType<NamedExpression>() ?? [])
				{
					if (namedExpression.GetSymbol() is not IField field || !IsMovableClosureFieldValue(namedExpression.Expression))
						return false;
					IMember definition = field.MemberDefinition ?? field;
					if (fieldInitializers.ContainsKey(definition))
						return false;
					fieldInitializers.Add(definition,
						(namedExpression.Name, namedExpression, null, namedExpression.Expression, null));
				}
				for (Statement? statement = declaration.GetNextStatement(); statement != null && statement != callStatement;
					statement = statement.GetNextStatement())
				{
					if (statement is not ExpressionStatement {
						Expression: AssignmentExpression {
							Operator: AssignmentOperatorType.Assign,
							Left: MemberReferenceExpression { Target: IdentifierExpression target } access,
							Right: var value
						}
					} || target.GetILVariable() != variable)
					{
						continue;
					}
					if (access.GetSymbol() is not IField field || !IsMovableClosureFieldValue(value))
						return false;
					IMember definition = field.MemberDefinition ?? field;
					if (fieldInitializers.ContainsKey(definition))
						return false;
					fieldInitializers.Add(definition, (access.MemberName, access, access, value, statement));
				}
				if (fieldInitializers.Count == 0)
					return false;

				var fieldReads = new List<(MemberReferenceExpression Access, Expression Value)>();
				IdentifierExpression? identityUse = null;
				foreach (var use in body.DescendantsAndSelf.OfType<IdentifierExpression>()
					.Where(identifier => identifier.GetILVariable() == variable).ToList())
				{
					if (use.Parent is not MemberReferenceExpression access || access.Target != use)
						return false;
					if (fieldInitializers.Values.Any(initializer => initializer.SetupAccess == access))
						continue;
					if (access.GetSymbol() is IField field
						&& fieldInitializers.TryGetValue(field.MemberDefinition ?? field, out var fieldInitializer))
					{
						fieldReads.Add((access, fieldInitializer.Value));
						continue;
					}
					if (access.GetSymbol() is not IMethod || identityUse != null
						|| !invocation.GetChildren(Slots.Argument)
							.Any(argument => argument.DescendantsAndSelf.Contains(access)))
					{
						return false;
					}
					identityUse = use;
				}
				if (identityUse == null)
					return false;

				objectCreation = (ObjectCreateExpression)objectCreation.Detach();
				objectCreation.Initializer ??= new ArrayInitializerExpression();
				foreach (var fieldInitializer in fieldInitializers.Values.Where(initializer => initializer.Statement != null))
				{
					var namedExpression = new NamedExpression(fieldInitializer.Name, fieldInitializer.Value.Clone());
					namedExpression.CopyAnnotationsFrom(fieldInitializer.AnnotationSource);
					objectCreation.Initializer.Elements.Add(namedExpression);
				}
				identityUse.ReplaceWith(objectCreation);
				foreach (var (access, value) in fieldReads)
					access.ReplaceWith(value.Clone());
				foreach (var fieldInitializer in fieldInitializers.Values.Where(initializer => initializer.Statement != null))
					fieldInitializer.Statement!.Remove();
				declaration.Remove();
				return true;

				static bool IsMovableClosureFieldValue(Expression expression)
				{
					return expression switch {
						IdentifierExpression identifier => identifier.GetILVariable() is { Kind: VariableKind.Parameter },
						PrimitiveExpression or NullReferenceExpression or DefaultValueExpression or TypeOfExpression => true,
						_ => false
					};
				}
			}

			/// <summary>
			/// Finds a single-declarator local <see cref="VariableDeclarationStatement"/> for the given
			/// variable among the statements preceding <paramref name="callStatement"/>.
			/// </summary>
			private static VariableDeclarationStatement? FindSingleDeclaratorDeclaration(BlockStatement body, Statement callStatement, ILVariable variable)
			{
				for (Statement? stmt = body.Statements.FirstOrDefault(); stmt != null && stmt != callStatement; stmt = stmt.GetNextStatement())
				{
					if (stmt is VariableDeclarationStatement { Variables: { Count: 1 } } vds
						&& vds.Variables.Single().GetILVariable() == variable)
					{
						return vds;
					}
				}
				return null;
			}

			public bool MoveFieldInitializersToDeclarations(InitializerSequence sequence, InitializerKind kind)
			{
				foreach (var (stmt, member, initializer, dependsOnBody, referencesInstanceMember) in sequence.Statements)
				{
					Debug.Assert(!dependsOnBody || kind is InitializerKind.Primary);
					Debug.Assert(!referencesInstanceMember, $"Cannot move initializer for {member}: {stmt}");

					if (!MemberToDeclaringSyntaxNodeMap.TryGetValue(member, out var declaringSyntaxNode))
					{
						// A primary-constructor parameter assignment whose backing member has no separate
						// declaration is redundant and dropped. For static/instance initializers a missing
						// declaration instead means the member is not part of this (partial) syntax tree --
						// e.g. when a single static constructor is decompiled in isolation -- so the
						// assignment must remain in the constructor body.
						if (kind is InitializerKind.Primary)
						{
							context.Step("Remove redundant primary constructor assignment", stmt);
							stmt.Remove();
						}
						continue;
					}

					VariableInitializer v;
					switch (declaringSyntaxNode)
					{
						case FieldDeclaration fd:
							v = fd.Variables.Single();
							if (v.Initializer is null)
							{
								context.Step("Move assignment to field initializer", stmt);
								var movedInitializer = initializer.Detach();
								v.Initializer = movedInitializer;
								context.EndStep(movedInitializer);
							}
							else if (kind == InitializerKind.Static)
							{
								// decimal constants already have an initializer in the AST at this point,
								// because it was added in CSharpDecompiler.DoDecompile(IField, ...)
								var constant = v.Initializer.GetResolveResult();
								var expression = initializer.GetResolveResult();
								if (constant.IsCompileTimeConstant && TryEvaluateDecimalConstant(expression, out decimal value))
								{
									// decimal values do not match, skip transformation?
									if (!value.Equals(constant.ConstantValue))
										continue;
								}
								else
								{
									// already has an initializer - do not modify
									Debug.Fail("Field already has an initializer");
								}
							}
							else
							{
								// already has an initializer - do not modify
								Debug.Fail("Field already has an initializer");
							}
							break;
						case PropertyDeclaration pd:
							Debug.Assert(pd.IsAutomaticProperty);
							if (pd.Initializer is null)
							{
								context.Step("Move assignment to property initializer", stmt);
								var movedInitializer = initializer.Detach();
								pd.Initializer = movedInitializer;
								context.EndStep(movedInitializer);
							}
							else
							{
								// already has an initializer - do not modify
								Debug.Fail("Property already has an initializer");
							}
							break;
						case EventDeclaration ev:
							v = ev.Variables.Single();
							if (v.Initializer is null)
							{
								context.Step("Move assignment to event initializer", stmt);
								var movedInitializer = initializer.Detach();
								v.Initializer = movedInitializer;
								context.EndStep(movedInitializer);
							}
							else
							{
								// already has an initializer - do not modify
								Debug.Fail("Event already has an initializer");
							}
							break;
						default:
							// cannot move initializer
							continue;
					}
					// Remove the statement from all constructors
					stmt.Remove();

					if (sequence.StatementToOtherCtorsMap != null &&
						sequence.StatementToOtherCtorsMap.TryGetValue(stmt, out var otherCtors))
					{
						var otherInitializers = new List<Expression>(otherCtors.Count);
						foreach (var (otherStmt, otherInitializer) in otherCtors)
						{
							otherStmt.Remove();
							otherInitializers.Add(otherInitializer);
						}
						// Preserve the discarded copies so the breakpoint for this initializer can be
						// emitted in every constructor that runs it, not just the one it was lifted from.
						if (otherInitializers.Count > 0)
						{
							initializer.AddAnnotation(new MemberInitializerInOtherConstructorsAnnotation(otherInitializers));
						}
					}

					if (sequence.IsUnsafe && IntroduceUnsafeModifier.IsUnsafe(initializer))
					{
						context.Step("Add unsafe modifier to initialized member", declaringSyntaxNode);
						declaringSyntaxNode.Modifiers |= Modifiers.Unsafe;
					}
				}
				return true;
			}

			public void RemoveImplicitConstructor()
			{
				Debug.Assert(MemberToDeclaringSyntaxNodeMap != null);
				Debug.Assert(TypeDefinition != null);

				// We do not want to hide the constructor if the user explicitly selected it in the tree view.
				if (TypeDeclaration == null)
					return;

				// Remove static constructor
				if (StaticConstructor != null)
				{
					Debug.Assert(StaticConstructorDecl != null);

					if (IsBeforeFieldInit && StaticConstructorDecl.Body is { Statements.Count: 0 })
					{
						context.Step("Remove empty static constructor", StaticConstructorDecl);
						StaticConstructorDecl.Remove();
					}
				}

				// Remove primary constructor body
				if (PrimaryConstructor != null)
				{
					Debug.Assert(PrimaryConstructorDecl != null);

					// A parameterless primary constructor is normally left implicit, but only a type
					// that declares no other constructor gets one back. Where another constructor
					// remains, dropping it would take the parameterless one with it and break every
					// 'new T()' and ': this()' that reaches for it. Writing it as an empty parameter
					// list on the record is no good either: that obliges every other constructor to
					// chain with ': this()' (CS8862). Leave it as an ordinary constructor.
					bool declaresAnotherConstructor = this.TypeDeclaration.Members
						.OfType<ConstructorDeclaration>()
						.Any(c => c != PrimaryConstructorDecl && !c.HasModifier(Modifiers.Static));
					if (declaresAnotherConstructor && !PrimaryConstructor.Parameters.Any()
						&& TypeDefinition.Kind != TypeKind.Struct)
					{
						return;
					}

					this.TypeDeclaration.HasPrimaryConstructor = PrimaryConstructor.Parameters.Any()
						|| PrimaryConstructorDecl.Initializer is not null
						|| TypeDefinition.Kind == TypeKind.Struct;

					// HACK: because our current AST model doesn't allow specifying an explicit ordering across slots,
					// we have to explicitly insert the primary constructor parameters,
					// MoveTo would just append the parameters to the list of children
					if (PrimaryConstructorDecl.Parameters.Count > 0)
					{
						var insertionPoint = (AstNode?)this.TypeDeclaration.TypeParameters.LastOrDefault() ?? this.TypeDeclaration.NameToken;
						foreach (var param in PrimaryConstructorDecl.Parameters)
						{
							context.Step("Move primary constructor parameter to type", param);
							param.Remove();
							this.TypeDeclaration.InsertChildAfter(insertionPoint, param, Slots.Parameter);
							insertionPoint = param;
						}
					}

					Debug.Assert(PrimaryConstructorParameterToBackingStoreMap != null);

					foreach (var pd in this.TypeDeclaration.PrimaryConstructorParameters)
					{
						var v = pd.Annotation<ILVariableResolveResult>()?.Variable;
						Debug.Assert(v?.Index >= 0);
						var p = PrimaryConstructor.Parameters[v.Index.Value];

						if (!PrimaryConstructorParameterToBackingStoreMap.TryGetValue(p, out var backingStore))
						{
							// no backing store, constructor parameter was left unassigned or
							// assigned to a different member in a sub-expression:
							// private int someField = param + param2;
							continue;
						}

						var (prop, field) = backingStore;

						if (prop != null)
						{
							var attributes = prop?.GetAttributes().Select(attr => context.TypeSystemAstBuilder.ConvertAttribute(attr)).ToArray();
							if (attributes?.Length > 0)
							{
								var section = new AttributeSection {
									AttributeTarget = "property"
								};
								section.Attributes.AddRange(attributes);
								pd.Attributes.Add(section);
							}
						}
						if (field != null)
						{
							var attributes = field.GetAttributes()
								.Where(a => !PatternStatementTransform.attributeTypesToRemoveFromAutoProperties.Contains(a.AttributeType.FullName))
								.Select(attr => context.TypeSystemAstBuilder.ConvertAttribute(attr)).ToArray();
							if (attributes.Length > 0)
							{
								var section = new AttributeSection {
									AttributeTarget = "field"
								};
								section.Attributes.AddRange(attributes);
								pd.Attributes.Add(section);
							}
						}
					}

					if (PrimaryConstructorDecl.HasModifier(Modifiers.Unsafe))
					{
						context.Step("Move unsafe modifier from primary constructor to type", this.TypeDeclaration);
						this.TypeDeclaration.Modifiers |= Modifiers.Unsafe;
					}

					foreach (var attributeSection in PrimaryConstructorDecl.Attributes)
					{
						context.Step("Move method attribute from primary constructor to type", attributeSection);
						attributeSection.AttributeTarget = "method";
						this.TypeDeclaration.Attributes.Add(attributeSection.Detach());
					}

					if (PrimaryConstructorDecl.Initializer is { } initializer && TypeDeclaration is { BaseTypes.Count: > 0 })
					{
						Debug.Assert(initializer.ConstructorInitializerType == ConstructorInitializerType.Base);

						var baseType = TypeDeclaration.BaseTypes.First();
						var newBaseType = new InvocationAstType();
						context.Step("Move primary constructor initializer to base type", baseType);
						baseType.ReplaceWith(newBaseType);
						newBaseType.BaseType = baseType;
						initializer.Arguments.MoveTo(newBaseType.Arguments);
						context.EndStep(newBaseType);
					}

					context.Step("Remove primary constructor body", PrimaryConstructorDecl);
					PrimaryConstructorDecl.Remove();
				}

				// More than one constructor - do not remove anything
				if (InstanceConstructors.Length != 1)
					return;

				var ctor = InstanceConstructors[0];
				var ctorMethod = (IMethod)ctor.GetSymbol()!;

				if (TypeDefinition.Kind == TypeKind.Struct && ctorMethod.Parameters.Count == 0 && InstanceInitializers != null)
				{
					// struct constructor with initializers is not optional
					return;
				}

				// dynamically create a pattern of an empty ctor
				ConstructorDeclaration emptyCtorPattern = new ConstructorDeclaration();
				emptyCtorPattern.Modifiers = TypeDefinition.IsAbstract ? Modifiers.Protected : Modifiers.Public;
				if (ctor.HasModifier(Modifiers.Unsafe))
					emptyCtorPattern.Modifiers |= Modifiers.Unsafe;
				emptyCtorPattern.Body = new BlockStatement();

				if (emptyCtorPattern.IsMatch(ctor))
				{
					bool retainBecauseOfDocumentation = context.Settings.ShowXmlDocumentation
						&& context.DecompileRun.DocumentationProvider?.GetDocumentation(ctorMethod) != null;
					if (!retainBecauseOfDocumentation)
					{
						context.Step("Remove implicit constructor", ctor);
						ctor.Remove();
					}
				}
			}

			/// <summary>
			/// Evaluates a call to the decimal-ctor.
			/// </summary>
			private static bool TryEvaluateDecimalConstant(Semantics.ResolveResult expression, out decimal value)
			{
				value = 0;
				if (!expression.Type.IsKnownType(KnownTypeCode.Decimal))
				{
					return false;
				}
				switch (expression)
				{
					case CSharpInvocationResolveResult rr:
						if (!(rr.GetSymbol() is IMethod { SymbolKind: SymbolKind.Constructor } ctor))
							return false;
						var args = rr.GetArgumentsForCall();
						if (args.Count == 1)
						{
							switch (args[0].ConstantValue)
							{
								case double d:
									value = new decimal(d);
									return true;
								case float f:
									value = new decimal(f);
									return true;
								case long l:
									value = new decimal(l);
									return true;
								case int i:
									value = new decimal(i);
									return true;
								case ulong ul:
									value = new decimal(ul);
									return true;
								case uint ui:
									value = new decimal(ui);
									return true;
								case int[] bits when bits.Length == 4 && (bits[3] & 0x7F00FFFF) == 0 && (bits[3] & 0xFF000000) <= 0x1C000000:
									value = new decimal(bits);
									return true;
								default:
									return false;
							}
						}
						else if (args.Count == 5 &&
							args[0].ConstantValue is int lo &&
							args[1].ConstantValue is int mid &&
							args[2].ConstantValue is int hi &&
							args[3].ConstantValue is bool isNegative &&
							args[4].ConstantValue is byte scale)
						{
							value = new decimal(lo, mid, hi, isNegative, scale);
							return true;
						}
						return false;
					default:
						if (expression.ConstantValue is decimal v)
						{
							value = v;
							return true;
						}
						return false;
				}
			}
		}

		[AllowNull]
		TransformContext context;

		public void Run(AstNode node, TransformContext context)
		{
			this.context = context;

			if (context.CurrentTypeDefinition != null)
			{
				TransformDeclaration(context.CurrentTypeDefinition, node, node.Children.OfType<EntityDeclaration>());
			}

			foreach (var typeDeclaration in node.Descendants.OfType<TypeDeclaration>())
			{
				var currentTypeDefinition = (ITypeDefinition)typeDeclaration.GetSymbol()!;
				TransformDeclaration(currentTypeDefinition, typeDeclaration, typeDeclaration.Members);
			}
		}

		private bool TransformDeclaration(ITypeDefinition currentTypeDefinition, AstNode node, IEnumerable<EntityDeclaration> members)
		{
			var analyzer = new ConstructorInitializerAnalyzer(context, currentTypeDefinition, node as TypeDeclaration);

			if (!analyzer.Analyze(members))
				return false;

			if (analyzer.PrimaryConstructorInitializers is { HasDuplicateAssignments: false })
			{
				analyzer.MoveFieldInitializersToDeclarations(analyzer.PrimaryConstructorInitializers, InitializerKind.Primary);
			}
			else if (analyzer.InstanceInitializers is { HasDuplicateAssignments: false })
			{
				analyzer.MoveFieldInitializersToDeclarations(analyzer.InstanceInitializers, InitializerKind.Instance);
			}

			if (analyzer.StaticInitializers is { HasDuplicateAssignments: false })
			{
				analyzer.MoveFieldInitializersToDeclarations(analyzer.StaticInitializers, InitializerKind.Static);
			}

			foreach (var constructorDeclaration in members.OfType<ConstructorDeclaration>())
			{
				analyzer.MoveConstructorInitializer(constructorDeclaration, (IMethod)constructorDeclaration.GetSymbol()!);
			}

			analyzer.RemoveImplicitConstructor();

			if (analyzer.BackingFieldToPrimaryConstructorParameterVariableMap != null)
			{
				foreach (Identifier identifier in node.Descendants.OfType<Identifier>())
				{
					if (identifier.Parent?.GetSymbol() is not IField field)
					{
						continue;
					}
					if (!analyzer.BackingFieldToPrimaryConstructorParameterVariableMap.TryGetValue((IField)field.MemberDefinition, out var v))
					{
						continue;
					}
					identifier.Parent.RemoveAnnotations<MemberResolveResult>();
					identifier.Parent.AddAnnotation(new ILVariableResolveResult(v));
					identifier.ReplaceWith(Identifier.Create(v.Name!));
				}
			}

			RenderLeftoverBackingFields(currentTypeDefinition, node, analyzer);

			return true;
		}

		/// <summary>
		/// Primary-constructor parameter backing fields (named <c>&lt;name&gt;P</c>) are hidden from the
		/// member list on the assumption they will be folded into a primary constructor. When that
		/// transform does not apply (e.g. the constructor also contains a field initializer the
		/// compiler lowered to statements, so it cannot become a primary constructor), the fields
		/// remain referenced but undeclared. Emit them as ordinary fields with a de-mangled name and
		/// rewrite their accesses to be explicitly qualified, so the references resolve.
		/// </summary>
		private void RenderLeftoverBackingFields(ITypeDefinition currentTypeDefinition, AstNode node, ConstructorInitializerAnalyzer analyzer)
		{
			List<IField>? leftoverFields = null;
			foreach (var field in currentTypeDefinition.Fields)
			{
				if (!IsGeneratedPrimaryConstructorBackingField(field))
					continue;
				if (analyzer.BackingFieldToPrimaryConstructorParameterVariableMap?.ContainsKey(field) == true)
					continue;
				leftoverFields ??= [];
				leftoverFields.Add(field);
			}

			if (leftoverFields == null)
				return;

			// In primary-constructor codegen the captures and field initializers run before the
			// base constructor call, so an un-promoted constructor ends with a trailing base call
			// instead of starting with it. A trailing default 'base()' that survived to here would be
			// printed as an invalid 'base..ctor();', so drop it; it has no observable effect.
			foreach (var ctor in node.Children.OfType<ConstructorDeclaration>())
			{
				var lastStatement = ctor.Body?.Statements.LastOrDefault();
				if (lastStatement == null)
					continue;
				var m = ThisCallClassPattern.Match(lastStatement);
				if (!m.Success)
					continue;
				if (m.Get<Expression>("target").Single() is not BaseReferenceExpression)
					continue;
				var invocation = m.Get<AstNode>("invocation").Single();
				if (invocation.GetSymbol() is not IMethod { IsConstructor: true })
					continue;
				if (invocation.GetChildren(Slots.Argument).Any())
					continue;
				lastStatement.Remove();
			}

			// strip the leading '<' and trailing '>P' of '<name>P' to recover the parameter name
			static string DemangleName(IField field) => field.Name.Substring(1, field.Name.Length - 3);

			foreach (var field in leftoverFields)
			{
				string name = DemangleName(field);
				var fieldDecl = (FieldDeclaration)context.TypeSystemAstBuilder.ConvertEntity(field);
				fieldDecl.Variables.Single().Name = name;
				// the synthesized field is an ordinary field now, so drop attributes that only
				// make sense on the hidden compiler-generated backing field
				foreach (var section in fieldDecl.Attributes.ToArray())
				{
					foreach (var attr in section.Attributes.ToArray())
					{
						if (PatternStatementTransform.attributeTypesToRemoveFromAutoProperties.Contains(attr.Type.GetSymbol() is ITypeDefinition td ? td.FullTypeName.ToString() : null))
						{
							attr.Remove();
						}
					}
					if (section.Attributes.Count == 0)
						section.Remove();
				}
				if (node is TypeDeclaration typeDeclaration)
				{
					var lastField = typeDeclaration.Members.OfType<FieldDeclaration>().LastOrDefault();
					var firstMember = typeDeclaration.Members.FirstOrDefault();
					if (lastField != null)
						typeDeclaration.Members.InsertAfter(lastField, fieldDecl);
					else if (firstMember != null)
						typeDeclaration.Members.InsertBefore(firstMember, fieldDecl);
					else
						typeDeclaration.Members.Add(fieldDecl);
				}
			}

			foreach (Identifier identifier in node.Descendants.OfType<Identifier>().ToArray())
			{
				if (identifier.Parent?.GetSymbol() is not IField accessedField)
					continue;
				var fieldDefinition = (IField)accessedField.MemberDefinition;
				if (!leftoverFields.Contains(fieldDefinition))
					continue;
				string name = DemangleName(fieldDefinition);
				if (identifier.Parent is IdentifierExpression identifierExpression)
				{
					// unqualified access may collide with a same-named parameter or local,
					// so qualify it explicitly with 'this'
					var mrr = identifierExpression.Annotation<MemberResolveResult>();
					var replacement = new MemberReferenceExpression(new ThisReferenceExpression(), name)
						.CopyAnnotationsFrom(identifierExpression);
					if (mrr != null)
					{
						replacement.RemoveAnnotations<MemberResolveResult>();
						replacement.AddAnnotation(new MemberResolveResult(
							new ResolveResult(currentTypeDefinition), fieldDefinition));
					}
					identifierExpression.ReplaceWith(replacement);
				}
				else
				{
					identifier.ReplaceWith(Identifier.Create(name));
				}
			}
		}
	}
}

// Copyright (c) 2014 Daniel Grunwald
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.DebugSteps;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.IL.ControlFlow;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Instrumentation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

#nullable enable

namespace ICSharpCode.Decompiler.CSharp
{
	/// <summary>
	/// Main class of the C# decompiler engine.
	/// </summary>
	/// <remarks>
	/// Instances of this class are not thread-safe. Use separate instances to decompile multiple members in parallel.
	/// (in particular, the transform instances are not thread-safe)
	/// </remarks>
	public class CSharpDecompiler
	{
		readonly IDecompilerTypeSystem typeSystem;
		readonly MetadataModule module;
		readonly MetadataReader metadata;
		readonly DecompilerSettings settings;
		SyntaxTree? syntaxTree;

		List<IILTransform> ilTransforms = GetILTransforms();

		/// <summary>
		/// Pre-yield/await transforms.
		/// </summary>
		internal static List<IILTransform> EarlyILTransforms(bool aggressivelyDuplicateReturnBlocks = false)
		{
			return new List<IILTransform> {
				new ControlFlowSimplification {
					aggressivelyDuplicateReturnBlocks = aggressivelyDuplicateReturnBlocks
				},
				new SplitVariables(),
				new ILInlining(),
			};
		}

		/// <summary>
		/// Returns all built-in transforms of the ILAst pipeline.
		/// </summary>
		public static List<IILTransform> GetILTransforms()
		{
			return new List<IILTransform> {
				new ControlFlowSimplification(),
				// Run SplitVariables only after ControlFlowSimplification duplicates return blocks,
				// so that the return variable is split and can be inlined.
				new SplitVariables(),
				new ILInlining(),
				new InlineReturnTransform(), // must run before DetectPinnedRegions
				new RemoveInfeasiblePathTransform(),
				new DetectPinnedRegions(), // must run after inlining but before non-critical control flow transforms
				new YieldReturnDecompiler(), // must run after inlining but before loop detection
				new AsyncAwaitDecompiler(),  // must run after inlining but before loop detection
				new DetectCatchWhenConditionBlocks(), // must run after inlining but before loop detection
				new DetectExitPoints(),
				new LdLocaDupInitObjTransform(),
				new EarlyExpressionTransforms(),
				new SplitVariables(), // split variables once again, because the stobj(ldloca V, ...) may open up new replacements
				// RemoveDeadVariableInit must run after EarlyExpressionTransforms so that stobj(ldloca V, ...)
				// is already collapsed into stloc(V, ...).
				new RemoveDeadVariableInit(),
				new ControlFlowSimplification(), //split variables may enable new branch to leave inlining
				new DynamicCallSiteTransform(),
				new SwitchDetection(),
				new SwitchOnStringTransform(),
				new SwitchOnNullableTransform(),
				// must run after the integer/string/nullable switch transforms so it only
				// reconstructs throw-helper default cases of switch expressions none of them claimed
				new SwitchExpressionDefaultCaseTransform(),
				new SplitVariables(), // split variables once again, because SwitchOnNullableTransform eliminates ldloca
				new IntroduceRefReadOnlyModifierOnLocals(),
				// Must run before ConditionDetection: giving a pointer stackalloc its own single-definition
				// local keeps it out of a conditional operand or a plain assignment (both would retype it
				// as Span<T>), and leaves each branch with more than one instruction so the conditional
				// operator transform does not fold the two stores back together.
				new SplitPointerStackAllocStores(),
				new BlockILTransform { // per-block transforms
					PostOrderTransforms = {
						// Even though it's a post-order block-transform as most other transforms,
						// let's keep LoopDetection separate for now until there's a compelling
						// reason to combine it with the other block transforms.
						// If we ran loop detection after some if structures are already detected,
						// we might make our life introducing good exit points more difficult.
						new LoopDetection()
					}
				},
				// re-run DetectExitPoints after loop detection
				new DetectExitPoints(),
				new PatternMatchingTransform(), // must run after LoopDetection and before ConditionDetection
				new BlockILTransform { // per-block transforms
					PostOrderTransforms = {
						new ConditionDetection(),
						new LockTransform(),
						new UsingTransform(),
						// CachedDelegateInitialization must run after ConditionDetection and before/in LoopingBlockTransform
						// and must run before NullCoalescingTransform
						new CachedDelegateInitialization(),
						new CachedReadOnlySpanInitialization(),
						new StatementTransform(
							// per-block transforms that depend on each other, and thus need to
							// run interleaved (statement by statement).
							// Pretty much all transforms that open up new expression inlining
							// opportunities belong in this category.
							new ILInlining() { options = InliningOptions.AllowInliningOfLdloca },
							// Inlining must be first, because it doesn't trigger re-runs.
							// Any other transform that opens up new inlining opportunities should call RequestRerun().
							new ExpressionTransforms(),
							new DynamicIsEventAssignmentTransform(),
							new TransformAssignment(), // inline and compound assignments
							new NullCoalescingTransform(),
							new NullableLiftingStatementTransform(),
							new NullPropagationStatementTransform(),
							new TransformArrayInitializers(),
							new TransformCollectionAndObjectInitializers(),
							new TransformExpressionTrees(),
							new IndexRangeTransform(),
							new DeconstructionTransform(),
							new NamedArgumentTransform(),
							new RemoveUnconstrainedGenericReferenceTypeCheck(),
							new UserDefinedLogicTransform(),
							new InterpolatedStringTransform()
						),
					}
				},
				new ProxyCallReplacer(),
				new FixRemainingIncrements(),
				new CopyPropagation(),
				new NormalizeVisualBasicClosures(),
				new DelegateConstruction(),
				new LocalFunctionDecompiler(),
				new TransformDisplayClassUsage(),
				new HighLevelLoopTransform(),
				new ReduceNestingTransform(),
				new RemoveRedundantReturn(),
				new IntroduceDynamicTypeOnLocals(),
				new IntroduceNativeIntTypeOnLocals(),
				new IntroduceTupleElementNamesOnLocals(),
				new AssignVariableNames(),
				new AssignDefaultToUnassignedOutParameters(),
			};
		}

		/// <summary>
		/// Decompiles the body of <paramref name="method"/> to ILAst for structural analysis,
		/// e.g. for recognizing compiler-generated code (see RecordDecompiler, AutoEventDecompiler).
		/// Runs the IL transform pipeline with a fixed set of decompiler settings, so the
		/// resulting shape is independent of the user-visible settings, and stops before the
		/// late transforms (variable naming etc.) that are only needed for code output.
		/// </summary>
		internal static Block? DecompileBodyForAnalysis(IMethod method, IDecompilerTypeSystem typeSystem, CancellationToken cancellationToken)
		{
			if (method.MetadataToken.IsNil)
				return null;
			var module = typeSystem.MainModule;
			var metadata = module.metadata;

			var methodDefHandle = (MethodDefinitionHandle)method.MetadataToken;
			var methodDef = metadata.GetMethodDefinition(methodDefHandle);
			if (!methodDef.HasBody())
				return null;

			var genericContext = new GenericContext(
				classTypeParameters: method.DeclaringTypeDefinition?.TypeParameters,
				methodTypeParameters: null);
			var body = module.MetadataFile.GetMethodBody(methodDef.RelativeVirtualAddress);
			var ilReader = new ILReader(module);
			var il = ilReader.ReadIL(methodDefHandle, body, genericContext, ILFunctionKind.TopLevelFunction, cancellationToken);
			var settings = new DecompilerSettings(LanguageVersion.CSharp1);
			var transforms = GetILTransforms();
			// Remove the last couple transforms -- we don't need variable names etc. here
			int lastBlockTransform = transforms.FindLastIndex(t => t is BlockILTransform);
			transforms.RemoveRange(lastBlockTransform + 1, transforms.Count - (lastBlockTransform + 1));
			// Use CombineExitsTransform so that "return other != null && ...;" is a single statement even in release builds
			transforms.Add(new CombineExitsTransform());
			il.RunTransforms(transforms,
				new ILTransformContext(il, typeSystem, debugInfo: null, settings) {
					CancellationToken = cancellationToken
				});
			if (il.Body is BlockContainer container)
			{
				return container.EntryPoint;
			}
			else if (il.Body is Block block)
			{
				return block;
			}
			else
			{
				return null;
			}
		}

		List<IAstTransform> astTransforms = GetAstTransforms();

		public Stepper Stepper { get; set; } = new Stepper();

		/// <summary>
		/// Returns all built-in transforms of the C# AST pipeline.
		/// </summary>
		public static List<IAstTransform> GetAstTransforms()
		{
			return new List<IAstTransform> {
				new PatternStatementTransform(),
				new ReplaceMethodCallsWithOperators(), // must run before DeclareVariables.EnsureExpressionStatementsAreValid
				new IntroduceUnsafeModifier(),
				new AddCheckedBlocks(),
				new DeclareVariables(), // should run after most transforms that modify statements
				// Must run after DeclareVariables and before TransformFieldAndConstructorInitializers:
				// a collection expression collapses the statements a lowered element fill spreads out,
				// which is what lets a constructor initializer taking one stay the first statement.
				new IntroduceCollectionExpressions(),
				new FoldInitOnlyAssignmentsIntoInitializer(),
				new TransformFieldAndConstructorInitializers(), // must run after DeclareVariables
				new IntroduceFieldKeyword(), // must run after TransformFieldAndConstructorInitializers
				new PrettifyAssignments(), // must run after DeclareVariables
				new IntroduceUsingDeclarations(),
				new IntroduceExtensionMethods(), // must run after IntroduceUsingDeclarations
				new IntroduceQueryExpressions(), // must run after IntroduceExtensionMethods
				new CombineQueryExpressions(),
				new NormalizeBlockStatements(),
				new FlattenSwitchBlocks(),
				new FixNameCollisions(),
				new RemoveRepeatedAttributes(), // must run after the decompiler's own attribute removals
				new AddXmlDocumentationTransform(),
			};
		}

		/// <summary>
		/// Token to check for requested cancellation of the decompilation.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// The type system created from the main module and referenced modules.
		/// </summary>
		public IDecompilerTypeSystem TypeSystem => typeSystem;

		/// <summary>
		/// Gets or sets the optional provider for debug info.
		/// </summary>
		public IDebugInfoProvider? DebugInfoProvider { get; set; }

		/// <summary>
		/// Gets or sets the optional provider for XML documentation strings.
		/// </summary>
		public IDocumentationProvider? DocumentationProvider { get; set; }

		/// <summary>
		/// IL transforms.
		/// </summary>
		public IList<IILTransform> ILTransforms {
			get { return ilTransforms; }
		}

		/// <summary>
		/// C# AST transforms.
		/// </summary>
		public IList<IAstTransform> AstTransforms {
			get { return astTransforms; }
		}

		/// <summary>
		/// Creates a new <see cref="CSharpDecompiler"/> instance from the given <paramref name="fileName"/> using the given <paramref name="settings"/>.
		/// </summary>
		public CSharpDecompiler(string fileName, DecompilerSettings settings)
			: this(CreateTypeSystemFromFile(fileName, settings), settings)
		{
		}

		/// <summary>
		/// Creates a new <see cref="CSharpDecompiler"/> instance from the given <paramref name="fileName"/> using the given <paramref name="assemblyResolver"/> and <paramref name="settings"/>.
		/// </summary>
		public CSharpDecompiler(string fileName, IAssemblyResolver assemblyResolver, DecompilerSettings settings)
			: this(LoadPEFile(fileName, settings), assemblyResolver, settings)
		{
		}

		/// <summary>
		/// Creates a new <see cref="CSharpDecompiler"/> instance from the given <paramref name="module"/> using the given <paramref name="assemblyResolver"/> and <paramref name="settings"/>.
		/// </summary>
		public CSharpDecompiler(MetadataFile module, IAssemblyResolver assemblyResolver, DecompilerSettings settings)
			: this(new DecompilerTypeSystem(module, assemblyResolver, settings), settings)
		{
		}

		/// <summary>
		/// Creates a new <see cref="CSharpDecompiler"/> instance from the given <paramref name="typeSystem"/> and the given <paramref name="settings"/>.
		/// </summary>
		public CSharpDecompiler(IDecompilerTypeSystem typeSystem, DecompilerSettings settings)
		{
			this.typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
			this.settings = settings;
			this.module = typeSystem.MainModule;
			this.metadata = module.MetadataFile.Metadata;
			if (module.TypeSystemOptions.HasFlag(TypeSystemOptions.Uncached))
				throw new ArgumentException("Cannot use an uncached type system in the decompiler.");
		}

		#region MemberIsHidden
		/// <summary>
		/// Determines whether a <paramref name="member"/> should be hidden from the decompiled code. This is used to exclude compiler-generated code that is handled by transforms from the output.
		/// </summary>
		/// <param name="module">The module containing the member.</param>
		/// <param name="member">The metadata token/handle of the member. Can be a TypeDef, MethodDef or FieldDef.</param>
		/// <param name="settings">The settings used to determine whether code should be hidden. E.g. if async methods are not transformed, async state machines are included in the decompiled code.</param>
		public static bool MemberIsHidden(MetadataFile? module, EntityHandle member, DecompilerSettings settings)
		{
			if (module == null || member.IsNil)
				return false;
			var metadata = module.Metadata;
			string name;
			switch (member.Kind)
			{
				case HandleKind.MethodDefinition:
					var methodHandle = (MethodDefinitionHandle)member;
					var method = metadata.GetMethodDefinition(methodHandle);
					var methodSemantics = module.MethodSemanticsLookup.GetSemantics(methodHandle).Item2;
					if (methodSemantics != 0 && methodSemantics != System.Reflection.MethodSemanticsAttributes.Other)
						return true;
					name = metadata.GetString(method.Name);
					if (name == ".ctor" && method.RelativeVirtualAddress == 0 && metadata.GetTypeDefinition(method.GetDeclaringType()).Attributes.HasFlag(System.Reflection.TypeAttributes.Import))
						return true;
					if (module is PEFile m && IsAccessorInterfaceImplementationRuntimeHelper(m, methodHandle))
						return true;
					if (settings.LocalFunctions && LocalFunctionDecompiler.IsLocalFunctionMethod(module, methodHandle))
						return true;
					if (settings.AnonymousMethods && methodHandle.HasGeneratedName(metadata) && methodHandle.IsCompilerGenerated(metadata))
						return name != "<Extension>$";
					if (settings.AsyncAwait && AsyncAwaitDecompiler.IsCompilerGeneratedMainMethod(module, methodHandle))
						return true;
					return false;
				case HandleKind.TypeDefinition:
					var typeHandle = (TypeDefinitionHandle)member;
					var type = metadata.GetTypeDefinition(typeHandle);
					name = metadata.GetString(type.Name);
					if (!type.GetDeclaringType().IsNil)
					{
						if (settings.LocalFunctions && LocalFunctionDecompiler.IsLocalFunctionDisplayClass(module, typeHandle))
							return true;
						if (settings.AnonymousMethods && IsClosureType(type, metadata))
							return true;
						if (settings.YieldReturn && YieldReturnDecompiler.IsCompilerGeneratorEnumerator(typeHandle, metadata))
							return true;
						if (settings.AsyncAwait && AsyncAwaitDecompiler.IsCompilerGeneratedStateMachine(typeHandle, metadata))
							return true;
						if (settings.AsyncEnumerator && AsyncAwaitDecompiler.IsCompilerGeneratorAsyncEnumerator(typeHandle, metadata))
							return true;
						if (settings.FixedBuffers && name.StartsWith("<", StringComparison.Ordinal) && name.Contains("__FixedBuffer"))
							return true;
						if (settings.InlineArrays && name.StartsWith("<>y__InlineArray", StringComparison.Ordinal) && name.EndsWith("`1", StringComparison.Ordinal))
							return true;
						if (settings.ExtensionMembers && (name.StartsWith("<>E__", StringComparison.Ordinal) || name.StartsWith("<G>$", StringComparison.Ordinal)))
							return true;
					}
					else if (type.IsCompilerGenerated(metadata))
					{
						if (settings.ArrayInitializers && name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
							return true;
						if (settings.AnonymousTypes && type.IsAnonymousType(metadata))
							return true;
						if (settings.Dynamic && type.IsDelegate(metadata) && (name.StartsWith("<>A", StringComparison.Ordinal) || name.StartsWith("<>F", StringComparison.Ordinal)))
							return true;
						// Read-only collection-expression wrappers whose uses TransformReadOnlyCollectionExpression
						// unwraps; only the two unwrapped variants are hidden, never <>z__ReadOnlyList.
						if (settings.CollectionExpressions && (name.StartsWith("<>z__ReadOnlyArray", StringComparison.Ordinal) || name.StartsWith("<>z__ReadOnlySingleElementList", StringComparison.Ordinal)))
							return true;
					}
					if (settings.ArrayInitializers && settings.SwitchStatementOnString && name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
						return true;
					return false;
				case HandleKind.FieldDefinition:
					var fieldHandle = (FieldDefinitionHandle)member;
					var field = metadata.GetFieldDefinition(fieldHandle);
					name = metadata.GetString(field.Name);
					if (field.IsCompilerGenerated(metadata))
					{
						if (settings.AnonymousMethods && IsAnonymousMethodCacheField(field, metadata))
							return true;
						if (settings.UsePrimaryConstructorSyntaxForNonRecordTypes && IsPrimaryConstructorParameterBackingField(field, metadata))
							return true;
						if (settings.AutomaticProperties && module.PropertyAndEventBackingFieldLookup.IsPropertyBackingField(fieldHandle, out var propertyHandle))
						{
							if (!settings.GetterOnlyAutomaticProperties)
							{
								PropertyAccessors accessors = metadata.GetPropertyDefinition(propertyHandle).GetAccessors();
								if (!accessors.Getter.IsNil && accessors.Setter.IsNil)
									return false;
							}

							return true;
						}

						if (settings.SwitchStatementOnString && IsSwitchOnStringCache(field, metadata))
							return true;
					}
					// event-fields are not [CompilerGenerated]
					if (settings.AutomaticEvents && module.PropertyAndEventBackingFieldLookup.IsEventBackingField(fieldHandle, out _))
					{
						return true;
					}
					if (settings.ArrayInitializers && metadata.GetString(metadata.GetTypeDefinition(field.GetDeclaringType()).Name).StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
					{
						// only hide fields starting with '__StaticArrayInit'
						if (name.StartsWith("__StaticArrayInit", StringComparison.Ordinal))
							return true;
						// hide fields starting with '$$method'
						if (name.StartsWith("$$method", StringComparison.Ordinal))
							return true;
						if (field.DecodeSignature(new Metadata.FullTypeNameSignatureDecoder(metadata), default).ToString().StartsWith("__StaticArrayInit", StringComparison.Ordinal))
							return true;
					}
					return false;
			}

			return false;
		}
		static bool IsPrimaryConstructorParameterBackingField(SRM.FieldDefinition field, MetadataReader metadata)
		{
			var name = metadata.GetString(field.Name);
			return name.StartsWith("<", StringComparison.Ordinal) && name.EndsWith(">P", StringComparison.Ordinal);
		}

		/// <summary>
		/// Rewrites a decompiled accessor as an ordinary method declaration. An accessor is only
		/// syntax inside a property; where the property itself cannot be written, its body has to go
		/// into a method named get_X/set_X, which is how every call site already reads.
		/// </summary>
		MethodDeclaration RewriteAccessorAsMethod(IMethod accessor, EntityDeclaration accessorDecl, TypeSystemAstBuilder astBuilder)
		{
			var fakeMethod = new FakeMethod(typeSystem, SymbolKind.Method) {
				Name = accessor.Name,
				DeclaringType = accessor.DeclaringType,
				ReturnType = accessor.ReturnType,
				Accessibility = accessor.Accessibility,
				IsStatic = accessor.IsStatic,
				Parameters = accessor.Parameters,
				TypeParameters = accessor.TypeParameters,
			};
			var declaration = (MethodDeclaration)astBuilder.ConvertEntity(fakeMethod);
			declaration.RemoveAnnotations<ResolveResult>();
			foreach (var annotation in accessorDecl.Annotations)
			{
				declaration.AddAnnotation(annotation);
			}
			declaration.Modifiers = GetAccessorMethodModifiers(accessor);
			foreach (var attribute in accessorDecl.Attributes.ToArray())
			{
				attribute.Remove();
				declaration.Attributes.Add(attribute);
			}
			if (accessorDecl.Children.OfType<BlockStatement>().FirstOrDefault() is { } body)
			{
				body.Remove();
				declaration.Body = body;
			}
			return declaration;
		}

		static Modifiers GetAccessorMethodModifiers(IMethod accessor)
		{
			Modifiers modifiers = TypeSystemAstBuilder.ModifierFromAccessibility(accessor.Accessibility, usePrivateProtected: true);
			if (accessor.IsStatic)
				modifiers |= Modifiers.Static;
			if (accessor.IsAbstract)
				modifiers |= Modifiers.Abstract;
			else if (accessor.IsOverride)
				modifiers |= Modifiers.Override;
			else if (accessor.IsVirtual)
				modifiers |= Modifiers.Virtual;
			if (accessor.IsSealed && accessor.IsOverride)
				modifiers |= Modifiers.Sealed;
			return modifiers;
		}

		/// <summary>
		/// Returns whether the property takes arguments that C# cannot put on a property declaration.
		/// Visual Basic lets any property take them; C# has only the indexer, so such a property has
		/// no declaration form at all - rendering it as a parameterless one both loses the arguments
		/// and makes the accessor calls that carry them illegal (CS0571).
		/// </summary>
		internal static bool HasParametersCSharpCannotDeclare(IProperty property)
		{
			if (property.IsIndexer)
				return false;
			return property.Getter is { Parameters.Count: > 0 }
				|| property.Setter is { Parameters.Count: > 1 };
		}

		static bool IsAccessorInterfaceImplementationRuntimeHelper(PEFile module, MethodDefinitionHandle handle)
		{
			var metadata = module.Metadata;
			var method = metadata.GetMethodDefinition(handle);
			if ((method.Attributes & System.Reflection.MethodAttributes.Static) != 0)
				return false;
			string rawName = metadata.GetString(method.Name);
			int dot = rawName.LastIndexOf('.');
			if (dot < 0)
				return false;
			string name = rawName.Substring(dot + 1);
			if (handle.GetMethodImplementations(metadata).Length == 0)
				return false;
			if (method.RelativeVirtualAddress == 0)
				return false;
			if (!name.StartsWith("get_", StringComparison.Ordinal) &&
				!name.StartsWith("set_", StringComparison.Ordinal) &&
				!name.StartsWith("add_", StringComparison.Ordinal) &&
				!name.StartsWith("remove_", StringComparison.Ordinal) &&
				!name.StartsWith("raise_", StringComparison.Ordinal))
			{
				return false;
			}

			var signature = metadata.GetBlobReader(method.Signature);
			(int genericParameterCount, int parameterCount) = SignatureBlobComparer.ReadParameterCount(ref signature);
			if (genericParameterCount == -1 || parameterCount == -1)
				return false;
			signature.Reset();

			int maximumMethodSize = 4 * (parameterCount + 1) + 5 + 1;

			var body = module.Reader.GetMethodBody(method.RelativeVirtualAddress);
			var reader = body.GetILReader();

			if (reader.RemainingBytes > maximumMethodSize)
				return false;

			for (int i = 0; i < parameterCount + 1; i++)
			{
				int index;
				switch (reader.DecodeOpCode())
				{
					case ILOpCode.Ldarg:
						index = reader.ReadUInt16();
						if (index != i)
							return false;
						break;
					case ILOpCode.Ldarg_s:
						index = reader.ReadByte();
						if (index != i)
							return false;
						break;
					case ILOpCode.Ldarg_0:
						if (i != 0)
							return false;
						break;
					case ILOpCode.Ldarg_1:
						if (i != 1)
							return false;
						break;
					case ILOpCode.Ldarg_2:
						if (i != 2)
							return false;
						break;
					case ILOpCode.Ldarg_3:
						if (i != 3)
							return false;
						break;
					default:
						return false;
				}
			}

			if (reader.DecodeOpCode() != ILOpCode.Call)
				return false;

			EntityHandle targetHandle = MetadataTokenHelpers.EntityHandleOrNil(reader.ReadInt32());
			if (targetHandle.IsNil)
				return false;

			if (reader.DecodeOpCode() != ILOpCode.Ret)
				return false;

			if (reader.RemainingBytes != 0)
				return false;

			BlobReader signature2;
			string otherName;

			switch (targetHandle.Kind)
			{
				case HandleKind.MethodDefinition:
					if (genericParameterCount != 0)
						return false;
					var methodDef = metadata.GetMethodDefinition((MethodDefinitionHandle)targetHandle);
					signature2 = metadata.GetBlobReader(methodDef.Signature);
					otherName = metadata.GetString(methodDef.Name);
					break;
				case HandleKind.MethodSpecification:
					if (genericParameterCount == 0)
						return false;
					var methodSpec = metadata.GetMethodSpecification((MethodSpecificationHandle)targetHandle);
					var instantiationBlob = metadata.GetBlobReader(methodSpec.Signature);
					if (!IsIdentityInstantiation(ref instantiationBlob, genericParameterCount))
						return false;
					switch (methodSpec.Method.Kind)
					{
						case HandleKind.MethodDefinition:
							var methodSpecDef = metadata.GetMethodDefinition((MethodDefinitionHandle)methodSpec.Method);
							signature2 = metadata.GetBlobReader(methodSpecDef.Signature);
							otherName = metadata.GetString(methodSpecDef.Name);
							break;
						case HandleKind.MemberReference:
							var methodSpecRef = metadata.GetMemberReference((MemberReferenceHandle)methodSpec.Method);
							if (methodSpecRef.GetKind() != MemberReferenceKind.Method)
								return false;
							signature2 = metadata.GetBlobReader(methodSpecRef.Signature);
							otherName = metadata.GetString(methodSpecRef.Name);
							break;
						default:
							return false;
					}
					break;
				case HandleKind.MemberReference:
					if (genericParameterCount != 0)
						return false;
					var methodRef = metadata.GetMemberReference((MemberReferenceHandle)targetHandle);
					if (methodRef.GetKind() != MemberReferenceKind.Method)
						return false;
					signature2 = metadata.GetBlobReader(methodRef.Signature);
					otherName = metadata.GetString(methodRef.Name);
					break;
				default:
					return false;
			}

			if (otherName != name)
				return false;
			return SignatureBlobComparer.EqualsMethodSignature(signature, signature2, metadata, metadata, skipModifiers: true);

			static bool IsIdentityInstantiation(ref BlobReader reader, int expectedCount)
			{
				// Format: GENRICINST count type1 type2 ...
				if (reader.ReadByte() != 0x0A) // GENERICINST
					return false;
				if (!reader.TryReadCompressedInteger(out int count) || count != expectedCount)
					return false;
				for (int i = 0; i < count; i++)
				{
					if (reader.ReadByte() != 0x1E) // ELEMENT_TYPE_MVAR
						return false;
					if (!reader.TryReadCompressedInteger(out int index) || index != i)
						return false;
				}
				return true;
			}
		}

		static bool IsSwitchOnStringCache(SRM.FieldDefinition field, MetadataReader metadata)
		{
			return metadata.GetString(field.Name).StartsWith("<>f__switch", StringComparison.Ordinal);
		}

		static bool IsAnonymousMethodCacheField(SRM.FieldDefinition field, MetadataReader metadata)
		{
			var name = metadata.GetString(field.Name);
			return name.StartsWith("CS$<>", StringComparison.Ordinal) || name.StartsWith("<>f__am", StringComparison.Ordinal) || name.StartsWith("<>f__mg", StringComparison.Ordinal);
		}

		static bool IsClosureType(SRM.TypeDefinition type, MetadataReader metadata)
		{
			var name = metadata.GetString(type.Name);
			if (!type.Name.IsGeneratedName(metadata) || !type.IsCompilerGenerated(metadata))
				return false;
			if (name.Contains("DisplayClass") || name.Contains("AnonStorey") || name.Contains("Closure$"))
				return true;
			return type.BaseType.IsKnownType(metadata, KnownTypeCode.Object) && !type.GetInterfaceImplementations().Any();
		}

		internal static bool IsTransparentIdentifier(string identifier)
		{
			return identifier.StartsWith("<>", StringComparison.Ordinal)
				&& (identifier.Contains("TransparentIdentifier") || identifier.Contains("TranspIdent"));
		}
		#endregion

		#region NativeOrdering

		/// <summary>
		/// Determines whether a given type requires that its methods be ordered precisely as they were originally defined.
		/// </summary>
		/// <param name="typeDef">The type whose members may need native ordering.</param>
		internal bool RequiresNativeOrdering(ITypeDefinition typeDef)
		{
			// The main scenario for requiring the native method ordering is COM interop, where the V-table is fixed by the ABI
			return ComHelper.IsComImport(typeDef);
		}

		/// <summary>
		/// Compare handles with the method definition ordering intact by using the underlying method's MetadataToken,
		/// which is defined as the index into a given metadata table. This should equate to the original order that
		/// methods and properties were defined by the author.
		/// </summary>
		/// <param name="typeDef">The type whose members to order using their method's MetadataToken</param>
		/// <returns>A sequence of all members ordered by MetadataToken</returns>
		internal IEnumerable<IMember> GetMembersWithNativeOrdering(ITypeDefinition typeDef)
		{
			EntityHandle GetOrderingHandle(IMember member)
			{
				// Note! Technically COM interfaces could define property getters and setters out of order or interleaved with other
				// methods, but C# doesn't support this so we can't define it that way.

				if (member is IMethod)
					return member.MetadataToken;
				else if (member is IProperty property)
					return property.Getter?.MetadataToken ?? property.Setter?.MetadataToken ?? property.MetadataToken;
				else if (member is IEvent @event)
					return @event.AddAccessor?.MetadataToken ?? @event.RemoveAccessor?.MetadataToken ?? @event.InvokeAccessor?.MetadataToken ?? @event.MetadataToken;
				else
					return member.MetadataToken;
			}

			return typeDef.Fields.Concat<IMember>(typeDef.Properties).Concat(typeDef.Methods).Concat(typeDef.Events).OrderBy((member) => GetOrderingHandle(member), HandleComparer.Default);
		}

		bool RequiresSequentialFieldOrdering(ITypeDefinition typeDef)
		{
			if (typeDef.MetadataToken.Kind != HandleKind.TypeDefinition)
				return false;
			var type = module.MetadataFile.Metadata.GetTypeDefinition((TypeDefinitionHandle)typeDef.MetadataToken);
			return (type.Attributes & System.Reflection.TypeAttributes.LayoutMask)
				== System.Reflection.TypeAttributes.SequentialLayout;
		}

		IEnumerable<IMember> GetMembersWithSequentialFieldOrdering(ITypeDefinition typeDef)
		{
			var properties = typeDef.Properties.ToDictionary(property => property.MetadataToken);
			var events = typeDef.Events.ToDictionary(@event => @event.MetadataToken);
			var replacedFields = new HashSet<EntityHandle>();
			var fieldOrder = new List<IMember>();
			foreach (var field in typeDef.Fields)
			{
				IMember member = field;
				if (field.MetadataToken.Kind == HandleKind.FieldDefinition
					&& MemberIsHidden(module.MetadataFile, field.MetadataToken, settings))
				{
					var fieldHandle = (FieldDefinitionHandle)field.MetadataToken;
					if (module.MetadataFile.PropertyAndEventBackingFieldLookup.IsPropertyBackingField(fieldHandle, out var propertyHandle)
						&& properties.TryGetValue(propertyHandle, out var property))
					{
						member = property;
						replacedFields.Add(field.MetadataToken);
					}
					else if (module.MetadataFile.PropertyAndEventBackingFieldLookup.IsEventBackingField(fieldHandle, out var eventHandle)
						&& events.TryGetValue(eventHandle, out var @event))
					{
						member = @event;
						replacedFields.Add(field.MetadataToken);
					}
				}
				fieldOrder.Add(member);
			}

			var propertyOrder = typeDef.Properties.Cast<IMember>().ToList();
			var eventOrder = typeDef.Events.Cast<IMember>().ToList();
			var baselineOrder = typeDef.Fields.Where(field => !replacedFields.Contains(field.MetadataToken)).Cast<IMember>()
				.Concat(propertyOrder).Concat(eventOrder).Distinct().ToList();
			return MergeMemberOrders(baselineOrder, fieldOrder, propertyOrder, eventOrder);
		}

		internal static List<IMember> MergeMemberOrders(List<IMember> baselineOrder, params IReadOnlyList<IMember>[] constrainedOrders)
		{
			var nodes = baselineOrder.Concat(constrainedOrders.SelectMany(order => order)).Distinct().ToList();
			var successors = nodes.ToDictionary(member => member, _ => new HashSet<IMember>());
			var predecessorCount = nodes.ToDictionary(member => member, _ => 0);
			foreach (var order in constrainedOrders)
				AddConstraints(order);

			var emitted = new HashSet<IMember>();
			var orderedMembers = new List<IMember>(nodes.Count);
			while (orderedMembers.Count < nodes.Count)
			{
				IMember? next = nodes.FirstOrDefault(member => !emitted.Contains(member) && predecessorCount[member] == 0);
				if (next == null)
				{
					// Conflicting metadata orders cannot be represented in C#. The first
					// constraint is the caller's required fallback order.
					return constrainedOrders[0].Concat(nodes).Distinct().ToList();
				}
				emitted.Add(next);
				orderedMembers.Add(next);
				foreach (var successor in successors[next])
					predecessorCount[successor]--;
			}
			return orderedMembers;

			void AddConstraints(IReadOnlyList<IMember> members)
			{
				for (int i = 1; i < members.Count; i++)
				{
					var predecessor = members[i - 1];
					var successor = members[i];
					if (!predecessor.Equals(successor) && successors[predecessor].Add(successor))
						predecessorCount[successor]++;
				}
			}
		}

		#endregion

		static PEFile LoadPEFile(string fileName, DecompilerSettings settings)
		{
			settings.LoadInMemory = true;
			return new PEFile(
				fileName,
				new FileStream(fileName, FileMode.Open, FileAccess.Read),
				streamOptions: PEStreamOptions.PrefetchEntireImage,
				metadataOptions: settings.ApplyWindowsRuntimeProjections ? MetadataReaderOptions.ApplyWindowsRuntimeProjections : MetadataReaderOptions.None
			);
		}

		static DecompilerTypeSystem CreateTypeSystemFromFile(string fileName, DecompilerSettings settings)
		{
			settings.LoadInMemory = true;
			var file = LoadPEFile(fileName, settings);
			var resolver = new UniversalAssemblyResolver(fileName, settings.ThrowOnAssemblyResolveErrors,
				file.DetectTargetFrameworkId(), file.DetectRuntimePack(),
				settings.LoadInMemory ? PEStreamOptions.PrefetchMetadata : PEStreamOptions.Default,
				settings.ApplyWindowsRuntimeProjections ? MetadataReaderOptions.ApplyWindowsRuntimeProjections : MetadataReaderOptions.None);
			return new DecompilerTypeSystem(file, resolver, settings);
		}

		static TypeSystemAstBuilder CreateAstBuilder(DecompilerSettings settings)
		{
			var typeSystemAstBuilder = new TypeSystemAstBuilder();
			typeSystemAstBuilder.ShowAttributes = true;
			typeSystemAstBuilder.UsePrivateProtectedAccessibility = settings.IntroducePrivateProtectedAccessibility;
			typeSystemAstBuilder.SortAttributes = settings.SortCustomAttributes;
			typeSystemAstBuilder.AlwaysUseShortTypeNames = true;
			typeSystemAstBuilder.AddResolveResultAnnotations = true;
			typeSystemAstBuilder.UseNullableSpecifierForValueTypes = settings.LiftNullables;
			typeSystemAstBuilder.SupportInitAccessors = settings.InitAccessors;
			typeSystemAstBuilder.SupportRecordClasses = settings.RecordClasses;
			typeSystemAstBuilder.SupportRecordStructs = settings.RecordStructs;
			typeSystemAstBuilder.SupportUnsignedRightShift = settings.UnsignedRightShift;
			typeSystemAstBuilder.SupportOperatorChecked = settings.CheckedOperators;
			typeSystemAstBuilder.AlwaysUseGlobal = settings.AlwaysUseGlobal;
			typeSystemAstBuilder.SupportExtensionDeclarations = settings.ExtensionMembers;
			return typeSystemAstBuilder;
		}

		IDocumentationProvider? CreateDefaultDocumentationProvider()
		{
			try
			{
				return XmlDocLoader.LoadDocumentation(module.MetadataFile);
			}
			catch (System.Xml.XmlException)
			{
				return null;
			}
		}

		DecompileRun CreateDecompileRun(HashSet<string> namespaces)
		{
			List<INamespace> resolvedNamespaces = new List<INamespace>();
			foreach (var ns in namespaces)
			{
				var resolvedNamespace = typeSystem.GetNamespaceByFullName(ns);
				if (resolvedNamespace != null)
				{
					resolvedNamespaces.Add(resolvedNamespace);
				}
			}

			UsingScope usingScope = new UsingScope(
				new CSharpTypeResolveContext(typeSystem.MainModule),
				typeSystem.RootNamespace,
				resolvedNamespaces.ToImmutableArray()
			);

			return new DecompileRun(settings, usingScope) {
				DocumentationProvider = DocumentationProvider ?? CreateDefaultDocumentationProvider(),
				CancellationToken = CancellationToken,
				Namespaces = namespaces
			};
		}

		void RunTransforms(AstNode rootNode, DecompileRun decompileRun, ITypeResolveContext decompilationContext)
		{
			var typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
			var context = new TransformContext(typeSystem, decompileRun, decompilationContext, typeSystemAstBuilder) {
				Stepper = Stepper
			};
			// The tree handed to the pipeline must already be well-formed; check it once up front so a
			// malformed builder output is caught here rather than blamed on the first transform (DEBUG only).
			rootNode.CheckInvariant();
			bool traceTransforms = DecompilerEventSource.Log.IsTransformTracingEnabled();
			try
			{
				foreach (var transform in astTransforms)
				{
					CancellationToken.ThrowIfCancellationRequested();
					context.StepStartGroup(transform.GetType().Name);
					long traceStart = traceTransforms ? Stopwatch.GetTimestamp() : 0;
					transform.Run(rootNode, context);
					if (traceTransforms)
						DecompilerEventSource.Log.AstTransformExecuted(transform, traceStart);
					// Verify the slot structure survived the transform (DEBUG only); mirrors the IL
					// pipeline's per-transform ILInstruction.CheckInvariant.
					rootNode.CheckInvariant();
					context.StepEndGroup(keepIfEmpty: true);
				}
			}
			catch (StepLimitReachedException)
			{
			}
			CancellationToken.ThrowIfCancellationRequested();
			rootNode.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
			CancellationToken.ThrowIfCancellationRequested();
			GenericGrammarAmbiguityVisitor.ResolveAmbiguities(rootNode);
		}

		string SyntaxTreeToString(SyntaxTree syntaxTree)
		{
			StringWriter w = new StringWriter();
			syntaxTree.AcceptVisitor(new CSharpOutputVisitor(w, settings.CSharpFormattingOptions));
			return w.ToString();
		}

		/// <summary>
		/// Decompile assembly and module attributes.
		/// </summary>
		public SyntaxTree DecompileModuleAndAssemblyAttributes()
		{
			var decompilationContext = new SimpleTypeResolveContext(typeSystem.MainModule);
			var namespaces = new HashSet<string>();
			syntaxTree = new SyntaxTree();
			RequiredNamespaceCollector.CollectAttributeNamespaces(module, namespaces);
			DecompileRun decompileRun = CreateDecompileRun(namespaces);
			DoDecompileModuleAndAssemblyAttributes(decompileRun, decompilationContext, syntaxTree);
			RunTransforms(syntaxTree, decompileRun, decompilationContext);
			return syntaxTree;
		}

		/// <summary>
		/// Decompile assembly and module attributes.
		/// </summary>
		public string DecompileModuleAndAssemblyAttributesToString()
		{
			return SyntaxTreeToString(DecompileModuleAndAssemblyAttributes());
		}

		void DoDecompileModuleAndAssemblyAttributes(DecompileRun decompileRun, ITypeResolveContext decompilationContext, SyntaxTree syntaxTree)
		{
			try
			{
				foreach (var a in typeSystem.MainModule.GetAssemblyAttributes())
				{
					var astBuilder = CreateAstBuilder(decompileRun.Settings);
					var attrSection = new AttributeSection(astBuilder.ConvertAttribute(a));
					attrSection.AttributeTarget = "assembly";
					syntaxTree.Members.Add(attrSection);
				}
				foreach (var a in typeSystem.MainModule.GetModuleAttributes())
				{
					var astBuilder = CreateAstBuilder(decompileRun.Settings);
					var attrSection = new AttributeSection(astBuilder.ConvertAttribute(a));
					attrSection.AttributeTarget = "module";
					syntaxTree.Members.Add(attrSection);
				}
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, null, innerException, "Error decompiling module and assembly attributes of " + module.AssemblyName);
			}
		}

		void DoDecompileTypes(IEnumerable<TypeDefinitionHandle> types, DecompileRun decompileRun, ITypeResolveContext decompilationContext, SyntaxTree syntaxTree)
		{
			string? currentNamespace = null;
			AstNode? groupNode = null;
			foreach (var typeDefHandle in types)
			{
				var typeDef = module.GetDefinition(typeDefHandle);
				if (typeDef.Name == "<Module>" && typeDef.Members.Count == 0)
					continue;
				if (MemberIsHidden(module.MetadataFile, typeDefHandle, settings))
					continue;
				if (string.IsNullOrEmpty(typeDef.Namespace))
				{
					groupNode = syntaxTree;
				}
				else
				{
					if (currentNamespace != typeDef.Namespace)
					{
						groupNode = new NamespaceDeclaration(typeDef.Namespace);
						syntaxTree.Members.Add(groupNode);
					}
				}
				currentNamespace = typeDef.Namespace;
				var typeDecl = DoDecompile(typeDef, decompileRun, decompilationContext.WithCurrentTypeDefinition(typeDef));
				groupNode!.AddChild(typeDecl, Slots.Member);
			}
		}

		/// <summary>
		/// Decompiles the whole module into a single syntax tree.
		/// </summary>
		public SyntaxTree DecompileWholeModuleAsSingleFile()
		{
			return DecompileWholeModuleAsSingleFile(false);
		}

		/// <summary>
		/// Decompiles the whole module into a single syntax tree.
		/// </summary>
		/// <param name="sortTypes">If true, top-level-types are emitted sorted by namespace/name.
		/// If false, types are emitted in metadata order.</param>
		public SyntaxTree DecompileWholeModuleAsSingleFile(bool sortTypes)
		{
			var decompilationContext = new SimpleTypeResolveContext(typeSystem.MainModule);
			syntaxTree = new SyntaxTree();
			var namespaces = new HashSet<string>();
			RequiredNamespaceCollector.CollectNamespaces(module, namespaces);
			var decompileRun = CreateDecompileRun(namespaces);
			DoDecompileModuleAndAssemblyAttributes(decompileRun, decompilationContext, syntaxTree);
			var typeDefs = metadata.GetTopLevelTypeDefinitions();
			if (sortTypes)
			{
				typeDefs = typeDefs.OrderBy(td => {
					var typeDef = module.metadata.GetTypeDefinition(td);
					return (module.metadata.GetString(typeDef.Namespace), module.metadata.GetString(typeDef.Name));
				});
			}
			DoDecompileTypes(typeDefs, decompileRun, decompilationContext, syntaxTree);
			RunTransforms(syntaxTree, decompileRun, decompilationContext);
			return syntaxTree;
		}

		/// <summary>
		/// Creates an <see cref="ILTransformContext"/> for the given <paramref name="function"/>.
		/// </summary>
		public ILTransformContext CreateILTransformContext(ILFunction function)
		{
			var namespaces = new HashSet<string>();
			RequiredNamespaceCollector.CollectNamespaces(function.Method, module, namespaces);
			var decompileRun = CreateDecompileRun(namespaces);
			return new ILTransformContext(function, typeSystem, DebugInfoProvider, settings) {
				CancellationToken = CancellationToken,
				DecompileRun = decompileRun
			};
		}

		/// <summary>
		/// Determines the "code-mappings" for a given TypeDef or MethodDef. See <see cref="CodeMappingInfo"/> for more information.
		/// </summary>
		public static CodeMappingInfo GetCodeMappingInfo(MetadataFile module, EntityHandle member)
		{
			var declaringType = (TypeDefinitionHandle)member.GetDeclaringType(module.Metadata);

			if (declaringType.IsNil && member.Kind == HandleKind.TypeDefinition)
			{
				declaringType = (TypeDefinitionHandle)member;
			}

			var info = new CodeMappingInfo(module, declaringType);

			var td = module.Metadata.GetTypeDefinition(declaringType);

			foreach (var method in td.GetMethods())
			{
				var parent = method;
				var part = method;

				var connectedMethods = new Queue<MethodDefinitionHandle>();
				var processedMethods = new HashSet<MethodDefinitionHandle>();
				var processedNestedTypes = new HashSet<TypeDefinitionHandle>();
				connectedMethods.Enqueue(part);

				while (connectedMethods.Count > 0)
				{
					part = connectedMethods.Dequeue();
					if (!processedMethods.Add(part))
						continue;
					try
					{
						if (TryGetExtensionImplementation(module.Metadata, part, out var impl))
						{
							connectedMethods.Enqueue(impl);
						}

						ReadCodeMappingInfo(module, info, parent, part, connectedMethods, processedNestedTypes);
					}
					catch (BadImageFormatException)
					{
						// ignore invalid IL
					}
				}
			}

			return info;
		}

		private static void ReadCodeMappingInfo(MetadataFile module, CodeMappingInfo info, MethodDefinitionHandle parent, MethodDefinitionHandle part, Queue<MethodDefinitionHandle> connectedMethods, HashSet<TypeDefinitionHandle> processedNestedTypes)
		{
			var md = module.Metadata.GetMethodDefinition(part);

			if (!md.HasBody())
			{
				info.AddMapping(parent, part);
				return;
			}

			var declaringType = md.GetDeclaringType();

			var blob = module.GetMethodBody(md.RelativeVirtualAddress).GetILReader();
			while (blob.RemainingBytes > 0)
			{
				var code = blob.DecodeOpCode();
				switch (code)
				{
					case ILOpCode.Newobj:
					case ILOpCode.Stfld:
						// async and yield fsms:
						var token = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
						if (token.IsNil)
							continue;
						TypeDefinitionHandle fsmTypeDef;
						switch (token.Kind)
						{
							case HandleKind.MethodDefinition:
								var fsmMethod = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)token);
								fsmTypeDef = fsmMethod.GetDeclaringType();
								break;
							case HandleKind.FieldDefinition:
								var fsmField = module.Metadata.GetFieldDefinition((FieldDefinitionHandle)token);
								fsmTypeDef = fsmField.GetDeclaringType();
								break;
							case HandleKind.MemberReference:
								var memberRef = module.Metadata.GetMemberReference((MemberReferenceHandle)token);
								fsmTypeDef = ExtractDeclaringType(memberRef);
								break;
							default:
								continue;
						}
						if (!fsmTypeDef.IsNil)
						{
							var fsmType = module.Metadata.GetTypeDefinition(fsmTypeDef);
							// Must be a nested type of the containing type.
							if (fsmType.GetDeclaringType() != declaringType)
								break;
							if (YieldReturnDecompiler.IsCompilerGeneratorEnumerator(fsmTypeDef, module.Metadata)
								|| AsyncAwaitDecompiler.IsCompilerGeneratedStateMachine(fsmTypeDef, module.Metadata))
							{
								if (!processedNestedTypes.Add(fsmTypeDef))
									break;
								foreach (var h in fsmType.GetMethods())
								{
									if (module.MethodSemanticsLookup.GetSemantics(h).Item2 != 0)
										continue;
									var otherMethod = module.Metadata.GetMethodDefinition(h);
									if (!otherMethod.GetCustomAttributes().HasKnownAttribute(module.Metadata, KnownAttribute.DebuggerHidden))
									{
										connectedMethods.Enqueue(h);
									}
								}
							}
						}
						break;
					case ILOpCode.Ldftn:
						// deal with ldftn instructions, i.e., lambdas
						token = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
						if (token.IsNil)
							continue;
						TypeDefinitionHandle closureTypeHandle;
						switch (token.Kind)
						{
							case HandleKind.MethodDefinition:
								if (((MethodDefinitionHandle)token).IsCompilerGeneratedOrIsInCompilerGeneratedClass(module.Metadata))
								{
									connectedMethods.Enqueue((MethodDefinitionHandle)token);
								}
								continue;
							case HandleKind.MemberReference:
								var memberRef = module.Metadata.GetMemberReference((MemberReferenceHandle)token);
								if (memberRef.GetKind() != MemberReferenceKind.Method)
									continue;
								closureTypeHandle = ExtractDeclaringType(memberRef);
								if (!closureTypeHandle.IsNil)
								{
									var closureType = module.Metadata.GetTypeDefinition(closureTypeHandle);
									if (closureTypeHandle != declaringType)
									{
										// Must be a nested type of the containing type.
										if (closureType.GetDeclaringType() != declaringType)
											break;
										if (!processedNestedTypes.Add(closureTypeHandle))
											break;
										foreach (var m in closureType.GetMethods())
										{
											connectedMethods.Enqueue(m);
										}
									}
									else
									{
										// Delegate body is declared in the same type
										foreach (var m in closureType.GetMethods())
										{
											var methodDef = module.Metadata.GetMethodDefinition(m);
											if (methodDef.Name == memberRef.Name && m.IsCompilerGeneratedOrIsInCompilerGeneratedClass(module.Metadata))
												connectedMethods.Enqueue(m);
										}
									}
									break;
								}
								break;
							default:
								continue;
						}
						break;
					case ILOpCode.Call:
					case ILOpCode.Callvirt:
						// deal with call/callvirt instructions, i.e., local function invocations
						token = MetadataTokenHelpers.EntityHandleOrNil(blob.ReadInt32());
						if (token.IsNil)
							continue;
						switch (token.Kind)
						{
							case HandleKind.MethodDefinition:
								break;
							case HandleKind.MethodSpecification:
								var methodSpec = module.Metadata.GetMethodSpecification((MethodSpecificationHandle)token);
								if (methodSpec.Method.IsNil || methodSpec.Method.Kind != HandleKind.MethodDefinition)
									continue;
								token = methodSpec.Method;
								break;
							default:
								continue;
						}
						if (LocalFunctionDecompiler.IsLocalFunctionMethod(module, (MethodDefinitionHandle)token))
						{
							connectedMethods.Enqueue((MethodDefinitionHandle)token);
						}
						break;
					default:
						blob.SkipOperand(code);
						break;
				}
			}

			info.AddMapping(parent, part);

			TypeDefinitionHandle ExtractDeclaringType(MemberReference memberRef)
			{
				switch (memberRef.Parent.Kind)
				{
					case HandleKind.TypeReference:
						// This should never happen in normal code, because we are looking at nested types
						// If it's not a nested type, it can't be a reference to the state machine or lambda anyway, and
						// those should be either TypeDef or TypeSpec.
						return default;
					case HandleKind.TypeDefinition:
						return (TypeDefinitionHandle)memberRef.Parent;
					case HandleKind.TypeSpecification:
						var ts = module.Metadata.GetTypeSpecification((TypeSpecificationHandle)memberRef.Parent);
						// Only read the generic type, ignore the type arguments
						var genericType = ts.GetGenericType(module.Metadata);
						// Again, we assume this is a type def, because we are only looking at nested types
						if (genericType.Kind != HandleKind.TypeDefinition)
							return default;
						return (TypeDefinitionHandle)genericType;
				}
				return default;
			}
		}

		private static bool TryGetExtensionImplementation(MetadataReader metadata, MethodDefinitionHandle definitionPart, out MethodDefinitionHandle implementationPart)
		{
			implementationPart = default;

			var def = metadata.GetMethodDefinition(definitionPart);
			var declTypeHandle = def.GetDeclaringType();
			var declType = metadata.GetTypeDefinition(declTypeHandle);
			var name = metadata.GetString(def.Name);
			var containerHandle = declType.GetDeclaringType();

			if (containerHandle.IsNil)
				return false;

			if (metadata.StringComparer.StartsWith(declType.Name, "<>E__") || metadata.StringComparer.StartsWith(declType.Name, "<G>$"))
			{
				implementationPart = FindImplementations(metadata.GetTypeDefinition(containerHandle).GetMethods());
			}
			else if (metadata.StringComparer.StartsWith(declType.Name, "<M>$"))
			{
				var container = metadata.GetTypeDefinition(containerHandle);
				var groupHandle = container.GetDeclaringType();
				if (groupHandle.IsNil)
					return false;

				implementationPart = FindImplementations(metadata.GetTypeDefinition(groupHandle).GetMethods());
			}
			else
			{
				return false;
			}

			return !implementationPart.IsNil;

			MethodDefinitionHandle FindImplementations(MethodDefinitionHandleCollection methods)
			{
				foreach (var h in methods)
				{
					var m = metadata.GetMethodDefinition(h);
					if (!metadata.StringComparer.Equals(m.Name, name))
						continue;

					// TODO : use SignatureBlobComparer to ensure that the correct method is resolved

					return h;
				}
				return default;
			}
		}

		/// <summary>
		/// Decompiles the whole module into a single string.
		/// </summary>
		public string DecompileWholeModuleAsString()
		{
			return SyntaxTreeToString(DecompileWholeModuleAsSingleFile());
		}

		/// <summary>
		/// Decompile the given types.
		/// </summary>
		/// <remarks>
		/// Unlike Decompile(IMemberDefinition[]), this method will add namespace declarations around the type definitions.
		/// </remarks>
		public SyntaxTree DecompileTypes(IEnumerable<TypeDefinitionHandle> types)
		{
			if (types == null)
				throw new ArgumentNullException(nameof(types));
			var decompilationContext = new SimpleTypeResolveContext(typeSystem.MainModule);
			syntaxTree = new SyntaxTree();
			var namespaces = new HashSet<string>();
			foreach (var type in types)
			{
				CancellationToken.ThrowIfCancellationRequested();
				if (type.IsNil)
					throw new ArgumentException("types contains null element");
				RequiredNamespaceCollector.CollectNamespaces(type, module, namespaces);
			}

			var decompileRun = CreateDecompileRun(namespaces);
			DoDecompileTypes(types, decompileRun, decompilationContext, syntaxTree);
			RunTransforms(syntaxTree, decompileRun, decompilationContext);
			return syntaxTree;
		}

		/// <summary>
		/// Decompile the given types.
		/// </summary>
		/// <remarks>
		/// Unlike Decompile(IMemberDefinition[]), this method will add namespace declarations around the type definitions.
		/// </remarks>
		public string DecompileTypesAsString(IEnumerable<TypeDefinitionHandle> types)
		{
			return SyntaxTreeToString(DecompileTypes(types));
		}

		/// <summary>
		/// Decompile the given type.
		/// </summary>
		/// <remarks>
		/// Unlike Decompile(IMemberDefinition[]), this method will add namespace declarations around the type definition.
		/// Note that decompiling types from modules other than the main module is not supported.
		/// </remarks>
		public SyntaxTree DecompileType(FullTypeName fullTypeName)
		{
			var type = typeSystem.FindType(fullTypeName.TopLevelTypeName).GetDefinition();
			if (type == null)
				throw new InvalidOperationException($"Could not find type definition {fullTypeName} in type system.");
			if (type.ParentModule != typeSystem.MainModule)
				throw new NotSupportedException($"Type {fullTypeName} was not found in the module being decompiled, but only in {type.ParentModule!.Name}");
			var decompilationContext = new SimpleTypeResolveContext(typeSystem.MainModule);
			var namespaces = new HashSet<string>();
			syntaxTree = new SyntaxTree();
			RequiredNamespaceCollector.CollectNamespaces(type.MetadataToken, module, namespaces);
			var decompileRun = CreateDecompileRun(namespaces);
			DoDecompileTypes(new[] { (TypeDefinitionHandle)type.MetadataToken }, decompileRun, decompilationContext, syntaxTree);
			RunTransforms(syntaxTree, decompileRun, decompilationContext);
			return syntaxTree;
		}

		/// <summary>
		/// Decompile the given type.
		/// </summary>
		/// <remarks>
		/// Unlike Decompile(IMemberDefinition[]), this method will add namespace declarations around the type definition.
		/// </remarks>
		public string DecompileTypeAsString(FullTypeName fullTypeName)
		{
			return SyntaxTreeToString(DecompileType(fullTypeName));
		}

		/// <summary>
		/// Decompile the specified types and/or members.
		/// </summary>
		public SyntaxTree Decompile(params EntityHandle[] definitions)
		{
			return Decompile((IEnumerable<EntityHandle>)definitions);
		}

		/// <summary>
		/// Decompile the specified types and/or members.
		/// </summary>
		public SyntaxTree Decompile(IEnumerable<EntityHandle> definitions)
		{
			if (definitions == null)
				throw new ArgumentNullException(nameof(definitions));
			syntaxTree = new SyntaxTree();
			var namespaces = new HashSet<string>();
			foreach (var entity in definitions)
			{
				if (entity.IsNil)
					throw new ArgumentException("definitions contains null element");
				RequiredNamespaceCollector.CollectNamespaces(entity, module, namespaces);
			}
			var decompileRun = CreateDecompileRun(namespaces);

			bool first = true;
			ITypeDefinition? parentTypeDef = null;
			ExtensionInfo? parentExtensionInfo = null;

			foreach (var entity in definitions)
			{
				switch (entity.Kind)
				{
					case HandleKind.TypeDefinition:
						ITypeDefinition typeDef = module.GetDefinition((TypeDefinitionHandle)entity);
						syntaxTree.Members.Add(DoDecompile(typeDef, decompileRun, new SimpleTypeResolveContext(typeDef)));
						if (first)
						{
							parentTypeDef = typeDef.DeclaringTypeDefinition;
						}
						else if (parentTypeDef != null)
						{
							parentTypeDef = FindCommonDeclaringTypeDefinition(parentTypeDef, typeDef.DeclaringTypeDefinition);
						}
						break;
					case HandleKind.MethodDefinition:
						IMethod method = module.GetDefinition((MethodDefinitionHandle)entity);
						parentExtensionInfo = method.ResolveExtensionInfo();
						syntaxTree.Members.Add(DoDecompile(method, decompileRun, new SimpleTypeResolveContext(method), parentExtensionInfo));
						if (first)
						{
							parentTypeDef = method.DeclaringTypeDefinition;
						}
						else if (parentTypeDef != null)
						{
							parentTypeDef = FindCommonDeclaringTypeDefinition(parentTypeDef, method.DeclaringTypeDefinition);
						}
						break;
					case HandleKind.FieldDefinition:
						IField field = module.GetDefinition((FieldDefinitionHandle)entity);
						syntaxTree.Members.Add(DoDecompile(field, decompileRun, new SimpleTypeResolveContext(field)));
						parentTypeDef = field.DeclaringTypeDefinition;
						break;
					case HandleKind.PropertyDefinition:
						IProperty property = module.GetDefinition((PropertyDefinitionHandle)entity);
						parentExtensionInfo = property.ResolveExtensionInfo();
						syntaxTree.Members.Add(DoDecompile(property, decompileRun, new SimpleTypeResolveContext(property), parentExtensionInfo));
						if (first)
						{
							parentTypeDef = property.DeclaringTypeDefinition;
						}
						else if (parentTypeDef != null)
						{
							parentTypeDef = FindCommonDeclaringTypeDefinition(parentTypeDef, property.DeclaringTypeDefinition);
						}
						break;
					case HandleKind.EventDefinition:
						IEvent ev = module.GetDefinition((EventDefinitionHandle)entity);
						syntaxTree.Members.Add(DoDecompile(ev, decompileRun, new SimpleTypeResolveContext(ev)));
						if (first)
						{
							parentTypeDef = ev.DeclaringTypeDefinition;
						}
						else if (parentTypeDef != null)
						{
							parentTypeDef = FindCommonDeclaringTypeDefinition(parentTypeDef, ev.DeclaringTypeDefinition);
						}
						break;
					default:
						throw new NotSupportedException(entity.Kind.ToString());
				}
				first = false;
			}
			RunTransforms(syntaxTree, decompileRun, parentTypeDef != null ? new SimpleTypeResolveContext(parentTypeDef) : new SimpleTypeResolveContext(typeSystem.MainModule));
			return syntaxTree;
		}

		public SyntaxTree DecompileExtension(EntityHandle handle)
		{
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			syntaxTree = new SyntaxTree();
			var namespaces = new HashSet<string>();
			RequiredNamespaceCollector.CollectNamespaces(handle, module, namespaces);
			var decompileRun = CreateDecompileRun(namespaces);

			switch (handle.Kind)
			{
				case HandleKind.TypeDefinition:
					ITypeDefinition typeDef = module.GetDefinition((TypeDefinitionHandle)handle);
					syntaxTree.Members.Add(DoDecompile(typeDef, decompileRun, new SimpleTypeResolveContext(typeDef), asExtension: true));
					RunTransforms(syntaxTree, decompileRun, new SimpleTypeResolveContext(typeDef));
					break;
				case HandleKind.MethodDefinition:
					IMethod methodDef = module.GetDefinition((MethodDefinitionHandle)handle);
					var extensionInfo = methodDef.ResolveExtensionInfo();
					Debug.Assert(extensionInfo != null);
					var memberInfo = extensionInfo.InfoOfExtensionMember((IMethod)methodDef.MemberDefinition).GetValueOrDefault();
					var subst = new TypeParameterSubstitution(memberInfo.ExtensionGroupingTypeParameters, null);
					methodDef = methodDef.Specialize(subst);
					EntityDeclaration entity = DoDecompile(methodDef, decompileRun, new SimpleTypeResolveContext(methodDef), extensionInfo);
					syntaxTree.Members.Add(entity);
					RemoveAttribute(entity, KnownAttribute.ExtensionMarker);
					RunTransforms(syntaxTree, decompileRun, new SimpleTypeResolveContext(methodDef.DeclaringTypeDefinition));
					break;
				case HandleKind.PropertyDefinition:
					IProperty propDef = module.GetDefinition((PropertyDefinitionHandle)handle);
					extensionInfo = propDef.ResolveExtensionInfo();
					Debug.Assert(extensionInfo != null);
					var accessor = propDef.Getter ?? propDef.Setter;
					memberInfo = extensionInfo.InfoOfExtensionMember((IMethod)accessor!.MemberDefinition).GetValueOrDefault();
					subst = new TypeParameterSubstitution(memberInfo.ExtensionGroupingTypeParameters, null);
					propDef = (IProperty)propDef.Specialize(subst);
					EntityDeclaration prop = DoDecompile(propDef, decompileRun, new SimpleTypeResolveContext(propDef), extensionInfo);
					syntaxTree.Members.Add(prop);
					RemoveAttribute(prop, KnownAttribute.ExtensionMarker);
					if (propDef.Getter != null)
					{
						RemoveAttribute(prop.GetChild(Slots.Getter)!, KnownAttribute.ExtensionMarker);
					}
					if (propDef.Setter != null)
					{
						RemoveAttribute(prop.GetChild(Slots.Setter)!, KnownAttribute.ExtensionMarker);
					}
					RunTransforms(syntaxTree, decompileRun, new SimpleTypeResolveContext(propDef.DeclaringTypeDefinition));
					break;
				default:
					throw new NotSupportedException($"HandleKind {handle.Kind} is not supported!");
			}
			return syntaxTree;
		}

		ITypeDefinition? FindCommonDeclaringTypeDefinition(ITypeDefinition? a, ITypeDefinition? b)
		{
			if (a == null || b == null)
				return null;
			var declaringTypes = a.GetDeclaringTypeDefinitions();
			var set = new HashSet<ITypeDefinition>(b.GetDeclaringTypeDefinitions());
			return declaringTypes.FirstOrDefault(set.Contains);
		}

		/// <summary>
		/// Decompile the specified types and/or members.
		/// </summary>
		public string DecompileAsString(params EntityHandle[] definitions)
		{
			return SyntaxTreeToString(Decompile(definitions));
		}

		/// <summary>
		/// Decompile the specified types and/or members.
		/// </summary>
		public string DecompileAsString(IEnumerable<EntityHandle> definitions)
		{
			return SyntaxTreeToString(Decompile(definitions));
		}

		readonly Dictionary<TypeDefinitionHandle, PartialTypeInfo> partialTypes = new();

		public void AddPartialTypeDefinition(PartialTypeInfo info)
		{
			if (!partialTypes.TryGetValue(info.DeclaringTypeDefinitionHandle, out var existingInfo))
			{
				partialTypes.Add(info.DeclaringTypeDefinitionHandle, info);
			}
			else
			{
				existingInfo.AddDeclaredMembers(info);
			}
		}

		IEnumerable<EntityDeclaration> AddInterfaceImplHelpers(
			EntityDeclaration memberDecl, IMethod method,
			TypeSystemAstBuilder astBuilder)
		{
			if (memberDecl.GetChild(Slots.PrivateImplementationType) is not null)
			{
				yield break; // cannot create forwarder for existing explicit interface impl
			}
			if (method.IsStatic)
			{
				yield break; // cannot create forwarder for static interface impl
			}
			if (memberDecl.HasModifier(Modifiers.Extern))
			{
				yield break; // cannot create forwarder for extern method
			}
			var handledAccessorOwners = new HashSet<IMember>();
			foreach (IMethod m in GetInterfaceAccessorImplementations(method))
			{
				if (m.IsAccessor && m.AccessorOwner is IMember accessorOwner && accessorOwner is IProperty or IEvent)
				{
					// A CLR MethodImpl can map an ordinary method name to an interface accessor. Emitting
					// IInterface.get_Property() as a C# method is illegal (CS0683); retain the ordinary
					// method and add one grouped explicit property/event forwarder instead.
					if (handledAccessorOwners.Add(accessorOwner))
					{
						var helper = CreateInterfaceAccessorImplHelper(method, accessorOwner, astBuilder);
						if (helper != null)
							yield return helper;
					}
					continue;
				}
				var methodDecl = new MethodDeclaration();
				// EntityDeclaration.ReturnType is typed non-null but its getter yields null when the Type
				// slot is empty; leave the forwarder's (already empty) return-type slot untouched in that case.
				if (memberDecl.ReturnType is { } memberReturnType)
				{
					methodDecl.ReturnType = memberReturnType.Clone();
					RemoveTupleElementNames(methodDecl.ReturnType);
				}
				methodDecl.PrivateImplementationType = astBuilder.ConvertType(m.DeclaringType);
				methodDecl.Name = m.Name;
				methodDecl.TypeParameters.AddRange(memberDecl.GetChildren(Slots.TypeParameter)
												   .Select(n => (TypeParameterDeclaration)n.Clone()));
				foreach (ParameterDeclaration parameter in memberDecl.GetChildren(Slots.Parameter))
				{
					var clone = (ParameterDeclaration)parameter.Clone();
					// Calls to an explicit implementation use the interface declaration's params behavior.
					// Retaining it on this synthetic bridge would invent a ParamArrayAttribute row.
					clone.IsParams = false;
					// This bridge has no counterpart in the original metadata. Retaining tuple names from the
					// ordinary implementation would make Roslyn invent TupleElementNamesAttribute rows for it.
					if (clone.Type is { } parameterType)
						RemoveTupleElementNames(parameterType);
					// Explicit interface implementations cannot be called with omitted arguments. Copying an
					// optional default onto this synthetic bridge also invents a Constant row and causes CS1066.
					clone.DefaultExpression = null;
					methodDecl.Parameters.Add(clone);
				}
				// Constraints are not copied because explicit interface implementations cannot have constraints. CS0460

				methodDecl.Body = new BlockStatement();
				var commentStatement = new EmptyStatement();
				commentStatement.AddTrailingTrivia(new Comment(
					"ILSpy generated this explicit interface implementation from .override directive in " + memberDecl.Name));
				methodDecl.Body.Add(commentStatement);
				var forwardingTarget = new MemberReferenceExpression(new ThisReferenceExpression(), memberDecl.Name,
					methodDecl.TypeParameters.Select(tp => new SimpleType(tp.Name)));
				forwardingTarget.AddAnnotation(new MemberResolveResult(new ThisResolveResult(method.DeclaringType), method));
				var forwardingCall = new InvocationExpression(forwardingTarget,
					methodDecl.Parameters.Select(ForwardParameter)
				);
				if (m.ReturnType.IsKnownType(KnownTypeCode.Void))
				{
					methodDecl.Body.Add(new ExpressionStatement(forwardingCall));
				}
				else
				{
					methodDecl.Body.Add(new ReturnStatement(forwardingCall));
				}
				yield return methodDecl;
			}
		}

		static void RemoveTupleElementNames(AstType type)
		{
			foreach (var element in type.DescendantsAndSelf.OfType<TupleTypeElement>())
				element.Name = null;
		}

		IEnumerable<IMethod> GetInterfaceMethodImplementations(IMethod method)
		{
			// Synthesized members (for example, the implicit parameterless constructor of a
			// generic struct) do not have a MethodDef row and therefore cannot own MethodImpls.
			if (method.MetadataToken.Kind != HandleKind.MethodDefinition)
				yield break;
			var genericContext = new Decompiler.TypeSystem.GenericContext(method);
			var methodHandle = (MethodDefinitionHandle)method.MetadataToken;
			foreach (var h in methodHandle.GetMethodImplementations(metadata))
			{
				var methodImpl = metadata.GetMethodImplementation(h);
				IMethod? declaration = module.ResolveMethod(methodImpl.MethodDeclaration, genericContext);
				if (declaration?.DeclaringType.Kind == TypeKind.Interface)
					yield return declaration;
			}
		}

		IEnumerable<IMethod> GetInterfaceAccessorImplementations(IMethod method)
		{
			foreach (IMethod declaration in GetInterfaceMethodImplementations(method))
				yield return declaration;
			foreach (IMethod declaration in GetImplicitInterfaceAccessorImplementations(method))
				yield return declaration;
		}

		IEnumerable<IMethod> GetImplicitInterfaceAccessorImplementations(IMethod method)
		{
			return GetImplicitInterfaceImplementations(method, interfaceAccessors: true);
		}

		IEnumerable<IMethod> GetInterfaceOrdinaryMethodImplementations(IMethod method)
		{
			foreach (IMethod declaration in GetInterfaceMethodImplementations(method))
			{
				if (!declaration.IsAccessor)
					yield return declaration;
			}
			foreach (IMethod declaration in GetImplicitInterfaceImplementations(method, interfaceAccessors: false))
				yield return declaration;
		}

		IEnumerable<IMethod> GetImplicitInterfaceImplementations(IMethod method, bool interfaceAccessors)
		{
			// CLR interface mapping is based on method name/signature, independently of Property/Event
			// metadata rows and the SpecialName flag. Thus ordinary methods can fill accessor slots and
			// accessors can fill ordinary-method slots, while C# rejects those mappings (CS0470/CS0686).
			if (method.IsStatic
				|| method.Accessibility != Accessibility.Public || method.IsExplicitInterfaceImplementation)
			{
				yield break;
			}
			if (interfaceAccessors ? method.SymbolKind != SymbolKind.Method : method.SymbolKind != SymbolKind.Accessor)
				yield break;
			var methodDefinition = metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
			if ((methodDefinition.Attributes & System.Reflection.MethodAttributes.Virtual) == 0)
				yield break;
			if (method.DeclaringTypeDefinition is not { } declaringType)
				yield break;
			if (declaringType.Kind == TypeKind.Interface)
			{
				// Interface mapping exists only for classes and structs: a member of an interface never
				// implicitly implements a member of a base interface, however well the names and
				// signatures line up. A default interface method can implement one only through an
				// explicit MethodImpl, which GetInterfaceMethodImplementations already reports.
				// Tlbimp-generated interop assemblies make the shape common, because a derived interface
				// re-declares every inherited member, and a parameterized property of the base interface
				// turns into a plain method carrying the accessor name.
				yield break;
			}

			var seen = new HashSet<IMethod>();
			foreach (IType directInterface in declaringType.DirectBaseTypes.Where(t => t.Kind == TypeKind.Interface))
			{
				foreach (IType interfaceType in directInterface.GetAllBaseTypes().Where(t => t.Kind == TypeKind.Interface))
				{
					IEnumerable<IMethod> candidates = interfaceAccessors
						? interfaceType.GetAccessors(a => a.Name == method.Name, GetMemberOptions.IgnoreInheritedMembers)
						: interfaceType.GetMethods(m => m.Name == method.Name, GetMemberOptions.IgnoreInheritedMembers);
					foreach (IMethod candidate in candidates)
					{
						if (!HaveSameRuntimeSignature(method, candidate))
						{
							continue;
						}
						if (seen.Add(candidate))
							yield return candidate;
					}
				}
			}
		}

		static bool HaveSameRuntimeSignature(IMethod implementation, IMethod declaration)
		{
			return implementation.Name == declaration.Name
				&& implementation.TypeParameters.Count == declaration.TypeParameters.Count
				&& ParameterListComparer.Instance.Equals(implementation.Parameters, declaration.Parameters)
				&& NormalizeTypeVisitor.TypeErasure.EquivalentTypes(implementation.ReturnType, declaration.ReturnType);
		}

		IEnumerable<MethodDeclaration> CreateOrdinaryNamedAccessorHelpers(
			IProperty property, TypeSystemAstBuilder astBuilder)
		{
			if (property.Getter is { } getter && NeedsHelper(getter, "get_" + property.Name))
				yield return CreateHelper(getter, isGetter: true);
			if (property.Setter is { } setter && NeedsHelper(setter, "set_" + property.Name))
				yield return CreateHelper(setter, isGetter: false);

			bool NeedsHelper(IMethod accessor, string csharpAccessorName)
			{
				if (!accessor.HasBody || accessor.IsVirtual || accessor.IsAbstract || accessor.TypeParameters.Count != 0
					|| accessor.Accessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)
					|| accessor.Name == csharpAccessorName
					|| accessor.MetadataToken.Kind != HandleKind.MethodDefinition
					|| metadata.GetMethodDefinition((MethodDefinitionHandle)accessor.MetadataToken)
						.HasFlag(System.Reflection.MethodAttributes.SpecialName))
				{
					return false;
				}
				if (string.IsNullOrEmpty(accessor.Name)
					|| accessor.Name == accessor.DeclaringTypeDefinition?.Name
					|| accessor.Name[0] != '_' && !char.IsLetter(accessor.Name[0]))
				{
					return false;
				}
				return accessor.Name.Skip(1).All(ch => ch == '_' || char.IsLetterOrDigit(ch));
			}

			MethodDeclaration CreateHelper(IMethod accessor, bool isGetter)
			{
				var fakeMethod = new FakeMethod(typeSystem, SymbolKind.Method) {
					Name = accessor.Name,
					DeclaringType = accessor.DeclaringType,
					ReturnType = accessor.ReturnType,
					Accessibility = accessor.Accessibility,
					IsStatic = accessor.IsStatic,
					Parameters = accessor.Parameters,
					TypeParameters = accessor.TypeParameters,
				};
				var declaration = (MethodDeclaration)astBuilder.ConvertEntity(fakeMethod);
				declaration.RemoveAnnotations<ResolveResult>();
				declaration.AddAnnotation(new MemberResolveResult(null, accessor));
				declaration.Body = new BlockStatement();

				Expression target;
				ResolveResult targetResolveResult;
				if (property.IsStatic)
				{
					target = new TypeReferenceExpression(astBuilder.ConvertType(property.DeclaringType));
					targetResolveResult = new TypeResolveResult(property.DeclaringType);
				}
				else
				{
					target = new ThisReferenceExpression();
					targetResolveResult = new ThisResolveResult(property.DeclaringType);
				}
				var arguments = declaration.Parameters.Take(property.Parameters.Count).Select(ForwardParameter);
				Expression access = property.IsIndexer
					? new IndexerExpression(target, arguments)
					: new MemberReferenceExpression(target, property.Name);
				access.AddAnnotation(new MemberResolveResult(targetResolveResult, property));
				if (isGetter)
				{
					if (accessor.ReturnType.Kind == TypeKind.ByReference)
						access = new DirectionExpression(FieldDirection.Ref, access);
					declaration.Body.Add(new ReturnStatement(access));
				}
				else
				{
					var value = new IdentifierExpression(declaration.Parameters.Last().Name!);
					declaration.Body.Add(new AssignmentExpression(access, value));
				}
				return declaration;
			}
		}

		EntityDeclaration? CreateInterfaceAccessorImplHelper(
			IMethod method, IMember interfaceMember,
			TypeSystemAstBuilder astBuilder)
		{
			IMethod? getter = null, setter = null, adder = null, remover = null;
			foreach (IMethod sibling in method.DeclaringTypeDefinition?.Methods ?? [])
			{
				foreach (IMethod declaration in GetInterfaceAccessorImplementations(sibling))
				{
					if (!declaration.IsAccessor || !interfaceMember.Equals(declaration.AccessorOwner))
						continue;
					switch (declaration.AccessorKind)
					{
						case System.Reflection.MethodSemanticsAttributes.Getter:
							getter ??= sibling;
							break;
						case System.Reflection.MethodSemanticsAttributes.Setter:
							setter ??= sibling;
							break;
						case System.Reflection.MethodSemanticsAttributes.Adder:
							adder ??= sibling;
							break;
						case System.Reflection.MethodSemanticsAttributes.Remover:
							remover ??= sibling;
							break;
					}
				}
			}

			if (interfaceMember is IProperty interfaceProperty)
			{
				if (!method.Equals(getter ?? setter))
					return null; // emit the grouped property only alongside its first accessor
				if (interfaceProperty.Parameters.Count > 0 && !interfaceProperty.IsIndexer)
				{
					// Only an indexer can declare parameters in C#. A parameterized property has nowhere
					// to declare them, so the forwarding call would pass identifiers that are not in
					// scope. Leave the implementing methods to stand on their own instead.
					return null;
				}

				var parameters = interfaceProperty.Parameters.Select(astBuilder.ConvertParameter).ToList();
				AstType returnType = astBuilder.ConvertType(interfaceProperty.ReturnType);
				if (interfaceProperty.ReturnTypeIsRefReadOnly && returnType is ComposedType composedType && composedType.HasRefSpecifier)
					composedType.HasReadOnlySpecifier = true;
				Accessor? getterDecl = getter != null ? CreateGetter(getter, parameters, interfaceProperty.ReturnType.Kind == TypeKind.ByReference) : null;
				Accessor? setterDecl = setter != null ? CreateSetter(setter, parameters, getter == null) : null;
				if (interfaceProperty.IsIndexer)
				{
					var indexerDecl = new IndexerDeclaration {
						ReturnType = returnType,
						PrivateImplementationType = astBuilder.ConvertType(interfaceProperty.DeclaringType),
						Getter = getterDecl,
						Setter = setterDecl,
					};
					indexerDecl.Parameters.AddRange(parameters);
					return indexerDecl;
				}
				return new PropertyDeclaration {
					ReturnType = returnType,
					PrivateImplementationType = astBuilder.ConvertType(interfaceProperty.DeclaringType),
					Name = interfaceProperty.Name,
					Getter = getterDecl,
					Setter = setterDecl,
				};
			}

			if (interfaceMember is IEvent interfaceEvent)
			{
				if (!method.Equals(adder ?? remover))
					return null; // emit the grouped event only alongside its first accessor
				var eventDecl = new CustomEventDeclaration {
					ReturnType = astBuilder.ConvertType(interfaceEvent.ReturnType),
					PrivateImplementationType = astBuilder.ConvertType(interfaceEvent.DeclaringType),
					Name = interfaceEvent.Name,
				};
				if (adder != null)
					eventDecl.AddAccessor = CreateEventAccessor(adder, isFirstAccessor: true);
				if (remover != null)
					eventDecl.RemoveAccessor = CreateEventAccessor(remover, isFirstAccessor: adder == null);
				return eventDecl;
			}

			return null;

			Accessor CreateGetter(IMethod implementation, IReadOnlyList<ParameterDeclaration> parameters, bool returnByRef)
			{
				Expression call = CreateForwardingCall(implementation, parameters.Select(ForwardParameter));
				if (returnByRef)
					call = new DirectionExpression(FieldDirection.Ref, call);
				var returnStatement = new ReturnStatement(call);
				if (HasMethodImplOverride(implementation, interfaceMember))
					returnStatement.AddLeadingTrivia(InterfaceImplComment(implementation.Name));
				var accessor = new Accessor { Body = new BlockStatement() };
				accessor.Body.Add(returnStatement);
				return accessor;
			}

			Accessor CreateSetter(IMethod implementation, IReadOnlyList<ParameterDeclaration> parameters, bool isFirstAccessor)
			{
				var arguments = parameters.Select(ForwardParameter).Append(new IdentifierExpression("value"));
				var statement = new ExpressionStatement(CreateForwardingCall(implementation, arguments));
				if (isFirstAccessor && HasMethodImplOverride(implementation, interfaceMember))
					statement.AddLeadingTrivia(InterfaceImplComment(implementation.Name));
				var accessor = new Accessor { Body = new BlockStatement() };
				accessor.Body.Add(statement);
				return accessor;
			}

			Accessor CreateEventAccessor(IMethod implementation, bool isFirstAccessor)
			{
				var statement = new ExpressionStatement(CreateForwardingCall(
					implementation, new[] { new IdentifierExpression("value") }));
				if (isFirstAccessor && HasMethodImplOverride(implementation, interfaceMember))
					statement.AddLeadingTrivia(InterfaceImplComment(implementation.Name));
				var accessor = new Accessor { Body = new BlockStatement() };
				accessor.Body.Add(statement);
				return accessor;
			}

			InvocationExpression CreateForwardingCall(IMethod implementation, IEnumerable<Expression> arguments)
			{
				var target = new MemberReferenceExpression(new ThisReferenceExpression(), implementation.Name);
				target.AddAnnotation(new MemberResolveResult(new ThisResolveResult(implementation.DeclaringType), implementation));
				return new InvocationExpression(target, arguments);
			}

			bool HasMethodImplOverride(IMethod implementation, IMember targetMember)
			{
				return GetInterfaceMethodImplementations(implementation)
					.Any(declaration => targetMember.Equals(declaration.AccessorOwner));
			}
		}

		Expression ForwardParameter(ParameterDeclaration p)
		{
			switch (p.ParameterModifier)
			{
				case ReferenceKind.None:
					return new IdentifierExpression(p.Name!);
				case ReferenceKind.Ref:
				case ReferenceKind.RefReadOnly:
					return new DirectionExpression(FieldDirection.Ref, new IdentifierExpression(p.Name!));
				case ReferenceKind.Out:
					return new DirectionExpression(FieldDirection.Out, new IdentifierExpression(p.Name!));
				case ReferenceKind.In:
					return new DirectionExpression(FieldDirection.In, new IdentifierExpression(p.Name!));
				default:
					throw new NotSupportedException();
			}
		}

		// Visual Basic implements an interface property or event with an accessor that carries a
		// .override directive but an ordinary (dotless) name, so it is rendered as a plain public
		// member and the interface member is left unimplemented (CS0535). Methods get a forwarder
		// from the .override directive (AddInterfaceImplHelpers above); these emit the property/event
		// analogue, forwarding to the public member that the accessor's .override resolves to.
		IEnumerable<EntityDeclaration> AddInterfaceImplHelpers(
			EntityDeclaration memberDecl, IProperty property,
			TypeSystemAstBuilder astBuilder)
		{
			if (memberDecl.GetChild(Slots.PrivateImplementationType) is not null)
			{
				yield break; // cannot create forwarder for existing explicit interface impl
			}
			if (property.IsStatic)
			{
				yield break; // cannot create forwarder for static interface impl
			}
			if (memberDecl.HasModifier(Modifiers.Extern))
			{
				yield break; // cannot create forwarder for extern property
			}
			foreach (EntityDeclaration helper in AddInterfaceMethodImplHelpers(memberDecl, property, astBuilder))
				yield return helper;
			if (property.IsIndexer)
			{
				yield break; // forwarder generation for indexers is not implemented
			}
			foreach (var interfaceMember in property.ExplicitlyImplementedInterfaceMembers)
			{
				if (interfaceMember is not IProperty interfaceProperty
					|| interfaceProperty.DeclaringType.Kind != TypeKind.Interface)
				{
					continue;
				}
				var propertyDecl = new PropertyDeclaration();
				if (memberDecl.ReturnType is { } propertyReturnType)
					propertyDecl.ReturnType = propertyReturnType.Clone();
				propertyDecl.PrivateImplementationType = astBuilder.ConvertType(interfaceProperty.DeclaringType);
				propertyDecl.Name = interfaceProperty.Name;
				bool commentEmitted = false;
				if (interfaceProperty.CanGet)
				{
					var getter = new Accessor { Body = new BlockStatement() };
					var propertyReference = new MemberReferenceExpression(new ThisReferenceExpression(), memberDecl.Name);
					propertyReference.AddAnnotation(new MemberResolveResult(new ThisResolveResult(property.DeclaringType), property));
					var returnStatement = new ReturnStatement(propertyReference);
					// Attach the comment as trivia rather than as a statement of its own, so a
					// single-return getter still collapses to an expression-bodied property.
					returnStatement.AddLeadingTrivia(InterfaceImplComment(memberDecl.Name));
					getter.Body.Add(returnStatement);
					propertyDecl.Getter = getter;
					commentEmitted = true;
				}
				if (interfaceProperty.CanSet)
				{
					var setter = new Accessor { Body = new BlockStatement() };
					var propertyReference = new MemberReferenceExpression(new ThisReferenceExpression(), memberDecl.Name);
					propertyReference.AddAnnotation(new MemberResolveResult(new ThisResolveResult(property.DeclaringType), property));
					var assignmentStatement = new ExpressionStatement(new AssignmentExpression(
						propertyReference,
						new IdentifierExpression("value")));
					if (!commentEmitted)
						assignmentStatement.AddLeadingTrivia(InterfaceImplComment(memberDecl.Name));
					setter.Body.Add(assignmentStatement);
					propertyDecl.Setter = setter;
				}
				yield return propertyDecl;
			}
		}

		IEnumerable<EntityDeclaration> AddInterfaceMethodImplHelpers(
			EntityDeclaration memberDecl, IProperty property,
			TypeSystemAstBuilder astBuilder)
		{
			foreach (IMethod accessor in new[] { property.Getter, property.Setter }.OfType<IMethod>())
			{
				foreach (IMethod interfaceMethod in GetInterfaceOrdinaryMethodImplementations(accessor))
				{
					var methodDecl = new MethodDeclaration {
						ReturnType = astBuilder.ConvertType(interfaceMethod.ReturnType),
						PrivateImplementationType = astBuilder.ConvertType(interfaceMethod.DeclaringType),
						Name = interfaceMethod.Name,
						Body = new BlockStatement(),
					};
					methodDecl.Parameters.AddRange(interfaceMethod.Parameters.Select(astBuilder.ConvertParameter));

					Expression propertyAccess;
					if (property.IsIndexer)
					{
						propertyAccess = new IndexerExpression(new ThisReferenceExpression(),
							methodDecl.Parameters.Take(property.Parameters.Count).Select(ForwardParameter));
					}
					else
					{
						propertyAccess = new MemberReferenceExpression(new ThisReferenceExpression(), memberDecl.Name);
					}
					propertyAccess.AddAnnotation(new MemberResolveResult(new ThisResolveResult(property.DeclaringType), property));

					Statement statement;
					if (accessor.AccessorKind == System.Reflection.MethodSemanticsAttributes.Getter)
					{
						if (interfaceMethod.ReturnType.Kind == TypeKind.ByReference)
							propertyAccess = new DirectionExpression(FieldDirection.Ref, propertyAccess);
						statement = new ReturnStatement(propertyAccess);
					}
					else
					{
						statement = new ExpressionStatement(new AssignmentExpression(
							propertyAccess, new IdentifierExpression(methodDecl.Parameters.Last().Name!)));
					}
					if (GetInterfaceMethodImplementations(accessor).Any(m => m.Equals(interfaceMethod)))
						statement.AddLeadingTrivia(InterfaceImplComment(memberDecl.Name));
					methodDecl.Body.Add(statement);
					yield return methodDecl;
				}
			}
		}

		IEnumerable<EntityDeclaration> AddInterfaceImplHelpers(
			EntityDeclaration memberDecl, IEvent @event,
			TypeSystemAstBuilder astBuilder)
		{
			if (memberDecl.GetChild(Slots.PrivateImplementationType) is not null)
			{
				yield break; // cannot create forwarder for existing explicit interface impl
			}
			if (@event.IsStatic)
			{
				yield break; // cannot create forwarder for static interface impl
			}
			if (memberDecl.HasModifier(Modifiers.Extern))
			{
				yield break; // cannot create forwarder for extern event
			}
			foreach (var interfaceMember in @event.ExplicitlyImplementedInterfaceMembers)
			{
				if (interfaceMember is not IEvent interfaceEvent
					|| interfaceEvent.DeclaringType.Kind != TypeKind.Interface)
				{
					continue;
				}
				var eventDecl = new CustomEventDeclaration();
				if (memberDecl.ReturnType is { } eventReturnType)
					eventDecl.ReturnType = eventReturnType.Clone();
				eventDecl.PrivateImplementationType = astBuilder.ConvertType(interfaceEvent.DeclaringType);
				eventDecl.Name = interfaceEvent.Name;
				// The forwarder targets the public member by name. A field-like EventDeclaration
				// exposes no Name (it lives in the VariableInitializer), so take the name from the
				// event symbol rather than from memberDecl, which would be empty for an automatic event.
				var addAccessor = new Accessor { Body = new BlockStatement() };
				var addReference = new MemberReferenceExpression(new ThisReferenceExpression(), @event.Name);
				addReference.AddAnnotation(new MemberResolveResult(new ThisResolveResult(@event.DeclaringType), @event));
				var addStatement = new ExpressionStatement(new AssignmentExpression(
					addReference,
					AssignmentOperatorType.Add, new IdentifierExpression("value")));
				addStatement.AddLeadingTrivia(InterfaceImplComment(@event.Name));
				addAccessor.Body.Add(addStatement);
				eventDecl.AddAccessor = addAccessor;
				var removeAccessor = new Accessor { Body = new BlockStatement() };
				var removeReference = new MemberReferenceExpression(new ThisReferenceExpression(), @event.Name);
				removeReference.AddAnnotation(new MemberResolveResult(new ThisResolveResult(@event.DeclaringType), @event));
				removeAccessor.Body.Add(new ExpressionStatement(new AssignmentExpression(
					removeReference,
					AssignmentOperatorType.Subtract, new IdentifierExpression("value"))));
				eventDecl.RemoveAccessor = removeAccessor;
				yield return eventDecl;
			}
		}

		static Comment InterfaceImplComment(string memberName)
		{
			return new Comment(
				"ILSpy generated this explicit interface implementation from .override directive in " + memberName);
		}

		/// <summary>
		/// Sets new modifier if the member hides some other member from a base type.
		/// </summary>
		/// <param name="member">The node of the member which new modifier state should be determined.</param>
		void SetNewModifier(EntityDeclaration member)
		{
			if (member is ExtensionDeclaration)
				return;

			var entity = (IEntity)member.GetSymbol()!;
			var lookup = new MemberLookup(entity.DeclaringTypeDefinition, entity.ParentModule);

			var baseTypes = entity.DeclaringType.GetNonInterfaceBaseTypes().Where(t => entity.DeclaringType != t).ToList();

			// A constant, field, property, event, or type introduced in a class or struct hides all base class members with the same name.
			bool hideBasedOnSignature = !(entity is ITypeDefinition
				|| entity.SymbolKind == SymbolKind.Field
				|| entity.SymbolKind == SymbolKind.Property
				|| entity.SymbolKind == SymbolKind.Event);

			const GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;

			if (HidesMemberOrTypeOfBaseType())
				member.Modifiers |= Modifiers.New;

			bool HidesMemberOrTypeOfBaseType()
			{
				var parameterListComparer = ParameterListComparer.WithOptions(includeModifiers: true);

				foreach (IType baseType in baseTypes)
				{
					if (!hideBasedOnSignature)
					{
						if (baseType.GetNestedTypes(t => t.Name == entity.Name && lookup.IsAccessible(t, true), options).Any())
							return true;
						if (baseType.GetMembers(m => m.Name == entity.Name && m.SymbolKind != SymbolKind.Indexer && lookup.IsAccessible(m, true), options).Any())
							return true;
					}
					else
					{
						if (entity.SymbolKind == SymbolKind.Indexer)
						{
							// An indexer introduced in a class or struct hides all base class indexers with the same signature (parameter count and types).
							if (baseType.GetProperties(p => p.SymbolKind == SymbolKind.Indexer && lookup.IsAccessible(p, true))
									.Any(p => parameterListComparer.Equals(((IProperty)entity).Parameters, p.Parameters)))
							{
								return true;
							}
						}
						else if (entity.SymbolKind == SymbolKind.Method)
						{
							// A method introduced in a class or struct hides all non-method base class members with the same name, and all
							// base class methods with the same signature (method name and parameter count, modifiers, and types).
							if (baseType.GetMembers(m => m.SymbolKind != SymbolKind.Indexer
													&& m.SymbolKind != SymbolKind.Constructor
													&& m.SymbolKind != SymbolKind.Destructor
													&& m.Name == entity.Name && lookup.IsAccessible(m, true))
								.Any(m => m.SymbolKind != SymbolKind.Method ||
									(((IMethod)entity).TypeParameters.Count == ((IMethod)m).TypeParameters.Count
										&& parameterListComparer.Equals(((IMethod)entity).Parameters, ((IMethod)m).Parameters))))
							{
								return true;
							}
						}
					}
				}

				return false;
			}
		}

		void FixParameterNames(EntityDeclaration entity)
		{
			var usedNames = new HashSet<string>(StringComparer.Ordinal);
			int i = 0;
			foreach (var parameter in entity.GetChildren(Slots.Parameter))
			{
				if (!parameter.Type.IsArgList())
					parameter.Name = ILReader.GetUniqueParameterName(usedNames, parameter.Name, i);
				i++;
			}
		}

		EntityDeclaration DoDecompile(ITypeDefinition typeDef, DecompileRun decompileRun, ITypeResolveContext decompilationContext, bool asExtension = false)
		{
			Debug.Assert(decompilationContext.CurrentTypeDefinition == typeDef);
			DecompilerEventSource.Log.DecompileTypeStart(typeDef);
			var entityMap = new MultiDictionary<IEntity, EntityDeclaration>();
			var workList = new Queue<IEntity>();
			TypeSystemAstBuilder typeSystemAstBuilder;
			try
			{
				typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
				EntityDeclaration entityDecl;
				if (asExtension)
				{
					var extensionInfo = typeDef.DeclaringTypeDefinition?.ExtensionInfo ?? typeDef.DeclaringTypeDefinition?.DeclaringTypeDefinition?.ExtensionInfo;
					Debug.Assert(extensionInfo != null);
					extensionInfo.IsExtensionMarkerType(typeDef, out var extensionGroup);
					entityDecl = typeSystemAstBuilder.ConvertExtension(extensionGroup);
				}
				else
				{
					entityDecl = typeSystemAstBuilder.ConvertEntity(typeDef);
				}

				if (entityDecl is DelegateDeclaration delegateDeclaration)
				{
					// Fix empty parameter names in delegate declarations
					FixParameterNames(delegateDeclaration);
				}

				if (entityDecl is not TypeDeclaration typeDecl)
				{
					if (entityDecl is ExtensionDeclaration ext && settings.ExtensionMembers)
					{
						var extensionInfo = typeDef.DeclaringTypeDefinition!.ExtensionInfo ?? typeDef.DeclaringTypeDefinition.DeclaringTypeDefinition!.ExtensionInfo;
						extensionInfo!.IsExtensionMarkerType(typeDef, out var group);
						DoDecompileExtensionMembers(ext, group.Marker, extensionInfo);
					}
					// e.g. DelegateDeclaration
					return entityDecl;
				}
				bool isRecord = typeDef.Kind switch {
					TypeKind.Class => settings.RecordClasses && typeDef.IsRecord,
					TypeKind.Struct => settings.RecordStructs && typeDef.IsRecord,
					_ => false,
				};
				RecordDecompiler? recordDecompiler = isRecord ? new RecordDecompiler(typeSystem, typeDef, settings, CancellationToken) : null;
				if (recordDecompiler != null)
					decompileRun.RecordDecompilers.Add(typeDef, recordDecompiler);

				// With C# 9 records, the relative order of fields and properties matters.
				// Sequential-layout types also need backing fields to remain at the source position of
				// the auto-property or field-like event that will recreate them.
				bool requiresSequentialFieldOrdering = RequiresSequentialFieldOrdering(typeDef);
				IEnumerable<IMember> fieldsPropertiesAndEvents = requiresSequentialFieldOrdering
					? GetMembersWithSequentialFieldOrdering(typeDef)
					: isRecord
						? recordDecompiler!.FieldsAndProperties.Concat(typeDef.Events)
						: typeDef.Fields.Concat<IMember>(typeDef.Properties).Concat(typeDef.Events);

				// For COM interop scenarios, the relative order of virtual functions/properties matters:
				IEnumerable<IMember> allOrderedMembers = RequiresNativeOrdering(typeDef) ? GetMembersWithNativeOrdering(typeDef) :
					fieldsPropertiesAndEvents.Concat(typeDef.Methods);

				var allOrderedEntities = typeDef.NestedTypes.Concat<IEntity>(allOrderedMembers).ToArray();

				if (!partialTypes.TryGetValue((TypeDefinitionHandle)typeDef.MetadataToken, out var partialTypeInfo))
				{
					partialTypeInfo = null;
				}

				if (settings.ExtensionMembers)
				{
					foreach (var group in typeDef.ExtensionInfo?.ExtensionGroups ?? [])
					{
						var ext = (ExtensionDeclaration)typeSystemAstBuilder.ConvertExtension(group);
						DoDecompileExtensionMembers(ext, group.Marker, typeDef.ExtensionInfo!);

						typeDecl.Members.Add(ext);
					}
				}

				// Decompile members that are not compiler-generated.
				foreach (var entity in allOrderedEntities)
				{
					if (entity.MetadataToken.IsNil)
					{
						continue;
					}
					if (MemberIsHidden(module.MetadataFile, entity.MetadataToken, settings)
						&& !IsBackingFieldOfNonAutomaticEvent(entity))
					{
						continue;
					}
					DoDecompileMember(entity, recordDecompiler, partialTypeInfo, typeDef.ExtensionInfo);
				}

				// Decompile compiler-generated members that are still needed.
				while (workList.Count > 0)
				{
					var entity = workList.Dequeue();
					if (entityMap.Contains(entity) || entity.MetadataToken.IsNil)
					{
						// Member is already decompiled.
						continue;
					}
					DoDecompileMember(entity, recordDecompiler, partialTypeInfo, typeDef.ExtensionInfo);
				}

				// Add all decompiled members to syntax tree in the correct order.
				foreach (var member in allOrderedEntities)
				{
					typeDecl.Members.AddRange(entityMap[member]);
				}

				if (typeDecl.Members.OfType<IndexerDeclaration>().Any(idx => idx.PrivateImplementationType is null))
				{
					// Remove the [DefaultMember] attribute if the class contains indexers
					RemoveAttribute(typeDecl, KnownAttribute.DefaultMember);
				}
				if (partialTypeInfo != null)
				{
					typeDecl.Modifiers |= Modifiers.Partial;
				}
				if (settings.IntroduceRefModifiersOnStructs)
				{
					RemoveObsoleteAttribute(typeDecl, "Types with embedded references are not supported in this version of your compiler.");
					RemoveCompilerFeatureRequiredAttribute(typeDecl, "RefStructs");
				}
				if (settings.RequiredMembers)
				{
					RemoveAttribute(typeDecl, KnownAttribute.Required);
				}
				if (typeDecl.ClassType == ClassType.Enum)
				{
					Debug.Assert(typeDef.Kind == TypeKind.Enum);
					EnumValueDisplayMode displayMode = DetectBestEnumValueDisplayMode(typeDef, module.MetadataFile);
					switch (displayMode)
					{
						case EnumValueDisplayMode.FirstOnly:
							foreach (var enumMember in typeDecl.Members.OfType<EnumMemberDeclaration>().Skip(1))
							{
								enumMember.Initializer = null;
							}
							break;
						case EnumValueDisplayMode.None:
							foreach (var enumMember in typeDecl.Members.OfType<EnumMemberDeclaration>())
							{
								enumMember.Initializer = null;
								if (enumMember.GetSymbol() is IField f && f.GetConstantValue() == null)
								{
									enumMember.AddLeadingTrivia(new Comment(" error: enumerator has no value"));
								}
							}
							break;
						case EnumValueDisplayMode.All:
							// nothing needs to be changed.
							break;
						case EnumValueDisplayMode.AllHex:
							foreach (var enumMember in typeDecl.Members.OfType<EnumMemberDeclaration>())
							{
								var constantValue = (enumMember.GetSymbol() as IField)!.GetConstantValue();
								if (constantValue == null || enumMember.Initializer is not PrimitiveExpression pe)
								{
									continue;
								}
								long initValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constantValue, false);
								if (initValue >= 10)
								{
									pe.Format = LiteralFormat.HexadecimalNumber;
								}
							}
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					foreach (var item in typeDecl.Members)
					{
						if (item is not EnumMemberDeclaration)
						{
							item.AddLeadingTrivia(new Comment(" error: nested types are not permitted in C#."));
						}
					}
				}
				return typeDecl;
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, typeDef, innerException);
			}
			finally
			{
				DecompilerEventSource.Log.DecompileTypeStop(typeDef);
			}

			// MemberIsHidden identifies event backing fields from the metadata name association
			// alone. When the event's accessors turn out not to be compiler-generated, the event
			// is decompiled with explicit accessors and no field-like declaration takes the
			// field's place, so the field must stay in the output even if no decompiled body
			// references it (referenced hidden members are re-added via the work list).
			bool IsBackingFieldOfNonAutomaticEvent(IEntity entity)
			{
				if (entity is not IField field || !settings.AutomaticEvents)
					return false;
				if (!module.MetadataFile.PropertyAndEventBackingFieldLookup.IsEventBackingField((FieldDefinitionHandle)field.MetadataToken, out var eventHandle))
					return false;
				if (AutoEventDecompiler.IsAutomaticEvent(typeSystem, module.GetDefinition(eventHandle), decompileRun, CancellationToken, out _))
					return false;
				// The field may be hidden for an unrelated reason as well; keep it hidden then.
				var settingsWithoutAutomaticEvents = settings.Clone();
				settingsWithoutAutomaticEvents.AutomaticEvents = false;
				return !MemberIsHidden(module.MetadataFile, field.MetadataToken, settingsWithoutAutomaticEvents);
			}

			void DoDecompileMember(IEntity entity, RecordDecompiler? recordDecompiler, PartialTypeInfo? partialType, ExtensionInfo? extensionInfo)
			{
				if (partialType != null && partialType.IsDeclaredMember(entity.MetadataToken))
				{
					return;
				}

				if (settings.ExtensionMembers && extensionInfo != null)
				{
					switch (entity)
					{
						case ITypeDefinition td when extensionInfo.IsExtensionGroupType(td) || extensionInfo.IsExtensionMarkerType(td, out _):
							return;
						case IMethod m when extensionInfo.InfoOfImplementationMember(m).HasValue:
							return;
					}
				}

				EntityDeclaration entityDecl;
				switch (entity)
				{
					case IField field:
						if (typeDef.Kind == TypeKind.Enum && !field.IsConst)
						{
							return;
						}
						if (TransformFieldAndConstructorInitializers.IsGeneratedPrimaryConstructorBackingField(field))
						{
							return;
						}
						entityDecl = DoDecompile(field, decompileRun, decompilationContext.WithCurrentMember(field));
						entityMap.Add(field, entityDecl);
						break;
					case IProperty property:
						if (recordDecompiler?.PropertyIsGenerated(property) == true)
						{
							return;
						}
						if (HasParametersCSharpCannotDeclare(property))
						{
							// Write the accessors as ordinary methods instead of the property. The call
							// sites already read as get_X(...)/set_X(...), which only compiles while no
							// property of that name hides them. They are keyed on the property, which is
							// what the member ordering knows about.
							foreach (var parameterizedAccessor in new[] { property.Getter, property.Setter })
							{
								if (parameterizedAccessor == null)
									continue;
								var accessorDecl = DoDecompile(parameterizedAccessor, decompileRun,
									decompilationContext.WithCurrentMember(parameterizedAccessor), null);
								var methodDecl = RewriteAccessorAsMethod(parameterizedAccessor, accessorDecl, typeSystemAstBuilder);
								RemoveAttribute(methodDecl, KnownAttribute.SpecialName);
								entityMap.Add(property, methodDecl);
								EnqueueReferencedMembers(methodDecl);
							}
							return;
						}
						entityDecl = DoDecompile(property, decompileRun, decompilationContext.WithCurrentMember(property), null);
						entityMap.Add(property, entityDecl);
						foreach (var helper in CreateOrdinaryNamedAccessorHelpers(property, typeSystemAstBuilder))
						{
							entityMap.Add(property, helper);
						}
						foreach (var helper in AddInterfaceImplHelpers(entityDecl, property, typeSystemAstBuilder))
						{
							entityMap.Add(property, helper);
						}
						break;
					case IMethod method:
						if (recordDecompiler?.MethodIsGenerated(method) == true)
						{
							return;
						}
						if (TryGetBaseAccessorMember(method, out var baseMember, out var basePrimaryAccessor))
						{
							// A virtual method can override a base property accessor by CLR slot even when
							// the derived type has no property row. C# must represent that slot as an
							// override property, so emit the accessor body through a fake member.
							if (!method.Equals(basePrimaryAccessor))
								return;
							entityDecl = DoDecompile((IProperty)baseMember, decompileRun,
								decompilationContext.WithCurrentMember(baseMember), null);
							foreach (var accessor in entityDecl.Children.OfType<Accessor>())
								RemoveAttribute(accessor, KnownAttribute.SpecialName);
							entityDecl.Modifiers &= ~(Modifiers.New | Modifiers.Virtual | Modifiers.Abstract);
							entityDecl.Modifiers |= Modifiers.Override;
							if (basePrimaryAccessor.IsSealed)
								entityDecl.Modifiers |= Modifiers.Sealed;
							entityMap.Add(method, entityDecl);
							break;
						}
						if (TryGetExplicitInterfaceAccessorMember(method, out var fakeMember, out var primaryAccessor))
						{
							// An explicit interface implementation whose .override targets a property/event
							// accessor but which has no property/event metadata row of its own. The accessor
							// group is emitted once, as a reconstructed property/event declaration, keyed on
							// the method that owns the emission.
							if (!method.Equals(primaryAccessor))
							{
								return;
							}
							entityDecl = fakeMember switch {
								IProperty p => DoDecompile(p, decompileRun, decompilationContext.WithCurrentMember(p), null),
								IEvent e => DoDecompile(e, decompileRun, decompilationContext.WithCurrentMember(e)),
								_ => throw new InvalidOperationException()
							};
							// The implementing accessor methods carry SpecialName because they are
							// classified as plain methods; the reconstructed accessors must not show it.
							foreach (var accessor in entityDecl.Children.OfType<Accessor>())
							{
								RemoveAttribute(accessor, KnownAttribute.SpecialName);
							}
							entityMap.Add(method, entityDecl);
							break;
						}
						entityDecl = DoDecompile(method, decompileRun, decompilationContext.WithCurrentMember(method), null);
						entityMap.Add(method, entityDecl);
						foreach (var helper in AddInterfaceImplHelpers(entityDecl, method, typeSystemAstBuilder))
						{
							entityMap.Add(method, helper);
						}
						break;
					case IEvent @event:
						entityDecl = DoDecompile(@event, decompileRun, decompilationContext.WithCurrentMember(@event));
						entityMap.Add(@event, entityDecl);
						foreach (var helper in AddInterfaceImplHelpers(entityDecl, @event, typeSystemAstBuilder))
						{
							entityMap.Add(@event, helper);
						}
						break;
					case ITypeDefinition type:
						entityDecl = DoDecompile(type, decompileRun, decompilationContext.WithCurrentTypeDefinition(type));
						SetNewModifier(entityDecl);
						entityMap.Add(type, entityDecl);
						break;
					default:
						throw new ArgumentOutOfRangeException("Unexpected member type");
				}

				EnqueueReferencedMembers(entityDecl);
			}

			void EnqueueReferencedMembers(EntityDeclaration entityDecl)
			{
				foreach (var node in entityDecl.Descendants)
				{
					var rr = node.GetResolveResult();
					if (rr is MemberResolveResult mrr
						&& mrr.Member.DeclaringTypeDefinition == typeDef
						&& !(mrr.Member is IMethod { IsLocalFunction: true }))
					{
						workList.Enqueue(mrr.Member);
					}
					else if (rr is TypeResolveResult trr
						&& trr.Type.GetDefinition()?.DeclaringTypeDefinition == typeDef)
					{
						workList.Enqueue(trr.Type.GetDefinition()!);
					}
				}
			}

			void DoDecompileExtensionMembers(ExtensionDeclaration ext, IMethod marker, ExtensionInfo extensionInfo)
			{
				foreach (var member in extensionInfo.GetMembersOfGroup(marker))
				{
					var extMember = member;
					if (entityMap.Contains(extMember) || extMember.MetadataToken.IsNil)
					{
						// Member is already decompiled.
						continue;
					}
					EntityDeclaration extMemberDecl;
					switch (extMember)
					{
						case IProperty p:
							var prop = DoDecompile(p, decompileRun, decompilationContext.WithCurrentMember(p), extensionInfo);
							RemoveAttribute(prop, KnownAttribute.ExtensionMarker);
							if (p.Getter != null)
							{
								RemoveAttribute(prop.GetChild(Slots.Getter)!, KnownAttribute.ExtensionMarker);
							}
							if (p.Setter != null)
							{
								RemoveAttribute(prop.GetChild(Slots.Setter)!, KnownAttribute.ExtensionMarker);
							}
							extMemberDecl = prop;
							break;
						case IMethod m:
							var meth = DoDecompile(m, decompileRun, decompilationContext.WithCurrentMember(m), extensionInfo);
							RemoveAttribute(meth, KnownAttribute.ExtensionMarker);
							extMemberDecl = meth;
							break;
						default:
							throw new NotSupportedException($"Extension member {extMember} is not supported for decompilation.");
					}
					ext.Members.Add(extMemberDecl);
					entityMap.Add(extMember, extMemberDecl);
				}
			}
		}

		EnumValueDisplayMode DetectBestEnumValueDisplayMode(ITypeDefinition typeDef, MetadataFile module)
		{
			if (typeDef.HasAttribute(KnownAttribute.Flags))
				return EnumValueDisplayMode.AllHex;
			bool first = true;
			long firstValue = 0, previousValue = 0;
			bool allPowersOfTwo = true;
			bool allConsecutive = true;
			foreach (var field in typeDef.Fields)
			{
				if (MemberIsHidden(module, field.MetadataToken, settings))
					continue;
				object? constantValue = field.GetConstantValue();
				if (constantValue == null)
					continue;
				long currentValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constantValue, false);
				allConsecutive = allConsecutive && (first || previousValue + 1 == currentValue);
				// N & (N - 1) == 0, iff N is a power of 2, for all N != 0.
				// We define that 0 is a power of 2 in the context of enum values.
				allPowersOfTwo = allPowersOfTwo && unchecked(currentValue & (currentValue - 1)) == 0;
				if (first)
				{
					firstValue = currentValue;
					first = false;
				}
				else if (currentValue <= previousValue)
				{
					// If the values are out of order, we fallback to displaying all values.
					return EnumValueDisplayMode.All;
				}
				else if (!allConsecutive && !allPowersOfTwo)
				{
					// We already know that the values are neither consecutive nor all powers of 2,
					// so we can abort, and just display all values as-is.
					return EnumValueDisplayMode.All;
				}
				previousValue = currentValue;
			}
			if (allPowersOfTwo)
			{
				if (previousValue > 8)
				{
					// If all values are powers of 2 and greater 8, display all enum values, but use hex.
					return EnumValueDisplayMode.AllHex;
				}
				else if (!allConsecutive)
				{
					// If all values are powers of 2, display all enum values.
					return EnumValueDisplayMode.All;
				}
			}
			if (settings.AlwaysShowEnumMemberValues)
			{
				// The user always wants to see all enum values, but we know hex is not necessary.
				return EnumValueDisplayMode.All;
			}
			// We know that all values are consecutive, so if the first value is not 0
			// display the first enum value only.
			return firstValue == 0 ? EnumValueDisplayMode.None : EnumValueDisplayMode.FirstOnly;
		}

		EntityDeclaration DoDecompile(IMethod method, DecompileRun decompileRun, ITypeResolveContext decompilationContext, ExtensionInfo? extensionInfo)
		{
			Debug.Assert(decompilationContext.CurrentMember == method);
			DecompilerEventSource.Log.DecompileMemberStart(method, DecompiledMemberKind.Method);
			try
			{
				var typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
				var methodDecl = typeSystemAstBuilder.ConvertEntity(method);
				int lastDot = method.Name.LastIndexOf('.');
				if (methodDecl is not OperatorDeclaration && method.IsExplicitInterfaceImplementation && lastDot >= 0)
				{
					methodDecl.Name = method.Name.Substring(lastDot + 1);
				}
				FixParameterNames(methodDecl);
				var methodDefinition = metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
				if (!settings.LocalFunctions && LocalFunctionDecompiler.LocalFunctionNeedsAccessibilityChange(method.ParentModule!.MetadataFile, (MethodDefinitionHandle)method.MetadataToken))
				{
					// if local functions are not active and we're dealing with a local function,
					// reduce the visibility of the method to private,
					// otherwise this leads to compile errors because the display classes have lesser accessibility.
					// Note: removing and then adding the static modifier again is necessary to set the private modifier before all other modifiers.
					methodDecl.Modifiers &= ~(Modifiers.Internal | Modifiers.Static);
					methodDecl.Modifiers |= Modifiers.Private | (method.IsStatic ? Modifiers.Static : 0);
				}
				if (methodDefinition.HasBody())
				{
					DecompileBody(method, methodDecl, decompileRun, decompilationContext, extensionInfo);
				}
				else if (!method.IsAbstract && method.DeclaringType.Kind != TypeKind.Interface)
				{
					methodDecl.Modifiers |= Modifiers.Extern;
				}
				if (method.SymbolKind == SymbolKind.Method && !method.IsExplicitInterfaceImplementation
					&& methodDefinition.HasFlag(System.Reflection.MethodAttributes.Virtual) == methodDefinition.HasFlag(System.Reflection.MethodAttributes.NewSlot))
				{
					SetNewModifier(methodDecl);
				}
				else if (!method.IsStatic && !method.IsExplicitInterfaceImplementation
					&& !method.IsVirtual && method.IsOverride
					&& InheritanceHelper.GetBaseMember(method) == null && IsTypeHierarchyKnown(method.DeclaringType))
				{
					methodDecl.Modifiers &= ~Modifiers.Override;
					if (!method.DeclaringTypeDefinition!.IsSealed)
					{
						methodDecl.Modifiers |= Modifiers.Virtual;
					}
				}
				if (IsCovariantReturnOverride(method))
				{
					RemoveAttribute(methodDecl, KnownAttribute.PreserveBaseOverrides);
					methodDecl.Modifiers &= ~(Modifiers.New | Modifiers.Virtual);
					methodDecl.Modifiers |= Modifiers.Override;
				}
				if (method.IsConstructor && settings.RequiredMembers && RemoveCompilerFeatureRequiredAttribute(methodDecl, "RequiredMembers"))
				{
					RemoveObsoleteAttribute(methodDecl, "Constructors of types with required members are not supported in this version of your compiler.");
				}
				return methodDecl;

				bool IsTypeHierarchyKnown(IType type)
				{
					var definition = type.GetDefinition();
					if (definition == null)
					{
						return false;
					}

					if (decompileRun.TypeHierarchyIsKnown.TryGetValue(definition, out var value))
						return value;
					value = method.DeclaringType.GetNonInterfaceBaseTypes().All(t => t.Kind != TypeKind.Unknown);
					decompileRun.TypeHierarchyIsKnown.Add(definition, value);
					return value;
				}
			}
			finally
			{
				DecompilerEventSource.Log.DecompileMemberStop(method, DecompiledMemberKind.Method);
			}
		}

		private bool IsCovariantReturnOverride(IEntity entity)
		{
			if (!settings.CovariantReturns)
				return false;
			if (!entity.HasAttribute(KnownAttribute.PreserveBaseOverrides))
				return false;
			return true;
		}

		internal static bool IsWindowsFormsInitializeComponentMethod(IMethod method)
		{
			return method.ReturnType.Kind == TypeKind.Void && method.Name == "InitializeComponent" && method.DeclaringTypeDefinition!.GetNonInterfaceBaseTypes().Any(t => t.FullName == "System.Windows.Forms.Control");
		}

		void DecompileBody(IMethod method, EntityDeclaration entityDecl, DecompileRun decompileRun, ITypeResolveContext decompilationContext, ExtensionInfo? extensionInfo)
		{
			try
			{
				var ilReader = new ILReader(typeSystem.MainModule) {
					UseDebugSymbols = settings.UseDebugSymbols,
					UseRefLocalsForAccurateOrderOfEvaluation = settings.UseRefLocalsForAccurateOrderOfEvaluation,
					DebugInfo = DebugInfoProvider
				};
				int parameterOffset = 0;
				if (extensionInfo != null)
				{
					if (!method.IsStatic)
						parameterOffset = 1; // implementation method has an additional receiver parameter
					method = extensionInfo.InfoOfExtensionMember((IMethod)method.MemberDefinition)!.Value.ImplementationMethod;
				}

				var methodDef = metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
				BlockStatement body = new BlockStatement();
				MethodBodyBlock methodBody;
				try
				{
					methodBody = module.MetadataFile.GetMethodBody(methodDef.RelativeVirtualAddress);
				}
				catch (BadImageFormatException ex)
				{
					body = new BlockStatement();
					var commentStatement = new EmptyStatement();
					commentStatement.AddTrailingTrivia(new Comment("Invalid MethodBodyBlock: " + ex.Message));
					body.Statements.Add(commentStatement);
					entityDecl.AddChild(body, Slots.Body);
					return;
				}
				var function = ilReader.ReadIL((MethodDefinitionHandle)method.MetadataToken, methodBody, cancellationToken: CancellationToken);
				function.CheckInvariant(ILPhase.Normal);

				AddAnnotationsToDeclaration(method, entityDecl, function, parameterOffset);

				var localSettings = settings.Clone();
				if (IsWindowsFormsInitializeComponentMethod(method))
				{
					localSettings.UseImplicitMethodGroupConversion = false;
					localSettings.UsingDeclarations = false;
					localSettings.AlwaysCastTargetsOfExplicitInterfaceImplementationCalls = true;
					localSettings.NamedArguments = false;
					localSettings.AlwaysQualifyMemberReferences = true;
				}

				var context = new ILTransformContext(function, typeSystem, DebugInfoProvider, localSettings) {
					CancellationToken = CancellationToken,
					DecompileRun = decompileRun
				};
				foreach (var transform in ilTransforms)
				{
					CancellationToken.ThrowIfCancellationRequested();
					transform.Run(function, context);
					function.CheckInvariant(ILPhase.Normal);
					// When decompiling definitions only, we can cancel decompilation of all steps
					// after yield and async detection, because only those are needed to properly set
					// IsAsync/IsIterator flags on ILFunction.
					if (!localSettings.DecompileMemberBodies && transform is AsyncAwaitDecompiler)
						break;
				}

				// Generate C# AST only if bodies should be displayed.
				if (localSettings.DecompileMemberBodies)
				{
					AddDefinesForConditionalAttributes(function, decompileRun);
					var statementBuilder = new StatementBuilder(
						typeSystem,
						decompilationContext,
						function,
						localSettings,
						decompileRun,
						CancellationToken
					);
					body = statementBuilder.ConvertAsBlock(function.Body);

					var warningAnchor = body.Statements.FirstOrDefault();
					foreach (string warning in function.Warnings)
					{
						var warningStatement = new EmptyStatement();
						warningStatement.AddTrailingTrivia(new Comment(warning));
						if (warningAnchor != null)
							body.Statements.InsertBefore(warningAnchor, warningStatement);
						else
							body.Statements.Add(warningStatement);
					}

					entityDecl.AddChild(body, Slots.Body);
				}

				CleanUpMethodDeclaration(entityDecl, body, function, localSettings.DecompileMemberBodies);
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, method, innerException);
			}
		}

		internal static void AddAnnotationsToDeclaration(IMethod method, EntityDeclaration entityDecl, ILFunction function, int parameterOffset = 0)
		{
			int i = parameterOffset;
			var parameters = function.Variables.Where(v => v.Kind == VariableKind.Parameter).ToDictionary(v => v.Index!.Value);
			foreach (var parameter in entityDecl.GetChildren(Slots.Parameter))
			{
				if (parameters.TryGetValue(i, out var v))
					parameter.AddAnnotation(new ILVariableResolveResult(v, method.Parameters[i].Type));
				i++;
			}
			entityDecl.AddAnnotation(function);
		}

		internal static void CleanUpMethodDeclaration(EntityDeclaration entityDecl, BlockStatement? body, ILFunction function, bool decompileBody = true)
		{
			if (function.IsIterator)
			{
				if (decompileBody && !body!.Descendants.Any(d => d is YieldReturnStatement || d is YieldBreakStatement))
				{
					body.Add(new YieldBreakStatement());
				}
				if (function.IsAsync)
				{
					RemoveAttribute(entityDecl, KnownAttribute.AsyncIteratorStateMachine);
				}
				else
				{
					RemoveAttribute(entityDecl, KnownAttribute.IteratorStateMachine);
				}
				if (function.StateMachineCompiledWithMono)
				{
					RemoveAttribute(entityDecl, KnownAttribute.DebuggerHidden);
				}
				if (function.StateMachineCompiledWithLegacyVisualBasic)
				{
					RemoveAttribute(entityDecl, KnownAttribute.DebuggerStepThrough);
					if (function.Method?.IsAccessor == true && entityDecl.Parent is EntityDeclaration parentDecl)
					{
						RemoveAttribute(parentDecl, KnownAttribute.DebuggerStepThrough);
					}
				}
			}
			if (function.IsAsync)
			{
				entityDecl.Modifiers |= Modifiers.Async;
				RemoveAttribute(entityDecl, KnownAttribute.AsyncStateMachine);
				if (function.MoveNextMethod != null && AsyncMethodCompilerGeneratesDebuggerStepThrough(function.Method))
				{
					RemoveFirstAttribute(entityDecl, KnownAttribute.DebuggerStepThrough);
				}
			}
		}

		static bool AsyncMethodCompilerGeneratesDebuggerStepThrough(IMethod? method)
		{
			const int disableOptimizations = 0x100;
			var debuggableAttribute = method?.ParentModule?.GetAssemblyAttributes().FirstOrDefault(
				a => a.AttributeType.FullName == "System.Diagnostics.DebuggableAttribute");
			if (debuggableAttribute?.FixedArguments.Length == 1
				&& debuggableAttribute.FixedArguments[0].Value is int debuggingModes)
			{
				return (debuggingModes & disableOptimizations) != 0;
			}
			// The native C# compiler emits DebuggerStepThrough on async methods even in optimized builds,
			// and its output may not contain DebuggableAttribute. Unknown compiler shapes are treated likewise.
			return true;
		}

		static bool RemoveFirstAttribute(EntityDeclaration entityDecl, KnownAttribute attributeType)
		{
			foreach (var section in entityDecl.Attributes)
			{
				foreach (var attr in section.Attributes)
				{
					var symbol = attr.Type.GetSymbol();
					if (symbol is ITypeDefinition td && td.FullTypeName == attributeType.GetTypeName())
					{
						attr.Remove();
						if (section.Attributes.Count == 0)
						{
							section.Remove();
						}
						return true;
					}
				}
			}
			return false;
		}

		internal static bool RemoveAttribute(EntityDeclaration entityDecl, KnownAttribute attributeType)
		{
			bool found = false;
			foreach (var section in entityDecl.Attributes)
			{
				foreach (var attr in section.Attributes)
				{
					var symbol = attr.Type.GetSymbol();
					if (symbol is ITypeDefinition td && td.FullTypeName == attributeType.GetTypeName())
					{
						attr.Remove();
						found = true;
					}
				}
				if (section.Attributes.Count == 0)
				{
					section.Remove();
				}
			}
			return found;
		}

		internal static bool RemoveCompilerFeatureRequiredAttribute(EntityDeclaration entityDecl, string feature)
		{
			bool found = false;
			foreach (var section in entityDecl.Attributes)
			{
				foreach (var attr in section.Attributes)
				{
					var symbol = attr.Type.GetSymbol();
					if (symbol is ITypeDefinition td && td.FullTypeName == KnownAttribute.CompilerFeatureRequired.GetTypeName()
						&& attr.Arguments.Count == 1 && attr.Arguments.SingleOrDefault() is PrimitiveExpression pe
						&& pe.Value is string s && s == feature)
					{
						attr.Remove();
						found = true;
					}
				}
				if (section.Attributes.Count == 0)
				{
					section.Remove();
				}
			}
			return found;
		}

		internal static bool RemoveObsoleteAttribute(EntityDeclaration entityDecl, string message)
		{
			bool found = false;
			foreach (var section in entityDecl.Attributes)
			{
				foreach (var attr in section.Attributes)
				{
					var symbol = attr.Type.GetSymbol();
					if (symbol is ITypeDefinition td && td.FullTypeName == KnownAttribute.Obsolete.GetTypeName()
						&& attr.Arguments.Count >= 1 && attr.Arguments.First() is PrimitiveExpression pe
						&& pe.Value is string s && s == message)
					{
						attr.Remove();
						found = true;
					}
				}
				if (section.Attributes.Count == 0)
				{
					section.Remove();
				}
			}
			return found;
		}

		bool FindAttribute(EntityDeclaration entityDecl, KnownAttribute attributeType, [NotNullWhen(true)] out Syntax.Attribute? attribute)
		{
			attribute = null;
			foreach (var section in entityDecl.Attributes)
			{
				foreach (var attr in section.Attributes)
				{
					var symbol = attr.Type.GetSymbol();
					if (symbol is ITypeDefinition td && td.FullTypeName == attributeType.GetTypeName())
					{
						attribute = attr;
						return true;
					}
				}
			}
			return false;
		}

		void AddDefinesForConditionalAttributes(ILFunction function, DecompileRun decompileRun)
		{
			foreach (var call in function.Descendants.OfType<CallInstruction>())
			{
				var attr = call.Method.GetAttribute(KnownAttribute.Conditional, inherit: true);
				var symbolName = attr?.FixedArguments.FirstOrDefault().Value as string;
				if (symbolName == null || !decompileRun.DefinedSymbols.Add(symbolName))
					continue;
				syntaxTree!.AddLeadingTrivia(new PreProcessorDirective(PreProcessorDirectiveType.Define, symbolName));
			}
		}

		EntityDeclaration DoDecompile(IField field, DecompileRun decompileRun, ITypeResolveContext decompilationContext)
		{
			Debug.Assert(decompilationContext.CurrentMember == field);
			DecompilerEventSource.Log.DecompileMemberStart(field, DecompiledMemberKind.Field);
			try
			{
				var typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
				if (decompilationContext.CurrentTypeDefinition!.Kind == TypeKind.Enum && field.IsConst)
				{
					var enumDec = new EnumMemberDeclaration { Name = field.Name };
					object? constantValue = field.GetConstantValue();
					if (constantValue != null)
					{
						TypeCode underlyingTypeCode = ReflectionHelper.GetTypeCode(decompilationContext.CurrentTypeDefinition.EnumUnderlyingType);
						if (underlyingTypeCode is >= TypeCode.SByte and <= TypeCode.UInt64)
						{
							long initValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constantValue, false);
							enumDec.Initializer = typeSystemAstBuilder.ConvertEnumValue(decompilationContext.CurrentTypeDefinition, initValue, field);
						}
						else
						{
							// Unusual underlying types (bool, native int, ...) cannot be losslessly
							// squeezed through the long-based member-reference beautification.
							enumDec.Initializer = typeSystemAstBuilder.ConvertConstantValue(decompilationContext.CurrentTypeDefinition.EnumUnderlyingType!, constantValue);
						}
					}
					enumDec.Attributes.AddRange(field.GetAttributes().Select(a => new AttributeSection(typeSystemAstBuilder.ConvertAttribute(a))));
					enumDec.AddAnnotation(new MemberResolveResult(null, field));
					return enumDec;
				}
				bool isMathPIOrE = ((field.Name == "PI" || field.Name == "E") && (field.DeclaringType.FullName == "System.Math" || field.DeclaringType.FullName == "System.MathF"));
				typeSystemAstBuilder.UseSpecialConstants = !(field.DeclaringType.Equals(field.ReturnType) || isMathPIOrE);
				var fieldDecl = typeSystemAstBuilder.ConvertEntity(field);
				SetNewModifier(fieldDecl);
				if (settings.RequiredMembers && RemoveAttribute(fieldDecl, KnownAttribute.Required))
				{
					fieldDecl.Modifiers |= Modifiers.Required;
				}
				if (settings.FixedBuffers && IsFixedField(field, out var elementType, out var elementCount))
				{
					var fixedFieldDecl = new FixedFieldDeclaration();
					fieldDecl.Attributes.MoveTo(fixedFieldDecl.Attributes);
					fixedFieldDecl.Modifiers = fieldDecl.Modifiers;
					fixedFieldDecl.ReturnType = typeSystemAstBuilder.ConvertType(elementType);
					fixedFieldDecl.Variables.Add(new FixedVariableInitializer(field.Name, new PrimitiveExpression(elementCount)));
					fixedFieldDecl.Variables.Single().CopyAnnotationsFrom(((FieldDeclaration)fieldDecl).Variables.Single());
					fixedFieldDecl.CopyAnnotationsFrom(fieldDecl);
					RemoveAttribute(fixedFieldDecl, KnownAttribute.FixedBuffer);
					return fixedFieldDecl;
				}
				var fieldDefinition = metadata.GetFieldDefinition((FieldDefinitionHandle)field.MetadataToken);
				if (fieldDefinition.HasFlag(System.Reflection.FieldAttributes.HasFieldRVA))
				{
					// Field data as specified in II.16.3.1 of ECMA-335 6th edition:
					// .data I_X = int32(123)
					// .field public static int32 _x at I_X
					string message;
					try
					{
						var initVal = fieldDefinition.GetInitialValue(module.MetadataFile, TypeSystem);
						message = string.Format(" Not supported: data({0}) ", BitConverter.ToString(initVal.ReadBytes(initVal.RemainingBytes)).Replace('-', ' '));
					}
					catch (BadImageFormatException ex)
					{
						message = ex.Message;
					}
					((FieldDeclaration)fieldDecl).Variables.Single().AddTrailingTrivia(new Comment(message, CommentType.MultiLine));
				}
				return fieldDecl;
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, field, innerException);
			}
			finally
			{
				DecompilerEventSource.Log.DecompileMemberStop(field, DecompiledMemberKind.Field);
			}
		}

		internal static bool IsFixedField(IField field, [NotNullWhen(true)] out IType? type, out int elementCount)
		{
			type = null;
			elementCount = 0;
			IAttribute? attr = field.GetAttribute(KnownAttribute.FixedBuffer);
			if (attr != null && attr.FixedArguments.Length == 2)
			{
				if (attr.FixedArguments[0].Value is IType trr && attr.FixedArguments[1].Value is int length)
				{
					type = trr;
					elementCount = length;
					return true;
				}
			}
			return false;
		}

		EntityDeclaration DoDecompile(IProperty property, DecompileRun decompileRun, ITypeResolveContext decompilationContext, ExtensionInfo? extensionInfo)
		{
			Debug.Assert(decompilationContext.CurrentMember == property);
			DecompilerEventSource.Log.DecompileMemberStart(property, DecompiledMemberKind.Property);
			try
			{
				var typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
				EntityDeclaration propertyDecl = typeSystemAstBuilder.ConvertEntity(property);
				if (property.IsExplicitInterfaceImplementation && !property.IsIndexer)
				{
					int lastDot = property.Name.LastIndexOf('.');
					propertyDecl.Name = property.Name.Substring(lastDot + 1);
				}
				FixParameterNames(propertyDecl);
				Accessor? getter, setter;
				if (propertyDecl is PropertyDeclaration)
				{
					getter = ((PropertyDeclaration)propertyDecl).Getter;
					setter = ((PropertyDeclaration)propertyDecl).Setter;
				}
				else
				{
					getter = ((IndexerDeclaration)propertyDecl).Getter;
					setter = ((IndexerDeclaration)propertyDecl).Setter;
				}

				bool getterHasBody = property.CanGet && property.Getter!.HasBody;
				bool setterHasBody = property.CanSet && property.Setter!.HasBody;
				if (getterHasBody)
				{
					DecompileBody(property.Getter!, getter!, decompileRun, decompilationContext, extensionInfo);
				}
				if (setterHasBody)
				{
					DecompileBody(property.Setter!, setter!, decompileRun, decompilationContext, extensionInfo);
				}
				if (!getterHasBody && !setterHasBody && !property.IsAbstract && property.DeclaringType.Kind != TypeKind.Interface)
				{
					propertyDecl.Modifiers |= Modifiers.Extern;
				}
				var accessorHandle = (MethodDefinitionHandle)(property.Getter ?? property.Setter)!.MetadataToken;
				var accessor = metadata.GetMethodDefinition(accessorHandle);
				if (!accessorHandle.GetMethodImplementations(metadata).Any() && accessor.HasFlag(System.Reflection.MethodAttributes.Virtual) == accessor.HasFlag(System.Reflection.MethodAttributes.NewSlot))
				{
					SetNewModifier(propertyDecl);
				}
				if (property.CanGet && IsCovariantReturnOverride(property.Getter!))
				{
					RemoveAttribute(getter!, KnownAttribute.PreserveBaseOverrides);
					propertyDecl.Modifiers &= ~(Modifiers.New | Modifiers.Virtual);
					propertyDecl.Modifiers |= Modifiers.Override;
				}
				if (settings.RequiredMembers && RemoveAttribute(propertyDecl, KnownAttribute.Required))
				{
					propertyDecl.Modifiers |= Modifiers.Required;
				}
				return propertyDecl;
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, property, innerException);
			}
			finally
			{
				DecompilerEventSource.Log.DecompileMemberStop(property, DecompiledMemberKind.Property);
			}
		}

		EntityDeclaration DoDecompile(IEvent ev, DecompileRun decompileRun, ITypeResolveContext decompilationContext)
		{
			Debug.Assert(decompilationContext.CurrentMember == ev);
			DecompilerEventSource.Log.DecompileMemberStart(ev, DecompiledMemberKind.Event);
			try
			{
				bool adderHasBody = ev.CanAdd && ev.AddAccessor!.HasBody;
				bool removerHasBody = ev.CanRemove && ev.RemoveAccessor!.HasBody;
				var typeSystemAstBuilder = CreateAstBuilder(decompileRun.Settings);
				IField? backingField = null;
				bool isAutomaticEvent = adderHasBody && removerHasBody && decompileRun.Settings.AutomaticEvents
					&& AutoEventDecompiler.IsAutomaticEvent(typeSystem, ev, decompileRun, CancellationToken, out backingField);
				// A recognized automatic event is built in field-like form directly; its
				// compiler-generated accessor bodies are never decompiled. Accessors without
				// bodies (abstract, extern, interface members) cannot be expressed as custom
				// accessors in C#, so only the field-like form is valid for them as well.
				typeSystemAstBuilder.UseCustomEvents = !isAutomaticEvent
					&& (ev.IsExplicitInterfaceImplementation
						|| adderHasBody
						|| removerHasBody);
				var eventDecl = typeSystemAstBuilder.ConvertEntity(ev);
				int lastDot = ev.Name.LastIndexOf('.');
				if (ev.IsExplicitInterfaceImplementation)
				{
					eventDecl.Name = ev.Name.Substring(lastDot + 1);
				}
				if (isAutomaticEvent)
				{
					AutoEventDecompiler.AddFieldLikeEventAttributes((EventDeclaration)eventDecl, typeSystemAstBuilder, ev, backingField!);
				}
				else
				{
					if (adderHasBody)
					{
						DecompileBody(ev.AddAccessor!, ((CustomEventDeclaration)eventDecl).AddAccessor!, decompileRun, decompilationContext, null);
					}
					if (removerHasBody)
					{
						DecompileBody(ev.RemoveAccessor!, ((CustomEventDeclaration)eventDecl).RemoveAccessor!, decompileRun, decompilationContext, null);
					}
					if (!adderHasBody && !removerHasBody && !ev.IsAbstract && ev.DeclaringType.Kind != TypeKind.Interface)
					{
						eventDecl.Modifiers |= Modifiers.Extern;
					}
				}
				var accessor = metadata.GetMethodDefinition((MethodDefinitionHandle)(ev.AddAccessor ?? ev.RemoveAccessor)!.MetadataToken);
				if (accessor.HasFlag(System.Reflection.MethodAttributes.Virtual) == accessor.HasFlag(System.Reflection.MethodAttributes.NewSlot))
				{
					SetNewModifier(eventDecl);
				}
				return eventDecl;
			}
			catch (Exception innerException) when (!(innerException is OperationCanceledException || innerException is DecompilerException))
			{
				throw new DecompilerException(module, ev, innerException);
			}
			finally
			{
				DecompilerEventSource.Log.DecompileMemberStop(ev, DecompiledMemberKind.Event);
			}
		}

		/// <summary>
		/// Detects a method without a property row that overrides a base property accessor by CLR
		/// virtual slot. C# cannot express that override as a method, so returns a fake property that
		/// uses the original implementing methods as its accessor bodies.
		/// </summary>
		bool TryGetBaseAccessorMember(IMethod method, [NotNullWhen(true)] out IMember? fakeMember, [NotNullWhen(true)] out IMethod? primaryAccessor)
		{
			fakeMember = null;
			primaryAccessor = null;
			if (method.SymbolKind != SymbolKind.Method || !method.IsOverride)
				return false;
			if (FindBaseAccessor(method) is not { AccessorOwner: IProperty baseProperty } baseAccessor)
				return false;

			IMethod? getter = null, setter = null;
			foreach (IMethod sibling in method.DeclaringTypeDefinition?.Methods ?? [])
			{
				if (sibling.SymbolKind != SymbolKind.Method || !sibling.IsOverride)
					continue;
				if (FindBaseAccessor(sibling) is not { AccessorOwner: IProperty siblingBaseProperty } siblingBaseAccessor
					|| !baseProperty.Equals(siblingBaseProperty))
				{
					continue;
				}
				switch (siblingBaseAccessor.AccessorKind)
				{
					case System.Reflection.MethodSemanticsAttributes.Getter:
						getter ??= sibling;
						break;
					case System.Reflection.MethodSemanticsAttributes.Setter:
						setter ??= sibling;
						break;
				}
			}
			if (getter == null && setter == null)
				return false;

			IMethod valueAccessor = (getter ?? setter)!;
			fakeMember = new ICSharpCode.Decompiler.TypeSystem.Implementation.FakeProperty(typeSystem) {
				Name = baseProperty.Name,
				DeclaringType = method.DeclaringType,
				IsStatic = method.IsStatic,
				Accessibility = valueAccessor.Accessibility,
				ReturnType = getter != null ? getter.ReturnType : setter!.Parameters.Last().Type,
				ReturnTypeIsRefReadOnly = getter != null && getter.ReturnTypeIsRefReadOnly,
				Getter = getter,
				Setter = setter,
				IsIndexer = baseProperty.IsIndexer,
				Parameters = valueAccessor.Parameters
					.Take(getter != null ? getter.Parameters.Count : setter!.Parameters.Count - 1)
					.ToArray(),
			};
			primaryAccessor = valueAccessor;
			return true;

			IMethod? FindBaseAccessor(IMethod implementation)
			{
				foreach (IType baseType in implementation.DeclaringType.GetNonInterfaceBaseTypes().Reverse())
				{
					if (baseType.GetDefinition()?.Equals(implementation.DeclaringTypeDefinition) == true)
						continue;
					foreach (IMethod candidate in baseType.GetAccessors(
						a => a.Name == implementation.Name, GetMemberOptions.IgnoreInheritedMembers))
					{
						if (HaveSameRuntimeSignature(implementation, candidate))
							return candidate;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Detects an explicit interface implementation whose <c>.override</c> targets a property or
		/// event accessor, but for which the implementing type carries no property/event metadata row.
		/// Such a method is classified as <see cref="SymbolKind.Method"/> and would otherwise be emitted
		/// as a dotted-name method (e.g. <c>IFoo.get_Token()</c>), which does not compile.
		/// When detected, a reconstructed property/event is returned via <paramref name="fakeMember"/>,
		/// gathering all sibling accessors that implement the same interface member, and
		/// <paramref name="primaryAccessor"/> identifies the single accessor that owns the emission.
		/// </summary>
		bool TryGetExplicitInterfaceAccessorMember(IMethod method, [NotNullWhen(true)] out IMember? fakeMember, [NotNullWhen(true)] out IMethod? primaryAccessor)
		{
			fakeMember = null;
			primaryAccessor = null;
			if (method.SymbolKind != SymbolKind.Method || !method.IsExplicitInterfaceImplementation)
				return false;
			if (method.ExplicitlyImplementedInterfaceMembers.FirstOrDefault() is not IMethod interfaceAccessor)
				return false;
			if (!interfaceAccessor.IsAccessor)
				return false;
			IMember interfaceMember = interfaceAccessor.AccessorOwner;
			if (interfaceMember is not (IProperty or IEvent))
				return false;

			// Gather the implementing accessor methods of this type that explicitly implement an
			// accessor of the same interface property/event. Drive the accessor roles off the
			// interface accessor's semantic kind, not the raw 'get_'/'set_' name string.
			IMethod? getter = null, setter = null, adder = null, remover = null;
			foreach (var sibling in method.DeclaringTypeDefinition?.Methods ?? [])
			{
				if (sibling.SymbolKind != SymbolKind.Method || !sibling.IsExplicitInterfaceImplementation)
					continue;
				if (sibling.ExplicitlyImplementedInterfaceMembers.FirstOrDefault() is not IMethod siblingInterfaceAccessor)
					continue;
				if (!siblingInterfaceAccessor.IsAccessor || !interfaceMember.Equals(siblingInterfaceAccessor.AccessorOwner))
					continue;
				switch (siblingInterfaceAccessor.AccessorKind)
				{
					case System.Reflection.MethodSemanticsAttributes.Getter:
						getter = sibling;
						break;
					case System.Reflection.MethodSemanticsAttributes.Setter:
						setter = sibling;
						break;
					case System.Reflection.MethodSemanticsAttributes.Adder:
						adder = sibling;
						break;
					case System.Reflection.MethodSemanticsAttributes.Remover:
						remover = sibling;
						break;
				}
			}

			string explicitName = GetExplicitInterfaceMemberName(method.Name, interfaceMember.Name);
			if (interfaceMember is IProperty interfaceProperty)
			{
				if (getter == null && setter == null)
					return false;
				IMethod valueAccessor = (getter ?? setter)!;
				var fakeProperty = new ICSharpCode.Decompiler.TypeSystem.Implementation.FakeProperty(typeSystem) {
					Name = explicitName,
					DeclaringType = method.DeclaringType,
					IsStatic = method.IsStatic,
					Accessibility = valueAccessor.Accessibility,
					IsExplicitInterfaceImplementation = true,
					ExplicitlyImplementedInterfaceMembers = new[] { interfaceMember },
					ReturnType = getter != null ? getter.ReturnType : setter!.Parameters.Last().Type,
					ReturnTypeIsRefReadOnly = getter != null && getter.ReturnTypeIsRefReadOnly,
					Getter = getter,
					Setter = setter,
					IsIndexer = interfaceProperty.IsIndexer,
					Parameters = valueAccessor.Parameters
						.Take(getter != null ? getter.Parameters.Count : setter!.Parameters.Count - 1)
						.ToArray(),
				};
				fakeMember = fakeProperty;
				primaryAccessor = valueAccessor;
				return true;
			}
			else
			{
				if (adder == null && remover == null)
					return false;
				IMethod valueAccessor = (adder ?? remover)!;
				var fakeEvent = new ICSharpCode.Decompiler.TypeSystem.Implementation.FakeEvent(typeSystem) {
					Name = explicitName,
					DeclaringType = method.DeclaringType,
					IsStatic = method.IsStatic,
					Accessibility = valueAccessor.Accessibility,
					IsExplicitInterfaceImplementation = true,
					ExplicitlyImplementedInterfaceMembers = new[] { interfaceMember },
					ReturnType = valueAccessor.Parameters.Last().Type,
					AddAccessor = adder,
					RemoveAccessor = remover,
				};
				fakeMember = fakeEvent;
				primaryAccessor = valueAccessor;
				return true;
			}
		}

		/// <summary>
		/// Builds the explicit-interface member name (e.g. <c>IFoo.Token</c>) from the implementing
		/// accessor method's name (e.g. <c>IFoo.get_Token</c>) and the interface member's short name.
		/// </summary>
		static string GetExplicitInterfaceMemberName(string accessorMethodName, string interfaceMemberName)
		{
			int lastDot = accessorMethodName.LastIndexOf('.');
			if (lastDot < 0)
				return interfaceMemberName;
			return accessorMethodName.Substring(0, lastDot + 1) + interfaceMemberName;
		}

		#region Sequence Points
		/// <summary>
		/// Creates sequence points for the given syntax tree.
		/// 
		/// This only works correctly when the nodes in the syntax tree have line/column information.
		/// </summary>
		public Dictionary<ILFunction, List<DebugInfo.SequencePoint>> CreateSequencePoints(SyntaxTree syntaxTree)
		{
			SequencePointBuilder spb = new SequencePointBuilder();
			syntaxTree.AcceptVisitor(spb);
			return spb.GetSequencePoints();
		}
		#endregion
	}
}

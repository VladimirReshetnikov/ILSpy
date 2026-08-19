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

using System;
using System.Linq;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Tests.TypeSystem;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture]
	class LegalizeYieldInTryCatchTests
	{
		ICompilation compilation = null!;
		MemberLookup memberLookup = null!;

		[OneTimeSetUp]
		public void SetUp()
		{
			compilation = new SimpleCompilation(
				TypeSystemLoaderTests.TestAssembly,
				TypeSystemLoaderTests.Mscorlib.WithOptions(TypeSystemOptions.Default));
			var currentType = compilation.FindType(typeof(LegalizeYieldInTryCatchTests)).GetDefinition()!;
			memberLookup = new MemberLookup(currentType, compilation.MainModule);
		}

		[Test]
		public void ExactEnumeratorCallAcceptsConcreteStructReceiver()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var call = InstanceCall(OpCode.Call, Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)),
				new LdLoca(enumerator));

			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(call,
				nameof(ProbeEnumerator.MoveNext), enumeratorType, enumerator), Is.True);
		}

		[Test]
		public void ExactEnumeratorCallRejectsWrongStructOpcode()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var call = InstanceCall(OpCode.CallVirt, Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)),
				new LdLoca(enumerator));

			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(call,
				nameof(ProbeEnumerator.MoveNext), enumeratorType, enumerator), Is.False);
		}

		[Test]
		public void ExactEnumeratorCallAcceptsReferenceReceiverCallVirtOnly()
		{
			var collectionType = TypeOf<ProbeCollection>();
			var collection = new ILVariable(VariableKind.Local, collectionType);
			var getEnumerator = Method(collectionType, nameof(ProbeCollection.GetEnumerator));
			var callVirt = InstanceCall(OpCode.CallVirt, getEnumerator, new LdLoc(collection));
			var call = InstanceCall(OpCode.Call, getEnumerator, new LdLoc(collection));

			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(callVirt,
				nameof(ProbeCollection.GetEnumerator), collectionType, collection), Is.True);
			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(call,
				nameof(ProbeCollection.GetEnumerator), collectionType, collection), Is.False);
		}

		[Test]
		public void ExactEnumeratorCallRejectsConstrainedInterfaceDispatch()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var interfaceType = TypeOf<IProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var call = InstanceCall(OpCode.CallVirt, Method(interfaceType, nameof(IProbeEnumerator.MoveNext)),
				new LdLoca(enumerator));
			call.ConstrainedTo = enumeratorType;

			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(call,
				nameof(IProbeEnumerator.MoveNext), enumeratorType, enumerator), Is.False);
		}

		[Test]
		public void ExactEnumeratorCallRejectsInterfaceMemberAndDifferentReceiver()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var interfaceType = TypeOf<IProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var interfaceEnumerator = new ILVariable(VariableKind.Local, interfaceType);
			var otherEnumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var interfaceCall = InstanceCall(OpCode.Call,
				Method(interfaceType, nameof(IProbeEnumerator.MoveNext)), new LdLoca(enumerator));
			var interfaceReceiverCall = InstanceCall(OpCode.CallVirt,
				Method(interfaceType, nameof(IProbeEnumerator.MoveNext)), new LdLoc(interfaceEnumerator));
			var wrongReceiverCall = InstanceCall(OpCode.Call,
				Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)), new LdLoca(otherEnumerator));

			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(interfaceCall,
				nameof(IProbeEnumerator.MoveNext), enumeratorType, enumerator), Is.False);
			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(interfaceReceiverCall,
				nameof(IProbeEnumerator.MoveNext), interfaceType, interfaceEnumerator), Is.False);
			Assert.That(LegalizeYieldInTryCatch.IsExactEnumeratorCall(wrongReceiverCall,
				nameof(ProbeEnumerator.MoveNext), enumeratorType, enumerator), Is.False);
		}

		[Test]
		public void NoDisposeProvenanceRequiresBareWhileLoopAnnotation()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var moveNext = InstanceCall(OpCode.Call,
				Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)), new LdLoca(enumerator));
			var loop = new BlockContainer(ContainerKind.While);
			var block = new Block();
			block.Instructions.Add(moveNext);
			loop.Blocks.Add(block);
			var foreachStatement = new ForeachStatement();
			foreachStatement.AddAnnotation(loop);

			Assert.That(LegalizeYieldInTryCatch.HasNoDisposeForeachProvenance(
				foreachStatement, moveNext), Is.True);
		}

		[Test]
		public void NoDisposeProvenanceRejectsUsingInstruction()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var moveNext = InstanceCall(OpCode.Call,
				Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)), new LdLoca(enumerator));
			var loop = new BlockContainer(ContainerKind.While);
			var block = new Block();
			block.Instructions.Add(moveNext);
			loop.Blocks.Add(block);
			var foreachStatement = new ForeachStatement();
			foreachStatement.AddAnnotation(loop);
			foreachStatement.AddAnnotation(new UsingInstruction(
				new ILVariable(VariableKind.UsingLocal, enumeratorType), new Nop(), new Nop()));

			Assert.That(LegalizeYieldInTryCatch.HasNoDisposeForeachProvenance(
				foreachStatement, moveNext), Is.False);
		}

		[Test]
		public void NoDisposeProvenanceRejectsMissingLoopAnnotation()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var enumerator = new ILVariable(VariableKind.Local, enumeratorType);
			var moveNext = InstanceCall(OpCode.Call,
				Method(enumeratorType, nameof(ProbeEnumerator.MoveNext)), new LdLoca(enumerator));

			Assert.That(LegalizeYieldInTryCatch.HasNoDisposeForeachProvenance(
				new ForeachStatement(), moveNext), Is.False);
		}

		[Test]
		public void NameabilityAcceptsAccessibleNamedType()
		{
			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(
				compilation.FindType(KnownTypeCode.Int32), memberLookup), Is.True);
		}

		[Test]
		public void NameabilityAcceptsAccessibleConstructedGenericType()
		{
			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(
				TypeOf<PublicGenericEnumerator<int>>(), memberLookup), Is.True);
		}

		[TestCaseSource(nameof(InaccessibleTypes))]
		public void NameabilityRejectsInaccessibleTypes(Type reflectionType)
		{
			var type = compilation.FindType(reflectionType);
			Assert.That(type.Kind, Is.EqualTo(TypeKind.Struct));
			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(type, memberLookup), Is.False);
		}

		[Test]
		public void NameabilityUsesContainingMemberAccessibilityContext()
		{
			var type = compilation.FindType(InaccessibleTypeOwner.PrivateEnumeratorType);
			var declaringType = compilation.FindType(typeof(InaccessibleTypeOwner)).GetDefinition()!;
			var currentMember = declaringType.GetProperties(property =>
				property.Name == nameof(InaccessibleTypeOwner.PrivateEnumeratorType)).Single().Getter!;
			var resolver = new CSharpResolver(new CSharpTypeResolveContext(
				compilation.MainModule, typeDefinition: declaringType, member: currentMember));

			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(type, memberLookup), Is.False);
			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(
				type, resolver.CreateMemberLookup()), Is.True);
		}

		static Type[] InaccessibleTypes => new[] {
			InaccessibleTypeOwner.PrivateEnumeratorType,
			InaccessibleTypeOwner.ConstructedEnumeratorType,
			InaccessibleTypeOwner.PublicNestedInPrivateType
		};

		[Test]
		public void NameabilityRejectsUnknownType()
		{
			Assert.That(LegalizeYieldInTryCatch.IsCSharpNameableAndAccessible(
				SpecialType.UnknownType, memberLookup), Is.False);
		}

		[TestCase("Invalid-Name", "ICSharpCode.Decompiler.Tests")]
		[TestCase("ValidName", "ICSharpCode.Invalid-Namespace")]
		[TestCase("ValidName", ".ICSharpCode")]
		public void NameabilityRejectsInvalidMetadataNames(string name, string @namespace)
		{
			Assert.That(LegalizeYieldInTryCatch.HasValidCSharpTypeName(name, @namespace), Is.False);
		}

		[Test]
		public void AccessibleGetterRequiresBothPropertyAndGetterAccessibility()
		{
			var publicCurrent = Property(TypeOf<ProbeEnumerator>(), nameof(ProbeEnumerator.Current));
			var privateGetter = Property(TypeOf<PropertyTest>(),
				nameof(PropertyTest.PropertyWithPrivateGetter));

			Assert.That(LegalizeYieldInTryCatch.HasAccessibleGetter(publicCurrent, memberLookup), Is.True);
			Assert.That(memberLookup.IsAccessible(privateGetter, allowProtectedAccess: false), Is.True);
			Assert.That(LegalizeYieldInTryCatch.HasAccessibleGetter(privateGetter, memberLookup), Is.False);
		}

		[Test]
		public void PlainEnumeratorDoesNotRequireDisposal()
		{
			Assert.That(LegalizeYieldInTryCatch.EnumeratorRequiresDisposal(
				TypeOf<PlainEnumerator>()), Is.False);
		}

		[Test]
		public void IDisposableEnumeratorRequiresDisposal()
		{
			var type = TypeOf<DisposableEnumerator>();
			Assert.That(type.GetAllBaseTypes()
				.Any(baseType => baseType.IsKnownType(KnownTypeCode.IDisposable)), Is.True);
			Assert.That(LegalizeYieldInTryCatch.EnumeratorRequiresDisposal(type), Is.True);
		}

		[Test]
		public void PatternDisposableEnumeratorRequiresDisposal()
		{
			Assert.That(LegalizeYieldInTryCatch.EnumeratorRequiresDisposal(
				TypeOf<PatternDisposableEnumerator>()), Is.True);
		}

		[Test]
		public void ForeachVariableConfinementAcceptsSingleYieldUse()
		{
			var context = CreateConfinementContext();

			Assert.That(IsConfined(context), Is.True);
		}

		[Test]
		public void ForeachVariableConfinementRejectsSecondStore()
		{
			var context = CreateConfinementContext();
			context.Body.Instructions.Add(new StLoc(context.ItemVariable, new LdcI4(0)));

			Assert.That(IsConfined(context), Is.False);
		}

		[Test]
		public void ForeachVariableConfinementRejectsLoadOutsideYieldValue()
		{
			var context = CreateConfinementContext();
			var consume = new Call(Method(TypeOf<LegalizeYieldInTryCatchTests>(), nameof(Consume)));
			consume.Arguments.Add(new LdLoc(context.ItemVariable));
			context.Body.Instructions.Add(consume);

			Assert.That(IsConfined(context), Is.False);
		}

		[Test]
		public void ForeachVariableConfinementRejectsAddressUse()
		{
			var context = CreateConfinementContext();
			var consume = new Call(Method(TypeOf<LegalizeYieldInTryCatchTests>(), nameof(ConsumeByRef)));
			consume.Arguments.Add(new LdLoca(context.ItemVariable));
			context.Body.Instructions.Add(consume);

			Assert.That(IsConfined(context), Is.False);
		}

		[Test]
		public void ForeachVariableConfinementRejectsCapture()
		{
			var context = CreateConfinementContext();
			context.ItemVariable.CaptureScope = new BlockContainer();

			Assert.That(IsConfined(context), Is.False);
		}

		[Test]
		public void ForeachVariableConfinementRejectsInitialValueUse()
		{
			var context = CreateConfinementContext();
			context.ItemVariable.UsesInitialValue = true;

			Assert.That(IsConfined(context), Is.False);
		}

		[Test]
		public void ForeachVariableConfinementRejectsAstUseOutsideYieldExpression()
		{
			var context = CreateConfinementContext();
			var identifier = new IdentifierExpression("item");
			identifier.AddAnnotation(new ILVariableResolveResult(
				context.ItemVariable, context.ItemVariable.Type));
			context.AstRoot.Statements.Add(new ExpressionStatement { Expression = identifier });

			Assert.That(IsConfined(context), Is.False);
		}

		ConfinementContext CreateConfinementContext()
		{
			var enumeratorType = TypeOf<ProbeEnumerator>();
			var itemType = compilation.FindType(KnownTypeCode.Int32);
			var enumeratorVariable = new ILVariable(VariableKind.Local, enumeratorType);
			var itemVariable = new ILVariable(VariableKind.ForeachLocal, itemType);
			var getCurrentCall = InstanceCall(OpCode.Call,
				Property(enumeratorType, nameof(ProbeEnumerator.Current)).Getter!,
				new LdLoca(enumeratorVariable));
			var body = new Block();
			body.Instructions.Add(new StLoc(itemVariable, getCurrentCall));
			var yieldInstruction = new IL.YieldReturn(new LdLoc(itemVariable));
			body.Instructions.Add(yieldInstruction);
			var function = new ILFunction(
				returnType: SpecialType.UnknownType,
				parameters: Array.Empty<IParameter>(),
				genericContext: new GenericContext(),
				body: body);
			function.Variables.Add(enumeratorVariable);
			function.Variables.Add(itemVariable);
			function.AddRef();

			var designation = new SingleVariableDesignation { Identifier = "item" };
			designation.AddAnnotation(new ILVariableResolveResult(itemVariable, itemType));
			var identifier = new IdentifierExpression("item");
			identifier.AddAnnotation(new ILVariableResolveResult(itemVariable, itemType));
			var yieldStatement = new YieldReturnStatement { Expression = identifier };
			yieldStatement.AddAnnotation(yieldInstruction);
			var foreachBody = new BlockStatement { yieldStatement };
			var foreachStatement = new ForeachStatement {
				VariableType = new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveType("int"),
				VariableDesignation = designation,
				InExpression = new IdentifierExpression("items"),
				EmbeddedStatement = foreachBody
			};
			var astRoot = new BlockStatement { foreachStatement };

			return new ConfinementContext(function, body, itemVariable, getCurrentCall,
				yieldInstruction, astRoot, designation, yieldStatement);
		}

		static bool IsConfined(ConfinementContext context)
		{
			return LegalizeYieldInTryCatch.IsForeachVariableConfined(
				context.ItemVariable, context.GetCurrentCall, context.YieldStatement,
				context.Designation, context.AstRoot, context.Function);
		}

		IType TypeOf<T>()
		{
			return compilation.FindType(typeof(T));
		}

		static IMethod Method(IType type, string name)
		{
			return type.GetMethods(method => method.Name == name).Single();
		}

		static IProperty Property(IType type, string name)
		{
			return type.GetProperties(property => property.Name == name).Single();
		}

		static CallInstruction InstanceCall(OpCode opCode, IMethod method, ILInstruction receiver)
		{
			var call = CallInstruction.Create(opCode, method);
			call.Arguments.Add(receiver);
			return call;
		}

		public static void Consume(int value)
		{
		}

		public static void ConsumeByRef(ref int value)
		{
		}

		public interface IProbeEnumerator
		{
			bool MoveNext();
			int Current { get; }
		}

		public struct ProbeEnumerator : IProbeEnumerator
		{
			public bool MoveNext() => false;
			public int Current => 0;
		}

		public sealed class ProbeCollection
		{
			public ProbeEnumerator GetEnumerator() => default;
		}

		public struct PlainEnumerator
		{
			public bool MoveNext() => false;
			public int Current => 0;
		}

		public struct DisposableEnumerator : IDisposable
		{
			public bool MoveNext() => false;
			public int Current => 0;
			void IDisposable.Dispose()
			{
			}
		}

		public struct PatternDisposableEnumerator
		{
			public bool MoveNext() => false;
			public int Current => 0;
			public void Dispose()
			{
			}
		}

		public struct PublicGenericEnumerator<T>
		{
		}

		sealed class ConfinementContext
		{
			public ILFunction Function { get; }
			public Block Body { get; }
			public ILVariable ItemVariable { get; }
			public CallInstruction GetCurrentCall { get; }
			public IL.YieldReturn YieldInstruction { get; }
			public BlockStatement AstRoot { get; }
			public SingleVariableDesignation Designation { get; }
			public YieldReturnStatement YieldStatement { get; }

			public ConfinementContext(ILFunction function, Block body, ILVariable itemVariable,
				CallInstruction getCurrentCall, IL.YieldReturn yieldInstruction,
				BlockStatement astRoot, SingleVariableDesignation designation,
				YieldReturnStatement yieldStatement)
			{
				Function = function;
				Body = body;
				ItemVariable = itemVariable;
				GetCurrentCall = getCurrentCall;
				YieldInstruction = yieldInstruction;
				AstRoot = astRoot;
				Designation = designation;
				YieldStatement = yieldStatement;
			}
		}
	}

	static class InaccessibleTypeOwner
	{
		private struct PrivateEnumerator
		{
		}

		private struct PrivateArgument
		{
		}

		private sealed class PrivateDeclaringType
		{
			public struct PublicNestedEnumerator
			{
			}
		}

		internal static Type PrivateEnumeratorType => typeof(PrivateEnumerator);
		internal static Type ConstructedEnumeratorType =>
			typeof(LegalizeYieldInTryCatchTests.PublicGenericEnumerator<PrivateArgument>);
		internal static Type PublicNestedInPrivateType =>
			typeof(PrivateDeclaringType.PublicNestedEnumerator);
	}
}

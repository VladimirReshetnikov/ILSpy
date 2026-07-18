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

using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.Decompiler.TypeSystem;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Output
{
	[TestFixture]
	public class CallBuilderTests
	{
		static readonly string SamplesPath = Path.Combine(Tester.TestCasePath, "..", "Output");
		const string SampleTypeName = "ICSharpCode.Decompiler.Tests.TestCases.CallBuilder.CallBuilderSamples";

		CompilerResults compiledSamples;
		string metadataSamplesAssembly;

		[OneTimeSetUp]
		public void CompileSamples()
		{
			var sourceFile = Path.Combine(SamplesPath, "CallBuilderSamples.cs");
			compiledSamples = Tester.CompileCSharp(sourceFile,
				CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.Library).GetAwaiter().GetResult();
			metadataSamplesAssembly = Tester.AssembleIL(
				Path.Combine(SamplesPath, "CallBuilderMetadataSamples.il"),
				AssemblerOptions.Library).GetAwaiter().GetResult();
		}

		[OneTimeTearDown]
		public void DeleteCompiledSamples()
		{
			compiledSamples?.DeleteTempFiles();
			if (metadataSamplesAssembly != null)
				Tester.RepeatOnIOError(() => File.Delete(metadataSamplesAssembly));
		}

		SyntaxTree DecompileSamples(PEFile module)
		{
			string assemblyPath = compiledSamples.PathToAssembly;
			var resolver = new UniversalAssemblyResolver(assemblyPath, false, module.Metadata.DetectTargetFrameworkId());
			var decompiler = new CSharpDecompiler(module, resolver, new DecompilerSettings());
			return decompiler.DecompileType(new FullTypeName(SampleTypeName));
		}

		[Test]
		public void ReorderedArgumentsRetainTheirSemanticMapAndDefaultValueAnnotation()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "ReorderedGenericTupleDefault");
			var invocation = method.Descendants.OfType<InvocationExpression>()
				.Single(i => i.Target is IdentifierExpression { Identifier: "ValueOrDefault" });

			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(invocation.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "key", "dictionary", "defaultValue" }), syntaxTree.ToString());
			Assert.That(resolveResult.GetArgumentToParameterMap(), Is.EqualTo(new[] { 1, 0, 2 }), syntaxTree.ToString());
			Assert.That(resolveResult.Arguments, Has.Count.EqualTo(3));

			var argumentsForCall = resolveResult.GetArgumentsForCall();
			Assert.That(argumentsForCall, Has.Count.EqualTo(3));
			Assert.That(argumentsForCall[0], Is.SameAs(resolveResult.Arguments[1]));
			Assert.That(argumentsForCall[1], Is.SameAs(resolveResult.Arguments[0]));
			Assert.That(argumentsForCall[2], Is.SameAs(resolveResult.Arguments[2]));
			Assert.That(argumentsForCall[0].Type.FullName, Is.EqualTo("System.Collections.Generic.IDictionary"));
			Assert.That(argumentsForCall[1].Type.IsKnownType(KnownTypeCode.Int32));

			var tupleType = argumentsForCall[2].Type as TupleType;
			Assert.That(tupleType, Is.Not.Null);
			Assert.That(tupleType.ElementNames, Is.EqualTo(new[] { "value", "fallback" }));

			var defaultExpression = invocation.Arguments
				.SelectMany(a => a.DescendantsAndSelf)
				.OfType<DefaultValueExpression>()
				.Single();
			var defaultValue = defaultExpression.Annotation<DefaultValue>();
			Assert.That(defaultValue, Is.Not.Null, "tuple-name recovery must retain the source IL annotation");
			Assert.That(defaultExpression.GetResolveResult().Type, Is.EqualTo(tupleType));
		}

		[Test]
		public void LowerPriorityMetadataTargetUsesNamedArguments()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "PreserveLowerPriorityOverload");
			var invocation = method.Descendants.OfType<InvocationExpression>()
				.Single(i => i.Target is IdentifierExpression { Identifier: "CreateByCurrentContext" });

			Assert.That(invocation.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "callerFilePath", "callerMemberName" }), syntaxTree.ToString());
			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(resolveResult.Member.Parameters, Has.Count.EqualTo(2), syntaxTree.ToString());
		}

		[Test]
		public void LowerPriorityConstructorTargetUsesNamedArguments()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "PreserveLowerPriorityConstructor");
			var objectCreation = method.Descendants.OfType<ObjectCreateExpression>().Single();

			Assert.That(objectCreation.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "callerFilePath", "callerMemberName" }), syntaxTree.ToString());
			var resolveResult = objectCreation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(resolveResult.Member.Parameters, Has.Count.EqualTo(2), syntaxTree.ToString());
		}

		[Test]
		public void LowerPriorityIndexerTargetUsesNamedArguments()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "PreserveLowerPriorityIndexer");
			var indexer = method.Descendants.OfType<IndexerExpression>().Single();

			Assert.That(indexer.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "callerFilePath", "callerMemberName" }), syntaxTree.ToString());
			var resolveResult = indexer.GetResolveResult() as MemberResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(((IProperty)resolveResult.Member).Parameters, Has.Count.EqualTo(2), syntaxTree.ToString());
		}

		[TestCase("PreserveReorderedLowerPriorityOverload", "ReorderedPriority")]
		public void ReorderedLowerPriorityMethodTargetUsesCompleteNamedArguments(string methodName, string invokedMethodName)
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>().Single(m => m.Name == methodName);
			var invocation = method.Descendants.OfType<InvocationExpression>()
				.Single(i => i.Target is IdentifierExpression { Identifier: var identifier } && identifier == invokedMethodName);

			Assert.That(invocation.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "a", "c", "b" }), syntaxTree.ToString());
			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(resolveResult.Member.Parameters[0].Type.IsKnownType(KnownTypeCode.String), Is.True, syntaxTree.ToString());
		}

		[TestCase("PreserveReorderedLowerPriorityIndexer")]
		[TestCase("PreserveReorderedLowerPriorityIndexerSetter")]
		public void ReorderedLowerPriorityIndexerTargetUsesCompleteNamedArguments(string methodName)
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>().Single(m => m.Name == methodName);
			var indexer = method.Descendants.OfType<IndexerExpression>().Single();

			Assert.That(indexer.Arguments.Select(a => (a as NamedArgumentExpression)?.Name),
				Is.EqualTo(new[] { "a", "b", "c" }), syntaxTree.ToString());
			var resolveResult = indexer.GetResolveResult() as MemberResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(((IProperty)resolveResult.Member).Parameters[0].Type.IsKnownType(KnownTypeCode.String), Is.True,
				syntaxTree.ToString());
		}

		[Test]
		public void LowerPriorityParamsTargetRetainsNamedArrayArgument()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "PreserveLowerPriorityParamsOverload");
			var invocation = method.Descendants.OfType<InvocationExpression>()
				.Single(i => i.Target is IdentifierExpression { Identifier: "ParamsPriority" });

			Assert.That(invocation.Arguments, Has.Count.EqualTo(1));
			Assert.That(invocation.Arguments[0], Is.TypeOf<NamedArgumentExpression>());
			Assert.That(((NamedArgumentExpression)invocation.Arguments[0]).Name, Is.EqualTo("values"), syntaxTree.ToString());
			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(((ArrayType)resolveResult.Member.Parameters[0].Type).ElementType.IsKnownType(KnownTypeCode.String), Is.True,
				syntaxTree.ToString());
		}

		[Test]
		public void OrdinaryOverloadUsesCastInsteadOfNamedArgument()
		{
			using var module = new PEFile(compiledSamples.PathToAssembly, PEStreamOptions.PrefetchEntireImage);
			var syntaxTree = DecompileSamples(module);
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "PreserveOrdinaryObjectOverload");
			var invocation = method.Descendants.OfType<InvocationExpression>()
				.Single(i => i.Target is IdentifierExpression { Identifier: "OrdinaryOverload" });

			Assert.That(invocation.Arguments, Has.Count.EqualTo(1));
			Assert.That(invocation.Arguments[0], Is.Not.TypeOf<NamedArgumentExpression>(), syntaxTree.ToString());
			Assert.That(invocation.Arguments[0].DescendantsAndSelf.OfType<CastExpression>(), Has.Exactly(1).Items,
				syntaxTree.ToString());
			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(resolveResult.Member.Parameters[0].Type.IsKnownType(KnownTypeCode.Object), Is.True,
				syntaxTree.ToString());
		}

		[Test]
		public void NullConditionalLowerPriorityExtensionTargetIsValidCSharp()
		{
			using var module = new PEFile(metadataSamplesAssembly, PEStreamOptions.PrefetchEntireImage);
			var resolver = new UniversalAssemblyResolver(metadataSamplesAssembly, false, module.Metadata.DetectTargetFrameworkId());
			var decompiler = new CSharpDecompiler(module, resolver, new DecompilerSettings());
			var syntaxTree = decompiler.DecompileType(new FullTypeName("CallBuilderMetadataSamples"));
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "SelectLowerPriorityExtensionConditionally");
			var invocation = method.Descendants.OfType<InvocationExpression>().Single();

			Assert.That(invocation.Arguments, Is.Empty, syntaxTree.ToString());
			Assert.That(invocation.Target, Is.TypeOf<MemberReferenceExpression>());
			var memberReference = (MemberReferenceExpression)invocation.Target;
			Assert.That(memberReference.MemberName, Is.EqualTo("Select"), syntaxTree.ToString());
			Assert.That(memberReference.Target, Is.TypeOf<UnaryOperatorExpression>());
			Assert.That(((UnaryOperatorExpression)memberReference.Target).Operator,
				Is.EqualTo(UnaryOperatorType.NullConditional), syntaxTree.ToString());
			var resolveResult = invocation.GetResolveResult() as CSharpInvocationResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(resolveResult.IsExtensionMethodInvocation, Is.True, syntaxTree.ToString());
			Assert.That(resolveResult.Member.Parameters[0].Type.FullName,
				Is.EqualTo("LowPriorityExtensionReceiver"), syntaxTree.ToString());
		}

		[Test]
		public void NamedExtensionReceiverMustMapToFirstParameter()
		{
			using var module = new PEFile(metadataSamplesAssembly, PEStreamOptions.PrefetchEntireImage);
			var resolver = new UniversalAssemblyResolver(metadataSamplesAssembly, false, module.Metadata.DetectTargetFrameworkId());
			var decompiler = new CSharpDecompiler(module, resolver, new DecompilerSettings());
			var syntaxTree = decompiler.DecompileType(new FullTypeName("CallBuilderMetadataSamples"));
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "SelectLowerPriorityExtensionConditionally");
			var extensionMethod = (IMethod)((CSharpInvocationResolveResult)method.Descendants.OfType<InvocationExpression>()
				.Single().GetResolveResult()).Member;
			var matchingArgument = new NamedArgumentExpression("lowReceiver", new NullReferenceExpression());
			var mismatchedArgument = new NamedArgumentExpression("otherParameter", new NullReferenceExpression());

			Assert.That(IntroduceExtensionMethods.TryGetExtensionReceiverArgument(
				extensionMethod, matchingArgument, out var receiver), Is.True);
			Assert.That(receiver, Is.SameAs(matchingArgument.Expression));
			Assert.That(IntroduceExtensionMethods.TryGetExtensionReceiverArgument(
				extensionMethod, mismatchedArgument, out _), Is.False);
		}

		[Test]
		public void IndexerSetterUsesPropertyParameterName()
		{
			using var module = new PEFile(metadataSamplesAssembly, PEStreamOptions.PrefetchEntireImage);
			var resolver = new UniversalAssemblyResolver(metadataSamplesAssembly, false, module.Metadata.DetectTargetFrameworkId());
			var decompiler = new CSharpDecompiler(module, resolver, new DecompilerSettings());
			var syntaxTree = decompiler.DecompileType(new FullTypeName("CallBuilderMetadataSamples"));
			var method = syntaxTree.Descendants.OfType<MethodDeclaration>()
				.Single(m => m.Name == "SetLowerPriorityIndexer");
			var indexer = method.Descendants.OfType<IndexerExpression>().Single();

			Assert.That(indexer.Arguments, Has.Count.EqualTo(1));
			Assert.That(indexer.Arguments[0], Is.TypeOf<NamedArgumentExpression>());
			Assert.That(((NamedArgumentExpression)indexer.Arguments[0]).Name, Is.EqualTo("lowName"), syntaxTree.ToString());
			var resolveResult = indexer.GetResolveResult() as MemberResolveResult;
			Assert.That(resolveResult, Is.Not.Null);
			Assert.That(((IProperty)resolveResult.Member).Parameters, Has.Count.EqualTo(1), syntaxTree.ToString());
		}
	}
}

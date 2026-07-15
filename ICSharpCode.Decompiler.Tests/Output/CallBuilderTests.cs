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
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
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

		[OneTimeSetUp]
		public void CompileSamples()
		{
			var sourceFile = Path.Combine(SamplesPath, "CallBuilderSamples.cs");
			compiledSamples = Tester.CompileCSharp(sourceFile,
				CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.Library).GetAwaiter().GetResult();
		}

		[OneTimeTearDown]
		public void DeleteCompiledSamples()
		{
			compiledSamples?.DeleteTempFiles();
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
	}
}

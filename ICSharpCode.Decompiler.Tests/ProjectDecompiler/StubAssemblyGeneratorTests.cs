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

using System;
using System.IO;
using System.Linq;

using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Tests.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.ProjectDecompiler
{
	/// <summary>
	/// A consumer's metadata records every type and member it expects a dependency to declare.
	/// The stub generator turns those expectations back into compilable C# declarations, so a
	/// missing dependency can be replaced by a stub assembly when recompiling decompiled output.
	/// </summary>
	[TestFixture]
	public class StubAssemblyGeneratorTests
	{
		const string DependencySource = @"
namespace Phantom.Core
{
	public class ServiceBase
	{
		public ServiceBase(int seed) { }
		public virtual string Describe(string name) => name;
		public static ServiceBase CreateDefault() => new ServiceBase(0);
		public int Version;
		public string Title { get; set; }
		public event System.EventHandler Started;
		protected void OnStarted() => Started?.Invoke(this, System.EventArgs.Empty);
	}

	public interface IWorker
	{
		void Work(int amount);
	}

	public struct Coordinates
	{
		public double X;
		public double Y;
	}

	public delegate int Combiner(int left, int right);

	public sealed class TaggedAttribute : System.Attribute
	{
		public TaggedAttribute(string tag) { }
	}

	public class MarkerAttributeBase : System.Attribute
	{
	}

	public class Constrained : Present.Lib.RequiredBase, Present.Lib.IRequired
	{
	}

	public class Repository<T>
	{
		public void Add(T item) { }
		public TResult Convert<TResult>(T item) => default;
	}
}";

		const string PresentLibSource = @"
namespace Present.Lib
{
	public class RequiredBase
	{
	}

	public interface IRequired
	{
	}

	public class Holder<T> where T : RequiredBase, IRequired, new()
	{
	}
}";

		const string ConsumerSource = @"
using Phantom.Core;
using Present.Lib;

namespace Client
{
	[System.AttributeUsage(System.AttributeTargets.Class)]
	public sealed class DerivedMarkerAttribute : MarkerAttributeBase
	{
	}

	[Tagged(""demo"")]
	public class Facade : IWorker
	{
		readonly ServiceBase service = ServiceBase.CreateDefault();

		public void Work(int amount)
		{
			var svc = new ServiceBase(amount);
			svc.Version = amount;
			svc.Title = svc.Describe(svc.Title);
			svc.Started += (s, e) => { };
			var coords = new Coordinates { X = 1.0, Y = 2.0 };
			double sum = coords.X + coords.Y;
			Combiner combiner = (l, r) => l + r;
			int combined = combiner(amount, (int)sum);
			var repo = new Repository<string>();
			repo.Add(svc.Title);
			int converted = repo.Convert<int>(svc.Title);
			var holder = new Holder<Constrained>();
		}
	}
}";

		static string consumerDllPath;
		static string presentLibPath;

		[OneTimeSetUp]
		public void SetUp()
		{
			var references = Tester.CoreDefaultReferences
				.Select(r => MetadataReference.CreateFromFile(
					Path.Combine(Tester.RefAssembliesToolset.GetPath(Tester.CurrentNetCoreAppVersion), r)))
				.ToArray();

			presentLibPath = Path.Combine(Path.GetTempPath(), "Present.Lib.dll");
			var presentLib = CSharpCompilation.Create("Present.Lib",
				new[] { CSharpSyntaxTree.ParseText(PresentLibSource) },
				references,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(presentLib);
			using (var fs = File.Create(presentLibPath))
			{
				Assert.That(presentLib.Emit(fs).Success);
			}

			var dependency = CSharpCompilation.Create("Phantom.Core",
				new[] { CSharpSyntaxTree.ParseText(DependencySource) },
				references.Append<MetadataReference>(MetadataReference.CreateFromFile(presentLibPath)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(dependency);
			using var dependencyStream = new MemoryStream();
			Assert.That(dependency.Emit(dependencyStream).Success);
			dependencyStream.Position = 0;

			var consumer = CSharpCompilation.Create("Client",
				new[] { CSharpSyntaxTree.ParseText(ConsumerSource) },
				references
					.Append<MetadataReference>(MetadataReference.CreateFromStream(dependencyStream))
					.Append<MetadataReference>(MetadataReference.CreateFromFile(presentLibPath)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(consumer);

			consumerDllPath = Path.Combine(Path.GetTempPath(), "StubGenTests.Client.dll");
			using (var fs = File.Create(consumerDllPath))
			{
				Assert.That(consumer.Emit(fs).Success);
			}
			// The dependency is deliberately NOT written anywhere: the generator must work from
			// the consumer's expectations alone.
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			if (consumerDllPath != null && File.Exists(consumerDllPath))
				File.Delete(consumerDllPath);
			if (presentLibPath != null && File.Exists(presentLibPath))
				File.Delete(presentLibPath);
		}

		static string GenerateStub()
		{
			using var consumerFile = new PEFile(consumerDllPath);
			var resolver = new UniversalAssemblyResolver(consumerDllPath, false,
				consumerFile.DetectTargetFrameworkId());
			var generator = new StubAssemblyGenerator(resolver);
			generator.AddConsumer(consumerFile);
			return generator.GenerateSource("Phantom.Core");
		}

		[Test]
		public void GeneratedSourceDeclaresTheExpectedSurface()
		{
			string source = GenerateStub();

			// Class with the referenced members. Kind and member shapes come from usage:
			// base call, static factory, field store, property accessors, event accessors.
			Assert.That(source, Does.Contain("class ServiceBase"));
			Assert.That(source, Does.Contain("ServiceBase(int"));
			Assert.That(source, Does.Contain("Describe(string"));
			Assert.That(source, Does.Contain("static"));
			Assert.That(source, Does.Contain("CreateDefault()"));
			Assert.That(source, Does.Contain("int Version"));
			Assert.That(source, Does.Contain("Title"));
			Assert.That(source, Does.Contain("event "));
			Assert.That(source, Does.Contain("Started"));

			// Interface: recognized from the consumer's interface-implementation row.
			Assert.That(source, Does.Contain("interface IWorker"));

			// Struct: recognized from valuetype-encoded signature positions.
			Assert.That(source, Does.Contain("struct Coordinates"));
			Assert.That(source, Does.Contain("double X"));

			// Delegate: recognized from the ctor(object, nint) + Invoke pair.
			Assert.That(source, Does.Contain("delegate"));
			Assert.That(source, Does.Contain("Combiner"));

			// Attribute: recognized from custom-attribute usage.
			Assert.That(source, Does.Contain("class TaggedAttribute : global::System.Attribute"));

			// Attribute base: recognized because a consumer type deriving from it carries
			// [AttributeUsage] - an attribute class must have System.Attribute ancestry.
			Assert.That(source, Does.Contain("class MarkerAttributeBase : global::System.Attribute"));

			// Members of stub classes are virtual so consumer overrides compile.
			Assert.That(source, Does.Contain("virtual"));

			// Generic type with a generic method.
			Assert.That(source, Does.Contain("class Repository<"));
			Assert.That(source, Does.Contain("Convert<"));

			// Constraint-derived bases: the consumer instantiates Present.Lib.Holder<Constrained>,
			// whose resolved type parameter demands RequiredBase, IRequired and new(). The stub
			// can only satisfy the instantiation by declaring that ancestry.
			Assert.That(source, Does.Contain("class Constrained : global::Present.Lib.RequiredBase, global::Present.Lib.IRequired"));

			// Namespaces are preserved.
			Assert.That(source, Does.Contain("namespace Phantom.Core"));
		}

		[Test]
		public void GeneratedSourceCompiles()
		{
			string source = GenerateStub();

			var references = Tester.CoreDefaultReferences
				.Select(r => MetadataReference.CreateFromFile(
					Path.Combine(Tester.RefAssembliesToolset.GetPath(Tester.CurrentNetCoreAppVersion), r)))
				.ToArray();
			var stub = CSharpCompilation.Create("Phantom.Core",
				new[] { CSharpSyntaxTree.ParseText(source) },
				references.Append<MetadataReference>(MetadataReference.CreateFromFile(presentLibPath)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(stub);
		}

		[Test]
		public void ConsumerSourceCompilesAgainstTheStub()
		{
			string source = GenerateStub();

			var references = Tester.CoreDefaultReferences
				.Select(r => MetadataReference.CreateFromFile(
					Path.Combine(Tester.RefAssembliesToolset.GetPath(Tester.CurrentNetCoreAppVersion), r)))
				.ToArray();
			var stub = CSharpCompilation.Create("Phantom.Core",
				new[] { CSharpSyntaxTree.ParseText(source) },
				references.Append<MetadataReference>(MetadataReference.CreateFromFile(presentLibPath)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(stub);
			using var stubStream = new MemoryStream();
			Assert.That(stub.Emit(stubStream).Success);
			stubStream.Position = 0;

			// The original consumer source, compiled against the stub in place of the real
			// dependency: the loop the generator exists for.
			var consumer = CSharpCompilation.Create("Client2",
				new[] { CSharpSyntaxTree.ParseText(ConsumerSource) },
				references
					.Append<MetadataReference>(MetadataReference.CreateFromStream(stubStream))
					.Append<MetadataReference>(MetadataReference.CreateFromFile(presentLibPath)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(consumer);
		}

		static void AssertNoErrors(CSharpCompilation compilation)
		{
			var errors = compilation.GetDiagnostics()
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.ToList();
			Assert.That(errors, Is.Empty,
				string.Join("\n", errors.Select(d => d.ToString())));
		}
	}
}

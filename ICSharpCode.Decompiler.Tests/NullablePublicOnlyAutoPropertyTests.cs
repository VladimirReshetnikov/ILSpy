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


using System.Collections.Generic;
using System.IO;
using System.Linq;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	/// <summary>
	/// Auto-property recovery must not depend on nullability annotations agreeing between a
	/// property and its backing field. Under the 'nullablePublicOnly' feature (how the .NET
	/// runtime libraries build their .NET Framework targets), the public property's return type
	/// carries the annotation while the private synthesized field stays oblivious; the field is
	/// that property's storage all the same.
	/// </summary>
	[TestFixture]
	public class NullablePublicOnlyAutoPropertyTests
	{
		const string Source = @"
#nullable enable
namespace Lib
{
	public class Holder
	{
		public string? Name { get; }
		public object? Discriminator { get; }

		public Holder(string? name, object? discriminator)
		{
			Name = name;
			Discriminator = discriminator;
		}
	}
}
";

		[Test]
		public void AutoPropertiesSurviveNullablePublicOnly()
		{
			var references = Tester.CoreDefaultReferences
				.Select(r => MetadataReference.CreateFromFile(
					Path.Combine(Tester.RefAssembliesToolset.GetPath(Tester.CurrentNetCoreAppVersion), r)))
				.ToArray();
			var parseOptions = new CSharpParseOptions()
				.WithFeatures(new[] { new KeyValuePair<string, string>("nullablePublicOnly", "true") });
			var compilation = CSharpCompilation.Create("NullablePublicOnlyAutoProperty",
				new[] { CSharpSyntaxTree.ParseText(Source, parseOptions) },
				references,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
					nullableContextOptions: NullableContextOptions.Enable));
			var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
			Assert.That(errors, Is.Empty, string.Join("\n", errors));

			string assemblyPath = Path.Combine(Path.GetTempPath(), "NullablePublicOnlyAutoProperty.dll");
			try
			{
				using (var fs = File.Create(assemblyPath))
				{
					Assert.That(compilation.Emit(fs).Success);
				}
				using var module = new PEFile(assemblyPath);
				var resolver = new UniversalAssemblyResolver(assemblyPath, false, module.DetectTargetFrameworkId());
				var decompiler = new CSharpDecompiler(module, resolver, new DecompilerSettings());
				string code = decompiler.DecompileTypeAsString(new FullTypeName("Lib.Holder"));

				Assert.That(code, Does.Contain("public string? Name { get; }"));
				Assert.That(code, Does.Contain("public object? Discriminator { get; }"));
				Assert.That(code, Does.Not.Contain("k__BackingField"));
			}
			finally
			{
				if (File.Exists(assemblyPath))
					File.Delete(assemblyPath);
			}
		}
	}
}

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
using System.Collections.Generic;
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
	/// A whole-project export must keep a file-local type in one generated file with every type
	/// that uses it: 'file' accessibility ends at the file boundary, so a helper exported on its
	/// own is invisible to its users and the project no longer compiles. Metadata records the
	/// declaring source file only through the type's mangled name, and the users only through
	/// their references to it - signatures, attributes, and method bodies alike.
	/// </summary>
	[TestFixture]
	public class FileLocalTypePlacementTests
	{
		// A debugger proxy referenced from an attribute only, a helper called from a method body
		// only, and a user in a different namespace than the helper - all declared in Holder.cs.
		const string HolderSource = @"
using System.Diagnostics;

namespace Lib
{
	[DebuggerTypeProxy(typeof(HolderDebugView))]
	public class Holder
	{
		public int Value;
	}

	file sealed class HolderDebugView
	{
		public HolderDebugView(Holder holder)
		{
			Items = new[] { holder.Value };
		}

		public int[] Items { get; }
	}

	file static class HolderHelpers
	{
		public static int Twice(int x)
		{
			return x * 2;
		}
	}
}

namespace Lib.Sub
{
	public class Doubler
	{
		public int Run(int x)
		{
			return HolderHelpers.Twice(x);
		}
	}
}
";

		// A file-local helper in the global namespace whose user sits inside a namespace: the
		// merged file then has a top-level type before the namespace declaration, which rules
		// out the file-scoped namespace form (CS8956).
		const string GlobalHelperSource = @"
file static class GlobalHelper
{
	public static int One()
	{
		return 1;
	}
}

namespace Lib.Deep
{
	public class GlobalHelperUser
	{
		public int Get()
		{
			return GlobalHelper.One();
		}
	}
}
";

		const string UnrelatedSource = @"
namespace Lib
{
	public class Unrelated
	{
	}
}
";

		static MetadataReference[] references;
		static string assemblyPath;

		[OneTimeSetUp]
		public void SetUp()
		{
			references = Tester.CoreDefaultReferences
				.Select(r => MetadataReference.CreateFromFile(
					Path.Combine(Tester.RefAssembliesToolset.GetPath(Tester.CurrentNetCoreAppVersion), r)))
				.ToArray();

			var compilation = CSharpCompilation.Create("FileLocalPlacement",
				new[] {
					CSharpSyntaxTree.ParseText(HolderSource, path: "Holder.cs"),
					CSharpSyntaxTree.ParseText(UnrelatedSource, path: "Unrelated.cs"),
					CSharpSyntaxTree.ParseText(GlobalHelperSource, path: "GlobalHelper.cs"),
				},
				references,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(compilation);

			assemblyPath = Path.Combine(Path.GetTempPath(), "FileLocalPlacementTests.dll");
			using var fs = File.Create(assemblyPath);
			Assert.That(compilation.Emit(fs).Success);
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			if (assemblyPath != null && File.Exists(assemblyPath))
				File.Delete(assemblyPath);
		}

		[Test]
		public void FileLocalTypesShareTheFileOfTheirUsers()
		{
			var files = Export();
			string holderFile = Path.Combine("Lib", "Holder.cs");
			Assert.That(files.Keys, Does.Contain(holderFile));
			string holder = files[holderFile];

			using (Assert.EnterMultipleScope())
			{
				Assert.That(holder, Does.Contain("file sealed class HolderDebugView"));
				Assert.That(holder, Does.Contain("file static class HolderHelpers"));
				Assert.That(holder, Does.Contain("class Holder"));
				Assert.That(holder, Does.Contain("class Doubler"));
				Assert.That(holder, Does.Contain("namespace Lib.Sub"));
			}
			foreach (var (path, text) in files.Where(f => f.Key != holderFile))
			{
				Assert.That(text, Does.Not.Contain("HolderDebugView").And.Not.Contain("HolderHelpers").And.Not.Contain("Doubler"),
					$"{path} must not mention the file-local types or their users");
			}
			Assert.That(files.Keys, Does.Contain(Path.Combine("Lib", "Unrelated.cs")));
		}

		[Test]
		public void ExportedProjectCompiles()
		{
			var files = Export();
			var recompiled = CSharpCompilation.Create("FileLocalPlacement.Recompiled",
				files.Where(f => f.Key.EndsWith(".cs", StringComparison.Ordinal))
					.Select(f => CSharpSyntaxTree.ParseText(f.Value, path: f.Key)),
				references,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			AssertNoErrors(recompiled);
		}

		/// <summary>
		/// Exports the test assembly in memory and returns the generated source files keyed by
		/// their project-relative path.
		/// </summary>
		static Dictionary<string, string> Export()
		{
			using var module = new PEFile(assemblyPath);
			var resolver = new UniversalAssemblyResolver(assemblyPath, false, module.DetectTargetFrameworkId());
			string targetDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var decompiler = new InMemoryProjectDecompiler(resolver);
			decompiler.DecompileProject(module, targetDirectory);
			return decompiler.Files.Where(f => f.Key.EndsWith(".cs", StringComparison.Ordinal)).ToDictionary(
				f => Path.GetRelativePath(targetDirectory, f.Key),
				f => f.Value.ToString());
		}

		sealed class InMemoryProjectDecompiler(IAssemblyResolver assemblyResolver) : WholeProjectDecompiler(assemblyResolver)
		{
			public Dictionary<string, StringWriter> Files { get; } = new();

			protected override TextWriter CreateFile(string path)
			{
				var writer = new StringWriter();
				lock (Files)
				{
					Files[path] = writer;
				}
				return writer;
			}

			protected override void CreateDirectory(string path)
			{
			}

			protected override IEnumerable<ProjectItemInfo> WriteMiscellaneousFilesInProject(PEFile module) => Enumerable.Empty<ProjectItemInfo>();
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

// Copyright (c) 2025 Daniel Grunwald
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
using System.Resources;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Tests.Helpers;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.ProjectDecompiler;

[TestFixture]
public sealed class WholeProjectDecompilerTests
{
	[Test]
	public void UseNestedDirectoriesForNamespacesTrueWorks()
	{
		string targetDirectory = Path.Combine(Environment.CurrentDirectory, Path.GetRandomFileName());
		TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
		decompiler.Settings.UseNestedDirectoriesForNamespaces = true;
		using PEFile module = new("ICSharpCode.Decompiler.dll");
		decompiler.DecompileProject(module, targetDirectory);
		AssertDirectoryDoesntExist(targetDirectory);

		string projectDecompilerDirectory = Path.Combine(targetDirectory, "ICSharpCode", "Decompiler", "CSharp", "ProjectDecompiler");
		string projectDecompilerFile = Path.Combine(projectDecompilerDirectory, $"{nameof(WholeProjectDecompiler)}.cs");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(decompiler.Files.ContainsKey(projectDecompilerFile), Is.True);
			Assert.That(decompiler.Directories.Contains(projectDecompilerDirectory), Is.True);
		}
	}

	[Test]
	public void UseNestedDirectoriesForNamespacesFalseWorks()
	{
		string targetDirectory = Path.Combine(Environment.CurrentDirectory, Path.GetRandomFileName());
		TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
		decompiler.Settings.UseNestedDirectoriesForNamespaces = false;
		using PEFile module = new("ICSharpCode.Decompiler.dll");
		decompiler.DecompileProject(module, targetDirectory);
		AssertDirectoryDoesntExist(targetDirectory);

		string projectDecompilerDirectory = Path.Combine(targetDirectory, "ICSharpCode.Decompiler.CSharp.ProjectDecompiler");
		string projectDecompilerFile = Path.Combine(projectDecompilerDirectory, $"{nameof(WholeProjectDecompiler)}.cs");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(decompiler.Files.ContainsKey(projectDecompilerFile), Is.True);
			Assert.That(decompiler.Directories.Contains(projectDecompilerDirectory), Is.True);
		}
	}

	[TestCase(true, true)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(false, false)]
	public void ProjectWriterEmitsNullableContextWhenEnabled(bool useSdkStyleProjectFormat, bool nullableReferenceTypes)
	{
		UniversalAssemblyResolver assemblyResolver = new(null, false, null);
		TestProjectInfoProvider project = new(assemblyResolver, nullableReferenceTypes);
		IProjectFileWriter writer = useSdkStyleProjectFormat ? ProjectFileWriterSdkStyle.Default : ProjectFileWriterDefault.Instance;
		using StringWriter output = new();
		using PEFile module = new("ICSharpCode.Decompiler.dll");
		writer.Write(output, project, [], module);
		string projectFile = output.ToString();
		if (nullableReferenceTypes)
		{
			Assert.That(projectFile, Does.Contain("<Nullable>annotations</Nullable>"));
		}
		else
		{
			Assert.That(projectFile, Does.Not.Contain("<Nullable>"));
		}
	}

	[Test]
	public void SdkStyleProjectWriterPreservesResXMetadata()
	{
		UniversalAssemblyResolver assemblyResolver = new(null, false, null);
		TestProjectInfoProvider project = new(assemblyResolver, nullableReferenceTypes: false);
		ProjectItemInfo resource = new ProjectItemInfo("EmbeddedResource", "Strings.resx")
			.With("LogicalName", "Test.Strings.resources")
			.With("WithCulture", "false");
		using StringWriter output = new();
		using PEFile module = new("ICSharpCode.Decompiler.dll");
		ProjectFileWriterSdkStyle.Default.Write(output, project, [resource], module);

		Assert.That(output.ToString(), Does.Contain(
			"<EmbeddedResource Update=\"Strings.resx\" LogicalName=\"Test.Strings.resources\" WithCulture=\"false\" />"));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void WholeProjectDecompilerProvidesNullableReferenceTypeSetting(bool nullableReferenceTypes)
	{
		DecompilerSettings settings = new() { NullableReferenceTypes = nullableReferenceTypes };
		WholeProjectDecompiler decompiler = new(settings, new UniversalAssemblyResolver(null, false, null),
			projectWriter: null, assemblyReferenceClassifier: null, debugInfoProvider: null);
		Assert.That(((INullableProjectInfoProvider)decompiler).NullableReferenceTypes, Is.EqualTo(nullableReferenceTypes));
	}

	[Test]
	public async Task GeneratedInternalTypeHelperDependsOnXamlBuildItems()
	{
		string ilFile = Path.Combine(Tester.TestCasePath, "ProjectDecompiler", "GeneratedInternalTypeHelper.il");
		string assembly = await Tester.AssembleIL(ilFile, AssemblerOptions.Library);
		try
		{
			using PEFile module = new(assembly);
			TestFriendlyProjectDecompiler rawResourceDecompiler = new(new UniversalAssemblyResolver(assembly, false, null));
			using StringWriter rawProject = new();
			rawResourceDecompiler.DecompileProject(module, Path.GetRandomFileName(), rawProject);

			TestFriendlyProjectDecompiler xamlDecompiler = new(new UniversalAssemblyResolver(assembly, false, null));
			xamlDecompiler.ResourceItems.Add(new ProjectItemInfo("Page", "Test.xaml"));
			using StringWriter xamlProject = new();
			xamlDecompiler.DecompileProject(module, Path.GetRandomFileName(), xamlProject);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(rawResourceDecompiler.ContainsSource("class GeneratedInternalTypeHelper"), Is.True);
				Assert.That(xamlDecompiler.ContainsSource("class GeneratedInternalTypeHelper"), Is.False);
			}
		}
		finally
		{
			Tester.RepeatOnIOError(() => File.Delete(assembly));
		}
	}

	[Test]
	public async Task EmbeddedReadonlySupportTypesArePreserved()
	{
		string ilFile = Path.Combine(Tester.TestCasePath, "ProjectDecompiler", "EmbeddedCompilerAttributes.il");
		string assembly = await Tester.AssembleIL(ilFile, AssemblerOptions.Library);
		try
		{
			using PEFile module = new(assembly);
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(assembly, false, null));
			using StringWriter project = new();
			decompiler.DecompileProject(module, Path.GetRandomFileName(), project);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(decompiler.ContainsSource("class IsReadOnlyAttribute"), Is.True);
				Assert.That(decompiler.ContainsSource("class IsByRefLikeAttribute"), Is.False);
				Assert.That(decompiler.ContainsSource("class EmbeddedAttribute"), Is.True);
			}
		}
		finally
		{
			Tester.RepeatOnIOError(() => File.Delete(assembly));
		}
	}

	[Test]
	public async Task EmbeddedNullablePublicOnlyAttributeIsRemoved()
	{
		string ilFile = Path.Combine(Tester.TestCasePath, "ProjectDecompiler", "EmbeddedCompilerAttributes.il");
		string assembly = await Tester.AssembleIL(ilFile, AssemblerOptions.Library);
		try
		{
			using PEFile module = new(assembly);
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(assembly, false, null));
			using StringWriter project = new();
			decompiler.DecompileProject(module, Path.GetRandomFileName(), project);

			using (Assert.EnterMultipleScope())
			{
				// The embedded attribute type definition must not be emitted as source.
				Assert.That(decompiler.ContainsSource("class NullablePublicOnlyAttribute"), Is.False);
				// The [module: NullablePublicOnly(false)] usage must not be emitted either.
				Assert.That(decompiler.ContainsSource("NullablePublicOnly(false)"), Is.False);
			}
		}
		finally
		{
			Tester.RepeatOnIOError(() => File.Delete(assembly));
		}
	}

	[Test]
	public async Task ProjectNullableContextPreservesAnnotatedExtensionMarkerName()
	{
		string sourceFile = Path.Combine(Tester.TestCasePath, "ProjectDecompiler", "ObliviousExtensionBlock.cs");
		CompilerResults original = await Tester.CompileCSharp(sourceFile,
			CompilerOptions.UseRoslynLatest | CompilerOptions.Preview | CompilerOptions.NullableEnable | CompilerOptions.Library);
		CompilerResults rebuilt = null;
		string decompiledSourceFile = null;
		try
		{
			using PEFile module = new(original.PathToAssembly);
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(original.PathToAssembly, false, null));
			using StringWriter project = new();
			decompiler.DecompileProject(module, Path.GetRandomFileName(), project);
			string source = decompiler.SourceContaining("extension(string value)");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Not.Contain("#nullable disable annotations"));
				Assert.That(source, Does.Not.Contain("#nullable restore annotations"));
			}

			decompiledSourceFile = Path.Combine(Path.GetTempPath(), $"ObliviousExtensionBlock-{Guid.NewGuid():N}.cs");
			File.WriteAllText(decompiledSourceFile, source);
			rebuilt = await Tester.CompileCSharp(decompiledSourceFile,
				CompilerOptions.UseRoslynLatest | CompilerOptions.Preview | CompilerOptions.NullableEnable | CompilerOptions.Library);

			using PEFile rebuiltModule = new(rebuilt.PathToAssembly);
			Assert.That(GetExtensionMarkerTypeNames(rebuiltModule), Is.EqualTo(GetExtensionMarkerTypeNames(module)));
		}
		finally
		{
			rebuilt?.DeleteTempFiles();
			original.DeleteTempFiles();
			if (decompiledSourceFile != null && File.Exists(decompiledSourceFile))
				File.Delete(decompiledSourceFile);
		}
	}

	[Test]
	public async Task ProjectNullableContextPreservesObliviousExtensionMarkerName()
	{
		string ilFile = Path.Combine(Tester.TestCasePath, "ProjectDecompiler", "ObliviousExtensionBlock.il");
		string assembly = await Tester.AssembleIL(ilFile, AssemblerOptions.Library);
		CompilerResults rebuilt = null;
		string decompiledSourceFile = null;
		try
		{
			using PEFile module = new(assembly);
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(assembly, false, null));
			using StringWriter project = new();
			decompiler.DecompileProject(module, Path.GetRandomFileName(), project);
			string source = decompiler.SourceContaining("extension(string receiver)");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("#nullable disable annotations"));
				Assert.That(source, Does.Contain("#nullable restore annotations"));
			}

			decompiledSourceFile = Path.Combine(Path.GetTempPath(), $"ObliviousExtensionBlock-{Guid.NewGuid():N}.cs");
			File.WriteAllText(decompiledSourceFile, source);
			rebuilt = await Tester.CompileCSharp(decompiledSourceFile,
				CompilerOptions.UseRoslynLatest | CompilerOptions.Preview | CompilerOptions.NullableEnable | CompilerOptions.Library);

			using PEFile rebuiltModule = new(rebuilt.PathToAssembly);
			Assert.That(GetExtensionMarkerTypeNames(rebuiltModule), Is.EqualTo(GetExtensionMarkerTypeNames(module)));
		}
		finally
		{
			rebuilt?.DeleteTempFiles();
			Tester.RepeatOnIOError(() => File.Delete(assembly));
			if (decompiledSourceFile != null && File.Exists(decompiledSourceFile))
				File.Delete(decompiledSourceFile);
		}
	}

	[Test]
	public void StringOnlyResourcesAreConvertedToResX()
	{
		string targetDirectory = CreateTemporaryDirectory();
		try
		{
			byte[] input = CreateResources();
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
			using MemoryStream stream = new(input, writable: false);

			ProjectItemInfo item = decompiler.WriteResource(targetDirectory, "Strings.resources", "Test.Strings.resources", stream);
			string outputFile = Path.Combine(targetDirectory, "Strings.resx");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(item.ItemType, Is.EqualTo("EmbeddedResource"));
				Assert.That(item.FileName, Is.EqualTo("Strings.resx"));
				Assert.That(item.AdditionalProperties["LogicalName"], Is.EqualTo("Test.Strings.resources"));
				Assert.That(item.AdditionalProperties["WithCulture"], Is.EqualTo("false"));
				Assert.That(File.Exists(outputFile), Is.True);
				Assert.That(File.Exists(Path.Combine(targetDirectory, "Strings.resources")), Is.False);
			}
			Assert.That(File.ReadAllText(outputFile), Does.Contain("<data name=\"Greeting\"").And.Contain("<value>Hello</value>"));
		}
		finally
		{
			Directory.Delete(targetDirectory, recursive: true);
		}
	}

	[TestCase(NonStringResourceKind.ByteArray)]
	[TestCase(NonStringResourceKind.Stream)]
	[TestCase(NonStringResourceKind.Integer)]
	public void ResourcesWithNonStringEntriesStayBinary(NonStringResourceKind resourceKind)
	{
		string targetDirectory = CreateTemporaryDirectory();
		try
		{
			byte[] input = CreateResources(resourceKind);
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
			using MemoryStream stream = new(input, writable: false);

			ProjectItemInfo item = decompiler.WriteResource(targetDirectory, "Mixed.resources", "Test.Mixed.resources", stream);
			string outputFile = Path.Combine(targetDirectory, "Mixed.resources");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(item.ItemType, Is.EqualTo("EmbeddedResource"));
				Assert.That(item.FileName, Is.EqualTo("Mixed.resources"));
				Assert.That(item.AdditionalProperties["LogicalName"], Is.EqualTo("Test.Mixed.resources"));
				Assert.That(item.AdditionalProperties["WithCulture"], Is.EqualTo("false"));
				Assert.That(File.Exists(outputFile), Is.True);
				Assert.That(File.Exists(Path.Combine(targetDirectory, "Mixed.resx")), Is.False);
			}
			Assert.That(File.ReadAllBytes(outputFile), Is.EqualTo(input));
		}
		finally
		{
			Directory.Delete(targetDirectory, recursive: true);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public void EmptyResourcesStayBinary(bool extractIndividualResources)
	{
		string targetDirectory = CreateTemporaryDirectory();
		try
		{
			byte[] input = CreateEmptyResources();
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
			decompiler.ExtractResources = extractIndividualResources;
			using MemoryStream stream = new(input, writable: false);

			ProjectItemInfo item = decompiler.WriteResource(targetDirectory, "Empty.resources", "Test.Empty.resources", stream);
			string outputFile = Path.Combine(targetDirectory, "Empty.resources");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(item.ItemType, Is.EqualTo("EmbeddedResource"));
				Assert.That(item.FileName, Is.EqualTo("Empty.resources"));
				Assert.That(item.AdditionalProperties["LogicalName"], Is.EqualTo("Test.Empty.resources"));
				Assert.That(item.AdditionalProperties["WithCulture"], Is.EqualTo("false"));
				Assert.That(File.Exists(outputFile), Is.True);
				Assert.That(File.Exists(Path.Combine(targetDirectory, "Empty.resx")), Is.False);
			}
			Assert.That(File.ReadAllBytes(outputFile), Is.EqualTo(input));
		}
		finally
		{
			Directory.Delete(targetDirectory, recursive: true);
		}
	}

	[Test]
	public void StreamOnlyResourcesStayInTheirContainerByDefault()
	{
		string targetDirectory = CreateTemporaryDirectory();
		try
		{
			byte[] input = CreateStreamOnlyResources();
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));

			ProjectItemInfo item = decompiler.WriteResource(
				targetDirectory, new ByteArrayResource("Test.g.resources", input));
			string outputFile = Path.Combine(targetDirectory, "Test.g.resources");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(item.ItemType, Is.EqualTo("EmbeddedResource"));
				Assert.That(item.FileName, Is.EqualTo("Test.g.resources"));
				Assert.That(item.AdditionalProperties["LogicalName"], Is.EqualTo("Test.g.resources"));
				Assert.That(item.AdditionalProperties["WithCulture"], Is.EqualTo("false"));
				Assert.That(File.Exists(outputFile), Is.True);
				Assert.That(File.Exists(Path.Combine(targetDirectory, "Views", "MainWindow.baml")), Is.False);
			}
			Assert.That(File.ReadAllBytes(outputFile), Is.EqualTo(input));
		}
		finally
		{
			Directory.Delete(targetDirectory, recursive: true);
		}
	}

	[Test]
	public void EmbeddedResourcesDisableCultureInference()
	{
		string targetDirectory = CreateTemporaryDirectory();
		try
		{
			TestFriendlyProjectDecompiler decompiler = new(new UniversalAssemblyResolver(null, false, null));
			using MemoryStream stream = new([1, 2, 3, 4], writable: false);

			ProjectItemInfo item = decompiler.WriteResource(
				targetDirectory, "Certificate.ca.crt", "Test.Certificate.ca.crt", stream);

			Assert.That(item.AdditionalProperties["WithCulture"], Is.EqualTo("false"));
		}
		finally
		{
			Directory.Delete(targetDirectory, recursive: true);
		}
	}

	static string CreateTemporaryDirectory()
	{
		string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetRandomFileName());
		Directory.CreateDirectory(directory);
		return directory;
	}

	static byte[] CreateResources(NonStringResourceKind? resourceKind = null)
	{
		MemoryStream output = new();
		using (ResourceWriter writer = new(output))
		{
			writer.AddResource("Greeting", "Hello");
			switch (resourceKind)
			{
				case NonStringResourceKind.ByteArray:
					writer.AddResource("Binary", new byte[] { 1, 2, 3, 4 });
					break;
				case NonStringResourceKind.Stream:
					writer.AddResource("Binary", new MemoryStream([1, 2, 3, 4]), closeAfterWrite: true);
					break;
				case NonStringResourceKind.Integer:
					writer.AddResource("Number", 42);
					break;
			}
			writer.Generate();
		}
		return output.ToArray();
	}

	static byte[] CreateEmptyResources()
	{
		MemoryStream output = new();
		using (ResourceWriter writer = new(output))
		{
			writer.Generate();
		}
		return output.ToArray();
	}

	static byte[] CreateStreamOnlyResources()
	{
		MemoryStream output = new();
		using (ResourceWriter writer = new(output))
		{
			writer.AddResource("Views/MainWindow.baml", new MemoryStream([1, 2, 3, 4]), closeAfterWrite: true);
			writer.Generate();
		}
		return output.ToArray();
	}

	static void AssertDirectoryDoesntExist(string directory)
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, recursive: true);
			Assert.Fail("Directory should not have been created.");
		}
	}

	static string[] GetExtensionMarkerTypeNames(PEFile module)
	{
		var metadata = module.Metadata;
		return metadata.TypeDefinitions
			.Select(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name))
			.Where(name => name.StartsWith("<M>$", StringComparison.Ordinal))
			.Order()
			.ToArray();
	}

	sealed class TestFriendlyProjectDecompiler(IAssemblyResolver assemblyResolver) : WholeProjectDecompiler(assemblyResolver)
	{
		public Dictionary<string, StringWriter> Files { get; } = [];
		public HashSet<string> Directories { get; } = [];
		public List<ProjectItemInfo> ResourceItems { get; } = [];
		public bool ExtractResources { get; set; }

		public bool ContainsSource(string text) => Files.Values.Any(writer => writer.ToString().Contains(text));

		public string SourceContaining(string text) => Files.Values.Select(writer => writer.ToString()).Single(source => source.Contains(text));

		protected override TextWriter CreateFile(string path)
		{
			StringWriter writer = new();
			lock (Files)
			{
				Files[path] = writer;
			}
			return writer;
		}

		protected override void CreateDirectory(string path)
		{
			lock (Directories)
			{
				Directories.Add(path);
			}
		}

		protected override IEnumerable<ProjectItemInfo> WriteMiscellaneousFilesInProject(PEFile module) => [];

		protected override IEnumerable<ProjectItemInfo> WriteResourceFilesInProject(MetadataFile module) => ResourceItems;
		protected override bool ExtractIndividualResources => ExtractResources;

		public ProjectItemInfo WriteResource(string targetDirectory, string fileName, string resourceName, Stream stream)
		{
			TargetDirectory = targetDirectory;
			return WriteResourceToFile(fileName, resourceName, stream).Single();
		}

		public ProjectItemInfo WriteResource(string targetDirectory, Resource resource)
		{
			TargetDirectory = targetDirectory;
			return WriteResourceFile(resource).Single();
		}
	}

	sealed class TestProjectInfoProvider(IAssemblyResolver assemblyResolver, bool nullableReferenceTypes) : IProjectInfoProvider, INullableProjectInfoProvider
	{
		public IAssemblyResolver AssemblyResolver => assemblyResolver;

		public IAssemblyReferenceClassifier AssemblyReferenceClassifier { get; } = new AssemblyReferenceClassifier();

		public CSharp.LanguageVersion LanguageVersion => CSharp.LanguageVersion.CSharp14_0;

		public bool CheckForOverflowUnderflow => false;

		public bool NullableReferenceTypes => nullableReferenceTypes;

		public Guid ProjectGuid { get; } = Guid.NewGuid();

		public string TargetDirectory => Environment.CurrentDirectory;

		public string StrongNameKeyFile => null;
	}

	public enum NonStringResourceKind
	{
		ByteArray,
		Stream,
		Integer
	}
}

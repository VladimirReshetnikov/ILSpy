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

using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

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
		decompiler.DecompileProject(new PEFile("ICSharpCode.Decompiler.dll"), targetDirectory);
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
		decompiler.DecompileProject(new PEFile("ICSharpCode.Decompiler.dll"), targetDirectory);
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
		IProjectFileWriter writer = useSdkStyleProjectFormat ? ProjectFileWriterSdkStyle.Create() : ProjectFileWriterDefault.Create();
		using StringWriter output = new();
		writer.Write(output, project, [], new PEFile("ICSharpCode.Decompiler.dll"));
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

	static void AssertDirectoryDoesntExist(string directory)
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, recursive: true);
			Assert.Fail("Directory should not have been created.");
		}
	}

	sealed class TestFriendlyProjectDecompiler(IAssemblyResolver assemblyResolver) : WholeProjectDecompiler(assemblyResolver)
	{
		public Dictionary<string, StringWriter> Files { get; } = [];
		public HashSet<string> Directories { get; } = [];

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

		protected override IEnumerable<ProjectItemInfo> WriteResourceFilesInProject(MetadataFile module) => [];

		public ProjectItemInfo WriteResource(string targetDirectory, string fileName, string resourceName, Stream stream)
		{
			TargetDirectory = targetDirectory;
			return WriteResourceToFile(fileName, resourceName, stream).Single();
		}
	}

	sealed class TestProjectInfoProvider(IAssemblyResolver assemblyResolver, bool nullableReferenceTypes) : IProjectInfoProvider, INullableProjectInfoProvider
	{
		public IAssemblyResolver AssemblyResolver => assemblyResolver;

		public AssemblyReferenceClassifier AssemblyReferenceClassifier { get; } = new();

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

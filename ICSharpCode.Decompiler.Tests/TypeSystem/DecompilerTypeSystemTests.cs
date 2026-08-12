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
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.TypeSystem
{
	[TestFixture]
	public class DecompilerTypeSystemTests
	{
		const TypeAttributes ForwarderTypeAttributes = (TypeAttributes)0x00200000;

		[Test]
		public void DirectReferenceWinsHigherVersionTransitiveNameLookupReference()
		{
			var version1 = new Version(1, 0, 0, 0);
			var version2 = new Version(2, 0, 0, 0);
			using var main = BuildAssembly("Main", version1, ("A", version1), ("B", version1));
			using var assemblyA = BuildAssembly("A", version1, ("B", version2));
			using var assemblyB1 = BuildAssembly("B", version1);
			using var assemblyB2 = BuildAssembly("B", version2);
			var resolver = new VersionedAssemblyResolver(assemblyA, assemblyB1, assemblyB2);

			var typeSystem = new DecompilerTypeSystem(main, resolver, TypeSystemOptions.None);

			var selectedB = (MetadataModule)typeSystem.ReferencedModules.Single(module => module.AssemblyName == "B");
			Assert.Multiple(() => {
				Assert.That(resolver.Requests, Has.Some.Matches<IAssemblyReference>(reference => reference.Name == "B" && reference.Version == version1));
				Assert.That(resolver.Requests, Has.Some.Matches<IAssemblyReference>(reference => reference.Name == "B" && reference.Version == version2));
				Assert.That(selectedB.AssemblyVersion, Is.EqualTo(version1));
				Assert.That(selectedB.IsNameLookupOnly, Is.False);
			});
		}

		[Test]
		public void FullImplicitReferenceDoesNotReplaceResolvedDirectReference()
		{
			var version1 = new Version(1, 0, 0, 0);
			var version2 = new Version(2, 0, 0, 0);
			var frameworkVersion = new Version(10, 0, 0, 0);
			using var main = BuildAssembly("System.Runtime", frameworkVersion,
				("A", version1), ("System.Runtime.InteropServices", version1));
			using var assemblyA = BuildAssembly("A", version1, ("System.Runtime.InteropServices", version2));
			using var interop1 = BuildAssembly("System.Runtime.InteropServices", version1);
			using var interop2 = BuildAssembly("System.Runtime.InteropServices", version2);
			var resolver = new VersionedAssemblyResolver(assemblyA, interop1, interop2);

			var typeSystem = new DecompilerTypeSystem(main, resolver, TypeSystemOptions.None);

			var selectedInterop = (MetadataModule)typeSystem.ReferencedModules.Single(module => module.AssemblyName == "System.Runtime.InteropServices");
			Assert.Multiple(() => {
				Assert.That(resolver.Requests, Has.Some.Matches<IAssemblyReference>(reference =>
					reference.Name == "System.Runtime.InteropServices" && reference.Version == version1));
				Assert.That(resolver.Requests, Has.Some.Matches<IAssemblyReference>(reference =>
					reference.Name == "System.Runtime.InteropServices" && reference.Version == version2));
				Assert.That(resolver.Requests, Has.None.Matches<IAssemblyReference>(reference =>
					reference.Name == "System.Runtime.InteropServices" && reference.Version == frameworkVersion));
				Assert.That(selectedInterop.AssemblyVersion, Is.EqualTo(version1));
				Assert.That(selectedInterop.IsNameLookupOnly, Is.False);
			});
		}

		[Test]
		public void FullPromotionPropagatesThroughPreviouslyProcessedForwarder()
		{
			var version1 = new Version(1, 0, 0, 0);
			var frameworkVersion = new Version(10, 0, 0, 0);
			using var main = BuildAssembly("System.Runtime", frameworkVersion, ("A", version1));
			using var assemblyA = BuildAssembly("A", version1, ("System.Runtime.InteropServices", frameworkVersion));
			using var interop = BuildForwarderAssembly("System.Runtime.InteropServices", frameworkVersion,
				("ForwardTarget", version1));
			using var forwardTarget = BuildAssembly("ForwardTarget", version1);
			var resolver = new VersionedAssemblyResolver(assemblyA, interop, forwardTarget);

			var typeSystem = new DecompilerTypeSystem(main, resolver, TypeSystemOptions.None);

			var selectedInterop = (MetadataModule)typeSystem.ReferencedModules.Single(module => module.AssemblyName == "System.Runtime.InteropServices");
			var selectedForwardTarget = (MetadataModule)typeSystem.ReferencedModules.Single(module => module.AssemblyName == "ForwardTarget");
			Assert.Multiple(() => {
				Assert.That(resolver.Requests.Count(reference => reference.Name == "System.Runtime.InteropServices"), Is.EqualTo(1),
					"promotion should reuse the in-flight or resolved assembly load");
				Assert.That(selectedInterop.IsNameLookupOnly, Is.False);
				Assert.That(selectedForwardTarget.IsNameLookupOnly, Is.False);
			});
		}

		static PEFile BuildAssembly(string name, Version version, params (string Name, Version Version)[] references)
		{
			return BuildAssembly(name, version, references, forwarderTarget: null);
		}

		static PEFile BuildForwarderAssembly(string name, Version version, (string Name, Version Version) forwarderTarget)
		{
			return BuildAssembly(name, version, new[] { forwarderTarget }, forwarderTarget.Name);
		}

		static PEFile BuildAssembly(string name, Version version,
			IReadOnlyList<(string Name, Version Version)> references, string forwarderTarget)
		{
			var metadata = new MetadataBuilder();
			metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
			metadata.AddAssembly(metadata.GetOrAddString(name), version, default, default, 0, AssemblyHashAlgorithm.None);
			AssemblyReferenceHandle forwarderTargetHandle = default;
			foreach (var reference in references)
			{
				var handle = metadata.AddAssemblyReference(metadata.GetOrAddString(reference.Name), reference.Version,
					default, default, default, default);
				if (reference.Name == forwarderTarget)
				{
					forwarderTargetHandle = handle;
				}
			}
			if (forwarderTarget != null)
			{
				metadata.AddExportedType(TypeAttributes.Public | ForwarderTypeAttributes,
					metadata.GetOrAddString("Forwarded"), metadata.GetOrAddString("ForwardedType"),
					forwarderTargetHandle, typeDefinitionId: 0);
			}
			metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), baseType: default,
				fieldList: MetadataTokens.FieldDefinitionHandle(1), methodList: MetadataTokens.MethodDefinitionHandle(1));

			var peBlob = new BlobBuilder();
			new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(),
				new MetadataRootBuilder(metadata), ilStream: new BlobBuilder()).Serialize(peBlob);
			return new PEFile(name + ".dll", new MemoryStream(peBlob.ToArray()));
		}

		sealed class VersionedAssemblyResolver : IAssemblyResolver
		{
			readonly IReadOnlyList<MetadataFile> assemblies;

			public List<IAssemblyReference> Requests { get; } = new List<IAssemblyReference>();

			public VersionedAssemblyResolver(params MetadataFile[] assemblies)
			{
				this.assemblies = assemblies;
			}

			public MetadataFile Resolve(IAssemblyReference reference)
			{
				Requests.Add(reference);
				return assemblies.SingleOrDefault(assembly =>
					assembly.Name == reference.Name
					&& assembly.Metadata.GetAssemblyDefinition().Version == reference.Version);
			}

			public MetadataFile ResolveModule(MetadataFile mainModule, string moduleName)
			{
				return null;
			}

			public Task<MetadataFile> ResolveAsync(IAssemblyReference reference)
			{
				return Task.FromResult(Resolve(reference));
			}

			public Task<MetadataFile> ResolveModuleAsync(MetadataFile mainModule, string moduleName)
			{
				return Task.FromResult<MetadataFile>(null);
			}
		}
	}
}

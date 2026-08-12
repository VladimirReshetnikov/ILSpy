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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;
using UniversalAssemblyResolver = ICSharpCode.Decompiler.Metadata.UniversalAssemblyResolver;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Metadata
{
	[TestFixture]
	public class AssemblyReferenceTests
	{
		/// <summary>
		/// Builds a minimal in-memory assembly with a single AssemblyRef row, and returns the
		/// <see cref="AssemblyReference"/> wrapper for it.
		/// </summary>
		static AssemblyReference BuildReference(string name, Version version, byte[] publicKeyOrToken, AssemblyFlags flags)
		{
			var builder = new MetadataBuilder();
			builder.AddAssembly(
				builder.GetOrAddString("TestAssembly"),
				new Version(1, 0, 0, 0),
				culture: default,
				publicKey: default,
				flags: default,
				hashAlgorithm: AssemblyHashAlgorithm.Sha1);
			builder.AddModule(
				generation: 0,
				builder.GetOrAddString("TestAssembly.dll"),
				builder.GetOrAddGuid(Guid.Empty),
				default,
				default);
			builder.AddTypeDefinition(
				default,
				default,
				builder.GetOrAddString("<Module>"),
				baseType: default,
				fieldList: MetadataTokens.FieldDefinitionHandle(1),
				methodList: MetadataTokens.MethodDefinitionHandle(1));
			builder.AddAssemblyReference(
				builder.GetOrAddString(name),
				version,
				culture: default,
				builder.GetOrAddBlob(publicKeyOrToken),
				flags,
				hashValue: default);

			var rootBuilder = new MetadataRootBuilder(builder);
			var peBuilder = new ManagedPEBuilder(
				PEHeaderBuilder.CreateLibraryHeader(),
				rootBuilder,
				ilStream: new BlobBuilder());
			var peBlob = new BlobBuilder();
			peBuilder.Serialize(peBlob);

			var reader = new PEReader(peBlob.ToImmutableArray()).GetMetadataReader();
			var handle = reader.AssemblyReferences.Single();
			return new AssemblyReference(reader, handle);
		}

		/// <summary>
		/// The public key of the running core library, together with the token the runtime derives
		/// from it. Using the runtime as the oracle keeps the expectation independent of our own
		/// derivation code.
		/// </summary>
		static (byte[] PublicKey, byte[] Token) CoreLibKeyAndToken()
		{
			var name = typeof(object).Assembly.GetName();
			return (name.GetPublicKey(), name.GetPublicKeyToken());
		}

		[Test]
		public void PublicKeyTokenIsDerivedFromAFullPublicKey()
		{
			var (publicKey, expectedToken) = CoreLibKeyAndToken();
			Assert.That(publicKey, Is.Not.Null.And.Not.Empty, "the running core library should be strong-named");

			var reference = BuildReference("SomeLibrary", new Version(1, 0, 0, 0), publicKey, AssemblyFlags.PublicKey);

			Assert.That(reference.GetPublicKeyToken(), Is.EqualTo(expectedToken));
		}

		[Test]
		public void PublicKeyTokenIsReturnedVerbatimWhenMetadataAlreadyStoresAToken()
		{
			var (_, token) = CoreLibKeyAndToken();

			var reference = BuildReference("SomeLibrary", new Version(1, 0, 0, 0), token, default);

			Assert.That(reference.GetPublicKeyToken(), Is.EqualTo(token));
		}

		[Test]
		public void PublicKeyTokenIsNullWhenTheReferenceCarriesNoBlob()
		{
			var reference = BuildReference("SomeLibrary", new Version(1, 0, 0, 0), Array.Empty<byte>(), default);

			Assert.That(reference.GetPublicKeyToken(), Is.Null);
		}

		[Test]
		public void WindowsRuntimeReferenceUsesExplicitSearchDirectory()
		{
			string assemblyName = "SyntheticWindowsRuntime" + Guid.NewGuid().ToString("N");
			string directory = Path.Combine(Path.GetTempPath(), assemblyName);
			Directory.CreateDirectory(directory);
			try
			{
				string winmdFile = Path.Combine(directory, assemblyName + ".winmd");
				string dllFile = Path.Combine(directory, assemblyName + ".dll");
				File.WriteAllBytes(winmdFile, Array.Empty<byte>());
				File.WriteAllBytes(dllFile, Array.Empty<byte>());

				var reference = BuildReference(
					assemblyName,
					new Version(255, 255, 255, 255),
					Array.Empty<byte>(),
					AssemblyFlags.WindowsRuntime);
				var resolver = new UniversalAssemblyResolver(null, false, ".NETFramework,Version=v4.8");
				resolver.AddSearchDirectory(directory);

				Assert.That(resolver.FindAssemblyFile(reference), Is.EqualTo(winmdFile));
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}
	}
}

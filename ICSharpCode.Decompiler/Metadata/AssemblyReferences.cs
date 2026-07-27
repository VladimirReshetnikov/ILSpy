// Copyright (c) 2018 Siegfried Pammer
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
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Exception thrown when an assembly or module reference cannot be resolved to a metadata file.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="UniversalAssemblyResolver"/> and host-level resolvers use this exception when
	/// <c>throwOnError</c> is enabled. The exception preserves both the requested identity and the
	/// best candidate path (if one was found) to make troubleshooting resolution policy issues easier.
	/// </para>
	/// <para>
	/// For module resolution failures, <see cref="Reference"/> is <see langword="null"/> and the
	/// module-specific properties are populated instead.
	/// </para>
	/// </remarks>
	public sealed class ResolutionException : Exception
	{
		/// <summary>
		/// Gets the unresolved assembly identity for assembly-resolution failures.
		/// </summary>
		public IAssemblyReference? Reference { get; }

		/// <summary>
		/// Gets the unresolved module file name for module-resolution failures.
		/// </summary>
		public string? ModuleName { get; }

		/// <summary>
		/// Gets the fully qualified path of the main module that declared <see cref="ModuleName"/>.
		/// </summary>
		public string? MainModuleFullPath { get; }

		/// <summary>
		/// Gets the candidate full path produced by probing, or <see langword="null"/> when no candidate was found.
		/// </summary>
		public string? ResolvedFullPath { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="ResolutionException"/> for a failed assembly reference.
		/// </summary>
		/// <param name="reference">Assembly identity that could not be resolved.</param>
		/// <param name="resolvedPath">Candidate path that was probed, or <see langword="null"/> if probing produced no path.</param>
		/// <param name="innerException">Underlying IO or image-format exception, if one was observed.</param>
		/// <exception cref="ArgumentNullException"><paramref name="reference"/> is <see langword="null"/>.</exception>
		public ResolutionException(IAssemblyReference reference, string? resolvedPath, Exception? innerException)
			: base($"Failed to resolve assembly: '{reference}'{Environment.NewLine}" +
				  $"Resolve result: {resolvedPath ?? "<not found>"}", innerException)
		{
			this.Reference = reference ?? throw new ArgumentNullException(nameof(reference));
			this.ResolvedFullPath = resolvedPath;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ResolutionException"/> for a failed module reference.
		/// </summary>
		/// <param name="mainModule">Path of the module that declared the unresolved module reference.</param>
		/// <param name="moduleName">Requested module file name from metadata.</param>
		/// <param name="resolvedPath">Candidate path that was probed, or <see langword="null"/> if probing produced no path.</param>
		/// <param name="innerException">Underlying IO or image-format exception, if one was observed.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="mainModule"/> or <paramref name="moduleName"/> is <see langword="null"/>.
		/// </exception>
		public ResolutionException(string mainModule, string moduleName, string? resolvedPath, Exception? innerException)
			: base($"Failed to resolve module: '{moduleName} of {mainModule}'{Environment.NewLine}" +
				  $"Resolve result: {resolvedPath ?? "<not found>"}", innerException)
		{
			this.MainModuleFullPath = mainModule ?? throw new ArgumentNullException(nameof(mainModule));
			this.ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
			this.ResolvedFullPath = resolvedPath;
		}
	}

	/// <summary>
	/// Resolves assembly and module references to <see cref="MetadataFile"/> instances.
	/// </summary>
	/// <remarks>
	/// Implementations are allowed to use multiple strategies (already-loaded modules, search directories,
	/// runtime packs, GAC, or custom host callbacks). Callers should treat returned metadata as read-only.
	/// </remarks>
	public interface IAssemblyResolver
	{
#if !VSADDIN
		/// <summary>
		/// Resolves an assembly reference synchronously.
		/// </summary>
		/// <param name="reference">Assembly identity to resolve.</param>
		/// <returns>The resolved metadata file, or <see langword="null"/> when resolution fails.</returns>
		MetadataFile? Resolve(IAssemblyReference reference);

		/// <summary>
		/// Resolves a module reference synchronously.
		/// </summary>
		/// <param name="mainModule">Main module that declares the module reference.</param>
		/// <param name="moduleName">Referenced module file name.</param>
		/// <returns>The resolved metadata file, or <see langword="null"/> when resolution fails.</returns>
		MetadataFile? ResolveModule(MetadataFile mainModule, string moduleName);

		/// <summary>
		/// Resolves an assembly reference asynchronously.
		/// </summary>
		/// <param name="reference">Assembly identity to resolve.</param>
		/// <returns>A task that completes with the resolved metadata file, or <see langword="null"/> when resolution fails.</returns>
		Task<MetadataFile?> ResolveAsync(IAssemblyReference reference);

		/// <summary>
		/// Resolves a module reference asynchronously.
		/// </summary>
		/// <param name="mainModule">Main module that declares the module reference.</param>
		/// <param name="moduleName">Referenced module file name.</param>
		/// <returns>A task that completes with the resolved metadata file, or <see langword="null"/> when resolution fails.</returns>
		Task<MetadataFile?> ResolveModuleAsync(MetadataFile mainModule, string moduleName);
#endif
	}

	/// <summary>
	/// Classifies assembly references for project-decompilation output decisions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="CSharp.ProjectDecompiler.WholeProjectDecompiler"/> uses classifiers to decide whether a resolved reference should be emitted
	/// as an explicit <c>&lt;Reference&gt;</c> item, omitted as a framework-provided assembly, or emitted without a
	/// <c>HintPath</c>.
	/// </para>
	/// <para>
	/// <see cref="UniversalAssemblyResolver"/> extends this base type to provide runtime-aware classification.
	/// </para>
	/// </remarks>
	public class AssemblyReferenceClassifier
	{
		/// <summary>
		/// For GAC assembly references, the WholeProjectDecompiler will omit the HintPath in the
		/// generated .csproj file.
		/// </summary>
		/// <param name="reference">Reference to classify.</param>
		/// <returns>
		/// <see langword="true"/> when the reference resolves through the machine-wide GAC and can be emitted without
		/// an explicit path; otherwise <see langword="false"/>.
		/// </returns>
		public virtual bool IsGacAssembly(IAssemblyReference reference)
		{
			return UniversalAssemblyResolver.GetAssemblyInGac(reference) != null;
		}

		/// <summary>
		/// For .NET Core framework references, the WholeProjectDecompiler will omit the
		/// assembly reference if the runtimePack is already included as an SDK.
		/// </summary>
		/// <param name="reference">Reference to classify.</param>
		/// <param name="runtimePack">When the method returns <see langword="true"/>, receives the owning runtime-pack identifier.</param>
		/// <returns>
		/// <see langword="true"/> when the reference is supplied by a known shared runtime pack and may be omitted from explicit
		/// project references; otherwise <see langword="false"/>.
		/// </returns>
		public virtual bool IsSharedAssembly(IAssemblyReference reference, [NotNullWhen(true)] out string? runtimePack)
		{
			runtimePack = null;
			return false;
		}
	}

	/// <summary>
	/// Minimal contract representing an assembly identity used by decompiler resolvers.
	/// </summary>
	public interface IAssemblyReference
	{
		/// <summary>Gets the simple assembly name.</summary>
		string Name { get; }
		/// <summary>Gets the display name (full identity string).</summary>
		string FullName { get; }
		/// <summary>Gets the assembly version, if available.</summary>
		Version? Version { get; }
		/// <summary>Gets the culture name, or <see langword="null"/> when not specified.</summary>
		string? Culture { get; }
		/// <summary>Gets the public key token bytes, or <see langword="null"/> when the reference is not strongly named.</summary>
		byte[]? PublicKeyToken { get; }

		/// <summary>Gets whether the reference points to Windows Runtime metadata.</summary>
		bool IsWindowsRuntime { get; }
		/// <summary>Gets whether the reference carries the retargetable flag.</summary>
		bool IsRetargetable { get; }
	}

	/// <summary>
	/// Implementation of <see cref="IAssemblyReference"/> based on assembly display-name text.
	/// </summary>
	public class AssemblyNameReference : IAssemblyReference
	{
		string? fullName;

		/// <summary>
		/// Gets the simple assembly name.
		/// </summary>
		public string Name { get; private set; } = string.Empty;

		/// <summary>
		/// Gets a normalized full display name built from the parsed identity components.
		/// </summary>
		public string FullName {
			get {
				if (fullName != null)
					return fullName;

				const string sep = ", ";

				var builder = new StringBuilder();
				builder.Append(Name);
				builder.Append(sep);
				builder.Append("Version=");
				builder.Append((Version ?? UniversalAssemblyResolver.ZeroVersion).ToString(fieldCount: 4));
				builder.Append(sep);
				builder.Append("Culture=");
				builder.Append(string.IsNullOrEmpty(Culture) ? "neutral" : Culture);
				builder.Append(sep);
				builder.Append("PublicKeyToken=");

				var pk_token = PublicKeyToken;
				if (pk_token != null && pk_token.Length > 0)
				{
					for (int i = 0; i < pk_token.Length; i++)
					{
						builder.Append(pk_token[i].ToString("x2"));
					}
				}
				else
					builder.Append("null");

				if (IsRetargetable)
				{
					builder.Append(sep);
					builder.Append("Retargetable=Yes");
				}

				return fullName = builder.ToString();
			}
		}

		/// <summary>Gets the parsed assembly version.</summary>
		public Version? Version { get; private set; }

		/// <summary>Gets the parsed culture (empty string represents neutral culture).</summary>
		public string? Culture { get; private set; }

		/// <summary>Gets the parsed public key token bytes.</summary>
		public byte[]? PublicKeyToken { get; private set; }

		/// <summary>Gets whether this reference represents Windows Runtime metadata.</summary>
		public bool IsWindowsRuntime { get; private set; }

		/// <summary>Gets whether this reference is marked retargetable.</summary>
		public bool IsRetargetable { get; private set; }

		/// <summary>
		/// Parses an assembly display name into an <see cref="AssemblyNameReference"/>.
		/// </summary>
		/// <param name="fullName">Display name such as <c>System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=...</c>.</param>
		/// <returns>The parsed reference object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="fullName"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="fullName"/> is empty or malformed.</exception>
		public static AssemblyNameReference Parse(string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException(nameof(fullName));
			if (fullName.Length == 0)
				throw new ArgumentException("Name can not be empty");

			var name = new AssemblyNameReference();
			var tokens = fullName.Split(',');
			for (int i = 0; i < tokens.Length; i++)
			{
				var token = tokens[i].Trim();

				if (i == 0)
				{
					name.Name = token;
					continue;
				}

				var parts = token.Split('=');
				if (parts.Length != 2)
					throw new ArgumentException("Malformed name");

				switch (parts[0].ToLowerInvariant())
				{
					case "version":
						name.Version = new Version(parts[1]);
						break;
					case "culture":
						name.Culture = parts[1] == "neutral" ? "" : parts[1];
						break;
					case "publickeytoken":
						var pk_token = parts[1];
						if (pk_token == "null")
							break;

						name.PublicKeyToken = new byte[pk_token.Length / 2];
						for (int j = 0; j < name.PublicKeyToken.Length; j++)
							name.PublicKeyToken[j] = Byte.Parse(pk_token.Substring(j * 2, 2), System.Globalization.NumberStyles.HexNumber);

						break;
				}
			}

			return name;
		}

		/// <summary>
		/// Returns <see cref="FullName"/>.
		/// </summary>
		/// <returns>The normalized full assembly identity string.</returns>
		public override string ToString()
		{
			return FullName;
		}
	}

#if !VSADDIN
	/// <summary>
	/// <see cref="IAssemblyReference"/> implementation backed by a <see cref="MetadataReader"/> assembly-reference row.
	/// </summary>
	public class AssemblyReference : IAssemblyReference
	{
		readonly System.Reflection.Metadata.AssemblyReference entry;

		/// <summary>
		/// Gets the metadata reader that owns <see cref="Handle"/>.
		/// </summary>
		public MetadataReader Metadata { get; }
		/// <summary>
		/// Gets the metadata handle for the referenced assembly row.
		/// </summary>
		public AssemblyReferenceHandle Handle { get; }

		/// <summary>
		/// Gets whether the referenced assembly is marked as Windows Runtime metadata.
		/// </summary>
		public bool IsWindowsRuntime => (entry.Flags & AssemblyFlags.WindowsRuntime) != 0;
		/// <summary>
		/// Gets whether the reference is marked retargetable.
		/// </summary>
		public bool IsRetargetable => (entry.Flags & AssemblyFlags.Retargetable) != 0;

		string? name;
		string? fullName;

		/// <summary>
		/// Gets the simple assembly name with malformed-metadata fallback text.
		/// </summary>
		public string Name {
			get {
				if (name == null)
				{
					try
					{
						name = Metadata.GetString(entry.Name);
					}
					catch (BadImageFormatException)
					{
						name = $"AR:{Handle}";
					}
				}
				return name;
			}
		}

		/// <summary>
		/// Gets the full assembly display name with malformed-metadata fallback text.
		/// </summary>
		public string FullName {
			get {
				if (fullName == null)
				{
					try
					{
						fullName = entry.GetFullAssemblyName(Metadata);
					}
					catch (BadImageFormatException)
					{
						fullName = $"fullname(AR:{Handle})";
					}
				}
				return fullName;
			}
		}

		/// <summary>Gets the referenced assembly version.</summary>
		public Version? Version => entry.Version;
		/// <summary>Gets the referenced culture name.</summary>
		public string Culture => Metadata.GetString(entry.Culture);
		/// <summary>Gets the public key token (explicit token or token derived from the full public key).</summary>
		byte[]? IAssemblyReference.PublicKeyToken => GetPublicKeyToken();
		byte[]? publicKeyToken;

		/// <summary>
		/// Gets the public key token for this reference.
		/// </summary>
		/// <returns>
		/// The token bytes, or <see langword="null"/> when the reference has no public-key or token blob.
		/// </returns>
		/// <remarks>
		/// If metadata stores a full public key (<see cref="AssemblyFlags.PublicKey"/>), the token is the last
		/// 8 bytes of its SHA-1 hash in reverse order, which is the form the runtime and the GAC layout use.
		/// </remarks>
		public byte[]? GetPublicKeyToken()
		{
			if (entry.PublicKeyOrToken.IsNil)
				return null;

			if (publicKeyToken == null)
			{
				var bytes = Metadata.GetBlobBytes(entry.PublicKeyOrToken);
				if ((entry.Flags & AssemblyFlags.PublicKey) != 0)
				{
					byte[] hash = new byte[20];
					Sha1ForNonSecretPurposes.HashData(bytes, hash);
					bytes = hash.Skip(12).Reverse().ToArray();
				}

				publicKeyToken = bytes;
			}

			return publicKeyToken;
		}

		ImmutableArray<TypeReferenceMetadata> typeReferences;
		/// <summary>
		/// Gets type references that are scoped to this assembly reference.
		/// </summary>
		/// <remarks>The sequence is cached and sorted by namespace then name for deterministic traversal.</remarks>
		public ImmutableArray<TypeReferenceMetadata> TypeReferences {
			get {
				var value = typeReferences;
				if (value.IsDefault)
				{
					value = Metadata.TypeReferences
						.Select(r => new TypeReferenceMetadata(Metadata, r))
						.Where(r => r.ResolutionScope == Handle)
						.OrderBy(r => r.Namespace)
						.ThenBy(r => r.Name)
						.ToImmutableArray();
					typeReferences = value;
				}
				return value;
			}
		}

		ImmutableArray<ExportedTypeMetadata> exportedTypes;
		/// <summary>
		/// Gets exported types forwarded to this assembly reference.
		/// </summary>
		/// <remarks>The sequence is cached and sorted by namespace then name for deterministic traversal.</remarks>
		public ImmutableArray<ExportedTypeMetadata> ExportedTypes {
			get {
				var value = exportedTypes;
				if (value.IsDefault)
				{
					value = Metadata.ExportedTypes
						.Select(r => new ExportedTypeMetadata(Metadata, r))
						.Where(r => r.Implementation == Handle)
						.OrderBy(r => r.Namespace)
						.ThenBy(r => r.Name)
						.ToImmutableArray();
					exportedTypes = value;
				}
				return value;
			}
		}

		/// <summary>
		/// Initializes an <see cref="AssemblyReference"/> from raw metadata components.
		/// </summary>
		/// <param name="metadata">Metadata reader that contains the reference row.</param>
		/// <param name="handle">Handle of the assembly-reference row.</param>
		/// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/> or <paramref name="handle"/> is nil.</exception>
		public AssemblyReference(MetadataReader metadata, AssemblyReferenceHandle handle)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = metadata;
			Handle = handle;
			entry = metadata.GetAssemblyReference(handle);
		}

		/// <summary>
		/// Initializes an <see cref="AssemblyReference"/> from a metadata file.
		/// </summary>
		/// <param name="module">Metadata file that contains the reference row.</param>
		/// <param name="handle">Handle of the assembly-reference row.</param>
		/// <exception cref="ArgumentNullException"><paramref name="module"/> is <see langword="null"/> or <paramref name="handle"/> is nil.</exception>
		public AssemblyReference(MetadataFile module, AssemblyReferenceHandle handle)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = module.Metadata;
			Handle = handle;
			entry = Metadata.GetAssemblyReference(handle);
		}

		/// <summary>
		/// Returns <see cref="FullName"/>.
		/// </summary>
		/// <returns>The full assembly display name.</returns>
		public override string ToString()
		{
			return FullName;
		}
	}
#endif
}

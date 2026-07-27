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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Discovers and exposes compiler-generated metadata that represents C# extension blocks.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The C# compiler emits extension declarations as synthetic nested types and methods.
	/// <see cref="ExtensionInfo"/> reverse-engineers those encodings and builds a bidirectional map between
	/// metadata-only extension signatures and the static implementation methods that contain executable bodies.
	/// </para>
	/// <para>
	/// Consumers such as <see cref="CSharp.CSharpDecompiler"/> use this map to hide implementation artifacts and
	/// reconstruct source-shaped extension declarations.
	/// </para>
	/// </remarks>
	public class ExtensionInfo
	{
		readonly Dictionary<IMember, ExtensionMemberInfo> extensionMemberMap;
		readonly Dictionary<IMember, ExtensionMemberInfo> implementationMemberMap;
		readonly List<(IMethod Marker, IReadOnlyList<ITypeParameter> TypeParameters)> extensionGroups;

		/// <summary>
		/// Creates extension metadata mappings for a single extension container type.
		/// </summary>
		/// <param name="module">Owning metadata module used to query raw type/method definitions.</param>
		/// <param name="extensionContainer">Static class that contains compiler-emitted extension artifacts.</param>
		public ExtensionInfo(MetadataModule module, ITypeDefinition extensionContainer)
		{
			this.extensionMemberMap = new();
			this.implementationMemberMap = new();
			this.extensionGroups = [];
			this.Container = extensionContainer;

			var metadata = module.MetadataFile.Metadata;

			foreach (var nestedType in extensionContainer.NestedTypes)
			{
				if (TryEncodingV1(nestedType))
				{
					continue;
				}

				TryEncodingV2(nestedType);
			}

			bool TryEncodingV1(ITypeDefinition extGroup)
			{
				if (!(extGroup is { Kind: TypeKind.Class, IsSealed: true }
					&& extGroup.Name.StartsWith("<>E__", System.StringComparison.Ordinal)))
				{
					return false;
				}

				TypeDefinition td = metadata.GetTypeDefinition((TypeDefinitionHandle)extGroup.MetadataToken);
				IMethod? marker = null;
				bool hasMultipleMarkers = false;
				List<IMethod> extensionMethods = [];
				ITypeParameter[] extensionGroupTypeParameters = extGroup.TypeParameters.ToArray();

				// For easier access to accessors we use SRM
				foreach (var h in td.GetMethods())
				{
					var method = module.GetDefinition(h);

					if (method.SymbolKind is SymbolKind.Constructor)
						continue;
					if (method is { Name: "<Extension>$", IsStatic: true, Parameters.Count: 1 })
					{
						if (marker == null)
							marker = method;
						else
							hasMultipleMarkers = true;
						continue;
					}

					extensionMethods.Add(method);
				}

				if (marker == null || hasMultipleMarkers)
					return false;

				extensionGroups.Add((marker, extensionGroupTypeParameters));

				CollectImplementationMethods(marker, extensionGroupTypeParameters, extensionMethods);
				return true;
			}

			bool TryEncodingV2(ITypeDefinition extensionGroupsContainer)
			{
				// there exists one nested type per extension target type
				if (!(extensionGroupsContainer is { Kind: TypeKind.Class, IsSealed: true }
					&& extensionGroupsContainer.Name.StartsWith("<G>$", StringComparison.Ordinal)))
				{
					return false;
				}

				// if there are multiple extension-blocks with the same target type,
				// but different names for the extension parameter,
				// there is a separate markerType, so there are multiple marker types per
				// target type
				foreach (var markerType in extensionGroupsContainer.NestedTypes)
				{
					if (!(markerType.Name.StartsWith("<M>$", StringComparison.Ordinal) && markerType.IsStatic))
						continue;
					var marker = markerType.Methods.SingleOrDefault(m => m.Name == "<Extension>$" && m.IsStatic && m.Parameters.Count == 1);
					if (marker == null)
						continue;

					ITypeParameter[] extensionGroupTypeParameters = new ITypeParameter[markerType.TypeParameterCount];
					var markerTypeTypeParameters = metadata.GetTypeDefinition((TypeDefinitionHandle)markerType.MetadataToken).GetGenericParameters();

					foreach (var h in markerTypeTypeParameters.WithIndex())
					{
						var tp = metadata.GetGenericParameter(h.Item2);
						extensionGroupTypeParameters[h.Item1] = MetadataTypeParameter.Create(module, markerType, h.Item1, h.Item2);
					}

					extensionGroups.Add((marker, extensionGroupTypeParameters));

					TypeDefinition td = metadata.GetTypeDefinition((TypeDefinitionHandle)extensionGroupsContainer.MetadataToken);
					List<IMethod> extensionMethods = [];

					// For easier access to accessors we use SRM
					foreach (var h in td.GetMethods())
					{
						var method = module.GetDefinition(h);

						if (method.SymbolKind is SymbolKind.Constructor)
							continue;

						var attribute = method.GetAttribute(KnownAttribute.ExtensionMarker);
						if (attribute == null)
							continue;

						if (attribute.FixedArguments[0].Value?.ToString() != markerType.Name)
							continue;

						extensionMethods.Add(method);
					}

					CollectImplementationMethods(marker, extensionGroupTypeParameters, extensionMethods);
				}

				return true;
			}

			void CollectImplementationMethods(IMethod marker, IReadOnlyList<ITypeParameter> typeParameters, List<IMethod> extensionMethods)
			{
				ITypeDefinition markerType = marker.DeclaringTypeDefinition!;
				List<(IMethod extension, IMethod implementation)> implementations = [];

				foreach (var extension in extensionMethods)
				{
					int expectedTypeParameterCount = extension.TypeParameters.Count + markerType.TypeParameterCount;
					bool hasInstance = !extension.IsStatic;
					int parameterOffset = hasInstance ? 1 : 0;
					int expectedParameterCount = extension.Parameters.Count + parameterOffset;
					TypeParameterSubstitution subst = new TypeParameterSubstitution([], [.. markerType.TypeArguments, .. extension.TypeArguments]);

					bool IsMatchingImplementation(IMethod impl)
					{
						if (!impl.IsStatic)
							return false;
						if (extension.Name != impl.Name)
							return false;
						if (expectedTypeParameterCount != impl.TypeParameters.Count)
							return false;
						if (expectedParameterCount != impl.Parameters.Count)
							return false;
						if (hasInstance)
						{
							IType ti = impl.Parameters[0].Type.AcceptVisitor(subst);
							IType tm = marker.Parameters.Single().Type;
							if (!NormalizeTypeVisitor.TypeErasure.EquivalentTypes(ti, tm))
								return false;
						}
						for (int i = 0; i < extension.Parameters.Count; i++)
						{
							IType ti = impl.Parameters[i + parameterOffset].Type.AcceptVisitor(subst);
							IType tm = extension.Parameters[i].Type;
							if (!NormalizeTypeVisitor.TypeErasure.EquivalentTypes(ti, tm))
								return false;
						}
						return NormalizeTypeVisitor.TypeErasure.EquivalentTypes(
							impl.ReturnType.AcceptVisitor(subst),
							extension.ReturnType
						);
					}

					IMethod? foundImpl = null;

					foreach (var impl in extensionContainer.Methods)
					{
						if (!IsMatchingImplementation(impl))
							continue;
						Debug.Assert(foundImpl == null, "Multiple matching implementations found");
						foundImpl = impl;
					}

					Debug.Assert(foundImpl != null, "No matching implementation found");

					implementations.Add((extension, foundImpl));
				}

				foreach (var (extension, implementation) in implementations)
				{
					var info = new ExtensionMemberInfo(marker, typeParameters, extension, implementation);
					this.extensionMemberMap[extension] = info;
					this.implementationMemberMap[implementation] = info;
				}
			}
		}

		/// <summary>
		/// Gets the static class that contains the compiler-emitted extension artifacts described by this map.
		/// </summary>
		public ITypeDefinition Container { get; }

		/// <summary>
		/// Gets decompiler metadata for a method that is declared inside an extension grouping type.
		/// </summary>
		/// <param name="method">Method that may represent an extension member declaration artifact.</param>
		/// <returns>
		/// Extension mapping information when <paramref name="method"/> is known as an extension member;
		/// otherwise <see langword="null"/>.
		/// </returns>
		public ExtensionMemberInfo? InfoOfExtensionMember(IMethod method)
		{
			return this.extensionMemberMap.TryGetValue(method, out var value) ? value : null;
		}

		/// <summary>
		/// Gets decompiler metadata for a static implementation method emitted in the extension container.
		/// </summary>
		/// <param name="method">Method that may be an implementation target for an extension declaration.</param>
		/// <returns>
		/// Extension mapping information when <paramref name="method"/> is recognized as an implementation method;
		/// otherwise <see langword="null"/>.
		/// </returns>
		public ExtensionMemberInfo? InfoOfImplementationMember(IMethod method)
		{
			return this.implementationMemberMap.TryGetValue(method, out var value) ? value : null;
		}

		/// <summary>
		/// Determines whether <paramref name="td"/> is a static class holding extension groups.
		/// </summary>
		/// <param name="td">Type definition to test.</param>
		/// <returns>
		/// <see langword="true"/> when at least one extension group in this map is nested inside
		/// <paramref name="td"/>; otherwise <see langword="false"/>.
		/// </returns>
		public bool IsExtensionGroupType(ITypeDefinition td)
		{
			return this.extensionGroups.Any(m => m.Marker.DeclaringTypeDefinition?.DeclaringTypeDefinition?.Equals(td) == true);
		}

		/// <summary>
		/// Determines whether <paramref name="td"/> is one of the compiler-generated extension marker types.
		/// </summary>
		/// <param name="td">Type definition to test.</param>
		/// <param name="extensionGroup">
		/// On success, the marker method and extension-block type parameters declared by <paramref name="td"/>.
		/// </param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="td"/> declares one of the compiler-generated
		/// <c>&lt;Extension&gt;$</c> marker methods tracked by this map; otherwise <see langword="false"/>.
		/// </returns>
		public bool IsExtensionMarkerType(ITypeDefinition td, out (IMethod Marker, IReadOnlyList<ITypeParameter> TypeParameters) extensionGroup)
		{
			extensionGroup = default;
			foreach (var group in this.extensionGroups)
			{
				if (group.Marker.DeclaringTypeDefinition?.Equals(td) == true)
				{
					extensionGroup = group;
					return true;
				}
			}
			return false;
		}

		public IEnumerable<IMember> GetMembersOfGroup(IMethod marker)
		{
			return GetMembers().Distinct();

			IEnumerable<IMember> GetMembers()
			{
				var markerType = marker.DeclaringTypeDefinition;
				Debug.Assert(markerType != null, "Marker should always be contained in a type");

				foreach (var info in extensionMemberMap.Values)
				{
					if (!markerType.Equals(info.ExtensionMarkerType))
						continue;
					var subst = new TypeParameterSubstitution(info.ExtensionGroupingTypeParameters, null);
					if (info.ExtensionMember.IsAccessor)
						yield return info.ExtensionMember.AccessorOwner.Specialize(subst);
					else
						yield return info.ExtensionMember.Specialize(subst);
				}
			}
		}

		public IReadOnlyList<(IMethod Marker, IReadOnlyList<ITypeParameter> TypeParameters)> ExtensionGroups => this.extensionGroups;
	}

	/// <summary>
	/// Describes the relationship between an extension declaration artifact and its executable implementation method.
	/// </summary>
	public readonly struct ExtensionMemberInfo(IMethod marker, IReadOnlyList<ITypeParameter> typeParameters, IMethod extension, IMethod implementation)
	{
		/// <summary>
		/// Metadata-only method called '&lt;Extension&gt;$'. Has the C# signature for the extension declaration.
		/// 
		/// <code>extension(ReceiverType name) {} -> void &lt;Extension&gt;$(ReceiverType name) {}</code>
		/// </summary>
		public readonly IMethod ExtensionMarkerMethod = marker;
		/// <summary>
		/// Metadata-only method with a signature as declared in C# within the extension declaration.
		/// This could also be an accessor of an extension property.
		/// </summary>
		public readonly IMethod ExtensionMember = extension;
		/// <summary>
		/// The actual implementation method in the outer class. The signature is a concatenation
		/// of the extension marker and the extension member's signatures.
		/// </summary>
		public readonly IMethod ImplementationMethod = implementation;

		/// <summary>
		/// The type parameters declared by the extension block.
		/// </summary>
		public readonly IReadOnlyList<ITypeParameter> ExtensionGroupingTypeParameters = typeParameters;

		/// <summary>
		/// Gets the enclosing static class that contains <see cref="ImplementationMethod"/>.
		/// </summary>
		public ITypeDefinition ExtensionContainer => ImplementationMethod.DeclaringTypeDefinition!;

		/// <summary>
		/// Gets the compiler-generated nested type that contains metadata-only extension member signatures.
		/// It declares the extension block's type parameters with minimal constraints; see
		/// <see cref="ExtensionMarkerType"/> for the full-fidelity constraint set.
		/// </summary>
		public ITypeDefinition ExtensionGroupingType => ExtensionMember.DeclaringTypeDefinition!;

		/// <summary>
		/// Gets the marker type that holds the extension declaration's type parameters with full fidelity
		/// of their C# constraints.
		/// </summary>
		public ITypeDefinition ExtensionMarkerType => ExtensionMarkerMethod.DeclaringTypeDefinition!;
	}
}

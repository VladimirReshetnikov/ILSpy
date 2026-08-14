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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler
{
	/// <summary>
	/// Generates C# stub declarations for a dependency that is not available, from the
	/// expectations recorded in consumer assemblies' metadata: every type reference into the
	/// dependency, and every member reference with its full signature. The result compiles into
	/// a stand-in assembly that satisfies those references at compile time.
	/// </summary>
	/// <remarks>
	/// The stub can only contain what consumers demand: members a consumer overrides without
	/// otherwise calling leave no member reference and are absent. Value-type-ness is recovered
	/// from signature and local-variable encodings; interfaces from interface-implementation
	/// rows; attributes from custom-attribute usage; delegates from the Invoke/.ctor pair.
	/// An enum cannot be told apart from a struct by references alone and comes out as a struct.
	/// Constraints of resolved generics instantiated with a stub type contribute required bases
	/// and interface-member implementations. Regenerating while a previously built stub is on the
	/// reference path degrades the result silently - the types then resolve and stop looking like
	/// expectations - so remove prior stub outputs before regenerating. Conversions between two
	/// stub types (their mutual hierarchy) leave no table evidence and are not recovered.
	/// </remarks>
	public sealed class StubAssemblyGenerator
	{
		readonly IAssemblyResolver assemblyResolver;
		readonly List<MetadataFile> consumers = new();

		public StubAssemblyGenerator(IAssemblyResolver assemblyResolver)
		{
			this.assemblyResolver = assemblyResolver ?? throw new ArgumentNullException(nameof(assemblyResolver));
		}

		public void AddConsumer(MetadataFile file)
		{
			consumers.Add(file ?? throw new ArgumentNullException(nameof(file)));
		}

		sealed class StubType
		{
			public StubType(FullTypeName name) => Name = name;
			public FullTypeName Name { get; }
			public bool IsInterface;
			public bool IsValueType;
			public bool IsAttribute;
			public bool HasBaseClassEvidence;
			public readonly List<IMethod> Methods = new();
			public readonly List<IField> Fields = new();
			public readonly HashSet<string> MemberKeys = new();
			public readonly List<IType> RequiredBases = new();
			public readonly HashSet<string> RequiredBaseKeys = new();
			public bool NeedsParameterlessCtor;

			public bool IsDelegate =>
				Methods.Any(m => m.Name == "Invoke")
				&& Methods.Any(m => m.SymbolKind == SymbolKind.Constructor && m.Parameters.Count == 2);
		}

		/// <summary>
		/// Generates the stub source for the dependency with the given simple assembly name,
		/// from all consumers added so far.
		/// </summary>
		public string GenerateSource(string targetAssemblySimpleName)
		{
			if (string.IsNullOrEmpty(targetAssemblySimpleName))
				throw new ArgumentException("Assembly simple name required", nameof(targetAssemblySimpleName));

			var stubs = new Dictionary<string, StubType>(StringComparer.Ordinal);

			foreach (var consumer in consumers)
			{
				CollectFromConsumer(consumer, targetAssemblySimpleName, stubs);
			}

			return Emit(stubs);
		}

		void CollectFromConsumer(MetadataFile consumer, string targetName, Dictionary<string, StubType> stubs)
		{
			var typeSystem = new DecompilerTypeSystem(consumer, assemblyResolver);
			var module = (MetadataModule)typeSystem.MainModule;
			var metadata = consumer.Metadata;

			// Which assembly-reference rows denote the target.
			var targetAsmRefs = new HashSet<EntityHandle>();
			foreach (var h in metadata.AssemblyReferences)
			{
				var ar = metadata.GetAssemblyReference(h);
				if (string.Equals(metadata.GetString(ar.Name), targetName, StringComparison.OrdinalIgnoreCase))
					targetAsmRefs.Add(h);
			}
			if (targetAsmRefs.Count == 0)
				return;

			// Which type references belong to the target (directly or as nested types).
			var belongs = new Dictionary<TypeReferenceHandle, bool>();
			bool Belongs(TypeReferenceHandle h)
			{
				if (belongs.TryGetValue(h, out bool known))
					return known;
				var tr = metadata.GetTypeReference(h);
				bool result = tr.ResolutionScope.Kind switch {
					HandleKind.AssemblyReference => targetAsmRefs.Contains(tr.ResolutionScope),
					HandleKind.TypeReference => Belongs((TypeReferenceHandle)tr.ResolutionScope),
					_ => false,
				};
				belongs[h] = result;
				return result;
			}

			StubType Ensure(FullTypeName name)
			{
				string key = name.ReflectionName;
				if (!stubs.TryGetValue(key, out var stub))
					stubs[key] = stub = new StubType(name);
				return stub;
			}

			// Every type reference into the target gets a stub, member-bearing or not.
			foreach (var h in metadata.TypeReferences)
			{
				if (Belongs(h))
					Ensure(h.GetFullTypeName(metadata));
			}
			if (stubs.Count == 0)
				return;

			bool TryGetStub(IType type, out StubType stub)
			{
				stub = null!;
				var def = type.GetDefinition();
				if (def != null)
					return false; // resolved elsewhere; not ours
				return stubs.TryGetValue(type.ReflectionName, out stub!);
			}

			// Kind evidence: base types and interface implementations of consumer types.
			foreach (var tdHandle in metadata.TypeDefinitions)
			{
				var td = metadata.GetTypeDefinition(tdHandle);
				var baseType = td.BaseType;
				if (!baseType.IsNil && baseType.Kind == HandleKind.TypeReference && Belongs((TypeReferenceHandle)baseType))
				{
					var baseStub = Ensure(((TypeReferenceHandle)baseType).GetFullTypeName(metadata));
					baseStub.HasBaseClassEvidence = true;
					// A type carrying [AttributeUsage] is an attribute class, and an attribute
					// class must have System.Attribute ancestry - so its stub base needs it too.
					foreach (var tdAttrHandle in td.GetCustomAttributes())
					{
						var attrCtor = metadata.GetCustomAttribute(tdAttrHandle).Constructor;
						if (attrCtor.Kind != HandleKind.MemberReference)
							continue;
						var attrParent = metadata.GetMemberReference((MemberReferenceHandle)attrCtor).Parent;
						if (attrParent.Kind == HandleKind.TypeReference
							&& ((TypeReferenceHandle)attrParent).GetFullTypeName(metadata).ReflectionName
								== "System.AttributeUsageAttribute")
						{
							baseStub.IsAttribute = true;
						}
					}
				}
				foreach (var iiHandle in td.GetInterfaceImplementations())
				{
					var iface = metadata.GetInterfaceImplementation(iiHandle).Interface;
					if (iface.Kind == HandleKind.TypeReference && Belongs((TypeReferenceHandle)iface))
						Ensure(((TypeReferenceHandle)iface).GetFullTypeName(metadata)).IsInterface = true;
					else if (iface.Kind == HandleKind.TypeSpecification)
					{
						var resolved = module.ResolveType(iface, default);
						if (TryGetStub(resolved is ParameterizedType pt ? pt.GenericType : resolved, out var stub))
							stub.IsInterface = true;
					}
				}
			}

			// Kind evidence: custom-attribute usage.
			foreach (var caHandle in metadata.CustomAttributes)
			{
				var ctorHandle = metadata.GetCustomAttribute(caHandle).Constructor;
				if (ctorHandle.Kind != HandleKind.MemberReference)
					continue;
				var parent = metadata.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
				if (parent.Kind == HandleKind.TypeReference && Belongs((TypeReferenceHandle)parent))
					Ensure(((TypeReferenceHandle)parent).GetFullTypeName(metadata)).IsAttribute = true;
			}

			// Members: every member reference whose declaring type is a stub.
			foreach (var mrHandle in metadata.MemberReferences)
			{
				var mr = metadata.GetMemberReference(mrHandle);
				FullTypeName declaringName;
				switch (mr.Parent.Kind)
				{
					case HandleKind.TypeReference when Belongs((TypeReferenceHandle)mr.Parent):
						declaringName = ((TypeReferenceHandle)mr.Parent).GetFullTypeName(metadata);
						break;
					case HandleKind.TypeSpecification:
					{
						var declaring = module.ResolveType(mr.Parent, default);
						var generic = declaring is ParameterizedType pt ? pt.GenericType : declaring;
						if (!TryGetStub(generic, out var specStub))
							continue;
						declaringName = specStub.Name;
						break;
					}
					default:
						continue;
				}

				var stubType = Ensure(declaringName);
				var entity = module.ResolveEntity(mrHandle);
				switch (entity)
				{
					case IMethod method:
						AddMethod(stubType, method);
						break;
					case IField field:
						if (stubType.MemberKeys.Add("F:" + field.Name))
							stubType.Fields.Add(field);
						break;
				}
			}

			// Constraint evidence: a stub type used as a type argument of a RESOLVED generic must
			// satisfy that type parameter's constraints, or no instantiation compiles. The resolved
			// definition states them outright; substitute the instantiation's own arguments so a
			// constraint like 'where THolder : MoleBase<T>' lands as the closed MoleBase<Arg>.
			void CollectConstraintEvidence(IType type)
			{
				if (type is ParameterizedType pt)
				{
					var def = pt.GenericType.GetDefinition();
					if (def != null && def.TypeParameterCount == pt.TypeArguments.Count)
					{
						var substitution = pt.GetSubstitution();
						for (int i = 0; i < pt.TypeArguments.Count; i++)
						{
							if (pt.TypeArguments[i] is not UnknownType unknown
								|| !stubs.TryGetValue(unknown.ReflectionName, out var argStub))
							{
								continue;
							}
							var tp = def.TypeParameters[i];
							foreach (var constraint in tp.DirectBaseTypes)
							{
								if (constraint.IsKnownType(KnownTypeCode.Object)
									|| constraint.IsKnownType(KnownTypeCode.ValueType))
								{
									continue;
								}
								var closed = constraint.AcceptVisitor(substitution);
								if (argStub.RequiredBaseKeys.Add(closed.ReflectionName))
									argStub.RequiredBases.Add(closed);
							}
							if (tp.HasDefaultConstructorConstraint)
								argStub.NeedsParameterlessCtor = true;
						}
					}
					foreach (var arg in pt.TypeArguments)
						CollectConstraintEvidence(arg);
				}
				else if (type is TypeWithElementType te)
				{
					CollectConstraintEvidence(te.ElementType);
				}
			}

			foreach (var stub in stubs.Values.ToList())
			{
				foreach (var method in stub.Methods)
				{
					CollectConstraintEvidence(method.ReturnType);
					foreach (var p in method.Parameters)
						CollectConstraintEvidence(p.Type);
				}
				foreach (var field in stub.Fields)
					CollectConstraintEvidence(field.Type);
			}
			int typeSpecCount = metadata.GetTableRowCount(TableIndex.TypeSpec);
			for (int row = 1; row <= typeSpecCount; row++)
			{
				try
				{
					CollectConstraintEvidence(module.ResolveType(MetadataTokens.TypeSpecificationHandle(row), default));
				}
				catch (BadImageFormatException)
				{
				}
			}
			int methodSpecCount = metadata.GetTableRowCount(TableIndex.MethodSpec);
			for (int row = 1; row <= methodSpecCount; row++)
			{
				IMethod? specialized;
				try
				{
					specialized = module.ResolveEntity(MetadataTokens.MethodSpecificationHandle(row)) as IMethod;
				}
				catch (BadImageFormatException)
				{
					continue;
				}
				if (specialized?.MemberDefinition is not IMethod specDef
					|| specDef.TypeParameters.Count != specialized.TypeArguments.Count
					|| specDef.DeclaringType?.GetDefinition() == null)
				{
					continue;
				}
				var specSubstitution = new TypeParameterSubstitution(
					specialized.DeclaringType?.TypeArguments, specialized.TypeArguments);
				for (int i = 0; i < specialized.TypeArguments.Count; i++)
				{
					if (specialized.TypeArguments[i] is not UnknownType unknown
						|| !stubs.TryGetValue(unknown.ReflectionName, out var argStub))
					{
						continue;
					}
					foreach (var constraint in specDef.TypeParameters[i].DirectBaseTypes)
					{
						if (constraint.IsKnownType(KnownTypeCode.Object)
							|| constraint.IsKnownType(KnownTypeCode.ValueType))
						{
							continue;
						}
						var closed = constraint.AcceptVisitor(specSubstitution);
						if (argStub.RequiredBaseKeys.Add(closed.ReflectionName))
							argStub.RequiredBases.Add(closed);
					}
					if (specDef.TypeParameters[i].HasDefaultConstructorConstraint)
						argStub.NeedsParameterlessCtor = true;
				}
			}

			// Value-type evidence, part 1: unknown types in the member signatures just collected.
			foreach (var stub in stubs.Values.ToList())
			{
				foreach (var method in stub.Methods)
				{
					NoteValueTypeEvidence(method.ReturnType, stubs);
					foreach (var p in method.Parameters)
						NoteValueTypeEvidence(p.Type, stubs);
				}
				foreach (var field in stub.Fields)
					NoteValueTypeEvidence(field.Type, stubs);
			}

			// Value-type evidence, part 2: local-variable signatures. A struct that only ever
			// lives in locals (constructed in place, fields accessed through the local) never
			// shows up value-type-encoded in any member signature.
			foreach (var sigHandle in EnumerateStandaloneSignatures(metadata))
			{
				// The StandAloneSig table also holds calli method signatures and field signatures, and
				// GetKind itself can throw on rows older toolchains emitted - treat any row that does
				// not decode as a well-formed local signature as simply not evidence.
				try
				{
					var sig = metadata.GetStandaloneSignature(sigHandle);
					if (sig.GetKind() != StandaloneSignatureKind.LocalVariables)
						continue;
					foreach (var localType in sig.DecodeLocalSignature(module.TypeProvider, default))
						NoteValueTypeEvidence(localType, stubs);
				}
				catch (BadImageFormatException)
				{
					continue;
				}
			}
		}

		static IEnumerable<StandaloneSignatureHandle> EnumerateStandaloneSignatures(MetadataReader metadata)
		{
			int count = metadata.GetTableRowCount(TableIndex.StandAloneSig);
			for (int row = 1; row <= count; row++)
				yield return MetadataTokens.StandaloneSignatureHandle(row);
		}

		static void AddMethod(StubType stubType, IMethod method)
		{
			string key = "M:" + method.Name + "`" + method.TypeParameters.Count
				+ "(" + string.Join(",", method.Parameters.Select(p => p.Type.ReflectionName)) + ")"
				+ (method.IsStatic ? "S" : "");
			if (stubType.MemberKeys.Add(key))
				stubType.Methods.Add(method);
		}

		static void NoteValueTypeEvidence(IType type, Dictionary<string, StubType> stubs)
		{
			switch (type)
			{
				case UnknownType unknown:
					if (unknown.IsReferenceType == false
						&& stubs.TryGetValue(unknown.ReflectionName, out var stub))
					{
						stub.IsValueType = true;
					}
					break;
				case ParameterizedType pt:
					NoteValueTypeEvidence(pt.GenericType, stubs);
					foreach (var arg in pt.TypeArguments)
						NoteValueTypeEvidence(arg, stubs);
					break;
				case TypeWithElementType te:
					NoteValueTypeEvidence(te.ElementType, stubs);
					break;
			}
		}

		/// <summary>
		/// Writes a buildable stub project - one .cs with the declarations and a matching
		/// .csproj - into <paramref name="outputDirectory"/>/&lt;name&gt;.Stubs/.
		/// </summary>
		public void GenerateProject(string targetAssemblySimpleName, string outputDirectory)
		{
			string source = GenerateSource(targetAssemblySimpleName);
			string projectDir = System.IO.Path.Combine(outputDirectory, targetAssemblySimpleName + ".Stubs");
			System.IO.Directory.CreateDirectory(projectDir);
			System.IO.File.WriteAllText(System.IO.Path.Combine(projectDir, targetAssemblySimpleName + ".cs"), source);

			string moniker = consumers.Count > 0
				? TargetServices.DetectTargetFramework(consumers[0]).Moniker ?? "netstandard2.0"
				: "netstandard2.0";
			var csproj = new StringBuilder();
			csproj.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
			csproj.AppendLine("  <PropertyGroup>");
			csproj.AppendLine("    <TargetFramework>" + moniker + "</TargetFramework>");
			csproj.AppendLine("    <AssemblyName>" + targetAssemblySimpleName + "</AssemblyName>");
			csproj.AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
			csproj.AppendLine("    <EnableDefaultItems>false</EnableDefaultItems>");
			csproj.AppendLine("    <NoWarn>CS0067;CS0169</NoWarn>");
			csproj.AppendLine("  </PropertyGroup>");
			csproj.AppendLine("  <ItemGroup>");
			csproj.AppendLine("    <Compile Include=\"" + targetAssemblySimpleName + ".cs\" />");
			csproj.AppendLine("  </ItemGroup>");
			csproj.AppendLine("</Project>");
			System.IO.File.WriteAllText(System.IO.Path.Combine(projectDir, targetAssemblySimpleName + ".csproj"), csproj.ToString());
		}

		#region Emission

		string Emit(Dictionary<string, StubType> stubs)
		{
			var sb = new StringBuilder();
			sb.AppendLine("// Stub assembly generated by ICSharpCode.Decompiler.");
			sb.AppendLine("// Declarations reconstructed from consumer metadata; member bodies throw.");
			sb.AppendLine("#pragma warning disable CS0067 // events declared but never raised");
			sb.AppendLine();

			foreach (var namespaceGroup in stubs.Values
				.Where(s => !s.Name.IsNested)
				.GroupBy(s => s.Name.TopLevelTypeName.Namespace, StringComparer.Ordinal)
				.OrderBy(g => g.Key, StringComparer.Ordinal))
			{
				bool hasNamespace = !string.IsNullOrEmpty(namespaceGroup.Key);
				if (hasNamespace)
					sb.AppendLine("namespace " + namespaceGroup.Key + " {");
				foreach (var stub in namespaceGroup.OrderBy(s => s.Name.Name, StringComparer.Ordinal))
				{
					EmitType(sb, stub, stubs, hasNamespace ? 1 : 0);
				}
				if (hasNamespace)
					sb.AppendLine("}");
			}
			return sb.ToString();
		}

		void EmitType(StringBuilder sb, StubType stub, Dictionary<string, StubType> stubs, int indent)
		{
			string pad = new string('\t', indent);
			int arity = stub.Name.TypeParameterCount;
			string typeParams = arity == 0 ? "" : "<" + string.Join(", ", Enumerable.Range(0, arity).Select(i => "T" + i)) + ">";
			string simpleName = stub.Name.Name;

			if (stub.IsDelegate)
			{
				var invoke = stub.Methods.First(m => m.Name == "Invoke");
				sb.Append(pad).Append("public delegate ").Append(PrintType(invoke.ReturnType, stubs))
					.Append(' ').Append(simpleName).Append(typeParams).Append('(')
					.Append(PrintParameters(invoke, stubs)).AppendLine(");");
				return;
			}

			string keyword = stub.IsInterface ? "interface" : stub.IsValueType ? "struct" : "class";
			sb.Append(pad).Append("public ").Append(keyword).Append(' ').Append(simpleName).Append(typeParams);
			// Bases. A class-kind constraint from the evidence is the base type; interface-kind
			// constraints are implemented. Attribute-usage evidence adds System.Attribute ancestry,
			// unless a class-kind constraint already dictates the base - then that base is assumed to
			// carry the Attribute ancestry itself. A struct can only take the interface constraints.
			string? classBase = null;
			var interfaceParts = new List<string>();
			foreach (var required in stub.RequiredBases)
			{
				if (required.Kind == TypeKind.Interface)
					interfaceParts.Add(PrintType(required, stubs));
				else if (!stub.IsValueType)
					classBase ??= PrintType(required, stubs);
			}
			if (classBase == null && stub.IsAttribute)
				classBase = "global::System.Attribute";
			var baseParts = new List<string>();
			if (classBase != null)
				baseParts.Add(classBase);
			baseParts.AddRange(interfaceParts);
			if (baseParts.Count > 0)
				sb.Append(" : ").Append(string.Join(", ", baseParts));
			sb.AppendLine(" {");

			// Fold accessors into properties and events before printing plain methods.
			var methods = stub.Methods.Where(m => m.Name != ".cctor").ToList();
			var properties = new Dictionary<string, (IMethod? Getter, IMethod? Setter)>();
			var events = new Dictionary<string, IMethod>();
			var plain = new List<IMethod>();
			foreach (var m in methods)
			{
				if (m.Name.StartsWith("get_", StringComparison.Ordinal))
				{
					properties.TryGetValue(m.Name.Substring(4), out var e);
					properties[m.Name.Substring(4)] = (m, e.Setter);
				}
				else if (m.Name.StartsWith("set_", StringComparison.Ordinal))
				{
					properties.TryGetValue(m.Name.Substring(4), out var e);
					properties[m.Name.Substring(4)] = (e.Getter, m);
				}
				else if (m.Name.StartsWith("add_", StringComparison.Ordinal))
					events[m.Name.Substring(4)] = m;
				else if (m.Name.StartsWith("remove_", StringComparison.Ordinal))
				{
					if (!events.ContainsKey(m.Name.Substring(7)))
						events[m.Name.Substring(7)] = m;
				}
				else
					plain.Add(m);
			}

			string memberPad = pad + "\t";
			foreach (var field in stub.Fields.OrderBy(f => f.Name, StringComparer.Ordinal))
			{
				sb.Append(memberPad).Append(stub.IsInterface ? "" : "public ")
					.Append(field.IsStatic ? "static " : "")
					.Append(PrintType(field.Type, stubs)).Append(' ').Append(field.Name).AppendLine(";");
			}

			foreach (var (name, accessors) in properties.OrderBy(p => p.Key, StringComparer.Ordinal))
			{
				var sample = accessors.Getter ?? accessors.Setter!;
				IType propertyType = accessors.Getter?.ReturnType ?? accessors.Setter!.Parameters.Last().Type;
				var indexParams = accessors.Getter?.Parameters
					?? accessors.Setter!.Parameters.Take(accessors.Setter.Parameters.Count - 1).ToList().AsReadOnly();
				sb.Append(memberPad).Append(stub.IsInterface ? "" : "public ")
					.Append(sample.IsStatic ? "static " : VirtualModifier(stub));
				sb.Append(PrintType(propertyType, stubs)).Append(' ');
				if (indexParams.Count > 0)
					sb.Append("this[").Append(PrintParameters(accessors.Getter ?? accessors.Setter!, stubs, indexerStyle: accessors.Getter == null)).Append(']');
				else
					sb.Append(name);
				sb.Append(" { ");
				if (accessors.Getter != null)
					sb.Append(stub.IsInterface ? "get; " : "get => throw null; ");
				if (accessors.Setter != null)
					sb.Append(stub.IsInterface ? "set; " : "set { } ");
				sb.AppendLine("}");
			}

			foreach (var (name, adder) in events.OrderBy(e => e.Key, StringComparer.Ordinal))
			{
				sb.Append(memberPad).Append(stub.IsInterface ? "" : "public ")
					.Append(adder.IsStatic ? "static " : VirtualModifier(stub))
					.Append("event ").Append(PrintType(adder.Parameters[0].Type, stubs)).Append(' ').Append(name);
				sb.AppendLine(stub.IsInterface ? ";" : " { add { } remove { } }");
			}

			foreach (var m in plain.OrderBy(m => m.Name, StringComparer.Ordinal).ThenBy(m => m.Parameters.Count))
			{
				if (m.SymbolKind == SymbolKind.Constructor)
				{
					sb.Append(memberPad).Append("public ").Append(simpleName).Append('(')
						.Append(PrintParameters(m, stubs)).AppendLine(") { }");
					continue;
				}
				string methodTypeParams = m.TypeParameters.Count == 0
					? ""
					: "<" + string.Join(", ", m.TypeParameters.Select(PrintTypeParameterName)) + ">";
				sb.Append(memberPad);
				if (!stub.IsInterface)
					sb.Append("public ").Append(m.IsStatic ? "static " : VirtualModifier(stub));
				sb.Append(PrintType(m.ReturnType, stubs)).Append(' ').Append(m.Name).Append(methodTypeParams)
					.Append('(').Append(PrintParameters(m, stubs)).Append(')');
				sb.AppendLine(stub.IsInterface ? ";" : " => throw null;");
			}

			// A required interface brings members, and the type only satisfies the constraint by
			// implementing every one of them, inherited interfaces included. Explicit implementations
			// cannot collide with the referenced surface, so they are always safe to add.
			if (!stub.IsInterface)
			{
				var implementedInterfaceMembers = new HashSet<string>();
				foreach (var required in stub.RequiredBases)
				{
					if (required.Kind != TypeKind.Interface)
						continue;
					foreach (var iface in new[] { required }
						.Concat(required.GetAllBaseTypes().Where(t => t.Kind == TypeKind.Interface)))
					{
						if (iface.GetDefinition() == null)
							continue;
						string ifaceName = PrintType(iface, stubs);
						foreach (var property in iface.GetProperties(null, GetMemberOptions.IgnoreInheritedMembers))
						{
							if (!implementedInterfaceMembers.Add(ifaceName + "." + property.Name))
								continue;
							sb.Append(memberPad).Append(PrintType(property.ReturnType, stubs)).Append(' ')
								.Append(ifaceName).Append('.').Append(property.Name).Append(" { ");
							if (property.CanGet)
								sb.Append("get => throw null; ");
							if (property.CanSet)
								sb.Append("set { } ");
							sb.AppendLine("}");
						}
						foreach (var ev in iface.GetEvents(null, GetMemberOptions.IgnoreInheritedMembers))
						{
							if (!implementedInterfaceMembers.Add(ifaceName + "." + ev.Name))
								continue;
							sb.Append(memberPad).Append("event ").Append(PrintType(ev.ReturnType, stubs)).Append(' ')
								.Append(ifaceName).Append('.').Append(ev.Name).AppendLine(" { add { } remove { } }");
						}
						foreach (var method in iface.GetMethods(null, GetMemberOptions.IgnoreInheritedMembers))
						{
							if (method.IsStatic || method.IsAccessor)
								continue;
							string key = ifaceName + "." + method.Name + "(" + string.Join(",", method.Parameters.Select(pp => pp.Type.ReflectionName)) + ")";
							if (!implementedInterfaceMembers.Add(key))
								continue;
							string ifaceMethodTypeParams = method.TypeParameters.Count == 0
								? ""
								: "<" + string.Join(", ", method.TypeParameters.Select(PrintTypeParameterName)) + ">";
							sb.Append(memberPad).Append(PrintType(method.ReturnType, stubs)).Append(' ')
								.Append(ifaceName).Append('.').Append(method.Name).Append(ifaceMethodTypeParams)
								.Append('(').Append(PrintParameters(method, stubs)).AppendLine(") => throw null;");
						}
					}
				}
			}

			// A new() constraint demands a parameterless constructor. The implicit default only
			// exists when no constructor is declared at all, so add one where other ctors crowded
			// it out.
			if (stub.NeedsParameterlessCtor && !stub.IsInterface && !stub.IsValueType
				&& plain.Any(m => m.SymbolKind == SymbolKind.Constructor)
				&& !plain.Any(m => m.SymbolKind == SymbolKind.Constructor && m.Parameters.Count == 0))
			{
				sb.Append(memberPad).Append("public ").Append(simpleName).AppendLine("() { }");
			}

			// Nested stub types are printed inside their declaring type.
			foreach (var nested in stubs.Values
				.Where(s => s.Name.IsNested && s.Name.GetDeclaringType().ReflectionName == stub.Name.ReflectionName)
				.OrderBy(s => s.Name.Name, StringComparer.Ordinal))
			{
				EmitType(sb, nested, stubs, indent + 1);
			}

			sb.Append(pad).AppendLine("}");
		}

		static string VirtualModifier(StubType stub)
		{
			// Struct members cannot be virtual; class members are, so that a consumer's
			// overrides - which leave no member reference of their own - still compile.
			return stub.IsValueType ? "" : "virtual ";
		}

		string PrintParameters(IMethod method, Dictionary<string, StubType> stubs, bool indexerStyle = false)
		{
			var parameters = (IReadOnlyList<IParameter>)method.Parameters;
			if (indexerStyle)
				parameters = parameters.Take(parameters.Count - 1).ToList();
			return string.Join(", ", parameters.Select((p, i) => {
				var type = p.Type;
				string prefix = "";
				if (type is ByReferenceType brt)
				{
					prefix = "ref ";
					type = brt.ElementType;
				}
				return prefix + PrintType(type, stubs) + " p" + i;
			}));
		}

		static string PrintTypeParameterName(ITypeParameter tp)
		{
			// Fake members name their type parameters "!!0" and friends; give them legal names.
			return IsValidIdentifier(tp.Name) ? tp.Name : "T" + tp.Index;
		}

		static bool IsValidIdentifier(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;
			if (!char.IsLetter(name[0]) && name[0] != '_')
				return false;
			return name.All(c => char.IsLetterOrDigit(c) || c == '_');
		}

		string PrintType(IType type, Dictionary<string, StubType> stubs)
		{
			switch (type)
			{
				case ArrayType array:
					return PrintType(array.ElementType, stubs) + "[" + new string(',', array.Dimensions - 1) + "]";
				case PointerType pointer:
					return PrintType(pointer.ElementType, stubs) + "*";
				case ByReferenceType byRef:
					// Only legal in parameter position, which PrintParameters handles; a stray
					// occurrence (e.g. ref return) prints as ref.
					return "ref " + PrintType(byRef.ElementType, stubs);
				case ParameterizedType parameterized:
					return PrintGenericType(parameterized.GenericType, stubs)
						+ "<" + string.Join(", ", parameterized.TypeArguments.Select(a => PrintType(a, stubs))) + ">";
				case ITypeParameter tp:
					return PrintTypeParameterName(tp);
			}

			switch (type.Kind)
			{
				case TypeKind.Void:
					return "void";
			}
			var def = type.GetDefinition();
			if (def != null)
			{
				string? keyword = KnownTypeReference.GetCSharpNameByTypeCode(def.KnownTypeCode);
				if (keyword != null)
					return keyword;
			}
			return PrintGenericType(type, stubs);
		}

		string PrintGenericType(IType type, Dictionary<string, StubType> stubs)
		{
			string reflectionName = type.ReflectionName;
			int backtick = reflectionName.IndexOf('`');
			string bare = backtick < 0 ? reflectionName : reflectionName.Substring(0, backtick);
			return "global::" + bare.Replace('+', '.');
		}

		#endregion
	}
}

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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler
{
	/// <summary>
	/// Decides which generated source file a whole-project export writes each top-level type to
	/// when the module declares file-local types (C# 11 <c>file</c>).
	/// </summary>
	/// <remarks>
	/// A file-local type is visible only inside the source file that declares it, so the export
	/// has to keep it in one generated file with every type that uses it, or the references no
	/// longer compile. Metadata keeps no record of source files; what it keeps is the mangled
	/// type name <c>&lt;<i>File</i>&gt;F<i>hash</i>__<i>Name</i></c>, which identifies the declaring
	/// file, and the references themselves - a user is any type whose signatures, custom
	/// attributes (<c>typeof</c> arguments included) or method bodies mention the file-local type.
	/// Users and helpers are grouped into one file per declaring source file, named after that
	/// file, so the common layout - <c>Holder.cs</c> declaring <c>Holder</c> plus its <c>file</c>
	/// helpers - comes back as one <c>Holder.cs</c>. A user that mentions helpers from several
	/// source files (the parts of a partial type, merged into one declaration) pulls those files
	/// together, which keeps the result compilable at the cost of the original file split.
	/// </remarks>
	sealed class FileLocalTypePlacement
	{
		readonly MetadataModule module;
		readonly MetadataReader metadata;
		// Every type definition (nested ones included) that belongs to a file-local top-level type,
		// mapped to that top-level type.
		readonly Dictionary<TypeDefinitionHandle, TypeDefinitionHandle> fileLocalOwner = new();
		readonly UnionFind<TypeDefinitionHandle> groups = new();
		readonly Dictionary<TypeDefinitionHandle, (string Namespace, string SourceFile)> placement = new();

		/// <summary>
		/// Computes the placement for <paramref name="topLevelTypes"/>; returns null when the module
		/// declares no file-local type, in which case every type keeps its own file.
		/// </summary>
		public static FileLocalTypePlacement? Create(MetadataModule module, IEnumerable<TypeDefinitionHandle> topLevelTypes)
		{
			var placement = new FileLocalTypePlacement(module);
			var types = topLevelTypes.ToList();
			if (!placement.CollectFileLocalTypes(types))
				return null;
			placement.ConnectUsers(types);
			placement.AssignFiles(types);
			return placement;
		}

		FileLocalTypePlacement(MetadataModule module)
		{
			this.module = module;
			this.metadata = module.metadata;
		}

		/// <summary>
		/// The namespace and source-file name (without extension) of the generated file
		/// <paramref name="type"/> belongs to, or null when it keeps the default file of its own.
		/// </summary>
		public (string Namespace, string SourceFile)? GetPlacement(TypeDefinitionHandle type)
		{
			return placement.TryGetValue(type, out var result) ? result : null;
		}

		bool CollectFileLocalTypes(List<TypeDefinitionHandle> topLevelTypes)
		{
			foreach (var handle in topLevelTypes)
			{
				var td = metadata.GetTypeDefinition(handle);
				if (!FileLocalTypeName.TryParse(metadata.GetString(td.Name), out _, out _))
					continue;
				fileLocalOwner[handle] = handle;
				foreach (var nested in td.GetNestedTypes())
					RegisterNested(nested, handle);
			}
			return fileLocalOwner.Count > 0;
		}

		void RegisterNested(TypeDefinitionHandle handle, TypeDefinitionHandle owner)
		{
			fileLocalOwner[handle] = owner;
			foreach (var nested in metadata.GetTypeDefinition(handle).GetNestedTypes())
				RegisterNested(nested, owner);
		}

		void ConnectUsers(List<TypeDefinitionHandle> topLevelTypes)
		{
			foreach (var handle in topLevelTypes)
			{
				var referenced = new HashSet<TypeDefinitionHandle>();
				new ReferenceCollector(this, referenced).CollectFrom(handle);
				referenced.Remove(handle);
				foreach (var target in referenced)
					groups.Merge(handle, target);
			}
		}

		void AssignFiles(List<TypeDefinitionHandle> topLevelTypes)
		{
			// Each group takes the namespace and source-file name of its first file-local type
			// in metadata order, so the result does not depend on enumeration details.
			var fileOfGroup = new Dictionary<TypeDefinitionHandle, (string, string)>();
			foreach (var handle in topLevelTypes)
			{
				if (!fileLocalOwner.ContainsKey(handle))
					continue;
				var root = groups.Find(handle);
				if (fileOfGroup.ContainsKey(root))
					continue;
				var td = metadata.GetTypeDefinition(handle);
				FileLocalTypeName.TryParse(metadata.GetString(td.Name), out _, out string? fileHash);
				fileOfGroup[root] = (metadata.GetString(td.Namespace), SourceFileName(fileHash!));
			}
			foreach (var handle in topLevelTypes)
			{
				if (fileOfGroup.TryGetValue(groups.Find(handle), out var file))
					placement[handle] = file;
			}
		}

		/// <summary>
		/// The source-file part of a mangled file-local name: <c>&lt;Holder&gt;F1A2B</c> came from
		/// <c>Holder.cs</c>. The compiler strips the extension and keeps only identifier-safe
		/// characters, so the result is usable as a file name as it stands.
		/// </summary>
		static string SourceFileName(string fileHash)
		{
			int end = fileHash.IndexOf('>');
			string name = end > 1 ? fileHash.Substring(1, end - 1) : "";
			return name.Length > 0 ? name : "FileLocalTypes";
		}

		/// <summary>
		/// Finds the file-local top-level types one top-level type (with its nested types) refers
		/// to anywhere metadata can mention a type.
		/// </summary>
		sealed class ReferenceCollector : TypeVisitor
		{
			readonly FileLocalTypePlacement owner;
			readonly HashSet<TypeDefinitionHandle> referenced;
			readonly HashSet<EntityHandle> visitedTokens = new();

			public ReferenceCollector(FileLocalTypePlacement owner, HashSet<TypeDefinitionHandle> referenced)
			{
				this.owner = owner;
				this.referenced = referenced;
			}

			public void CollectFrom(TypeDefinitionHandle handle)
			{
				var type = owner.module.GetDefinition(handle);
				VisitEntityAttributes(type);
				foreach (var baseType in type.DirectBaseTypes)
					baseType.AcceptVisitor(this);
				foreach (var tp in type.TypeParameters)
					VisitTypeParameterConstraints(tp);
				foreach (var field in type.Fields)
				{
					VisitEntityAttributes(field);
					field.Type.AcceptVisitor(this);
				}
				foreach (var property in type.Properties)
				{
					VisitEntityAttributes(property);
					property.ReturnType.AcceptVisitor(this);
					foreach (var p in property.Parameters)
						VisitParameter(p);
				}
				foreach (var ev in type.Events)
				{
					VisitEntityAttributes(ev);
					ev.ReturnType.AcceptVisitor(this);
				}
				foreach (var method in type.Methods)
				{
					VisitEntityAttributes(method);
					foreach (var attr in method.GetReturnTypeAttributes())
						VisitAttribute(attr);
					method.ReturnType.AcceptVisitor(this);
					foreach (var p in method.Parameters)
						VisitParameter(p);
					foreach (var tp in method.TypeParameters)
						VisitTypeParameterConstraints(tp);
					if (method.MetadataToken.Kind == HandleKind.MethodDefinition)
						VisitMethodBody((MethodDefinitionHandle)method.MetadataToken);
				}
				foreach (var nested in type.NestedTypes)
				{
					if (nested.MetadataToken.Kind == HandleKind.TypeDefinition)
						CollectFrom((TypeDefinitionHandle)nested.MetadataToken);
				}
			}

			void VisitParameter(IParameter p)
			{
				foreach (var attr in p.GetAttributes())
					VisitAttribute(attr);
				p.Type.AcceptVisitor(this);
			}

			void VisitTypeParameterConstraints(ITypeParameter tp)
			{
				foreach (var attr in tp.GetAttributes())
					VisitAttribute(attr);
				foreach (var constraint in tp.TypeConstraints)
					constraint.Type.AcceptVisitor(this);
			}

			void VisitEntityAttributes(IEntity entity)
			{
				foreach (var attr in entity.GetAttributes())
					VisitAttribute(attr);
			}

			void VisitAttribute(IAttribute attribute)
			{
				attribute.AttributeType.AcceptVisitor(this);
				foreach (var arg in attribute.FixedArguments)
					VisitAttributeArgument(arg.Type, arg.Value);
				foreach (var arg in attribute.NamedArguments)
					VisitAttributeArgument(arg.Type, arg.Value);
			}

			void VisitAttributeArgument(IType type, object? value)
			{
				type.AcceptVisitor(this);
				switch (value)
				{
					case IType t:
						// typeof(X) arguments - the debugger-proxy and type-converter attributes.
						t.AcceptVisitor(this);
						break;
					case ImmutableArray<CustomAttributeTypedArgument<IType>> array:
						foreach (var element in array)
							VisitAttributeArgument(element.Type, element.Value);
						break;
				}
			}

			void VisitMethodBody(MethodDefinitionHandle handle)
			{
				var md = owner.metadata.GetMethodDefinition(handle);
				if (md.RelativeVirtualAddress == 0)
					return;
				MethodBodyBlock body;
				try
				{
					body = owner.module.MetadataFile.GetMethodBody(md.RelativeVirtualAddress);
				}
				catch (BadImageFormatException)
				{
					return;
				}
				if (!body.LocalSignature.IsNil)
					VisitToken(body.LocalSignature);
				foreach (var region in body.ExceptionRegions)
				{
					if (!region.CatchType.IsNil)
						VisitToken(region.CatchType);
				}
				var blob = body.GetILReader();
				while (blob.RemainingBytes > 0)
				{
					ILOpCode opCode;
					try
					{
						opCode = blob.DecodeOpCode();
					}
					catch (BadImageFormatException)
					{
						return;
					}
					switch (opCode.GetOperandType())
					{
						case OperandType.Field:
						case OperandType.Method:
						case OperandType.Sig:
						case OperandType.Tok:
						case OperandType.Type:
							if (blob.RemainingBytes < 4)
								return;
							VisitToken(MetadataTokens.EntityHandle(blob.ReadInt32()));
							break;
						default:
							try
							{
								blob.SkipOperand(opCode);
							}
							catch (BadImageFormatException)
							{
								return;
							}
							break;
					}
				}
			}

			void VisitToken(EntityHandle token)
			{
				if (token.IsNil || !visitedTokens.Add(token))
					return;
				try
				{
					switch (token.Kind)
					{
						case HandleKind.TypeDefinition:
							Record((TypeDefinitionHandle)token);
							break;
						case HandleKind.TypeReference:
							// Another module's type: it cannot be file-local here.
							break;
						case HandleKind.TypeSpecification:
							owner.module.ResolveType(token, default).AcceptVisitor(this);
							break;
						case HandleKind.FieldDefinition:
							Record(owner.metadata.GetFieldDefinition((FieldDefinitionHandle)token).GetDeclaringType());
							break;
						case HandleKind.MethodDefinition:
							Record(owner.metadata.GetMethodDefinition((MethodDefinitionHandle)token).GetDeclaringType());
							break;
						case HandleKind.MemberReference:
							VisitToken(owner.metadata.GetMemberReference((MemberReferenceHandle)token).Parent);
							break;
						case HandleKind.MethodSpecification:
							var spec = owner.metadata.GetMethodSpecification((MethodSpecificationHandle)token);
							VisitToken(spec.Method);
							if (owner.module.ResolveEntity(token) is IMethod instantiated)
							{
								foreach (var arg in instantiated.TypeArguments)
									arg.AcceptVisitor(this);
							}
							break;
						case HandleKind.StandaloneSignature:
							var sig = owner.metadata.GetStandaloneSignature((StandaloneSignatureHandle)token);
							if (sig.GetKind() == StandaloneSignatureKind.LocalVariables)
							{
								foreach (var local in owner.module.DecodeLocalSignature((StandaloneSignatureHandle)token, default))
									local.AcceptVisitor(this);
							}
							break;
					}
				}
				catch (BadImageFormatException)
				{
					// A malformed token is the disassembler's problem to report, not a reason to
					// misplace the well-formed types around it.
				}
			}

			void Record(TypeDefinitionHandle handle)
			{
				if (owner.fileLocalOwner.TryGetValue(handle, out var topLevel))
					referenced.Add(topLevel);
			}

			public override IType VisitTypeDefinition(ITypeDefinition type)
			{
				if (type.MetadataToken.Kind == HandleKind.TypeDefinition && type.ParentModule == owner.module)
					Record((TypeDefinitionHandle)type.MetadataToken);
				return base.VisitTypeDefinition(type);
			}
		}
	}
}

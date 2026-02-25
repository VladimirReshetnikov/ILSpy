// Copyright (c) 2022 Siegfried Pammer
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
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Tracks which members of a top-level type were already emitted in another source fragment
	/// when exporting a project. This is used to merge partial type contributions without duplicating
	/// metadata members in generated code files.
	/// </summary>
	public class PartialTypeInfo
	{
		readonly HashSet<EntityHandle> declaredMembers = new();

		/// <summary>
		/// Creates tracking information for the specified declaring type.
		/// </summary>
		/// <param name="declaringTypeDefinition">The type definition whose members are tracked.</param>
		public PartialTypeInfo(ITypeDefinition declaringTypeDefinition)
		{
			DeclaringTypeDefinitionHandle = (TypeDefinitionHandle)declaringTypeDefinition.MetadataToken;
		}

		/// <summary>
		/// Creates tracking information for the specified declaring type handle.
		/// </summary>
		/// <param name="declaringTypeDefinitionHandle">The metadata handle of the declaring type.</param>
		public PartialTypeInfo(TypeDefinitionHandle declaringTypeDefinitionHandle)
		{
			DeclaringTypeDefinitionHandle = declaringTypeDefinitionHandle;
		}

		/// <summary>
		/// Gets the metadata handle of the type whose members are tracked.
		/// </summary>
		public TypeDefinitionHandle DeclaringTypeDefinitionHandle { get; }

		/// <summary>
		/// Marks a member as already declared in one of the emitted partial type fragments.
		/// </summary>
		/// <param name="member">The member to register.</param>
		public void AddDeclaredMember(IMember member)
		{
			declaredMembers.Add(member.MetadataToken);
		}

		/// <summary>
		/// Marks a member handle as already declared in one of the emitted partial type fragments.
		/// </summary>
		/// <param name="handle">The metadata handle to register.</param>
		public void AddDeclaredMember(EntityHandle handle)
		{
			declaredMembers.Add(handle);
		}

		/// <summary>
		/// Checks whether the specified member was already registered as declared.
		/// </summary>
		/// <param name="member">The member to look up.</param>
		/// <returns><see langword="true"/> if the member is already registered; otherwise <see langword="false"/>.</returns>
		public bool IsDeclaredMember(IMember member)
		{
			return declaredMembers.Contains(member.MetadataToken);
		}

		/// <summary>
		/// Checks whether the specified metadata handle was already registered as declared.
		/// </summary>
		/// <param name="handle">The metadata handle to look up.</param>
		/// <returns><see langword="true"/> if the handle is already registered; otherwise <see langword="false"/>.</returns>
		public bool IsDeclaredMember(EntityHandle handle)
		{
			return declaredMembers.Contains(handle);
		}

		/// <summary>
		/// Merges declarations from another <see cref="PartialTypeInfo"/> instance for the same declaring type.
		/// </summary>
		/// <param name="info">The additional declared-member set to merge.</param>
		public void AddDeclaredMembers(PartialTypeInfo info)
		{
			foreach (var member in info.declaredMembers)
			{
				declaredMembers.Add(member);
			}
		}

		/// <summary>
		/// Gets a debugger-oriented hexadecimal token list of all tracked members.
		/// </summary>
		public string DebugOutput => string.Join(", ", declaredMembers.Select(m => MetadataTokens.GetToken(m).ToString("X")));
	}
}

// Copyright (c) 2022 Tom Englert
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

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Sorts disassembled metadata entities by deterministic, name-based keys.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Metadata tables preserve declaration order, which can vary between builds and emitters. This processor normalizes
	/// that order before <see cref="ReflectionDisassembler"/> writes output so textual IL is easier to diff and compare.
	/// </para>
	/// <para>
	/// Each entity kind uses the minimal key needed to preserve useful grouping semantics. For methods, the key includes
	/// method name, generic arity, and parameter signature (but excludes return type) to mirror IL method identity rules.
	/// </para>
	/// </remarks>
	public class SortByNameProcessor : IEntityProcessor
	{
		/// <summary>
		/// Returns interface implementations ordered by implemented interface name.
		/// </summary>
		/// <param name="module">Module that resolves interface handles to type names.</param>
		/// <param name="items">Interface implementation entries selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(InterfaceImplementationHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<InterfaceImplementationHandle> Process(MetadataFile module,
			IReadOnlyCollection<InterfaceImplementationHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns type definitions ordered by fully qualified IL type name.
		/// </summary>
		/// <param name="module">Module that resolves type-definition handles.</param>
		/// <param name="items">Type definitions selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(TypeDefinitionHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<TypeDefinitionHandle> Process(MetadataFile module,
			IReadOnlyCollection<TypeDefinitionHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns method definitions ordered by IL method identity key.
		/// </summary>
		/// <param name="module">Module that resolves method names and signatures.</param>
		/// <param name="items">Method definitions selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(MethodDefinitionHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<MethodDefinitionHandle> Process(MetadataFile module,
			IReadOnlyCollection<MethodDefinitionHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns property definitions ordered by simple metadata name.
		/// </summary>
		/// <param name="module">Module that resolves property-definition handles.</param>
		/// <param name="items">Property definitions selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(PropertyDefinitionHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<PropertyDefinitionHandle> Process(MetadataFile module,
			IReadOnlyCollection<PropertyDefinitionHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns event definitions ordered by simple metadata name.
		/// </summary>
		/// <param name="module">Module that resolves event-definition handles.</param>
		/// <param name="items">Event definitions selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(EventDefinitionHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<EventDefinitionHandle> Process(MetadataFile module,
			IReadOnlyCollection<EventDefinitionHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns field definitions ordered by simple metadata name.
		/// </summary>
		/// <param name="module">Module that resolves field-definition handles.</param>
		/// <param name="items">Field definitions selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(FieldDefinitionHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<FieldDefinitionHandle> Process(MetadataFile module,
			IReadOnlyCollection<FieldDefinitionHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Returns custom attributes ordered by constructor declaring type.
		/// </summary>
		/// <param name="module">Module that resolves custom-attribute constructor handles.</param>
		/// <param name="items">Custom attribute handles selected for output.</param>
		/// <returns>A new collection sorted by <see cref="GetSortKey(CustomAttributeHandle, MetadataFile)"/>.</returns>
		public IReadOnlyCollection<CustomAttributeHandle> Process(MetadataFile module,
			IReadOnlyCollection<CustomAttributeHandle> items)
		{
			return items.OrderBy(item => GetSortKey(item, module)).ToArray();
		}

		/// <summary>
		/// Computes the ordering key for a type definition.
		/// </summary>
		/// <param name="handle">Type definition whose name should be used as the sort key.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The fully qualified IL type name.</returns>
		private static string GetSortKey(TypeDefinitionHandle handle, MetadataFile module) =>
			handle.GetFullTypeName(module.Metadata).ToILNameString();

		/// <summary>
		/// Computes the ordering key for a method definition using IL name + arity + parameter list.
		/// </summary>
		/// <param name="handle">Method definition whose name/signature should be encoded.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>A key that groups overloads by IL member identity.</returns>
		/// <remarks>
		/// Return type is intentionally excluded because IL method overloading identity does not include it,
		/// and including it would split methods that are otherwise equivalent from an IL member reference perspective.
		/// </remarks>
		private static string GetSortKey(MethodDefinitionHandle handle, MetadataFile module)
		{
			PlainTextOutput output = new PlainTextOutput();
			MethodDefinition definition = module.Metadata.GetMethodDefinition(handle);

			// Start with the methods name, skip return type
			output.Write(module.Metadata.GetString(definition.Name));

			DisassemblerSignatureTypeProvider signatureProvider = new DisassemblerSignatureTypeProvider(module, output);
			MethodSignature<Action<ILNameSyntax>> signature =
				definition.DecodeSignature(signatureProvider, new MetadataGenericContext(handle, module));

			if (signature.GenericParameterCount > 0)
			{
				output.Write($"`{signature.GenericParameterCount}");
			}

			InstructionOutputExtensions.WriteParameterList(output, signature);

			return output.ToString();
		}

		/// <summary>
		/// Computes the ordering key for an interface implementation.
		/// </summary>
		/// <param name="handle">Interface implementation handle to inspect.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The full IL type name of the implemented interface.</returns>
		private static string GetSortKey(InterfaceImplementationHandle handle, MetadataFile module) =>
			module.Metadata.GetInterfaceImplementation(handle)
				.Interface
				.GetFullTypeName(module.Metadata)
				.ToILNameString();

		/// <summary>
		/// Computes the ordering key for a field definition.
		/// </summary>
		/// <param name="handle">Field definition handle to inspect.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The simple field name.</returns>
		private static string GetSortKey(FieldDefinitionHandle handle, MetadataFile module) =>
			module.Metadata.GetString(module.Metadata.GetFieldDefinition(handle).Name);

		/// <summary>
		/// Computes the ordering key for a property definition.
		/// </summary>
		/// <param name="handle">Property definition handle to inspect.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The simple property name.</returns>
		private static string GetSortKey(PropertyDefinitionHandle handle, MetadataFile module) =>
			module.Metadata.GetString(module.Metadata.GetPropertyDefinition(handle).Name);

		/// <summary>
		/// Computes the ordering key for an event definition.
		/// </summary>
		/// <param name="handle">Event definition handle to inspect.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The simple event name.</returns>
		private static string GetSortKey(EventDefinitionHandle handle, MetadataFile module) =>
			module.Metadata.GetString(module.Metadata.GetEventDefinition(handle).Name);

		/// <summary>
		/// Computes the ordering key for a custom attribute from its constructor's declaring type.
		/// </summary>
		/// <param name="handle">Custom attribute handle to inspect.</param>
		/// <param name="module">Module containing metadata for <paramref name="handle"/>.</param>
		/// <returns>The full IL type name of the attribute constructor's declaring type.</returns>
		private static string GetSortKey(CustomAttributeHandle handle, MetadataFile module) =>
			module.Metadata.GetCustomAttribute(handle)
				.Constructor
				.GetDeclaringType(module.Metadata)
				.GetFullTypeName(module.Metadata)
				.ToILNameString();
	}
}

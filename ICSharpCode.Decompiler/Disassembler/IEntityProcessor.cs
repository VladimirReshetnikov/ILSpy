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

using System.Collections.Generic;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Provides a hook for post-processing metadata handle collections before they are emitted by
	/// <see cref="ReflectionDisassembler"/>.
	/// </summary>
	public interface IEntityProcessor
	{
		/// <summary>
		/// Reorders or filters interface implementation entries associated with the current type.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The interface implementation handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<InterfaceImplementationHandle> Process(MetadataFile module, IReadOnlyCollection<InterfaceImplementationHandle> items);

		/// <summary>
		/// Reorders or filters nested type definitions before they are written.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The type definition handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<TypeDefinitionHandle> Process(MetadataFile module, IReadOnlyCollection<TypeDefinitionHandle> items);

		/// <summary>
		/// Reorders or filters method definitions before method bodies/signatures are emitted.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The method definition handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<MethodDefinitionHandle> Process(MetadataFile module, IReadOnlyCollection<MethodDefinitionHandle> items);

		/// <summary>
		/// Reorders or filters property definitions before accessors and metadata are written.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The property definition handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<PropertyDefinitionHandle> Process(MetadataFile module, IReadOnlyCollection<PropertyDefinitionHandle> items);

		/// <summary>
		/// Reorders or filters event definitions before associated methods are emitted.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The event definition handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<EventDefinitionHandle> Process(MetadataFile module, IReadOnlyCollection<EventDefinitionHandle> items);

		/// <summary>
		/// Reorders or filters field definitions before they are emitted.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The field definition handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<FieldDefinitionHandle> Process(MetadataFile module, IReadOnlyCollection<FieldDefinitionHandle> items);

		/// <summary>
		/// Reorders or filters custom attributes before they are emitted on a declaration.
		/// </summary>
		/// <param name="module">Metadata source that can be used to inspect each handle.</param>
		/// <param name="items">The custom attribute handles selected for output.</param>
		/// <returns>The handles to emit, in the order they should appear in disassembled IL.</returns>
		IReadOnlyCollection<CustomAttributeHandle> Process(MetadataFile module, IReadOnlyCollection<CustomAttributeHandle> items);
	}
}

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

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace ICSharpCode.Decompiler.DebugInfo
{
	/// <summary>
	/// Represents a local variable entry from debug metadata.
	/// </summary>
	public struct Variable
	{
		/// <summary>
		/// Initializes a new local-variable descriptor.
		/// </summary>
		/// <param name="index">Zero-based local slot index in the method body.</param>
		/// <param name="name">Display name stored in debug symbols.</param>
		public Variable(int index, string name)
		{
			Index = index;
			Name = name;
		}

		/// <summary>
		/// Gets the zero-based local slot index.
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Gets the local variable name from debug symbols.
		/// </summary>
		public string Name { get; }
	}

	/// <summary>
	/// Additional type-shaping metadata for locals encoded in Portable PDB custom debug information.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The decompiler uses this metadata to reconstruct source-level types that are not directly represented
	/// in ECMA-335 signatures, such as tuple element names and <c>dynamic</c> positions.
	/// </para>
	/// <para>
	/// Both fields are optional. Providers populate only the payloads they can decode from the symbol format,
	/// and consumers must treat <see langword="null"/> as "information not available".
	/// </para>
	/// </remarks>
	public struct PdbExtraTypeInfo
	{
		/// <summary>
		/// Tuple element names in the flattened order used by compiler metadata.
		/// </summary>
		public string[] TupleElementNames;

		/// <summary>
		/// Bit-expanded dynamic flags that mark which positions in the type graph should be treated as <c>dynamic</c>.
		/// </summary>
		public bool[] DynamicFlags;
	}

	/// <summary>
	/// Provides debug-symbol data used by the decompiler for names, sequence points, and language-specific type metadata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations are expected to be resilient to malformed or partially missing debug metadata.
	/// Callers throughout the decompiler pipeline treat failures as absence of symbol data and continue with
	/// generated names or synthesized sequence-point information.
	/// </para>
	/// <para>
	/// Methods on this interface generally return empty collections or <see langword="false"/> when no data is available,
	/// rather than throwing, so that decompilation remains best-effort even when PDB loading fails.
	/// </para>
	/// </remarks>
	public interface IDebugInfoProvider
	{
		/// <summary>
		/// Gets a human-readable status string describing where debug information was loaded from.
		/// </summary>
		/// <value>
		/// A UI-ready description (for example, embedded-symbol status or external PDB file path).
		/// </value>
		string Description { get; }

		/// <summary>
		/// Gets sequence points for a method.
		/// </summary>
		/// <param name="method">Metadata handle for the method whose sequence points are requested.</param>
		/// <returns>
		/// A list of sequence points ordered as stored by the underlying symbol provider.
		/// Returns an empty list when no sequence points are available.
		/// </returns>
		IList<SequencePoint> GetSequencePoints(MethodDefinitionHandle method);

		/// <summary>
		/// Gets local variable records for a method.
		/// </summary>
		/// <param name="method">Metadata handle for the method whose locals are requested.</param>
		/// <returns>
		/// A list of local variable descriptors from debug symbols.
		/// Returns an empty list when symbols do not provide local names.
		/// </returns>
		IList<Variable> GetVariables(MethodDefinitionHandle method);

		/// <summary>
		/// Tries to resolve the debug-symbol name for a local variable slot.
		/// </summary>
		/// <param name="method">Metadata handle for the containing method.</param>
		/// <param name="index">Zero-based local slot index.</param>
		/// <param name="name">When this method returns <see langword="true"/>, receives the variable name.</param>
		/// <returns><see langword="true"/> when a matching local with a name exists; otherwise, <see langword="false"/>.</returns>
		bool TryGetName(MethodDefinitionHandle method, int index, out string name);

		/// <summary>
		/// Tries to resolve Portable PDB type-shaping metadata for a local variable.
		/// </summary>
		/// <param name="method">Metadata handle for the containing method.</param>
		/// <param name="index">Zero-based local slot index.</param>
		/// <param name="extraTypeInfo">When this method returns <see langword="true"/>, receives tuple and/or dynamic metadata.</param>
		/// <returns><see langword="true"/> if any extra type info was found for the local; otherwise, <see langword="false"/>.</returns>
		bool TryGetExtraTypeInfo(MethodDefinitionHandle method, int index, out PdbExtraTypeInfo extraTypeInfo);

		/// <summary>
		/// Gets the file name that should be used as the source identity for this debug provider.
		/// </summary>
		/// <value>
		/// The backing symbol file path when symbols come from an external file, or the module file path
		/// when symbols are embedded.
		/// </value>
		string SourceFileName { get; }
	}
}

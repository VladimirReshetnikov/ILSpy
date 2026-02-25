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
	public interface IDebugInfoProvider
	{
		/// <summary>
		/// Gets a human-readable status string describing where debug information was loaded from.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets sequence points for a method.
		/// </summary>
		/// <param name="method">Metadata handle for the method whose sequence points are requested.</param>
		/// <returns>A list of sequence points. Returns an empty list when no sequence points are available.</returns>
		IList<SequencePoint> GetSequencePoints(MethodDefinitionHandle method);

		/// <summary>
		/// Gets local variable records for a method.
		/// </summary>
		/// <param name="method">Metadata handle for the method whose locals are requested.</param>
		/// <returns>A list of local variable descriptors from debug symbols.</returns>
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
		string SourceFileName { get; }
	}
}

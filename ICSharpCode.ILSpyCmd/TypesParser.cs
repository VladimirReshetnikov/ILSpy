using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyCmd
{
	/// <summary>
	/// Parses command-line <c>-l/--list</c> entity selectors into a normalized set of <see cref="TypeKind"/> values.
	/// </summary>
	public static class TypesParser
	{
		/// <summary>
		/// Converts the user-provided type selector tokens into unique type kinds.
		/// </summary>
		/// <param name="values">
		/// Raw selector values from the command line. Supported forms are explicit names
		/// (<c>class</c>, <c>struct</c>, <c>interface</c>, <c>delegate</c>, <c>enum</c>) and compact letter forms
		/// (<c>c</c>, <c>s</c>, <c>i</c>, <c>d</c>, <c>e</c>).
		/// </param>
		/// <returns>
		/// A set of distinct <see cref="TypeKind"/> values recognized from <paramref name="values"/>.
		/// Unknown tokens are ignored.
		/// </returns>
		public static HashSet<TypeKind> ParseSelection(string[] values)
		{
			var possibleValues = new Dictionary<string, TypeKind>(StringComparer.OrdinalIgnoreCase) { ["class"] = TypeKind.Class, ["struct"] = TypeKind.Struct, ["interface"] = TypeKind.Interface, ["enum"] = TypeKind.Enum, ["delegate"] = TypeKind.Delegate };
			HashSet<TypeKind> kinds = new HashSet<TypeKind>();
			if (values.Length == 1 && !possibleValues.Keys.Any(v => values[0].StartsWith(v, StringComparison.OrdinalIgnoreCase)))
			{
				foreach (char ch in values[0])
				{
					switch (ch)
					{
						case 'c':
							kinds.Add(TypeKind.Class);
							break;
						case 'i':
							kinds.Add(TypeKind.Interface);
							break;
						case 's':
							kinds.Add(TypeKind.Struct);
							break;
						case 'd':
							kinds.Add(TypeKind.Delegate);
							break;
						case 'e':
							kinds.Add(TypeKind.Enum);
							break;
					}
				}
			}
			else
			{
				foreach (var value in values)
				{
					string v = value;
					while (v.Length > 0 && !possibleValues.ContainsKey(v))
						v = v.Remove(v.Length - 1);
					if (possibleValues.TryGetValue(v, out var kind))
						kinds.Add(kind);
				}
			}
			return kinds;
		}
	}
}

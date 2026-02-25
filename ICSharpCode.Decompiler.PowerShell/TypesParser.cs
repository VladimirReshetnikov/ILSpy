using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.PowerShell
{
	/// <summary>
	/// Parses user-facing type selection tokens used by the PowerShell cmdlets into
	/// <see cref="TypeKind"/> values.
	/// </summary>
	public static class TypesParser
	{
		/// <summary>
		/// Converts explicit names (<c>class</c>, <c>interface</c>, ...) or compact single-letter
		/// combinations (for example <c>cis</c>) into a set of type kinds.
		/// </summary>
		/// <param name="values">The user-provided type selection tokens.</param>
		/// <returns>A set of requested <see cref="TypeKind"/> values.</returns>
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

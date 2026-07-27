// Copyright (c) 2018 Daniel Grunwald
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
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Metadata
{
	/// <summary>
	/// Lookup structure that, for an accessor, can find the associated property/event.
	/// </summary>
	class MethodSemanticsLookup
	{
		const MethodSemanticsAttributes csharpAccessors =
			MethodSemanticsAttributes.Getter | MethodSemanticsAttributes.Setter
			| MethodSemanticsAttributes.Adder | MethodSemanticsAttributes.Remover;

		readonly struct Entry : IComparable<Entry>
		{
			/// <summary>
			/// Gets the semantic role that the accessor method fulfills.
			/// </summary>
			public readonly MethodSemanticsAttributes Semantics;
			/// <summary>
			/// Gets the method definition row number used for lookup ordering.
			/// </summary>
			public readonly int MethodRowNumber;
			/// <summary>
			/// Gets the accessor method handle reconstructed from <see cref="MethodRowNumber"/>.
			/// </summary>
			public MethodDefinitionHandle Method => MetadataTokens.MethodDefinitionHandle(MethodRowNumber);
			/// <summary>
			/// Gets the associated event or property handle.
			/// </summary>
			public readonly EntityHandle Association;

			/// <summary>
			/// Initializes a lookup entry for one accessor method.
			/// </summary>
			/// <param name="semantics">Semantic role of the accessor method.</param>
			/// <param name="method">Method definition handle for the accessor.</param>
			/// <param name="association">Associated event or property handle.</param>
			public Entry(MethodSemanticsAttributes semantics, MethodDefinitionHandle method, EntityHandle association)
			{
				Semantics = semantics;
				MethodRowNumber = MetadataTokens.GetRowNumber(method);
				Association = association;
			}

			/// <summary>
			/// Compares entries by method row number so they can be binary-searched by accessor method.
			/// </summary>
			/// <param name="other">Entry to compare against.</param>
			/// <returns>A value indicating relative order by method row number.</returns>
			public int CompareTo(Entry other)
			{
				return MethodRowNumber.CompareTo(other.MethodRowNumber);
			}
		}

		// entries, sorted by MethodRowNumber
		readonly List<Entry> entries;

		/// <summary>
		/// Builds a compact lookup from accessor methods to their associated property/event semantics.
		/// </summary>
		/// <param name="metadata">Metadata reader used to enumerate property and event accessor records.</param>
		/// <param name="filter">Semantics flags that should be indexed.</param>
		/// <exception cref="NotSupportedException">
		/// <paramref name="filter"/> includes <see cref="MethodSemanticsAttributes.Other"/>, which
		/// System.Reflection.Metadata does not expose.
		/// </exception>
		public MethodSemanticsLookup(MetadataReader metadata, MethodSemanticsAttributes filter = csharpAccessors)
		{
			if ((filter & MethodSemanticsAttributes.Other) != 0)
			{
				throw new NotSupportedException("SRM doesn't provide access to 'other' accessors");
			}
			entries = new List<Entry>(metadata.GetTableRowCount(TableIndex.MethodSemantics));
			foreach (var propHandle in metadata.PropertyDefinitions)
			{
				var prop = metadata.GetPropertyDefinition(propHandle);
				var accessors = prop.GetAccessors();
				AddEntry(MethodSemanticsAttributes.Getter, accessors.Getter, propHandle);
				AddEntry(MethodSemanticsAttributes.Setter, accessors.Setter, propHandle);
			}
			foreach (var eventHandle in metadata.EventDefinitions)
			{
				var ev = metadata.GetEventDefinition(eventHandle);
				var accessors = ev.GetAccessors();
				AddEntry(MethodSemanticsAttributes.Adder, accessors.Adder, eventHandle);
				AddEntry(MethodSemanticsAttributes.Remover, accessors.Remover, eventHandle);
				AddEntry(MethodSemanticsAttributes.Raiser, accessors.Raiser, eventHandle);
			}
			entries.Sort();

			void AddEntry(MethodSemanticsAttributes semantics, MethodDefinitionHandle method, EntityHandle association)
			{
				if ((semantics & filter) == 0 || method.IsNil)
					return;
				entries.Add(new Entry(semantics, method, association));
			}
		}

		/// <summary>
		/// Gets the associated property/event handle and accessor semantics for a method.
		/// </summary>
		/// <param name="method">Method definition handle to look up.</param>
		/// <returns>
		/// A tuple of association handle and semantics flags; returns <c>(default, 0)</c> when no entry exists.
		/// </returns>
		public (EntityHandle, MethodSemanticsAttributes) GetSemantics(MethodDefinitionHandle method)
		{
			int pos = entries.BinarySearch(new Entry(0, method, default(EntityHandle)));
			if (pos >= 0)
			{
				return (entries[pos].Association, entries[pos].Semantics);
			}
			else
			{
				return (default(EntityHandle), 0);
			}
		}
	}
}

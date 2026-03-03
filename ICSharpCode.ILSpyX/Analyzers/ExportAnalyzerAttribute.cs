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
using System.Composition;
using System.Linq;
using System.Reflection;

namespace ICSharpCode.ILSpyX.Analyzers
{
	/// <summary>
	/// Marks a class as an analyzer export and supplies metadata used by analyzer menus.
	/// </summary>
	/// <remarks>
	/// The attribute derives from <see cref="ExportAttribute"/> so ILSpy's composition container can discover
	/// analyzers without additional registration code.
	/// </remarks>
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ExportAnalyzerAttribute : ExportAttribute, IAnalyzerMetadata
	{
		/// <summary>
		/// Initializes an analyzer export with the contract name expected by ILSpy's analyzer host.
		/// </summary>
		public ExportAnalyzerAttribute() : base("Analyzer", typeof(IAnalyzer))
		{ }

		/// <summary>
		/// Gets the UI header shown for the analyzer root node.
		/// </summary>
		public required string Header { get; init; }

		/// <summary>
		/// Gets or sets the relative ordering used when analyzers are listed in menus.
		/// Lower values appear first.
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// Enumerates analyzer types from the current assembly that are decorated with <see cref="ExportAnalyzerAttribute"/>.
		/// </summary>
		/// <returns>
		/// A sequence of tuples containing attribute metadata and the analyzer implementation type.
		/// </returns>
		public static IEnumerable<(ExportAnalyzerAttribute AttributeData, Type AnalyzerType)> GetAnnotatedAnalyzers()
		{
			foreach (var type in typeof(ExportAnalyzerAttribute).Assembly.GetTypes())
			{
				if (type.GetCustomAttribute(typeof(ExportAnalyzerAttribute), false) is ExportAnalyzerAttribute exportAnalyzerAttribute)
				{
					yield return (exportAnalyzerAttribute, type);
				}
			}
		}
	}
}

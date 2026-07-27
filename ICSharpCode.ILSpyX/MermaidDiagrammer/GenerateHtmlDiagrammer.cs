// Copyright (c) 2024 Holger Schmidt
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

using System.Collections.Generic;

namespace ICSharpCode.ILSpyX.MermaidDiagrammer
{
	/// <summary>The command for creating an HTML5 diagramming app with an API optimized for binding command line parameters.
	/// To use it outside of that context, set its properties and call <see cref="Run"/>.</summary>
	public partial class GenerateHtmlDiagrammer
	{
		internal const string RepoUrl = "https://github.com/icsharpcode/ILSpy";

		/// <summary>
		/// Gets or sets the path to the assembly that is analyzed to build the diagrammer model.
		/// </summary>
		public required string Assembly { get; set; }

		/// <summary>
		/// Gets or sets the output directory for generated files.
		/// When not specified, output is written next to <see cref="Assembly"/> in a
		/// <c>&lt;AssemblyName&gt; diagrammer</c> folder.
		/// </summary>
		public string? OutputFolder { get; set; }

		/// <summary>
		/// Gets or sets a regular expression that must match a type's
		/// <see cref="ICSharpCode.Decompiler.TypeSystem.INamedElement.ReflectionName"/> for that type
		/// to be included in the diagrammer model.
		/// </summary>
		public string? Include { get; set; }

		/// <summary>
		/// Gets or sets a regular expression that excludes types whose
		/// <see cref="ICSharpCode.Decompiler.TypeSystem.INamedElement.ReflectionName"/> it matches
		/// from the diagrammer model after include filtering.
		/// </summary>
		public string? Exclude { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether only <c>model.json</c> should be produced.
		/// When <see langword="false"/>, an <c>index.html</c> app and its static assets are emitted.
		/// </summary>
		public bool JsonOnly { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a report with excluded type names is written
		/// to <c>excluded types.txt</c> in the output folder.
		/// </summary>
		public bool ReportExcludedTypes { get; set; }

		/// <summary>
		/// Gets or sets the path to the XML documentation file used to
		/// annotate generated type and member entries.
		/// When not set, the generator probes for a sibling <c>.xml</c> file next to <see cref="Assembly"/>.
		/// </summary>
		public string? XmlDocs { get; set; }

		/// <summary>Namespaces to strip from <see cref="XmlDocs"/>.
		/// Implemented as a list of exact replacements instead of a single, more powerful RegEx because replacement in
		/// <see cref="XmlDocumentationFormatter.GetDoco(Decompiler.TypeSystem.IEntity)"/>
		/// happens on the unstructured string where matching and replacing the namespaces of referenced types, members and method parameters
		/// using RegExes would add a lot of complicated RegEx-heavy code for a rather unimportant feature.</summary>
		public IEnumerable<string>? StrippedNamespaces { get; set; }
	}
}

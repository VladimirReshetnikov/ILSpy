// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Text;
using System.Threading;
using System.Xml.Linq;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Documentation
{
	/// <summary>
	/// Represents a node in processed XML documentation, with optional expansion of <c>&lt;inheritdoc/&gt;</c>.
	/// </summary>
	/// <remarks>
	/// Instances may represent either element nodes (with attributes and children) or synthetic text nodes used to preserve mixed content.
	/// </remarks>
	public class XmlDocumentationElement
	{
		readonly XElement? element;
		readonly IEntity? declaringEntity;
		readonly Func<string, IEntity?>? crefResolver;
		volatile string? textContent;

		/// <summary>
		/// Inheritance level; used to prevent cyclic doc inheritance.
		/// </summary>
		int nestingLevel;

		/// <summary>
		/// Creates an element-backed documentation node.
		/// </summary>
		/// <param name="element">XML element represented by this node.</param>
		/// <param name="declaringEntity">Entity that owns the documentation block, used as the implicit source for <c>inheritdoc</c>.</param>
		/// <param name="crefResolver">Resolver used to map <c>cref</c> attribute values to entities.</param>
		/// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
		public XmlDocumentationElement(XElement element, IEntity? declaringEntity, Func<string, IEntity?>? crefResolver)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			this.element = element;
			this.declaringEntity = declaringEntity;
			this.crefResolver = crefResolver;
		}

		/// <summary>
		/// Creates a text-node documentation element.
		/// </summary>
		/// <param name="text">Literal text content represented by this node.</param>
		/// <param name="declaringEntity">Entity that owns the surrounding documentation block.</param>
		/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
		public XmlDocumentationElement(string text, IEntity? declaringEntity)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			this.declaringEntity = declaringEntity;
			this.textContent = text;
		}

		/// <summary>
		/// Gets the entity on which this documentation node originated.
		/// </summary>
		/// <value>
		/// The declaration owner for this node, or <see langword="null"/> when the node was created without entity context.
		/// </value>
		public IEntity? DeclaringEntity {
			get { return declaringEntity; }
		}

		IEntity? referencedEntity;
		volatile bool referencedEntityInitialized;

		/// <summary>
		/// Gets the entity referenced by this node's <c>cref</c> attribute.
		/// </summary>
		/// <value>
		/// Resolved entity when <c>cref</c> exists and can be resolved; otherwise <see langword="null"/>.
		/// Resolution failures are intentionally swallowed.
		/// </value>
		public IEntity? ReferencedEntity {
			get {
				if (!referencedEntityInitialized)
				{
					string? cref = GetAttribute("cref");
					try
					{
						if (!string.IsNullOrEmpty(cref) && crefResolver != null)
							referencedEntity = crefResolver(cref!);
					}
					catch
					{
						referencedEntity = null;
					}
					referencedEntityInitialized = true;
				}
				return referencedEntity;
			}
		}

		/// <summary>
		/// Gets the local XML element name.
		/// </summary>
		/// <value>Element local name for element nodes; empty string for text nodes.</value>
		public string Name {
			get {
				return element != null ? element.Name.LocalName : string.Empty;
			}
		}

		/// <summary>
		/// Gets an attribute value from the underlying XML element.
		/// </summary>
		/// <param name="name">Attribute name to retrieve.</param>
		/// <returns>Attribute value when present; otherwise <see langword="null"/>.</returns>
		public string? GetAttribute(string? name)
		{
			return name == null ? null : element?.Attribute(name)?.Value;
		}

		/// <summary>
		/// Gets whether this instance represents plain text rather than an XML element.
		/// </summary>
		public bool IsTextNode {
			get { return element == null; }
		}

		/// <summary>
		/// Gets the concatenated textual content of this node.
		/// </summary>
		/// <value>
		/// For text nodes, the stored text value. For element nodes, concatenation of descendant <see cref="Children"/> text content.
		/// </value>
		public string TextContent {
			get {
				if (textContent == null)
				{
					StringBuilder b = new StringBuilder();
					foreach (var child in this.Children)
						b.Append(child.TextContent);
					textContent = b.ToString();
				}
				return textContent;
			}
		}

		IList<XmlDocumentationElement>? children;

		/// <summary>
		/// Gets child documentation nodes after applying inheritance expansion and boundary whitespace normalization.
		/// </summary>
		/// <value>
		/// The cached list of child nodes; empty for a text node. The same instance is returned on every
		/// access rather than a defensive copy, so callers must not modify it.
		/// </value>
		public IList<XmlDocumentationElement> Children {
			get {
				if (element == null)
					return EmptyList<XmlDocumentationElement>.Instance;
				return LazyInitializer.EnsureInitialized(
					ref this.children,
					() => CreateElements(element.Nodes(), declaringEntity, crefResolver, nestingLevel))!;
			}
		}

		static readonly string[] doNotInheritIfAlreadyPresent = {
			"example", "exclude", "filterpriority", "preliminary", "summary",
			"remarks", "returns", "threadsafety", "value"
		};

		static List<XmlDocumentationElement> CreateElements(IEnumerable<XObject?> childObjects, IEntity? declaringEntity, Func<string, IEntity?>? crefResolver, int nestingLevel)
		{
			List<XmlDocumentationElement> list = new List<XmlDocumentationElement>();
			foreach (var child in childObjects)
			{
				var childText = child as XText;
				var childTag = child as XCData;
				var childElement = child as XElement;
				if (childText != null)
				{
					list.Add(new XmlDocumentationElement(childText.Value, declaringEntity));
				}
				else if (childTag != null)
				{
					list.Add(new XmlDocumentationElement(childTag.Value, declaringEntity));
				}
				else if (childElement != null)
				{
					if (nestingLevel < 5 && childElement.Name == "inheritdoc")
					{
						string? cref = childElement.Attribute("cref")?.Value;
						IEntity? inheritedFrom = null;
						string? inheritedDocumentation = null;
						if (cref != null && crefResolver != null)
						{
							inheritedFrom = crefResolver(cref);
							if (inheritedFrom != null)
								inheritedDocumentation = "<doc>" + inheritedFrom.GetDocumentation() + "</doc>";
						}
						else if (declaringEntity != null)
						{
							foreach (IMember baseMember in InheritanceHelper.GetBaseMembers((IMember)declaringEntity, includeImplementedInterfaces: true))
							{
								inheritedDocumentation = baseMember.GetDocumentation();
								if (inheritedDocumentation != null)
								{
									inheritedFrom = baseMember;
									inheritedDocumentation = "<doc>" + inheritedDocumentation + "</doc>";
									break;
								}
							}
						}

						if (inheritedDocumentation != null)
						{
							var doc = XDocument.Parse(inheritedDocumentation).Element("doc");

							// XPath filter not yet implemented
							if (doc != null && childElement.Parent?.Parent == null && childElement.Attribute("select")?.Value == null)
							{
								// Inheriting documentation at the root level
								List<string> doNotInherit = new List<string>();
								doNotInherit.Add("overloads");
								doNotInherit.AddRange(childObjects.OfType<XElement>().Select(e => e.Name.LocalName).Intersect(
									doNotInheritIfAlreadyPresent));

								var inheritedChildren = doc.Nodes().Where(
									inheritedObject => {
										XElement? inheritedElement = inheritedObject as XElement;
										return !(inheritedElement != null && doNotInherit.Contains(inheritedElement.Name.LocalName));
									});

								list.AddRange(CreateElements(inheritedChildren, inheritedFrom, crefResolver, nestingLevel + 1));
							}
						}
					}
					else
					{
						list.Add(new XmlDocumentationElement(childElement, declaringEntity, crefResolver) { nestingLevel = nestingLevel });
					}
				}
			}
			if (list.Count > 0 && list[0].IsTextNode)
			{
				if (string.IsNullOrWhiteSpace(list[0].textContent))
					list.RemoveAt(0);
				else
					list[0].textContent = list[0].textContent!.TrimStart();
			}
			if (list.Count > 0 && list[list.Count - 1].IsTextNode)
			{
				if (string.IsNullOrWhiteSpace(list[list.Count - 1].textContent))
					list.RemoveAt(list.Count - 1);
				else
					list[list.Count - 1].textContent = list[list.Count - 1].textContent!.TrimEnd();
			}
			return list;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			if (element != null)
				return "<" + element.Name + ">";
			else
				return this.TextContent;
		}
	}
}

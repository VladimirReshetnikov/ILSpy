// Copyright (c) 2024 Tom Englert
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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace ICSharpCode.ILSpyX.Settings
{
	/// <summary>
	/// Persists user-selected <see cref="Decompiler.DecompilerSettings"/> values as an ILSpy settings section.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Options of the base decompiler settings type are serialized unless they are explicitly marked
	/// non-browsable. This keeps the persisted payload focused on UI-exposed toggles and avoids storing
	/// switches that were deliberately hidden.
	/// </para>
	/// <para>
	/// The persisted values are represented as XML attributes whose names match the corresponding setting property names.
	/// </para>
	/// </remarks>
	public class DecompilerSettings : Decompiler.DecompilerSettings, ISettingsSection
	{
		static readonly PropertyInfo[] properties = typeof(Decompiler.DecompilerSettings).GetProperties()
				.Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
				.ToArray();

		/// <summary>
		/// Gets the XML element name that contains persisted decompiler settings.
		/// </summary>
		public XName SectionName => "DecompilerSettings";

		/// <summary>
		/// Writes all browsable decompiler options into an XML element.
		/// </summary>
		/// <returns>An element named <c>DecompilerSettings</c> with one attribute per persisted option.</returns>
		public XElement SaveToXml()
		{
			var section = new XElement(SectionName);

			foreach (var p in properties)
			{
				section.SetAttributeValue(p.Name, p.GetValue(this));
			}

			return section;
		}

		/// <summary>
		/// Restores decompiler options from persisted XML attributes.
		/// </summary>
		/// <param name="section">The XML element to read values from.</param>
		/// <remarks>
		/// Missing attributes are ignored so that newly introduced options keep their in-memory defaults when older settings files are loaded.
		/// </remarks>
		public void LoadFromXml(XElement section)
		{
			foreach (var p in properties)
			{
				var value = (bool?)section.Attribute(p.Name);
				if (value.HasValue)
					p.SetValue(this, value.Value);
			}
		}

		/// <summary>
		/// Creates a copy of this settings instance.
		/// </summary>
		/// <returns>A cloned settings object with the same option values.</returns>
		public override DecompilerSettings Clone()
		{
			return (DecompilerSettings)base.Clone();
		}

		/// <summary>
		/// Looks up a decompiler option by property name.
		/// </summary>
		/// <param name="name">The option/property name to resolve.</param>
		/// <param name="property">When this method returns <see langword="true"/>, receives metadata for the matching property; otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if <paramref name="name"/> matches a known browsable option; otherwise <see langword="false"/>.</returns>
		public static bool IsKnownOption(string name, [NotNullWhen(true)] out PropertyInfo? property)
		{
			property = null;
			foreach (var item in properties)
			{
				if (item.Name != name)
					continue;
				property = item;
				return true;
			}

			return false;
		}
	}
}

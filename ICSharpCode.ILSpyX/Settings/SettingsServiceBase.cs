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

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Xml.Linq;

namespace ICSharpCode.ILSpyX.Settings
{
	/// <summary>
	/// Marks a settings object whose values are nested under a parent settings section.
	/// </summary>
	public interface IChildSettings
	{
		/// <summary>
		/// Gets the section that owns this child settings object.
		/// </summary>
		ISettingsSection Parent { get; }
	}

	/// <summary>
	/// Defines the persistence contract for one logical settings section.
	/// </summary>
	public interface ISettingsSection : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the XML element name used to store this section.
		/// </summary>
		XName SectionName { get; }

		/// <summary>
		/// Loads section state from the specified XML element.
		/// </summary>
		/// <param name="section">XML element for this section. Callers pass an empty element when no persisted section exists.</param>
		void LoadFromXml(XElement section);

		/// <summary>
		/// Serializes this section into an XML element that can be persisted.
		/// </summary>
		/// <returns>The serialized XML representation of this section.</returns>
		XElement SaveToXml();
	}

	/// <summary>
	/// Provides lazy loading and caching of <see cref="ISettingsSection"/> instances backed by an <see cref="ISettingsProvider"/>.
	/// </summary>
	/// <param name="spySettings">The settings store used to load persisted section elements.</param>
	public class SettingsServiceBase(ISettingsProvider spySettings)
	{
		protected readonly ConcurrentDictionary<Type, ISettingsSection> sections = new();

		protected ISettingsProvider SpySettings { get; set; } = spySettings;

		/// <summary>
		/// Gets a settings section instance, creating and loading it on first access.
		/// </summary>
		/// <typeparam name="T">Settings section type.</typeparam>
		/// <returns>A cached instance of <typeparamref name="T"/>.</returns>
		public T GetSettings<T>() where T : ISettingsSection, new()
		{
			return (T)sections.GetOrAdd(typeof(T), _ => {
				T section = new T();

				var sectionElement = SpySettings[section.SectionName];

				section.LoadFromXml(sectionElement);
				section.PropertyChanged += Section_PropertyChanged;

				return section;
			});
		}

		/// <summary>
		/// Persists a section by replacing or appending its element under the specified root.
		/// </summary>
		/// <param name="section">The section to serialize.</param>
		/// <param name="root">The root XML element that contains all settings sections.</param>
		protected static void SaveSection(ISettingsSection section, XElement root)
		{
			var element = section.SaveToXml();

			var existingElement = root.Element(section.SectionName);
			if (existingElement != null)
				existingElement.ReplaceWith(element);
			else
				root.Add(element);
		}

		/// <summary>
		/// Handles <see cref="INotifyPropertyChanged.PropertyChanged"/> events raised by loaded sections.
		/// </summary>
		/// <param name="sender">The section (or child settings object) that raised the change event.</param>
		/// <param name="e">Property-change metadata.</param>
		protected virtual void Section_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
		}
	}
}

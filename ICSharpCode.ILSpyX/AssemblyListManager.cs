// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpyX.FileLoaders;
using ICSharpCode.ILSpyX.Settings;

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// Manages the available assembly lists.
	/// 
	/// Contains the list of list names; and provides methods for loading/saving and creating/deleting lists.
	/// </summary>
	public sealed class AssemblyListManager
	{
		/// <summary>Built-in list template name for .NET Framework 4.x desktop assemblies.</summary>
		public const string DotNet4List = ".NET 4 (WPF)";
		/// <summary>Built-in list template name for .NET Framework 3.5 assemblies.</summary>
		public const string DotNet35List = ".NET 3.5";
		/// <summary>Built-in list template name for ASP.NET MVC 3 assemblies.</summary>
		public const string ASPDotNetMVC3List = "ASP.NET (MVC3)";

		private readonly ISettingsProvider settingsProvider;

		/// <summary>
		/// Creates a manager backed by the provided settings store and initializes <see cref="AssemblyLists"/>
		/// from the persisted list metadata.
		/// </summary>
		/// <param name="settingsProvider">
		/// Settings source used to load and persist the <c>AssemblyLists</c> section.
		/// </param>
		public AssemblyListManager(ISettingsProvider settingsProvider)
		{
			this.settingsProvider = settingsProvider;
			XElement doc = this.settingsProvider["AssemblyLists"];
			foreach (var list in doc.Elements("List"))
			{
				var name = (string?)list.Attribute("name");
				if (name != null)
				{
					AssemblyLists.Add(name);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether newly loaded assemblies in this manager should use WinRT metadata projections.
		/// </summary>
		public bool ApplyWinRTProjections { get; set; }

		/// <summary>
		/// Gets or sets whether newly loaded assemblies should attempt to consume debug symbols.
		/// </summary>
		public bool UseDebugSymbols { get; set; }

		/// <summary>
		/// Gets the currently known assembly list names, suitable for data binding in host UIs.
		/// </summary>
		public ObservableCollection<string> AssemblyLists { get; } = [];

		/// <summary>
		/// Gets the shared file-loader registry used when opening assemblies for lists managed by this instance.
		/// </summary>
		public FileLoaderRegistry LoaderRegistry { get; } = new();

		/// <summary>
		/// Loads an assembly list from the ILSpySettings.
		/// If no list with the specified name is found, the default list is loaded instead.
		/// </summary>
		/// <param name="listName">Name of the list to load.</param>
		/// <returns>
		/// The persisted list with that name, or a new empty list carrying the requested name when settings
		/// contain no such entry.
		/// </returns>
		public AssemblyList LoadList(string listName)
		{
			AssemblyList list = DoLoadList(listName);
			if (!AssemblyLists.Contains(list.ListName))
				AssemblyLists.Add(list.ListName);
			return list;
		}

		AssemblyList DoLoadList(string? listName)
		{
			XElement doc = this.settingsProvider["AssemblyLists"];
			if (listName != null)
			{
				foreach (var list in doc.Elements("List"))
				{
					if ((string?)list.Attribute("name") == listName)
					{
						return new AssemblyList(this, list);
					}
				}
			}
			return new AssemblyList(this, listName ?? DefaultListName);
		}

		/// <summary>
		/// Clones an existing list into a new list name.
		/// </summary>
		/// <param name="selectedAssemblyList">Name of the list to copy from.</param>
		/// <param name="newListName">Name for the cloned list.</param>
		/// <returns><see langword="true"/> when the clone was created; <see langword="false"/> when the target name already exists.</returns>
		public bool CloneList(string selectedAssemblyList, string newListName)
		{
			var list = DoLoadList(selectedAssemblyList);
			var newList = new AssemblyList(list, newListName);
			return AddListIfNotExists(newList);
		}

		/// <summary>
		/// Renames a list by copying its contents to a new name and deleting the original entry.
		/// </summary>
		/// <param name="selectedAssemblyList">Current list name.</param>
		/// <param name="newListName">Replacement list name.</param>
		/// <returns><see langword="true"/> when the rename succeeded; otherwise <see langword="false"/>.</returns>
		public bool RenameList(string selectedAssemblyList, string newListName)
		{
			var list = DoLoadList(selectedAssemblyList);
			var newList = new AssemblyList(list, newListName);
			return DeleteList(selectedAssemblyList) && AddListIfNotExists(newList);
		}

		/// <summary>Default name assigned when no explicit list name is provided.</summary>
		public const string DefaultListName = "(Default)";

		/// <summary>
		/// Saves the specified assembly list into the config file.
		/// </summary>
		/// <param name="list">Assembly list snapshot to persist.</param>
		public void SaveList(AssemblyList list)
		{
			this.settingsProvider.Update(
				root => {
					XElement? doc = root.Element("AssemblyLists");
					if (doc == null)
					{
						doc = new XElement("AssemblyLists");
						root.Add(doc);
					}

					XElement? listElement = doc.Elements("List")
						.FirstOrDefault(e => (string?)e.Attribute("name") == list.ListName);
					if (listElement != null)
						listElement.ReplaceWith(list.SaveAsXml());
					else
						doc.Add(list.SaveAsXml());
				});
		}

		/// <summary>
		/// Adds a list name to the manager and persists the list when that name is not already present.
		/// </summary>
		/// <param name="list">List to register and save.</param>
		/// <returns><see langword="true"/> when the list was newly added; otherwise <see langword="false"/>.</returns>
		public bool AddListIfNotExists(AssemblyList list)
		{
			if (!AssemblyLists.Contains(list.ListName))
			{
				AssemblyLists.Add(list.ListName);
				SaveList(list);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes a persisted assembly list by name.
		/// </summary>
		/// <param name="name">Exact list name to remove.</param>
		/// <returns><see langword="true"/> when a list with the given name existed and was deleted.</returns>
		public bool DeleteList(string name)
		{
			if (AssemblyLists.Remove(name))
			{
				this.settingsProvider.Update(
					delegate (XElement root) {
						XElement? doc = root.Element("AssemblyLists");
						if (doc == null)
						{
							return;
						}
						XElement? listElement = doc.Elements("List").FirstOrDefault(e => (string?)e.Attribute("name") == name);
						if (listElement != null)
							listElement.Remove();
					});
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes all known list names and deletes the persisted <c>AssemblyLists</c> section from settings.
		/// </summary>
		public void ClearAll()
		{
			AssemblyLists.Clear();
			this.settingsProvider.Update(
				root => {
					XElement? doc = root.Element("AssemblyLists");
					doc?.Remove();
				});
		}

		/// <summary>
		/// Creates ILSpy's predefined framework lists when no lists are currently available.
		/// </summary>
		public void CreateDefaultAssemblyLists()
		{
			if (AssemblyLists.Count > 0)
				return;

			if (!AssemblyLists.Contains(DotNet4List))
			{
				AssemblyList dotnet4 = CreateDefaultList(DotNet4List);
				if (dotnet4.Count > 0)
				{
					AddListIfNotExists(dotnet4);
				}
			}

			if (!AssemblyLists.Contains(DotNet35List))
			{
				AssemblyList dotnet35 = CreateDefaultList(DotNet35List);
				if (dotnet35.Count > 0)
				{
					AddListIfNotExists(dotnet35);
				}
			}

			if (!AssemblyLists.Contains(ASPDotNetMVC3List))
			{
				AssemblyList mvc = CreateDefaultList(ASPDotNetMVC3List);
				if (mvc.Count > 0)
				{
					AddListIfNotExists(mvc);
				}
			}
		}

		/// <summary>
		/// Creates a new in-memory list with the specified name.
		/// </summary>
		/// <param name="name">Name assigned to the created list.</param>
		/// <returns>A list instance that can be populated and then saved via <see cref="AddListIfNotExists"/>.</returns>
		public AssemblyList CreateList(string name)
		{
			return new AssemblyList(this, name);
		}

		/// <summary>
		/// Creates one of ILSpy's predefined framework assembly lists.
		/// </summary>
		/// <param name="name">Preconfigured list identifier (for example <see cref="DotNet4List"/>).</param>
		/// <param name="path">
		/// Directory of framework assemblies used to populate the list when <paramref name="name"/> is not one of
		/// the built-in template names; ignored for the built-in templates.
		/// </param>
		/// <param name="newName">Optional display name override for the resulting list.</param>
		/// <returns>A populated list that may be empty when none of the expected assemblies are found.</returns>
		public AssemblyList CreateDefaultList(string name, string? path = null, string? newName = null)
		{
			var list = new AssemblyList(this, newName ?? name);
			switch (name)
			{
				case DotNet4List:
					AddToListFromGAC("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					break;
				case DotNet35List:
					AddToListFromGAC("mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data.DataSetExtensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xml.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					break;
				case ASPDotNetMVC3List:
					AddToListFromGAC("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("System.Web.Abstractions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.ApplicationServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.DynamicData, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.Mvc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.Routing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					AddToListFromGAC("System.Web.WebPages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Web.Helpers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
					AddToListFromGAC("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
					AddToListFromGAC("Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
					break;
				case object _ when path != null:
					AddFrameworkAssembliesFromDirectory(list, path);
					break;
			}
			return list;

			void AddToListFromGAC(string fullName)
			{
				AssemblyNameReference reference = AssemblyNameReference.Parse(fullName);
				string? file = UniversalAssemblyResolver.GetAssemblyInGac(reference);
				if (file != null)
					list.OpenAssembly(file);
			}
		}

		/// <summary>
		/// Opens every managed framework assembly in <paramref name="directory"/> into
		/// <paramref name="list"/>, applying the same filter the preconfigured runtime lists use
		/// (skips native helpers like *_cor3.dll and lower-cased runtime libraries such as
		/// coreclr.dll / clrjit.dll). Shared by the "preconfigured runtime list" path and the
		/// first-run default list, which seeds itself from the shared-framework directory ILSpy
		/// is running on.
		/// </summary>
		public void AddFrameworkAssembliesFromDirectory(AssemblyList list, string directory)
		{
			foreach (var file in Directory.GetFiles(directory, "*.dll"))
			{
				if (IsIncludedFrameworkFile(Path.GetFileName(file)))
					list.OpenAssembly(file);
			}
		}

		static bool IsIncludedFrameworkFile(string fileName)
		{
			if (fileName == "Microsoft.DiaSymReader.Native.amd64.dll")
				return false;
			if (fileName.EndsWith("_cor3.dll", StringComparison.OrdinalIgnoreCase))
				return false;
			if (char.IsUpper(fileName[0]))
				return true;
			if (fileName == "netstandard.dll")
				return true;
			if (fileName == "mscorlib.dll")
				return true;
			return false;
		}
	}
}

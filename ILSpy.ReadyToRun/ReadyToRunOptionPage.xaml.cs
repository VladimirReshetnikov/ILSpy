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

using System.Composition;

using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.Util;

using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Composition.AttributedModel;

namespace ICSharpCode.ILSpy.ReadyToRun
{
	[DataTemplate(typeof(ReadyToRunOptionsViewModel))]
	[NonShared]
	partial class ReadyToRunOptionPage
	{
		/// <summary>
		/// Initializes the ReadyToRun options view and wires its XAML-defined controls.
		/// </summary>
		public ReadyToRunOptionPage()
		{
			InitializeComponent();
		}
	}

	[ExportOptionPage(Order = 40)]
	[NonShared]
	class ReadyToRunOptionsViewModel : ObservableObjectBase, IOptionPage
	{
		private ReadyToRunOptions options;

		/// <summary>
		/// Gets or sets the mutable ReadyToRun settings object currently being edited by the page.
		/// </summary>
		public ReadyToRunOptions Options {
			get => options;
			set => SetProperty(ref options, value);
		}

		/// <summary>
		/// Gets the localized title displayed for this option page in ILSpy's settings dialog.
		/// </summary>
		public string Title => global::ILSpy.ReadyToRun.Properties.Resources.ReadyToRun;

		/// <summary>
		/// Loads the persisted ReadyToRun options from the provided settings snapshot.
		/// </summary>
		/// <param name="snapshot">Snapshot that supplies existing settings sections for the options dialog session.</param>
		public void Load(SettingsSnapshot snapshot)
		{
			Options = snapshot.GetSettings<ReadyToRunOptions>();
		}

		/// <summary>
		/// Restores default ReadyToRun option values by loading from an empty settings element.
		/// </summary>
		public void LoadDefaults()
		{
			Options.LoadFromXml(new("empty"));
		}
	}
}
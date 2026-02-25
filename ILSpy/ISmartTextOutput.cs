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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Themes;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Adds additional WPF-specific output features to <see cref="ITextOutput"/>.
	/// </summary>
	public interface ISmartTextOutput : ITextOutput
	{
		/// <summary>
		/// Inserts an interactive UI element at the current position in the text output.
		/// </summary>
		/// <param name="element">Factory that creates the element when the output view is materialized.</param>
		void AddUIElement(Func<UIElement> element);

		/// <summary>
		/// Starts a highlight span for subsequently written text.
		/// </summary>
		/// <param name="highlightingColor">The highlighting style to apply until <see cref="EndSpan"/> is called.</param>
		void BeginSpan(HighlightingColor highlightingColor);
		/// <summary>
		/// Ends the current highlight span created by <see cref="BeginSpan"/>.
		/// </summary>
		void EndSpan();

		/// <summary>
		/// Gets/sets the title displayed in the document tab's header.
		/// </summary>
		string Title { get; set; }
	}

	public static class SmartTextOutputExtensions
	{
		/// <summary>
		/// Adds a themed button to the output stream.
		/// </summary>
		/// <param name="output">The output surface that should host the button.</param>
		/// <param name="icon">Optional icon displayed before <paramref name="text"/>.</param>
		/// <param name="text">Caption shown on the button.</param>
		/// <param name="click">Handler invoked when the user clicks the button.</param>
		public static void AddButton(this ISmartTextOutput output, ImageSource icon, string text, RoutedEventHandler click)
		{
			output.AddUIElement(
				delegate {
					Button button = ThemeManager.Current.CreateButton();
					button.Cursor = Cursors.Arrow;
					button.Margin = new Thickness(2);
					button.Padding = new Thickness(9, 1, 9, 1);
					button.MinWidth = 73;
					if (icon != null)
					{
						button.Content = new StackPanel {
							Orientation = Orientation.Horizontal,
							Children = {
								new Image { Width = 16, Height = 16, Source = icon, Margin = new Thickness(0, 0, 4, 0) },
								new TextBlock { Text = text }
							}
						};
					}
					else
					{
						button.Content = text;
					}
					button.Click += click;
					return button;
				});
		}
	}
}

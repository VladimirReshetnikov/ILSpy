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

#nullable enable

namespace ICSharpCode.Decompiler.CSharp
{
	/// <summary>
	/// Identifies the C# language feature set that the decompiler is allowed to emit.
	/// </summary>
	/// <remarks>
	/// Values are intentionally sortable so callers can compare versions numerically.
	/// This is used by <see cref="DecompilerSettings.SetLanguageVersion(LanguageVersion)"/> and
	/// <see cref="DecompilerSettings.GetMinimumRequiredVersion"/> to enable/disable feature switches.
	/// </remarks>
	public enum LanguageVersion
	{
		/// <summary>Targets C# 1.0 syntax.</summary>
		CSharp1 = 1,
		/// <summary>Targets C# 2.0 syntax (generics, iterators, nullable value types, and anonymous methods).</summary>
		CSharp2 = 2,
		/// <summary>Targets C# 3.0 syntax (LINQ/query expressions, extension methods, anonymous/object initializers).</summary>
		CSharp3 = 3,
		/// <summary>Targets C# 4.0 syntax (dynamic binding, optional parameters, and named arguments).</summary>
		CSharp4 = 4,
		/// <summary>Targets C# 5.0 syntax (async/await).</summary>
		CSharp5 = 5,
		/// <summary>Targets C# 6.0 syntax (string interpolation, expression-bodied members, null propagation).</summary>
		CSharp6 = 6,
		/// <summary>Targets C# 7.0 syntax.</summary>
		CSharp7 = 7,
		/// <summary>Targets C# 7.1 syntax.</summary>
		CSharp7_1 = 701,
		/// <summary>Targets C# 7.2 syntax.</summary>
		CSharp7_2 = 702,
		/// <summary>Targets C# 7.3 syntax.</summary>
		CSharp7_3 = 703,
		/// <summary>Targets C# 8.0 syntax.</summary>
		CSharp8_0 = 800,
		/// <summary>Targets C# 9.0 syntax.</summary>
		CSharp9_0 = 900,
		/// <summary>Targets C# 10.0 syntax.</summary>
		CSharp10_0 = 1000,
		/// <summary>Targets C# 11.0 syntax.</summary>
		CSharp11_0 = 1100,
		/// <summary>Targets C# 12.0 syntax.</summary>
		CSharp12_0 = 1200,
		/// <summary>Targets C# 13.0 syntax.</summary>
		CSharp13_0 = 1300,
		/// <summary>Targets C# 14.0 syntax.</summary>
		CSharp14_0 = 1400,
		/// <summary>Targets C# 15.0 syntax.</summary>
		CSharp15_0 = 1500,
		/// <summary>Uses the current preview language level supported by this snapshot.</summary>
		Preview = 1500,
		/// <summary>Uses the highest available language level without an upper bound check.</summary>
		Latest = 0x7FFFFFFF
	}
}

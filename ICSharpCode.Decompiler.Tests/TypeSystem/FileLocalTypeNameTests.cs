// Copyright (c) 2024 ICSharpCode
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

using ICSharpCode.Decompiler.TypeSystem;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.TypeSystem
{
	[TestFixture]
	public class FileLocalTypeNameTests
	{
		[TestCase("<Accessor>F8A381ED791521B3EC40CC54F5B824892A06C621EA7BCFD9791E5A02ABCED3F22__AccessorFlagsExtensions", "AccessorFlagsExtensions")]
		[TestCase("<Lib>FABCDEF__Helper", "Helper")]
		[TestCase("<File.Name>FA1__Type_With_Underscores", "Type_With_Underscores")]
		[TestCase("<X>Fabcdef0123456789__lowerHash", "lowerHash")]
		public void RecognizesFileLocalMetadataNames(string metadataName, string expectedSourceName)
		{
			Assert.That(ReflectionHelper.TryGetFileLocalTypeName(metadataName, out string sourceName), Is.True);
			Assert.That(sourceName, Is.EqualTo(expectedSourceName));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("PlainType")]
		[TestCase("<>c__DisplayClass0_0")]      // closure
		[TestCase("<>f__AnonymousType0`1")]      // anonymous type
		[TestCase("<Method>d__5")]               // async/iterator state machine
		[TestCase("<Method>g__Local|0_0")]       // local function
		[TestCase("<Lib>F__Helper")]             // missing hash digits
		[TestCase("<Lib>FABCDEFHelper")]         // missing "__" separator
		[TestCase("<Lib>FABCDEF__")]             // empty source name
		[TestCase("<>FABCDEF__Helper")]          // empty file-name part
		[TestCase("<Lib>GABCDEF__Helper")]       // wrong discriminator (not 'F')
		public void RejectsNonFileLocalNames(string metadataName)
		{
			Assert.That(ReflectionHelper.TryGetFileLocalTypeName(metadataName, out _), Is.False);
		}
	}
}

// Copyright (c) 2026 Vladimir Reshetnikov
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
using ICSharpCode.Decompiler.TypeSystem.Implementation;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.TypeSystem
{
	[TestFixture]
	public class ResolveExtensionInfoTests
	{
		/// <summary>
		/// A member reference whose declaring type cannot be resolved becomes a fake member on an
		/// unknown type, with no type definition behind it. Asking whether such a member sits in
		/// an extension container has a definite answer - it does not - and must not crash: the
		/// overload-resolution-priority comparison asks this of every candidate at a call site
		/// where resolution did not land on the expected method, and a missing dependency is the
		/// ordinary way to get there.
		/// </summary>
		[Test]
		public void FakeMemberOnUnresolvedTypeIsNotInExtensionContainer()
		{
			var compilation = new SimpleCompilation(TypeSystemLoaderTests.TestAssembly, TypeSystemLoaderTests.Mscorlib);
			var unresolved = new UnknownType(new FullTypeName("Phantom.HolderExtensions"));
			var fake = new FakeMethod(compilation, SymbolKind.Method) {
				DeclaringType = unresolved,
				Name = "Measure",
				IsStatic = true,
			};
			Assert.That(((IMember)fake).DeclaringTypeDefinition, Is.Null, "precondition: a fake member on an unknown type has no definition");

			Assert.That(fake.ResolveExtensionInfo(), Is.Null);
		}
	}
}

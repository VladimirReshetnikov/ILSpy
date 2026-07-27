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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Provides compatibility helpers and extension methods for System.Reflection.Metadata APIs.
	/// </summary>
	public static partial class SRMExtensions
	{
		/// <summary>
		/// Backported flag value for generic parameters that allow byref-like type arguments
		/// (C# <c>allows ref struct</c>, IL <c>byreflike</c>).
		/// </summary>
		/// <remarks>
		/// Some target frameworks do not expose this enum member directly, so ILSpy carries the raw bit value.
		/// </remarks>
		internal const GenericParameterAttributes AllowByRefLike = (GenericParameterAttributes)0x0020;
		internal const MethodImplAttributes MethodImplAsync = (MethodImplAttributes)0x2000;

		/// <summary>
		/// Gets all <see cref="MethodImplementationHandle"/> rows in the declaring type that use
		/// <paramref name="handle"/> as the method body.
		/// </summary>
		/// <param name="handle">The method whose matching <c>.override</c> records should be returned.</param>
		/// <param name="reader">Metadata reader used to inspect the owning type and method implementation table.</param>
		/// <returns>
		/// An immutable array containing every method implementation that points to
		/// <paramref name="handle"/> as <c>MethodBody</c>.
		/// </returns>
		public static ImmutableArray<MethodImplementationHandle> GetMethodImplementations(
			this MethodDefinitionHandle handle, MetadataReader reader)
		{
			var resultBuilder = ImmutableArray.CreateBuilder<MethodImplementationHandle>();
			var typeDefinition = reader.GetTypeDefinition(reader.GetMethodDefinition(handle)
				.GetDeclaringType());

			foreach (var methodImplementationHandle in typeDefinition.GetMethodImplementations())
			{
				var methodImplementation = reader.GetMethodImplementation(methodImplementationHandle);
				if (methodImplementation.MethodBody == handle)
				{
					resultBuilder.Add(methodImplementationHandle);
				}
			}

			return resultBuilder.ToImmutable();
		}
	}
}

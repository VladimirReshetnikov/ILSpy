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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.TypeSystem
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class TupleTypeTests
	{
		ICompilation compilation;
		IType intType;
		IType stringType;

		[OneTimeSetUp]
		public void SetUp()
		{
			compilation = new SimpleCompilation(TypeSystemLoaderTests.TestAssembly,
				TypeSystemLoaderTests.Mscorlib, TypeSystemLoaderTests.SystemCore);
			intType = compilation.FindType(typeof(int));
			stringType = compilation.FindType(typeof(string));
		}

		TupleType Tuple(string firstName = null, string secondName = null)
		{
			return new TupleType(compilation,
				ImmutableArray.Create(intType, stringType),
				ImmutableArray.Create(firstName, secondName));
		}

		static void AssertNames(IType type, params string[] expectedNames)
		{
			var tuple = type as TupleType;
			Assert.That(tuple, Is.Not.Null);
			Assert.That(tuple.ElementNames.ToArray(), Is.EqualTo(expectedNames));
		}

		[Test]
		public void MergeThroughNullableNestedGenericPreservesTargetDecorations()
		{
			var listDefinition = compilation.FindType(typeof(List<>));
			var keyValuePairDefinition = compilation.FindType(typeof(KeyValuePair<,>));
			var targetList = new ParameterizedType(listDefinition, Tuple());
			var sourceList = new ParameterizedType(
				listDefinition.ChangeNullability(Nullability.Nullable), Tuple("value", "fallback"));
			var target = new ParameterizedType(keyValuePairDefinition, targetList, intType);
			var source = new ParameterizedType(keyValuePairDefinition, sourceList, intType);

			var merged = TupleType.MergeTupleElementNames(target, source) as ParameterizedType;

			Assert.That(merged, Is.Not.Null);
			var mergedList = merged.TypeArguments[0] as ParameterizedType;
			Assert.That(mergedList, Is.Not.Null);
			Assert.That(mergedList.GenericType, Is.SameAs(listDefinition));
			AssertNames(mergedList.TypeArguments[0], "value", "fallback");
		}

		[Test]
		public void MergeThroughPointerAndByReferenceWrappers()
		{
			var pointer = TupleType.MergeTupleElementNames(
				new PointerType(Tuple()), new PointerType(Tuple("left", "right"))) as PointerType;
			Assert.That(pointer, Is.Not.Null);
			AssertNames(pointer.ElementType, "left", "right");

			var byReference = TupleType.MergeTupleElementNames(
				new ByReferenceType(Tuple()), new ByReferenceType(Tuple("left", "right"))) as ByReferenceType;
			Assert.That(byReference, Is.Not.Null);
			AssertNames(byReference.ElementType, "left", "right");
		}

		[Test]
		public void MergeThroughFunctionPointerSignature()
		{
			var module = (MetadataModule)compilation.MainModule;
			var target = new FunctionPointerType(module,
				SignatureCallingConvention.Default, ImmutableArray<IType>.Empty,
				Tuple(), returnIsRefReadOnly: false,
				ImmutableArray.Create<IType>(Tuple()), ImmutableArray.Create(ReferenceKind.None));
			var source = new FunctionPointerType(module,
				SignatureCallingConvention.Default, ImmutableArray<IType>.Empty,
				Tuple("result", "error"), returnIsRefReadOnly: false,
				ImmutableArray.Create<IType>(Tuple("value", "fallback")), ImmutableArray.Create(ReferenceKind.None));

			var merged = TupleType.MergeTupleElementNames(target, source) as FunctionPointerType;

			Assert.That(merged, Is.Not.Null);
			AssertNames(merged.ReturnType, "result", "error");
			AssertNames(merged.ParameterTypes[0], "value", "fallback");
		}

		[Test]
		public void MergeThroughExactlyMatchingModifier()
		{
			var target = new ModifiedType(intType, Tuple(), isRequired: true);
			var source = new ModifiedType(intType, Tuple("left", "right"), isRequired: true);

			var merged = TupleType.MergeTupleElementNames(target, source) as ModifiedType;

			Assert.That(merged, Is.Not.Null);
			Assert.That(merged.Kind, Is.EqualTo(TypeKind.ModReq));
			Assert.That(merged.Modifier, Is.SameAs(intType));
			AssertNames(merged.ElementType, "left", "right");
		}

		[Test]
		public void MismatchedModifierContainingTupleBailsOut()
		{
			var target = new ModifiedType(intType, Tuple(), isRequired: true);
			var source = new ModifiedType(stringType, Tuple("left", "right"), isRequired: false);

			Assert.That(TupleType.MergeTupleElementNames(target, source), Is.Null);
		}

		[Test]
		public void ConflictingTupleNamesBailOut()
		{
			Assert.That(TupleType.MergeTupleElementNames(
				Tuple("first", "second"), Tuple("left", "right")), Is.Null);
		}
	}
}

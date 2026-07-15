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

// This fixture models the shape emitted by older C# compilers: tuple names survive on
// generated lambda-method parameters, but local signatures and generic instantiations erase them.
// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 9:0:0:0
}
.assembly extern System.Linq
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 9:0:0:0
}
.assembly TupleNamesFromConsumerLambdas
{
  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56 65 72 73 69 6F 6E 3D 76 39 2E 30 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module TupleNamesFromConsumerLambdas.dll

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .field public static initonly class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' '<>9'

    .method private hidebysig specialname rtspecialname static void .cctor() cil managed
    {
      .maxstack 8
      IL_0000: newobj instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::.ctor()
      IL_0005: stsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
      IL_000a: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: call instance void [System.Runtime]System.Object::.ctor()
      IL_0006: ret
    }

    .method assembly hidebysig instance int32 '<Direct>b__0_0'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
      IL_0006: ret
    }

    .method assembly hidebysig instance int32 '<Chain>b__1_0'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !1 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item2
      IL_0006: ret
    }

    .method assembly hidebysig instance int32 '<NullableChain>b__4_0'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
      IL_0006: ret
    }

    .method assembly hidebysig instance int32 '<Conflict>b__2_0'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
      IL_0006: ret
    }

    .method assembly hidebysig instance int32 '<Conflict>b__2_1'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 01 78 01 79 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !1 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item2
      IL_0006: ret
    }

    .method assembly hidebysig instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> '<ProjectionQuery>b__3_0'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair) cil managed
    {
      .maxstack 4
      IL_0000: ldc.i4.1
      IL_0001: newarr [System.Runtime]System.Int32
      IL_0006: dup
      IL_0007: ldc.i4.0
      IL_0008: ldarg.1
      IL_0009: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
      IL_000e: stelem.i4
      IL_000f: ret
    }

    .method assembly hidebysig instance valuetype [System.Runtime]System.ValueTuple`2<int32, int32> '<ProjectionQuery>b__3_1'(
        valuetype [System.Runtime]System.ValueTuple`2<int32, int32> pair,
        int32 result) cil managed
    {
      .param [0]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
      .maxstack 8
      IL_0000: ldarg.1
      IL_0001: ldfld !0 valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::Item1
      IL_0006: ldarg.2
      IL_0007: newobj instance void valuetype [System.Runtime]System.ValueTuple`2<int32, int32>::.ctor(!0, !1)
      IL_000c: ret
    }
  }

  .method public hidebysig static class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!T>
          PassThrough<T>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!T> source) cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: ret
  }

  .method public hidebysig static class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!T>
          WhereNotNull<valuetype .ctor ([System.Runtime]System.ValueType) T>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.Nullable`1<!!T>> source) cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ret
  }

  .method public hidebysig static int32 Direct(
      class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> input) cil managed
  {
    .maxstack 4
    .locals init (class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> source)
    IL_0000: ldarg.0
    IL_0001: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0006: stloc.0
    IL_0002: ldloc.0
    IL_0003: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0008: ldftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<Direct>b__0_0'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_000e: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>::.ctor(object, native int)
    IL_0013: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
    IL_0018: call !!0 [System.Linq]System.Linq.Enumerable::First<int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_001d: ldloc.0
    IL_001e: call int32 [System.Linq]System.Linq.Enumerable::Count<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0023: add
    IL_0024: ret
  }

  .method public hidebysig static int32 Chain(
      class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> input) cil managed
  {
    .maxstack 4
    .locals init (class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> source)
    IL_0000: ldarg.0
    IL_0001: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0006: stloc.0
    IL_0002: ldloc.0
    IL_0003: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0008: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_000d: ldftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<Chain>b__1_0'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_0013: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>::.ctor(object, native int)
    IL_0018: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
    IL_001d: call !!0 [System.Linq]System.Linq.Enumerable::First<int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0022: ldloc.0
    IL_0023: call int32 [System.Linq]System.Linq.Enumerable::Count<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0028: add
    IL_0029: ret
  }

  .method public hidebysig static int32 NullableChain(
      class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>> input) cil managed
  {
    .maxstack 4
    .locals init (class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>> source)
    IL_0000: ldarg.0
    IL_0001: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0006: stloc.0
    IL_0007: ldloc.0
    IL_0008: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::WhereNotNull<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.Nullable`1<!!0>>)
    IL_000d: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0012: ldftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<NullableChain>b__4_0'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_0018: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>::.ctor(object, native int)
    IL_001d: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
    IL_0022: call !!0 [System.Linq]System.Linq.Enumerable::First<int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0027: ldloc.0
    IL_0028: call int32 [System.Linq]System.Linq.Enumerable::Count<valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_002d: add
    IL_002e: ret
  }

  .method public hidebysig static int32 Conflict(
      class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> input) cil managed
  {
    .maxstack 4
    .locals init (class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> source)
    IL_0000: ldarg.0
    IL_0001: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0006: stloc.0
    IL_0002: ldloc.0
    IL_0003: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0008: ldftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<Conflict>b__2_0'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_000e: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>::.ctor(object, native int)
    IL_0013: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
    IL_0018: call !!0 [System.Linq]System.Linq.Enumerable::First<int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_001d: ldloc.0
    IL_001e: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0023: ldftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<Conflict>b__2_1'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_0029: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>::.ctor(object, native int)
    IL_002e: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, !!1>)
    IL_0033: call !!0 [System.Linq]System.Linq.Enumerable::First<int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0038: add
    IL_0039: ret
  }

  .method public hidebysig static valuetype [System.Runtime]System.ValueTuple`2<int32, int32>[] ProjectionQuery(
      class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> input) cil managed
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6C 65 66 74 05 72 69 67 68 74 00 00 )
    .maxstack 5
    .locals init (class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>> source)
    IL_0000: ldarg.0
    IL_0001: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas::PassThrough<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0006: stloc.0
    IL_count_0: ldloc.0
    IL_count_1: call int32 [System.Linq]System.Linq.Enumerable::Count<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_count_2: pop
    IL_0002: ldloc.0
    IL_0003: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0008: ldftn instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<ProjectionQuery>b__3_0'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>)
    IL_000e: newobj instance void class [System.Runtime]System.Func`2<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32>>::.ctor(object, native int)
    IL_0013: ldsfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<>9'
    IL_0018: ldftn instance valuetype [System.Runtime]System.ValueTuple`2<int32, int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.TupleNamesFromConsumerLambdas/'<>c'::'<ProjectionQuery>b__3_1'(valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32)
    IL_001e: newobj instance void class [System.Runtime]System.Func`3<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32, valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>::.ctor(object, native int)
    IL_0023: call class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!2> [System.Linq]System.Linq.Enumerable::SelectMany<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>, int32, valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, class [System.Runtime]System.Func`2<!!0, class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1>>, class [System.Runtime]System.Func`3<!!0, !!1, !!2>)
    IL_0028: call !!0[] [System.Linq]System.Linq.Enumerable::ToArray<valuetype [System.Runtime]System.ValueTuple`2<int32, int32>>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_002d: ret
  }
}

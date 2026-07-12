// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}
.assembly extern System.Linq
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}
.assembly QueryExpressionInParameter
{
  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56 65 72 73 69 6F 6E 3D 76 39 2E 30 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module QueryExpressionInParameter.dll

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point
       extends [System.Runtime]System.ValueType
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  .field public initonly int32 X
  .field public initonly int32 Y
} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item
       extends [System.Runtime]System.ValueType
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
  .field public initonly string Name
  .field public initonly valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point Point
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(string name,
                               [in] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point& point) cil managed
  {
    .param [2]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item::Name
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  ldobj      ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point
    IL_000e:  stfld      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item::Point
    IL_0013:  ret
  } // end of method Item::.ctor

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter
       extends [System.Runtime]System.Object
{
  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .field public static initonly class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c' '<>9'
    .field public static class [System.Runtime]System.Func`3<string,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item> '<>9__0_1'
    .method private hidebysig specialname rtspecialname static
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  newobj     instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::.ctor()
      IL_0005:  stsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::'<>9'
      IL_000a:  ret
    } // end of method '<>c'::.cctor

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c'::.ctor

    .method assembly hidebysig instance valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item
            '<ExtractItems>b__0_1'(string g,
                                   valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point p) cil managed
    {
      // The range variable 'p' is passed directly by its address ('ldarga') to the
      // 'in' (readonly-ref) parameter of the Item constructor: this is the shape older
      // Roslyn versions emit (newer ones spill 'p' into a temporary first).
      .maxstack  2
      IL_0000:  ldarg.1
      IL_0001:  ldarga.s   p
      IL_0003:  newobj     instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item::.ctor(string,
                                                                                                     valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point&)
      IL_0008:  ret
    } // end of method '<>c'::'<ExtractItems>b__0_1'

  } // end of class '<>c'

  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass0_0'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .field public class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>> values
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass0_0'::.ctor

    .method assembly hidebysig instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>
            '<ExtractItems>b__0'(string g) cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c__DisplayClass0_0'::values
      IL_0006:  ldarg.1
      IL_0007:  callvirt   instance !1 class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>>::Invoke(!0)
      IL_000c:  ret
    } // end of method '<>c__DisplayClass0_0'::'<ExtractItems>b__0'

  } // end of class '<>c__DisplayClass0_0'

  .method public hidebysig static valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item[]
          ExtractItems(class [System.Runtime]System.Collections.Generic.IEnumerable`1<string> groups,
                       class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>> values) cil managed
  {
    .maxstack  4
    .locals init (class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c__DisplayClass0_0' V_0)
    IL_0000:  newobj     instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c__DisplayClass0_0'::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c__DisplayClass0_0'::values
    IL_000d:  ldarg.0
    IL_000e:  ldloc.0
    IL_000f:  ldftn      instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c__DisplayClass0_0'::'<ExtractItems>b__0'(string)
    IL_0015:  newobj     instance void class [System.Runtime]System.Func`2<string,class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point>>::.ctor(object,
                                                                                                                                                                                                                             native int)
    IL_001a:  ldsfld     class [System.Runtime]System.Func`3<string,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::'<>9__0_1'
    IL_001f:  dup
    IL_0020:  brtrue.s   IL_0039

    IL_0022:  pop
    IL_0023:  ldsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::'<>9'
    IL_0028:  ldftn      instance valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::'<ExtractItems>b__0_1'(string,
                                                                                                                                                                                                          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point)
    IL_002e:  newobj     instance void class [System.Runtime]System.Func`3<string,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item>::.ctor(object,
                                                                                                                                                                                                                          native int)
    IL_0033:  dup
    IL_0034:  stsfld     class [System.Runtime]System.Func`3<string,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter/'<>c'::'<>9__0_1'
    IL_0039:  call       class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!2> [System.Linq]System.Linq.Enumerable::SelectMany<string,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Point,valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>,
                                                                                                                                                                                                                                                                                        class [System.Runtime]System.Func`2<!!0,class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1>>,
                                                                                                                                                                                                                                                                                        class [System.Runtime]System.Func`3<!!0,!!1,!!2>)
    IL_003e:  call       !!0[] [System.Linq]System.Linq.Enumerable::ToArray<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Item>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
    IL_0043:  ret
  } // end of method QueryExpressionInParameter::ExtractItems

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method QueryExpressionInParameter::.ctor

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.QueryExpressionInParameter

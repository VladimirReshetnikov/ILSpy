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
.assembly OrderByThenByRangeVariable
{
  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56 65 72 73 69 6F 6E 3D 76 39 2E 30 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module OrderByThenByRangeVariable.dll

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable
       extends [System.Runtime]System.Object
{
  .class auto ansi nested public beforefieldinit Category
         extends [System.Runtime]System.Object
  {
    .field public int32 OrderNum
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Category::.ctor

  } // end of class Category

  .class auto ansi nested public beforefieldinit Record
         extends [System.Runtime]System.Object
  {
    .field public class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category Group
    .field public class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category Language
    .field public string Name
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Record::.ctor

  } // end of class Record

  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .field public static initonly class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c' '<>9'
    .field public static class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> '<>9__2_0'
    .field public static class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> '<>9__2_1'
    .field public static class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,string> '<>9__2_2'
    .method private hidebysig specialname rtspecialname static
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  newobj     instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::.ctor()
      IL_0005:  stsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9'
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

    .method assembly hidebysig instance int32
            '<Sort>b__2_0'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record it) cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldfld      class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record::Group
      IL_0006:  ldfld      int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category::OrderNum
      IL_000b:  ret
    } // end of method '<>c'::'<Sort>b__2_0'

    .method assembly hidebysig instance int32
            '<Sort>b__2_1'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record it) cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldfld      class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record::Language
      IL_0006:  ldfld      int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Category::OrderNum
      IL_000b:  ret
    } // end of method '<>c'::'<Sort>b__2_1'

    .method assembly hidebysig instance string
            '<Sort>b__2_2'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record g) cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldfld      string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record::Name
      IL_0006:  ret
    } // end of method '<>c'::'<Sort>b__2_2'

  } // end of class '<>c'

  .method public hidebysig instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<string>
          Sort(class [System.Runtime]System.Collections.Generic.IEnumerable`1<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record> records) cil managed
  {
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  ldsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_0'
    IL_0006:  dup
    IL_0007:  brtrue.s   IL_0020

    IL_0009:  pop
    IL_000a:  ldsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9'
    IL_000f:  ldftn      instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<Sort>b__2_0'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record)
    IL_0015:  newobj     instance void class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32>::.ctor(object,
                                                                                                                                                                                 native int)
    IL_001a:  dup
    IL_001b:  stsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_0'
    IL_0020:  call       class [System.Linq]System.Linq.IOrderedEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::OrderBy<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>,
                                                                                                                                                                                                                              class [System.Runtime]System.Func`2<!!0,!!1>)
    IL_0025:  ldsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_1'
    IL_002a:  dup
    IL_002b:  brtrue.s   IL_0044

    IL_002d:  pop
    IL_002e:  ldsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9'
    IL_0033:  ldftn      instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<Sort>b__2_1'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record)
    IL_0039:  newobj     instance void class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32>::.ctor(object,
                                                                                                                                                                                 native int)
    IL_003e:  dup
    IL_003f:  stsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_1'
    IL_0044:  call       class [System.Linq]System.Linq.IOrderedEnumerable`1<!!0> [System.Linq]System.Linq.Enumerable::ThenBy<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,int32>(class [System.Linq]System.Linq.IOrderedEnumerable`1<!!0>,
                                                                                                                                                                                                                             class [System.Runtime]System.Func`2<!!0,!!1>)
    IL_0049:  ldsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,string> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_2'
    IL_004e:  dup
    IL_004f:  brtrue.s   IL_0068

    IL_0051:  pop
    IL_0052:  ldsfld     class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c' ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9'
    IL_0057:  ldftn      instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<Sort>b__2_2'(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record)
    IL_005d:  newobj     instance void class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,string>::.ctor(object,
                                                                                                                                                                                  native int)
    IL_0062:  dup
    IL_0063:  stsfld     class [System.Runtime]System.Func`2<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,string> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/'<>c'::'<>9__2_2'
    IL_0068:  call       class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!1> [System.Linq]System.Linq.Enumerable::Select<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable/Record,string>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>,
                                                                                                                                                                                                                                         class [System.Runtime]System.Func`2<!!0,!!1>)
    IL_006d:  ret
  } // end of method OrderByThenByRangeVariable::Sort

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method OrderByThenByRangeVariable::.ctor

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.OrderByThenByRangeVariable

// A semi-auto-property backing field whose declaring type ALSO has a member literally named
// 'field'. The C# compiler never emits this shape (a 'field'-keyword property cannot coexist with a
// member named 'field' referenced in the accessor), but it must not be mis-reconstructed: rewriting
// the backing field to the bare 'field' keyword would re-bind the member access. IntroduceFieldKeyword
// must therefore keep the explicit backing field here.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly FieldKeywordCollision
{
  .ver 1:0:0:0
}
.module FieldKeywordCollision.dll

.class private auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision
       extends [mscorlib]System.Object
{
  .field public int32 'field'
  .field private int32 '<Prop>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig specialname instance int32 get_Prop() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::'<Prop>k__BackingField'
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::'field'
    add
    ret
  }

  .property instance int32 Prop()
  {
    .get instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::get_Prop()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

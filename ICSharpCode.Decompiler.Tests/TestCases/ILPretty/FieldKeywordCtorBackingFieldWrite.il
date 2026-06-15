// A semi-auto-property whose getter carries custom logic (so it is not foldable to a trivial
// auto-property) over the compiler-generated '<Text>k__BackingField', combined with a constructor
// that writes that backing field directly. Roslyn never emits a constructor 'stfld' to a
// 'field'-keyword backing field, so this shape is hand-written.
//
// The backing field is hidden as a property backing field, yet neither absorbing transform applies:
// the getter's custom logic blocks folding to an auto-property, and the constructor write is a
// reference outside the accessors, so IntroduceFieldKeyword cannot use the 'field' keyword. The
// hidden field must therefore be re-materialized as an ordinary explicit field (de-mangled, with the
// compiler-generated attributes stripped) so the getter and constructor references still resolve.
// The de-mangled name 'Text' collides with the property, so FixNameCollisions renames it to 'm_Text'.
//
// The type is generic: in a generic type the hidden backing field is not re-emitted on demand, so a
// leaked reference would otherwise dangle (CS0103). This mirrors the JetBrains dotPeek TextualMatcher.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly FieldKeywordCtorBackingFieldWrite
{
  .ver 1:0:0:0
}
.module FieldKeywordCtorBackingFieldWrite.dll

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<T>
       extends [mscorlib]System.Object
{
  .field private string '<Text>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .field public initonly string Fallback

  .method public hidebysig specialname instance string get_Text() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::'<Text>k__BackingField'
    dup
    brtrue.s RET
    pop
    ldarg.0
    ldfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::Fallback
  RET:
    ret
  }

  .property instance string Text()
  {
    .get instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1::get_Text()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor(string customText, string fallback) cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    ldarg.2
    stfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::Fallback
    ldarg.0
    ldarg.1
    stfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::'<Text>k__BackingField'
    ret
  }
}

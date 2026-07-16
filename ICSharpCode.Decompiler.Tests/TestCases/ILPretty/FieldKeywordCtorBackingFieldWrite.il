// A getter-only semi-auto-property may be assigned in the matching constructor. The constructor
// store to '<Text>k__BackingField' can therefore become an assignment to Text while accessor reads
// become 'field'. A write from an ordinary method is not representable that way, so the backing
// field for ExternallyWritten must still be re-materialized as an ordinary explicit field.
//
// The type is generic to ensure both absorbed and surviving references are matched through their
// specialized field instances as well as their member definitions.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern System.Runtime
{
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
  .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.AllowNullAttribute::.ctor() = ( 01 00 00 00 )
  .field private string '<ExternallyWritten>k__BackingField'
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

  .method public hidebysig specialname instance string get_ExternallyWritten() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::'<ExternallyWritten>k__BackingField'
    dup
    brtrue.s EXTERNAL_RET
    pop
    ldarg.0
    ldfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::Fallback
  EXTERNAL_RET:
    ret
  }

  .property instance string ExternallyWritten()
  {
    .get instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1::get_ExternallyWritten()
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

  .method public hidebysig instance void SetExternallyWritten(string 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld string class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCtorBackingFieldWrite`1<!T>::'<ExternallyWritten>k__BackingField'
    ret
  }
}

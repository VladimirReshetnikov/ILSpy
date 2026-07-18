// Roslyn normally synthesizes [DebuggerBrowsable(Never)] on an automatic-property backing
// field. However, it can suppress that synthesized field attribute when the property itself has
// DebuggerBrowsableAttribute. Preserve the original field attribute explicitly in that case.
// Cover both an ordinary auto-property and a C# 14 semi-auto-property using contextual 'field'.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly AutoPropertyBackingFieldDebuggerBrowsable
{
  .ver 1:0:0:0
}
.module AutoPropertyBackingFieldDebuggerBrowsable.dll

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable
       extends [mscorlib]System.Object
{
  .field private initonly string '<Key>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .field private int32 '<Adjusted>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .method public hidebysig specialname instance string get_Key() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::'<Key>k__BackingField'
    ret
  }

  .property instance string Key()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )
    .get instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::get_Key()
  }

  .method public hidebysig specialname instance int32 get_Adjusted() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::'<Adjusted>k__BackingField'
    ldc.i4.1
    add
    ret
  }

  .method public hidebysig specialname instance void set_Adjusted(int32 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::'<Adjusted>k__BackingField'
    ret
  }

  .property instance int32 Adjusted()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 03 00 00 00 00 00 )
    .get instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::get_Adjusted()
    .set instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::set_Adjusted(int32)
  }

  .method public hidebysig specialname rtspecialname instance void .ctor(string key) cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    ldarg.1
    stfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsable::'<Key>k__BackingField'
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsableGeneric`1<T>
       extends [mscorlib]System.Object
{
  .field private initonly !T '<Value>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .method public hidebysig specialname instance !T get_Value() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld !0 class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsableGeneric`1<!T>::'<Value>k__BackingField'
    ret
  }

  .property instance !T Value()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 03 00 00 00 00 00 )
    .get instance !0 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsableGeneric`1::get_Value()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor(!T 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    ldarg.1
    stfld !0 class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AutoPropertyBackingFieldDebuggerBrowsableGeneric`1<!T>::'<Value>k__BackingField'
    ret
  }
}

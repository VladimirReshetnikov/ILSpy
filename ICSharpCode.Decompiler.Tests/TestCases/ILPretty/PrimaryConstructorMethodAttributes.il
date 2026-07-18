// Attributes on a constructor must be retargeted to [method: ...] when the constructor is folded
// into C# primary-constructor syntax. Cover both class and struct primary constructors.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly PrimaryConstructorMethodAttributes
{
  .ver 1:0:0:0
}
.module PrimaryConstructorMethodAttributes.dll

.class private auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 20 00 00 00 00 00 )

  .field private initonly string '<Value>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor(string 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Attribute::.ctor()
    ldarg.0
    ldarg.1
    stfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute::'<Value>k__BackingField'
    ret
  }

  .method public hidebysig specialname instance string get_Value() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute::'<Value>k__BackingField'
    ret
  }

  .property instance string Value()
  {
    .get instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute::get_Value()
  }
}

.class private auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorClass
       extends [mscorlib]System.Object
{
  .field private initonly string '<Value>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor(string 'value') cil managed
  {
    .custom instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute::.ctor(string) = ( 01 00 05 63 6C 61 73 73 00 00 )
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorClass::'<Value>k__BackingField'
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig specialname instance string get_Value() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorClass::'<Value>k__BackingField'
    ret
  }

  .property instance string Value()
  {
    .get instance string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorClass::get_Value()
  }
}

.class private sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorStruct
       extends [mscorlib]System.ValueType
{
  .field private initonly int32 '<Value>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) = ( 01 00 00 00 00 00 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor(int32 'value') cil managed
  {
    .custom instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ConstructorMarkerAttribute::.ctor(string) = ( 01 00 06 73 74 72 75 63 74 00 00 )
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorStruct::'<Value>k__BackingField'
    ret
  }

  .method public hidebysig specialname instance int32 get_Value() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorStruct::'<Value>k__BackingField'
    ret
  }

  .property instance int32 Value()
  {
    .get instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AttributedPrimaryConstructorStruct::get_Value()
  }
}

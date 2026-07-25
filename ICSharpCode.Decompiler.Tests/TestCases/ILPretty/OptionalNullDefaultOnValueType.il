.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly OptionalNullDefaultOnValueType
{
  .ver 1:0:0:0
}
.module OptionalNullDefaultOnValueType.dll

.class public sequential ansi sealed beforefieldinit OptionalHandle
       extends [mscorlib]System.ValueType
{
  .field public int32 Value
}

// Visual Basic writes an optional parameter's default as a null constant even where the parameter
// is a value type. C# requires the DefaultParameterValue argument to match the parameter type
// (CS1908), so a null against a non-nullable value type cannot be written at all.
.class public auto ansi beforefieldinit OptionalDefaults
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void OutStruct([out] [opt] valuetype OptionalHandle& h) cil managed
  {
    .param [1] = nullref
    ldarg.1
    initobj OptionalHandle
    ret
  }

  .method public hidebysig instance void ByValueStruct([opt] valuetype OptionalHandle h) cil managed
  {
    .param [1] = nullref
    ret
  }

  .method public hidebysig instance void OutReference([out] [opt] string& s) cil managed
  {
    .param [1] = nullref
    ldarg.1
    ldnull
    stind.ref
    ret
  }

  .method public hidebysig instance void ByValueInt([opt] int32 i) cil managed
  {
    .param [1] = int32(3)
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

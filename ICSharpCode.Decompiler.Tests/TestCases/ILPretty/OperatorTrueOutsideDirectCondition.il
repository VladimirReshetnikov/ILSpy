#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly OperatorTrueOutsideDirectCondition { }
.module OperatorTrueOutsideDirectCondition.dll

.class public sequential ansi sealed beforefieldinit TruthValue
       extends [CORE_ASSEMBLY]System.ValueType
{
  .field private initonly int32 state

  .method public hidebysig specialname rtspecialname instance void .ctor(int32 state) cil managed
  {
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld int32 TruthValue::state
    ret
  }

  .method public hidebysig specialname static bool op_True(valuetype TruthValue 'value') cil managed
  {
    .maxstack 8
    ldarga.s 'value'
    ldfld int32 TruthValue::state
    ldc.i4.1
    ceq
    ret
  }

  .method public hidebysig specialname static bool op_False(valuetype TruthValue 'value') cil managed
  {
    .maxstack 8
    ldarga.s 'value'
    ldfld int32 TruthValue::state
    ldc.i4.0
    ceq
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit Conditions
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static bool IsTrue(valuetype TruthValue 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call bool TruthValue::op_True(valuetype TruthValue)
    ret
  }

  .method public hidebysig static bool IsOrdinaryTrue(int32 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call bool OrdinaryMethods::op_True(int32)
    ret
  }

  .method public hidebysig static void RunWhenTrue(valuetype TruthValue 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call bool TruthValue::op_True(valuetype TruthValue)
    brfalse.s IL_0011

    ldstr "true"
    call void [CORE_ASSEMBLY]System.Console::WriteLine(string)

    IL_0011: ret
  }

  .method public hidebysig static void RunWhenNotTrue(valuetype TruthValue 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    call bool TruthValue::op_True(valuetype TruthValue)
    brtrue.s IL_0011

    ldstr "not true"
    call void [CORE_ASSEMBLY]System.Console::WriteLine(string)

    IL_0011: ret
  }

  .method public hidebysig static void RunWhenNotTrueAndEnabled(valuetype TruthValue 'value', bool enabled) cil managed
  {
    .maxstack 8
    ldarg.0
    call bool TruthValue::op_True(valuetype TruthValue)
    brtrue.s IL_0018

    ldarg.1
    brfalse.s IL_0018

    ldstr "not true and enabled"
    call void [CORE_ASSEMBLY]System.Console::WriteLine(string)

    IL_0018: ret
  }
}

.class public abstract auto ansi sealed beforefieldinit OrdinaryMethods
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static bool op_True(int32 'value') cil managed
  {
    .maxstack 8
    ldarg.0
    ldc.i4.0
    cgt
    ret
  }
}

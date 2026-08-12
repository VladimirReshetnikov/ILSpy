.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly SkipInitUnavailable
{
  .ver 1:0:0:0
}
.module SkipInitUnavailable.dll

// '.locals' without 'init' plus a use of the initial value normally makes the decompiler restore
// definite assignment with 'Unsafe.SkipInit(out T local)' (and bind an unassigned ref local via
// 'Unsafe.NullRef<T>()'). This assembly only references the .NET Framework 4.0 mscorlib, where
// System.Runtime.CompilerServices.Unsafe does not exist, so those spellings would not compile
// (CS0234). When the API cannot be resolved the declaration must fall back to a zero
// initializer ('T local = default(T);') and the ref local to a placeholder local of the element
// type. Zeroing is not observable in the recompiled code: the local is written before every
// read on the paths the original compiler considered, and a CLR running '.locals init' bodies
// zeroes the slot anyway.
.class public auto ansi beforefieldinit SkipInitUnavailable
       extends [mscorlib]System.Object
{
  // Plain value-type local: read at the loop head before the first store.
  .method public hidebysig static void Loop(int32 n) cil managed
  {
    .locals (
      [0] int32 current,
      [1] int32 i
    )
    ldc.i4.0
    stloc.1
    br.s IL_test

  IL_body:
    ldloc.0
    call void [mscorlib]System.Console::Write(int32)
    ldloc.1
    stloc.0
    ldloc.1
    ldc.i4.1
    add
    stloc.1

  IL_test:
    ldloc.1
    ldarg.0
    blt.s IL_body
    ret
  }

  // Generic local: only assigned inside the loop, read after it, so the initial value is
  // observable when the loop runs zero times.
  .method public hidebysig static !!T LoopGeneric<T>(!!T 'value', int32 count) cil managed
  {
    .maxstack 2
    .locals (
      [0] !!T,
      [1] int32
    )
    ldc.i4.0
    stloc.1
    br.s IL_test

  IL_body:
    ldarg.0
    stloc.0
    ldloc.1
    ldc.i4.1
    add
    stloc.1

  IL_test:
    ldloc.1
    ldarg.1
    blt.s IL_body

    ldloc.0
    ret
  }

  // Unassigned managed-pointer local: dereferenced at the loop head before the first store.
  .method public hidebysig static int32 LoopRef(int32 'value', int32 count) cil managed
  {
    .maxstack 3
    .locals (
      [0] int32&,
      [1] int32,
      [2] int32
    )
    ldc.i4.0
    stloc.2
    br.s IL_test

  IL_body:
    ldloc.0
    ldind.i4
    stloc.1
    ldarga.s 'value'
    stloc.0
    ldloc.2
    ldc.i4.1
    add
    stloc.2

  IL_test:
    ldloc.2
    ldarg.1
    blt.s IL_body

    ldloc.1
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

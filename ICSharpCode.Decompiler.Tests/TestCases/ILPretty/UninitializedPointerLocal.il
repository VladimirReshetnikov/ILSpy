.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly UninitializedPointerLocal
{
  .ver 1:0:0:0
}
.module UninitializedPointerLocal.dll

// '.locals' without 'init' means the locals are not zero-initialized, which is what makes the
// decompiler reach for Unsafe.SkipInit when a declaration has to move above a back edge. A pointer
// cannot be a generic type argument, so SkipInit<T> has no spelling for one (CS0306).
.class public auto ansi beforefieldinit UninitializedPointer
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Loop(int32 n, uint8* source) cil managed
  {
    .locals (
      [0] uint8* p,
      [1] int32 i
    )
    ldc.i4.0
    stloc.1
    br.s IL_test

  IL_body:
    ldloc.0
    ldind.u1
    call void [mscorlib]System.Console::Write(int32)
    ldarg.1
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

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

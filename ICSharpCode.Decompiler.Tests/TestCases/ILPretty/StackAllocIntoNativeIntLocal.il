.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly StackAllocIntoNativeIntLocal
{
  .ver 1:0:0:0
}
.module StackAllocIntoNativeIntLocal.dll

.class public auto ansi beforefieldinit StackAllocTarget
       extends [mscorlib]System.Object
{
  // The buffer local is a native int rather than a pointer, and it is assigned in more than one
  // place, so its declaration is hoisted above the stackalloc. C# has no cast from a stackalloc
  // (CS8346), so the allocation has to become the initializer of a pointer local of its own.
  .method public hidebysig static void Fill(int32 flag) cil managed
  {
    .maxstack 4
    .locals init (
      [0] native int buffer
    )
    ldc.i4.0
    conv.i
    stloc.0
    ldarg.0
    brfalse.s IL_after

    ldc.i4.4
    sizeof [mscorlib]System.Int32
    mul
    localloc
    stloc.0
    ldloc.0
    ldc.i4.7
    stind.i4

  IL_after:
    ldloc.0
    ldind.i4
    call void [mscorlib]System.Console::Write(int32)
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

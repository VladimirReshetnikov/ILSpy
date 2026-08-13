.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly ConstructorHelperMethod { }
.module ConstructorHelperMethod.dll

// A chained base-constructor call whose argument is computed by loops: no expression can carry
// the computation, so the decompiler synthesizes a private static helper method holding the
// displaced statements and calls it inside the base(...) initializer.
.class public auto ansi beforefieldinit HelperBase
       extends [mscorlib]System.Object
{
  .field public string Tag
  .field public int32[] Data
  .field public bool Flag

  .method public hidebysig specialname rtspecialname instance void
          .ctor(string tag, int32[] data, bool flag) cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    ldarg.1
    stfld string HelperBase::Tag
    ldarg.0
    ldarg.2
    stfld int32[] HelperBase::Data
    ldarg.0
    ldarg.3
    stfld bool HelperBase::Flag
    ret
  }
}

.class public auto ansi beforefieldinit HelperDerived
       extends HelperBase
{
  .method public hidebysig specialname rtspecialname instance void
          .ctor(int32[] items, string tag) cil managed
  {
    .maxstack 3
    .locals init (
      [0] class [mscorlib]System.Collections.Generic.List`1<int32> list,
      [1] int32 i,
      [2] int32 j
    )

    newobj instance void class [mscorlib]System.Collections.Generic.List`1<int32>::.ctor()
    stloc.0
    ldc.i4.0
    stloc.1
    br.s IL_FirstCond

    IL_FirstLoop:
    ldloc.0
    ldarg.1
    ldloc.1
    ldelem.i4
    callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
    ldloc.1
    ldc.i4.1
    add
    stloc.1

    IL_FirstCond:
    ldloc.1
    ldarg.1
    ldlen
    conv.i4
    blt.s IL_FirstLoop

    ldc.i4.0
    stloc.2
    br.s IL_SecondCond

    IL_SecondLoop:
    ldloc.0
    ldloc.2
    callvirt instance void class [mscorlib]System.Collections.Generic.List`1<int32>::Add(!0)
    ldloc.2
    ldc.i4.2
    add
    stloc.2

    IL_SecondCond:
    ldloc.2
    ldc.i4.8
    blt.s IL_SecondLoop

    ldarg.0
    ldarg.2
    ldloc.0
    callvirt instance !0[] class [mscorlib]System.Collections.Generic.List`1<int32>::ToArray()
    ldc.i4.1
    call instance void HelperBase::.ctor(string, int32[], bool)
    ret
  }
}

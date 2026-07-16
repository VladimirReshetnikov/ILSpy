// A switch whose arms all produce the same value type has that natural type even when the
// evaluation-stack category is too coarse to identify it. The local must remain a ValueTuple;
// treating it as object would make the subsequent field loads reinterpret an object-reference
// slot as tuple storage.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 4:0:0:0
}
.assembly SwitchTupleNaturalType
{
  .ver 1:0:0:0
}
.module SwitchTupleNaturalType.dll

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.SwitchTupleNaturalType
       extends [mscorlib]System.Object
{
  .method public hidebysig static float64 Sum(int32 kind, float64 first, float64 second) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64> tuple,
      [1] float64 item,
      [2] float64 item2,
      [3] float64 item3
    )

    ldarg.0
    switch (CASE_ZERO, CASE_ONE, CASE_TWO, CASE_THREE, CASE_FOUR, CASE_FIVE)
    br DEFAULT_CASE

  CASE_ZERO:
    ldarg.1
    ldarg.2
    ldc.r8 0.0
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  CASE_ONE:
    ldarg.2
    ldarg.1
    ldc.r8 0.0
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  CASE_TWO:
    ldc.r8 0.0
    ldarg.1
    ldarg.2
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  CASE_THREE:
    ldc.r8 0.0
    ldarg.2
    ldarg.1
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  CASE_FOUR:
    ldarg.2
    ldc.r8 0.0
    ldarg.1
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  CASE_FIVE:
    ldarg.1
    ldc.r8 0.0
    ldarg.2
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0
    br JOIN

  DEFAULT_CASE:
    ldc.r8 0.0
    ldc.r8 0.0
    ldc.r8 0.0
    newobj instance void valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::.ctor(!0, !1, !2)
    stloc.0

  JOIN:
    ldloc.0
    dup
    ldfld !0 valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::Item1
    stloc.1
    dup
    ldfld !1 valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::Item2
    stloc.2
    ldfld !2 valuetype [mscorlib]System.ValueTuple`3<float64, float64, float64>::Item3
    stloc.3
    ldloc.1
    ldc.r8 1.0
    add
    stloc.1
    ldloc.2
    ldc.r8 1.0
    add
    stloc.2
    ldloc.3
    ldc.r8 1.0
    add
    stloc.3
    ldloc.1
    ldloc.2
    add
    ldloc.3
    add
    ret
  }
}

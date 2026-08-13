.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}

.assembly ModernRefSafetyHoistedInArgument { }
.module ModernRefSafetyHoistedInArgument.dll
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = (01 00 0B 00 00 00 00 00)

// Under modern ref-safety rules an unannotated 'in char' parameter of a span-returning method may
// escape into the returned span, so a call that feeds it a compiler temporary and assigns the
// result to a local declared in an outer scope does not recompile (CS8347). The temporary has to
// survive as a local declared in the destination's scope. The declaration-position call keeps its
// literal: initializing a fresh local narrows that local's own scope instead.
.class public abstract auto ansi sealed beforefieldinit StringExtensions
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (01 00 00 00)

  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<char>
          SliceOffAfterLast([in] valuetype [System.Runtime]System.ReadOnlySpan`1<char>& span, [in] char& delimiter) cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (01 00 00 00)
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .param [2]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 8

    ldarg.0
    ldc.i4.0
    call instance valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.ReadOnlySpan`1<char>::Slice(int32)
    ret
  }

  .method public hidebysig static void Walk(string text) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [System.Runtime]System.ReadOnlySpan`1<char> span,
      [1] char delimiter1,
      [2] valuetype [System.Runtime]System.ReadOnlySpan`1<char> source,
      [3] char delimiter2
    )

    ldarg.0
    call valuetype [System.Runtime]System.ReadOnlySpan`1<char> [System.Runtime]System.MemoryExtensions::AsSpan(string)
    stloc.2
    ldloca.s 2
    ldc.i4.s 46
    stloc.1
    ldloca.s 1
    call valuetype [System.Runtime]System.ReadOnlySpan`1<char> StringExtensions::SliceOffAfterLast(valuetype [System.Runtime]System.ReadOnlySpan`1<char>&, char&)
    stloc.0
    br.s IL_Cond

    IL_Loop:
    ldloca.s 0
    ldc.i4.s 46
    stloc.3
    ldloca.s 3
    call valuetype [System.Runtime]System.ReadOnlySpan`1<char> StringExtensions::SliceOffAfterLast(valuetype [System.Runtime]System.ReadOnlySpan`1<char>&, char&)
    stloc.0

    IL_Cond:
    ldloca.s 0
    call instance int32 valuetype [System.Runtime]System.ReadOnlySpan`1<char>::get_Length()
    ldc.i4.0
    bgt.s IL_Loop

    ret
  }
}

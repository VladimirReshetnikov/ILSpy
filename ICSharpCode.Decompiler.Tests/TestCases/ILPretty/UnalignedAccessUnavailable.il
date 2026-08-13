.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly UnalignedAccessUnavailable
{
  .ver 1:0:0:0
}
.module UnalignedAccessUnavailable.dll

// An 'unaligned.' memory access normally decompiles to Unsafe.ReadUnaligned/WriteUnaligned so
// the recompiled IL keeps the unaligned guarantee. This assembly only references the .NET
// Framework 4.0 mscorlib, where System.Runtime.CompilerServices.Unsafe does not exist, so those
// spellings would not compile (CS0234). When the API cannot be resolved the access must fall
// back to the plain dereference the target framework can express. The recompiled IL then lacks
// the 'unaligned.' prefix; that only matters on strict-alignment platforms, and the framework
// this assembly targets has no way to spell the prefix at all.
.class public auto ansi beforefieldinit UnalignedAccessUnavailable
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 ReadInt32(uint8* p) cil managed
  {
    .maxstack 1
    ldarg.0
    unaligned. 1
    ldobj int32
    ret
  }

  .method public hidebysig static void WriteInt32(uint8* p, int32 'value') cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    unaligned. 1
    stobj int32
    ret
  }

  .method public hidebysig static int64 ReadInt64(int64& r) cil managed
  {
    .maxstack 1
    ldarg.0
    unaligned. 1
    ldobj int64
    ret
  }

  .method public hidebysig static void WriteInt64(int64& r, int64 'value') cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    unaligned. 1
    stobj int64
    ret
  }
}

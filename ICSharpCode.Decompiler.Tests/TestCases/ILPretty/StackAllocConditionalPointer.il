// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 9:0:0:0
}
.assembly StackAllocConditionalPointer
{
  .ver 1:0:0:0
}
.module StackAllocConditionalPointer.dll
.corflags 0x00000001    //  ILONLY

// A pointer that receives its buffer from 'stackalloc' in one branch and from another source in the
// other is assigned in more than one place, so it is declared separately from its stores. The
// stackalloc must still be emitted as the initializer of its own pointer local ('char* p =
// stackalloc ...; ptr = p;'): a stackalloc in an assignment or conditional-operand position is
// typed Span<char>, which does not convert to char* (CS8346).

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.StackAllocConditionalPointer
       extends [System.Runtime]System.Object
{
  .method public hidebysig static void Consume(char* p) cil managed
  {
    .maxstack  8
    ret
  }

  .method public hidebysig static char* Heap(int32 n) cil managed
  {
    .maxstack  8
    ldc.i4.0
    conv.u
    ret
  }

  .method public hidebysig static void M(bool cond,
                                         int32 cchLength) cil managed
  {
    .maxstack  2
    .locals init (char* V_0)
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_000e
    IL_0003:  ldarg.1
    IL_0004:  ldc.i4.1
    IL_0005:  add
    IL_0006:  conv.u
    IL_0007:  ldc.i4.2
    IL_0008:  mul.ovf.un
    IL_0009:  localloc
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0015
    IL_000e:  ldarg.1
    IL_000f:  call       char* ICSharpCode.Decompiler.Tests.TestCases.ILPretty.StackAllocConditionalPointer::Heap(int32)
    IL_0014:  stloc.0
    IL_0015:  ldloc.0
    IL_0016:  call       void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.StackAllocConditionalPointer::Consume(char*)
    IL_001b:  ret
  }

  // SplitVariables gives the null initialization and stackalloc store separate SSA variables,
  // but both still refer to local slot V_0. They may later be merged into one C# variable name,
  // so the stackalloc store must be redirected into its own declaration initializer.
  .method public hidebysig static void DefaultInitialized(int32 cchLength) cil managed
  {
    .maxstack  2
    .locals init (char* V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  conv.u
    IL_0002:  stloc.0
    IL_0003:  ldarg.0
    IL_0004:  ldc.i4.1
    IL_0005:  add
    IL_0006:  conv.u
    IL_0007:  ldc.i4.2
    IL_0008:  mul.ovf.un
    IL_0009:  localloc
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  call       void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.StackAllocConditionalPointer::Consume(char*)
    IL_0012:  ret
  }
}

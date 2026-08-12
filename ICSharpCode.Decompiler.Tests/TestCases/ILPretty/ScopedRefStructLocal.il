// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 9:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 9:0:0:0
}
.assembly extern System.Memory
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 9:0:0:0
}
.assembly ScopedRefStructLocal
{
  .ver 1:0:0:0
}
.module ScopedRefStructLocal.dll
.corflags 0x00000001    //  ILONLY

// A ref-struct (Span<byte>) local is assigned in both branches of an if/else whose then-branch
// has a side effect (so the assignment cannot be folded into a conditional initializer). The
// hoisted declaration therefore falls through to the bare form. When every use of the local stays
// inside the method and the assigned value is stack-referring, the decompiler must emit 'scoped'
// to keep the narrow assignments legal (CS8352/CS8353).
//
// 'Confined' assigns a direct stackalloc; the others assign values only *derived* from a stackalloc
// (a slice of one, a copy of a stackalloc local, or a helper that slices a stackalloc buffer
// argument) - all still require 'scoped'. The stack-derived locals are also exercised against sinks
// that the escape check must recognize as confining: a discarded ref-struct-returning call, and a
// copy into another confined local. 'Escaping' returns its local, and 'WideArraySpanStaysBare'
// assigns a value backed by an array parameter (not stack-referring), so both must stay bare.
// 'SwapInitializedSpans' copies stackalloc-initialized locals through a default-initialized
// temporary. The temporary must be scoped even though the stack-referring source appears in a
// variable initializer rather than a later assignment. The two locals declared directly from
// stackalloc stay bare because those initializers already establish method scope.

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Confined(bool c,
                                                  int32 n) cil managed
  {
    .maxstack  2
    .locals init (valuetype [System.Runtime]System.Span`1<uint8> V_0)
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_001c

    IL_0003:  ldstr      "side effect"
    IL_0008:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000d:  ldc.i4.s   16
    IL_000f:  conv.u
    IL_0010:  localloc
    IL_0012:  ldc.i4.s   16
    IL_0014:  newobj     instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*,
                                                                                             int32)
    IL_0019:  stloc.0
    IL_001a:  br.s       IL_0027

    IL_001c:  ldc.i4.8
    IL_001d:  conv.u
    IL_001e:  localloc
    IL_0020:  ldc.i4.8
    IL_0021:  newobj     instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*,
                                                                                             int32)
    IL_0026:  stloc.0
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldc.i4.0
    IL_002a:  call       instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
    IL_002f:  ldarg.1
    IL_0030:  conv.u1
    IL_0031:  stind.i1
    IL_0032:  ldloca.s   V_0
    IL_0034:  ldc.i4.0
    IL_0035:  call       instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
    IL_003a:  ldind.u1
    IL_003b:  ldloca.s   V_0
    IL_003d:  call       instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
    IL_0042:  add
    IL_0043:  ret
  } // end of method ScopedRefStructLocal::Confined

  .method public hidebysig static valuetype [System.Runtime]System.Span`1<uint8>
          Escaping(bool c,
                   int32 n) cil managed
  {
    .maxstack  2
    .locals init (valuetype [System.Runtime]System.Span`1<uint8> V_0)
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_001c

    IL_0003:  ldstr      "side effect"
    IL_0008:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000d:  ldc.i4.s   16
    IL_000f:  conv.u
    IL_0010:  localloc
    IL_0012:  ldc.i4.s   16
    IL_0014:  newobj     instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*,
                                                                                             int32)
    IL_0019:  stloc.0
    IL_001a:  br.s       IL_0027

    IL_001c:  ldc.i4.8
    IL_001d:  conv.u
    IL_001e:  localloc
    IL_0020:  ldc.i4.8
    IL_0021:  newobj     instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*,
                                                                                             int32)
    IL_0026:  stloc.0
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldc.i4.0
    IL_002a:  call       instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
    IL_002f:  ldarg.1
    IL_0030:  conv.u1
    IL_0031:  stind.i1
    IL_0032:  ldloc.0
    IL_0033:  ret
  } // end of method ScopedRefStructLocal::Escaping

  .method public hidebysig static int32 DerivedFromSliceAndHelper(bool c,
                                                                  int32 n) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [System.Runtime]System.Span`1<uint8>,
      [1] valuetype [System.Runtime]System.Span`1<uint8>
    )
    IL_0000: ldc.i4.s 16
    IL_0002: conv.u
    IL_0003: localloc
    IL_0005: ldc.i4.s 16
    IL_0007: newobj instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*, int32)
    IL_000c: stloc.0
    IL_000d: ldarg.0
    IL_000e: brfalse.s IL_0026

    IL_0010: ldstr "side effect"
    IL_0015: call void [System.Console]System.Console::WriteLine(string)
    IL_001a: ldloca.s 0
    IL_001c: ldc.i4.1
    IL_001d: ldc.i4.4
    IL_001e: call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::Slice(int32, int32)
    IL_0023: stloc.1
    IL_0024: br.s IL_002d

    IL_0026: ldloc.0
    IL_0027: call valuetype [System.Runtime]System.Span`1<uint8> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal::Wrap(valuetype [System.Runtime]System.Span`1<uint8>)
    IL_002c: stloc.1

    IL_002d: ldloca.s 1
    IL_002f: ldc.i4.0
    IL_0030: call instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
    IL_0035: ldarg.1
    IL_0036: conv.u1
    IL_0037: stind.i1
    IL_0038: ldloca.s 1
    IL_003a: call instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
    IL_003f: ret
  } // end of method ScopedRefStructLocal::DerivedFromSliceAndHelper

  .method public hidebysig static int32 DiscardedRefStructSink(bool c,
                                                               int32 n) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [System.Runtime]System.Span`1<uint8>,
      [1] valuetype [System.Runtime]System.Span`1<uint8>
    )
    IL_0000: ldc.i4.s 16
    IL_0002: conv.u
    IL_0003: localloc
    IL_0005: ldc.i4.s 16
    IL_0007: newobj instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*, int32)
    IL_000c: stloc.0
    IL_000d: ldarg.0
    IL_000e: brfalse.s IL_0026

    IL_0010: ldstr "side effect"
    IL_0015: call void [System.Console]System.Console::WriteLine(string)
    IL_001a: ldloca.s 0
    IL_001c: ldc.i4.1
    IL_001d: ldc.i4.4
    IL_001e: call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::Slice(int32, int32)
    IL_0023: stloc.1
    IL_0024: br.s IL_0028

    IL_0026: ldloc.0
    IL_0027: stloc.1

    IL_0028: ldloc.1
    IL_0029: call valuetype [System.Runtime]System.Span`1<uint8> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal::Echo(valuetype [System.Runtime]System.Span`1<uint8>)
    IL_002e: pop
    IL_002f: ldarg.1
    IL_0030: ret
  } // end of method ScopedRefStructLocal::DiscardedRefStructSink

  .method public hidebysig static int32 CopiedIntoConfinedLocal(bool c,
                                                                int32 n) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [System.Runtime]System.Span`1<uint8>,
      [1] valuetype [System.Runtime]System.Span`1<uint8>,
      [2] valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
    )
    IL_0000: ldc.i4.s 16
    IL_0002: conv.u
    IL_0003: localloc
    IL_0005: ldc.i4.s 16
    IL_0007: newobj instance void valuetype [System.Runtime]System.Span`1<uint8>::.ctor(void*, int32)
    IL_000c: stloc.0
    IL_000d: ldarg.0
    IL_000e: brfalse.s IL_0026

    IL_0010: ldstr "side effect"
    IL_0015: call void [System.Console]System.Console::WriteLine(string)
    IL_001a: ldloca.s 0
    IL_001c: ldc.i4.1
    IL_001d: ldc.i4.4
    IL_001e: call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::Slice(int32, int32)
    IL_0023: stloc.1
    IL_0024: br.s IL_0028

    IL_0026: ldloc.0
    IL_0027: stloc.1

    IL_0028: ldloc.1
    IL_0029: call valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::op_Implicit(valuetype [System.Runtime]System.Span`1<!0>)
    IL_002e: stloc.2
    IL_002f: ldloc.2
    IL_0030: call int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal::Count(valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>)
    IL_0035: ldloca.s 2
    IL_0037: call instance int32 valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>::get_Length()
    IL_003c: add
    IL_003d: ret
  } // end of method ScopedRefStructLocal::CopiedIntoConfinedLocal

  .method public hidebysig static int32 WideArraySpanStaysBare(bool c,
                                                               uint8[] 'array',
                                                               int32 n) cil managed
  {
    .maxstack 2
    .locals init (
      [0] valuetype [System.Runtime]System.Span`1<uint8>
    )
    IL_0000: ldarg.0
    IL_0001: brfalse.s IL_0017

    IL_0003: ldstr "side effect"
    IL_0008: call void [System.Console]System.Console::WriteLine(string)
    IL_000d: ldarg.1
    IL_000e: ldc.i4.1
    IL_000f: call valuetype [System.Runtime]System.Span`1<!!0> [System.Memory]System.MemoryExtensions::AsSpan<uint8>(!!0[], int32)
    IL_0014: stloc.0
    IL_0015: br.s IL_001e

    IL_0017: ldarg.1
    IL_0018: call valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::op_Implicit(!0[])
    IL_001d: stloc.0

    IL_001e: ldloca.s 0
    IL_0020: ldc.i4.0
    IL_0021: call instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
    IL_0026: ldarg.2
    IL_0027: conv.u1
    IL_0028: stind.i1
    IL_0029: ldloca.s 0
    IL_002b: call instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
    IL_0030: ret
  } // end of method ScopedRefStructLocal::WideArraySpanStaysBare

  .method public hidebysig static int32 SwapInitializedSpans(int32 n) cil managed
  {
    .maxstack 3
    .locals init (
      [0] valuetype [System.Runtime]System.Span`1<int32>,
      [1] valuetype [System.Runtime]System.Span`1<int32>,
      [2] valuetype [System.Runtime]System.Span`1<int32>,
      [3] valuetype [System.Runtime]System.Span`1<int32>
    )
    IL_0000: ldloca.s 0
    IL_0002: initobj valuetype [System.Runtime]System.Span`1<int32>
    IL_0008: ldc.i4.s 64
    IL_000a: conv.u
    IL_000b: localloc
    IL_000d: ldc.i4.s 16
    IL_000f: newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*, int32)
    IL_0014: stloc.3
    IL_0015: ldloc.3
    IL_0016: stloc.1
    IL_0017: ldc.i4.s 64
    IL_0019: conv.u
    IL_001a: localloc
    IL_001c: ldc.i4.s 16
    IL_001e: newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*, int32)
    IL_0023: stloc.3
    IL_0024: ldloc.3
    IL_0025: stloc.2
    IL_0026: ldloca.s 1
    IL_0028: ldc.i4.0
    IL_0029: call instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_002e: ldarg.0
    IL_002f: stind.i4
    IL_0030: ldloca.s 2
    IL_0032: ldc.i4.0
    IL_0033: call instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_0038: ldarg.0
    IL_0039: ldc.i4.1
    IL_003a: add
    IL_003b: stind.i4
    IL_003c: ldloc.2
    IL_003d: stloc.0
    IL_003e: ldloc.1
    IL_003f: stloc.2
    IL_0040: ldloc.0
    IL_0041: stloc.1
    IL_0042: ldloca.s 1
    IL_0044: ldc.i4.0
    IL_0045: call instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_004a: ldind.i4
    IL_004b: ldloca.s 2
    IL_004d: ldc.i4.0
    IL_004e: call instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_0053: ldind.i4
    IL_0054: add
    IL_0055: ret
  } // end of method ScopedRefStructLocal::SwapInitializedSpans

  .method private hidebysig static valuetype [System.Runtime]System.Span`1<uint8>
          Wrap(valuetype [System.Runtime]System.Span`1<uint8> b) cil managed
  {
    .maxstack 8
    IL_0000: ldarga.s b
    IL_0002: ldc.i4.0
    IL_0003: ldarga.s b
    IL_0005: call instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
    IL_000a: call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::Slice(int32, int32)
    IL_000f: ret
  } // end of method ScopedRefStructLocal::Wrap

  .method private hidebysig static valuetype [System.Runtime]System.Span`1<uint8>
          Echo(valuetype [System.Runtime]System.Span`1<uint8> s) cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: ret
  } // end of method ScopedRefStructLocal::Echo

  .method private hidebysig static int32 Count(valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> s) cil managed
  {
    .maxstack 8
    IL_0000: ldarga.s s
    IL_0002: call instance int32 valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>::get_Length()
    IL_0007: ret
  } // end of method ScopedRefStructLocal::Count

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal

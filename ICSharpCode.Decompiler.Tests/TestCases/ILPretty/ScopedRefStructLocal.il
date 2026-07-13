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
.assembly ScopedRefStructLocal
{
  .ver 1:0:0:0
}
.module ScopedRefStructLocal.dll
.corflags 0x00000001    //  ILONLY

// A ref-struct (Span<byte>) local is assigned a narrow-scoped stackalloc slice in both
// branches of an if/else whose then-branch has a side effect (so the assignment cannot be
// folded into a conditional initializer). The hoisted declaration therefore falls through
// to the bare form. In 'Confined' every use of the local stays inside the method, so the
// decompiler must emit 'scoped' to keep the narrow assignments legal (CS8352/CS8353). In
// 'Escaping' the local is returned, so it must stay bare - adding 'scoped' there would
// inject a new ref-safety error.

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

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ScopedRefStructLocal

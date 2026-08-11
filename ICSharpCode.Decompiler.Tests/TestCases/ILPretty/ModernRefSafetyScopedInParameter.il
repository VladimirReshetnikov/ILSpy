.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}

.assembly ModernRefSafetyScopedInParameter { }
.module ModernRefSafetyScopedInParameter.dll
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = (01 00 0B 00 00 00 00 00)

// A module carrying modern ref-safety metadata owns its source-level lifetime contracts. Even a
// safe-looking method without ScopedRefAttribute must not be rewritten by legacy inference.
.class public sequential ansi sealed beforefieldinit ModernSequence
       extends [System.Runtime]System.ValueType
{
  .field public uint8[] Data
}

.class public abstract auto ansi sealed beforefieldinit ModernRefSafetyScopedInParameter
       extends [System.Runtime]System.Object
{
  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
          GetSpan([in] valuetype ModernSequence& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 8

    ldarg.0
    ldfld uint8[] ModernSequence::Data
    call valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>::op_Implicit(!0[])
    ret
  }
}

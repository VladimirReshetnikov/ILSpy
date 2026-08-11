.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}

.assembly LegacyScopedInParameter { }
.module LegacyScopedInParameter.dll

// Legacy C# compilers did not describe ref-safe contexts in metadata. Recompiling a helper that
// returns a ref struct through an ordinary 'in' parameter makes calls with a temporary or property
// value fail with CS8156/CS8347, even when the method never lets the parameter's storage escape.
.class public sequential ansi sealed beforefieldinit LegacySequence
       extends [System.Runtime]System.ValueType
{
  .field private uint8[] data

  .method public hidebysig specialname rtspecialname
          instance void .ctor(uint8[] data) cil managed
  {
    .maxstack 8
    ldarg.0
    ldarg.1
    stfld uint8[] LegacySequence::data
    ret
  }

  .method public hidebysig specialname instance bool get_HasData() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld uint8[] LegacySequence::data
    ldnull
    cgt.un
    ret
  }

  .method public hidebysig specialname instance valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8> get_Memory() cil managed
  {
    .maxstack 8
    ldarg.0
    ldfld uint8[] LegacySequence::data
    call valuetype [System.Runtime]System.ReadOnlyMemory`1<!0> valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8>::op_Implicit(!0[])
    ret
  }

  .property instance bool HasData()
  {
    .get instance bool LegacySequence::get_HasData()
  }

  .property instance valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8> Memory()
  {
    .get instance valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8> LegacySequence::get_Memory()
  }
}

.class public abstract auto ansi sealed beforefieldinit LegacyUnsafe
       extends [System.Runtime]System.Object
{
  .method public hidebysig static !!T& AsRef<T>([in] !!T& source) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (01 00 00 00)
	.maxstack 1

	ldarg.0
	call !!0& [System.Runtime]System.Runtime.CompilerServices.Unsafe::AsRef<!!T>(!!0&)
	ret
  }
}

.class public abstract auto ansi sealed beforefieldinit LegacyScopedInParameter
       extends [System.Runtime]System.Object
{
  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
          GetSpan([in] valuetype LegacySequence& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 8
    .locals init (
      [0] valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8>
    )

    ldarg.0
    call instance bool LegacySequence::get_HasData()
    brfalse.s NO_DATA

    ldarg.0
    call instance valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8> LegacySequence::get_Memory()
    stloc.0
    ldloca.s 0
    call instance valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8>::get_Span()
    ret

  NO_DATA:
    ldarg.0
    call uint8[] LegacyScopedInParameter::Copy(valuetype LegacySequence&)
    call valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>::op_Implicit(!0[])
    ret
  }

  .method private hidebysig static uint8[] Copy([in] valuetype LegacySequence& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 8
    ldc.i4.0
    newarr [System.Runtime]System.Byte
    ret
  }

  .method private hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
          GetNullableSpan([in] valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 8
    .locals init (
      [0] valuetype LegacySequence,
      [1] valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
    )

    ldarg.0
    call instance bool valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>::get_HasValue()
    brtrue.s HAS_VALUE

    ldloca.s 1
    initobj valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
    ldloc.1
    ret

  HAS_VALUE:
    ldarg.0
    call instance !0 valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>::get_Value()
    stloc.0
    ldloca.s 0
    call valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> LegacyScopedInParameter::GetSpan(valuetype LegacySequence&)
    ret
  }

  .method private hidebysig static valuetype LegacySequence Create() cil managed
  {
    .maxstack 8
    ldc.i4.1
    newarr [System.Runtime]System.Byte
    newobj instance void LegacySequence::.ctor(uint8[])
    ret
  }

  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> FromTemporary() cil managed
  {
    .maxstack 8
    .locals init (
      [0] valuetype LegacySequence
    )

    call valuetype LegacySequence LegacyScopedInParameter::Create()
    stloc.0
    ldloca.s 0
    call valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> LegacyScopedInParameter::GetSpan(valuetype LegacySequence&)
    ret
  }

  .method public hidebysig specialname static valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence> get_NullableSequence() cil managed
  {
    .maxstack 8
    .locals init (
      [0] valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>
    )

    ldloca.s 0
    initobj valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>
    ldloc.0
    ret
  }

  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> FromNullableProperty() cil managed
  {
    .maxstack 8
    .locals init (
      [0] valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>
    )

    call valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence> LegacyScopedInParameter::get_NullableSequence()
    stloc.0
    ldloca.s 0
    call valuetype [System.Runtime]System.ReadOnlySpan`1<uint8> LegacyScopedInParameter::GetNullableSpan(valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence>&)
    ret
  }

  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<uint8>
          ExplicitlyUnscoped([in] valuetype LegacySequence& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (01 00 00 00)
    .maxstack 8
    .locals init (
      [0] valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8>
    )

    ldarg.0
    call instance valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8> LegacySequence::get_Memory()
    stloc.0
    ldloca.s 0
    call instance valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.ReadOnlyMemory`1<uint8>::get_Span()
    ret
  }

  // Unlike the two inferred helpers, this unannotated parameter storage flows through a byref
  // helper into the returned span. It must remain plain 'in'.
  .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<valuetype LegacySequence>
          StorageEscapes([in] valuetype LegacySequence& sequence) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (01 00 00 00)
    .maxstack 2

    ldarg.0
    call !!0& LegacyUnsafe::AsRef<valuetype LegacySequence>(!!0&)
    ldc.i4.1
    call valuetype [System.Runtime]System.ReadOnlySpan`1<!!0> [System.Runtime]System.Runtime.InteropServices.MemoryMarshal::CreateReadOnlySpan<valuetype LegacySequence>(!!0&, int32)
    ret
  }

  .property valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence> NullableSequence()
  {
    .get valuetype [System.Runtime]System.Nullable`1<valuetype LegacySequence> LegacyScopedInParameter::get_NullableSequence()
  }
}

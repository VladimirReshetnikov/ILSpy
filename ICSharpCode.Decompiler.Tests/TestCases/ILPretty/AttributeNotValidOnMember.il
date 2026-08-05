#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly AttributeNotValidOnMember { }
.module AttributeNotValidOnMember.dll

// Usable only on a class declaration (AttributeTargets.Class = 4). Metadata can still carry it on
// any member, because a compiler writes synthesized attributes straight to metadata without the
// usage check C# applies to source. Every C# declaration below therefore has to hold it back:
// naming it there does not compile (CS0592).
.class public auto ansi sealed beforefieldinit OnlyClassAttribute
       extends [CORE_ASSEMBLY]System.Attribute
{
  .custom instance void [CORE_ASSEMBLY]System.AttributeUsageAttribute::.ctor(valuetype [CORE_ASSEMBLY]System.AttributeTargets) = (
    01 00 04 00 00 00 00 00
  )
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Attribute::.ctor()
    ret
  }
}

// Usable everywhere (AttributeTargets.All = 32767). The control: every one of these must survive,
// so a fix that simply drops attributes from members would fail this fixture.
.class public auto ansi sealed beforefieldinit AnywhereAttribute
       extends [CORE_ASSEMBLY]System.Attribute
{
  .custom instance void [CORE_ASSEMBLY]System.AttributeUsageAttribute::.ctor(valuetype [CORE_ASSEMBLY]System.AttributeTargets) = (
    01 00 FF 7F 00 00 00 00
  )
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Attribute::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Holder
       extends [CORE_ASSEMBLY]System.Object
{
  .field public int32 Plain
  .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )

  .field public int32 Kept
  .custom instance void AnywhereAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig instance int32 GetValue(int32 arg) cil managed
  {
    .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .param [0]
      .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .param [1]
      .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance int32 get_Number() cil managed
  {
    .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldc.i4.0
    ret
  }

  .property instance int32 Number()
  {
    .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .get instance int32 Holder::get_Number()
  }

  .method public hidebysig specialname instance void add_Fired(class [CORE_ASSEMBLY]System.EventHandler 'value') cil managed
  {
    .maxstack 8
    ret
  }

  .method public hidebysig specialname instance void remove_Fired(class [CORE_ASSEMBLY]System.EventHandler 'value') cil managed
  {
    .maxstack 8
    ret
  }

  .event [CORE_ASSEMBLY]System.EventHandler Fired
  {
    .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .addon instance void Holder::add_Fired(class [CORE_ASSEMBLY]System.EventHandler)
    .removeon instance void Holder::remove_Fired(class [CORE_ASSEMBLY]System.EventHandler)
  }

  .method public hidebysig specialname static class Holder op_Addition(class Holder a, class Holder b) cil managed
  {
    .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Object::.ctor()
    ret
  }
}

.class public auto ansi sealed Season
       extends [CORE_ASSEMBLY]System.Enum
{
  .field public specialname rtspecialname int32 value__

  .field public static literal valuetype Season Spring = int32(0)
  .custom instance void OnlyClassAttribute::.ctor() = ( 01 00 00 00 )

  .field public static literal valuetype Season Summer = int32(1)
  .custom instance void AnywhereAttribute::.ctor() = ( 01 00 00 00 )
}

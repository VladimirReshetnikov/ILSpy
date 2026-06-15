#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}

// A this-/base-ctor call must be the first statement of a constructor so it can be turned
// into a constructor initializer. Some compilers hoist a value that is used more than once
// in a ctor-call argument into a local declared right before the call, which moves the call
// out of the first-statement position. This case verifies that such a local is inlined back
// into the call's arguments.

.class public auto ansi beforefieldinit GridDataBase
    extends [CORE_ASSEMBLY]System.Object
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor (object owner, string name, string kind) cil managed
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi beforefieldinit GridData
    extends GridDataBase
{
    .method public hidebysig specialname
        instance class GridData get_Parent () cil managed
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor (class GridData node) cil managed
    {
        .maxstack 4
        .locals init (
            [0] class GridData
        )

        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: ldarg.1
        IL_0003: callvirt instance class GridData GridData::get_Parent()
        IL_0008: stloc.0
        IL_0009: ldloc.0
        IL_000a: brtrue.s IL_0014

        IL_000c: ldarg.1
        IL_000d: callvirt instance string [CORE_ASSEMBLY]System.Object::ToString()
        IL_0012: br.s IL_001a

        IL_0014: ldloc.0
        IL_0015: callvirt instance string [CORE_ASSEMBLY]System.Object::ToString()

        IL_001a: ldstr "Kind"
        IL_001f: call instance void GridDataBase::.ctor(object, string, string)
        IL_0024: ret
    }

    .property instance class GridData Parent()
    {
        .get instance class GridData GridData::get_Parent()
    }
}

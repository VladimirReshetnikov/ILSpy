// Constructors whose chained this-/base-ctor call is not the first body statement.
// This shape cannot be authored in C# (a statement before a ctor initializer is illegal),
// but obfuscators emit it. The decompiler must still recognize the chained call instead of
// printing an uncompilable 'base..ctor(...)'.

#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly ConstructorInitializerAfterLeadingStatements { }

.class public auto ansi beforefieldinit BaseType
    extends [CORE_ASSEMBLY]System.Object
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor (string s) cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor (int32 mode) cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor (class [CORE_ASSEMBLY]System.Func`1<int32> factory) cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
    }
}

// A temporary read twice (ternary form) feeds the base-ctor argument; its
// initializer is a side-effect-free field read, so it may be folded into both uses.
.class public auto ansi beforefieldinit DoubleUseTemporary
    extends BaseType
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor (class Holder h) cil managed
    {
        .maxstack 3
        .locals init (string V_0)

        ldarg.1
        ldfld string Holder::Name
        stloc.0

        ldarg.0
        ldloc.0
        brtrue.s NOT_NULL
        ldstr "fallback"
        br.s DONE
    NOT_NULL:
        ldloc.0
    DONE:
        call instance void BaseType::.ctor(string)

        ret
    }
}

// Field initializers and a local read twice precede the parameterless base-ctor call.
.class public auto ansi beforefieldinit FieldInitsBeforeBaseCall
    extends BaseType
{
    .field public initonly bool Flag
    .field public initonly int32 Value

    .method public hidebysig specialname rtspecialname
        instance void .ctor (int32 seed) cil managed
    {
        .maxstack 3
        .locals init (int32 V_0)

        ldarg.1
        ldc.i4.1
        add
        stloc.0

        ldarg.0
        ldloc.0
        ldc.i4.0
        cgt
        ldloc.0
        ldc.i4 1000
        clt
        and
        stfld bool FieldInitsBeforeBaseCall::Flag

        ldarg.0
        call instance void BaseType::.ctor()

        ldarg.0
        ldloc.0
        stfld int32 FieldInitsBeforeBaseCall::Value

        ret
    }
}

// A guard clause precedes a base-ctor call whose argument is a parameter.
.class public auto ansi beforefieldinit GuardBeforeBaseCall
    extends BaseType
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor (int32 mode) cil managed
    {
        .maxstack 8

        ldarg.1
        ldc.i4.0
        bge.s OK
        ldstr "mode"
        newobj instance void [CORE_ASSEMBLY]System.ArgumentOutOfRangeException::.ctor(string)
        throw
    OK:
        ldarg.0
        ldarg.1
        call instance void BaseType::.ctor(int32)

        ret
    }
}

.class public auto ansi beforefieldinit Holder
    extends [CORE_ASSEMBLY]System.Object
{
    .field public string Name
}

// A single-use temporary feeds the base-ctor argument.
.class public auto ansi beforefieldinit SingleUseTemporary
    extends BaseType
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor (class Holder h) cil managed
    {
        .maxstack 3
        .locals init (string V_0)

        ldarg.1
        ldfld string Holder::Name
        stloc.0

        ldarg.0
        ldloc.0
        call instance void BaseType::.ctor(string)

        ret
    }
}

.class public auto ansi beforefieldinit Closure
    extends [CORE_ASSEMBLY]System.Object
{
    .field public int32 X

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance int32 Invoke () cil managed
    {
        .maxstack 8
        ldarg.0
        ldfld int32 Closure::X
        ret
    }
}

// A closure-like object is initialized before the base call, supplies a method group to
// the call, and is read again afterwards. The setup must move into the ctor initializer.
.class public auto ansi beforefieldinit InitializedObjectTemporary
    extends BaseType
{
    .field public int32 Copy

    .method public hidebysig specialname rtspecialname
        instance void .ctor (int32 x) cil managed
    {
        .maxstack 3
        .locals init (class Closure V_0)

        newobj instance void Closure::.ctor()
        stloc.0
        ldloc.0
        ldarg.1
        stfld int32 Closure::X

        ldarg.0
        ldloc.0
        ldftn instance int32 Closure::Invoke()
        newobj instance void class [CORE_ASSEMBLY]System.Func`1<int32>::.ctor(object, native int)
        call instance void BaseType::.ctor(class [CORE_ASSEMBLY]System.Func`1<int32>)

        ldarg.0
        ldloc.0
        ldfld int32 Closure::X
        stfld int32 InitializedObjectTemporary::Copy
        ret
    }
}

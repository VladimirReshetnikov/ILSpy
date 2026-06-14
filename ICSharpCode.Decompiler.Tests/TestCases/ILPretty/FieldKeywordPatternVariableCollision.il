// A property accessor that declares a user variable named 'field'. Under C# 14 'field' is a
// contextual keyword inside a property accessor and binds to the property's synthesized backing
// field, so a variable named 'field' (here a type-pattern variable) must be renamed by the
// decompiler; otherwise its uses re-bind to the backing field and no longer compile.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly FieldKeywordPatternVariableCollision { }

.class interface public abstract auto ansi IField
{
    .method public hidebysig newslot specialname abstract virtual
        instance bool get_IsField () cil managed { }

    .property instance bool IsField()
    {
        .get instance bool IField::get_IsField()
    }
}

.class public auto ansi beforefieldinit Wrapper
    extends [System.Runtime]System.Object
{
    .field private object Member

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method private hidebysig specialname
        instance bool get_IsField () cil managed
    {
        .maxstack 2
        .locals init (class IField V_0)

        ldarg.0
        ldfld object Wrapper::Member
        isinst IField
        dup
        stloc.0
        brfalse.s RET_FALSE

        ldloc.0
        callvirt instance bool IField::get_IsField()
        ret

    RET_FALSE:
        ldc.i4.0
        ret
    }

    .property instance bool IsField()
    {
        .get instance bool Wrapper::get_IsField()
    }
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly InitOnlySettersAfterInitializer
{
  .ver 1:0:0:0
}
.module InitOnlySettersAfterInitializer.dll

.class public abstract auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsExternalInit
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
}

.class public auto ansi beforefieldinit Options
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance string get_First() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig specialname instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit)
          set_First(string 'value') cil managed
  {
    ret
  }

  .property instance string First()
  {
    .get instance string Options::get_First()
    .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_First(string)
  }

  .method public hidebysig specialname instance string get_Second() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig specialname instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit)
          set_Second(string 'value') cil managed
  {
    ret
  }

  .property instance string Second()
  {
    .get instance string Options::get_Second()
    .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Second(string)
  }

  .method public hidebysig specialname instance int32 get_Third() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit)
          set_Third(int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Third()
  {
    .get instance int32 Options::get_Third()
    .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Third(int32)
  }

  .method public hidebysig specialname instance int32 get_Fourth() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit)
          set_Fourth(int32 'value') cil managed
  {
    ret
  }

  .property instance int32 Fourth()
  {
    .get instance int32 Options::get_Fourth()
    .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Fourth(int32)
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit Factory
       extends [mscorlib]System.Object
{
  // The initializer is split where a member's value needs a branch of its own: the temporary is
  // copied into a second local and the remaining members are set one statement at a time. Every one
  // of those setters is init-only, so they only compile back inside the initializer.
  .method public hidebysig static class Options Create(bool flag) cil managed
  {
    .maxstack 3
    .locals init (
      [0] class Options alias,
      [1] int32 branched,
      [2] class Options temp
    )
    newobj instance void Options::.ctor()
    stloc.2

    ldloc.2
    ldstr "a"
    callvirt instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_First(string)

    ldloc.2
    ldstr "b"
    callvirt instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Second(string)

    ldloc.2
    stloc.0

    ldarg.0
    brfalse.s IL_else
    ldc.i4.1
    br.s IL_store
  IL_else:
    ldc.i4.2
  IL_store:
    stloc.1

    ldloc.0
    ldloc.1
    callvirt instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Third(int32)

    ldloc.0
    ldc.i4.3
    callvirt instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) Options::set_Fourth(int32)

    ldloc.2
    ret
  }
}

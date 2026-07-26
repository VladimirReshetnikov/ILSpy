.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ProtectedSetterOnGrandparent
{
  .ver 1:0:0:0
}
.module ProtectedSetterOnGrandparent.dll

.class public auto ansi beforefieldinit EventArgsBase
       extends [mscorlib]System.Object
{
  .field private string text

  .method public hidebysig newslot specialname virtual instance string get_Text() cil managed
  {
    ldarg.0
    ldfld string EventArgsBase::text
    ret
  }

  .method family hidebysig specialname instance void set_Text(string 'value') cil managed
  {
    ldarg.0
    ldarg.1
    stfld string EventArgsBase::text
    ret
  }

  .property instance string Text()
  {
    .get instance string EventArgsBase::get_Text()
    .set instance void EventArgsBase::set_Text(string)
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit MessageEventArgs
       extends EventArgsBase
{
  .method public hidebysig specialname virtual instance string get_Text() cil managed
  {
    ldarg.0
    call instance string EventArgsBase::get_Text()
    ret
  }

  .property instance string Text()
  {
    .get instance string MessageEventArgs::get_Text()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void EventArgsBase::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ParameterEventArgs
       extends MessageEventArgs
{
  // The setter lives on the grandparent and is protected. A cast to its declaring type would reach
  // it through a qualifier of the wrong type (CS1540); only 'base' can name it here. The getter is
  // public and overridden along the way, which is what the cast is for.
  .method public hidebysig specialname virtual instance string get_Text() cil managed
  {
    .maxstack 2
    ldarg.0
    call instance string EventArgsBase::get_Text()
    brtrue.s IL_ret

    ldarg.0
    ldstr "computed"
    call instance void EventArgsBase::set_Text(string)

  IL_ret:
    ldarg.0
    call instance string EventArgsBase::get_Text()
    ret
  }

  .property instance string Text()
  {
    .get instance string ParameterEventArgs::get_Text()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void MessageEventArgs::.ctor()
    ret
  }
}

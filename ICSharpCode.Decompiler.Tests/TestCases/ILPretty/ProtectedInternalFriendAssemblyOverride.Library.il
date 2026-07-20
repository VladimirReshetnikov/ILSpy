.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ProtectedInternalFriendAssemblyOverride.Library
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 27 50 72 6F 74 65 63 74 65 64 49 6E 74 65 72 6E 61 6C 46 72 69 65 6E 64 41 73 73 65 6D 62 6C 79 4F 76 65 72 72 69 64 65 00 00 )
  .ver 1:0:0:0
}
.module ProtectedInternalFriendAssemblyOverride.Library.dll

.class public abstract auto ansi beforefieldinit SettingsUpgraderBase
       extends [mscorlib]System.Object
{
  .method famorassem hidebysig newslot specialname abstract virtual
          instance string get_SectionName() cil managed
  {
  }

  .property instance string SectionName()
  {
    .get instance string SettingsUpgraderBase::get_SectionName()
  }

  .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

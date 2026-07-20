.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ProtectedInternalCrossAssemblyOverride.Library
{
  .ver 1:0:0:0
}
.module ProtectedInternalCrossAssemblyOverride.Library.dll

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

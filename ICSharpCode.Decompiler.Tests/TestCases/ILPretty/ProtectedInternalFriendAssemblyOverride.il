.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern ProtectedInternalFriendAssemblyOverride.Library
{
  .ver 1:0:0:0
}
.assembly ProtectedInternalFriendAssemblyOverride
{
  .ver 1:0:0:0
}
.module ProtectedInternalFriendAssemblyOverride.dll

.class public auto ansi beforefieldinit SettingsUpgrader
       extends [ProtectedInternalFriendAssemblyOverride.Library]SettingsUpgraderBase
{
  .method famorassem hidebysig specialname virtual
          instance string get_SectionName() cil managed
  {
    ldstr "nunit"
    ret
  }

  .property instance string SectionName()
  {
    .get instance string SettingsUpgrader::get_SectionName()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [ProtectedInternalFriendAssemblyOverride.Library]SettingsUpgraderBase::.ctor()
    ret
  }
}

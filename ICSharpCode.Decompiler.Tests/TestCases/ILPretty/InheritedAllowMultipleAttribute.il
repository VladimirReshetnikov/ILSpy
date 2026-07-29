.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly InheritedAllowMultipleAttribute
{
  .ver 1:0:0:0
}
.module InheritedAllowMultipleAttribute.dll

// AttributeUsage is itself an inherited attribute, so this attribute class allows multiple
// applications even though it declares no AttributeUsage of its own. Both applications on
// RepeatedInheritedMultiple must therefore survive.
.class public auto ansi beforefieldinit BaseMultiAttribute
       extends [mscorlib]System.Attribute
{
  // [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 01 00 54 02 0D 41 6C 6C 6F 77 4D 75 6C 74 69 70 6C 65 01 )

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Attribute::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit DerivedMultiAttribute
       extends BaseMultiAttribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void BaseMultiAttribute::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit RepeatedInheritedMultiple
       extends [mscorlib]System.Object
{
  .custom instance void DerivedMultiAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void DerivedMultiAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

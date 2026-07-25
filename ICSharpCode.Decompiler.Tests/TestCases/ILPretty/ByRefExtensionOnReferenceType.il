.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern System.Core
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ByRefExtensionOnReferenceType
{
  .ver 1:0:0:0
}
.module ByRefExtensionOnReferenceType.dll

.class public auto ansi beforefieldinit Builder
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public sequential ansi sealed beforefieldinit Counter
       extends [mscorlib]System.ValueType
{
  .field public int32 Value
}

.class public abstract auto ansi sealed beforefieldinit Extensions
       extends [mscorlib]System.Object
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )

  // Visual Basic writes extension methods that take a class by reference. C# allows a by-reference
  // receiver only on a value type or a struct-constrained type parameter (CS8337), so this one has
  // to stay a plain static method.
  .method public hidebysig static void ScrubClass(class Builder& b) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }

  // A by-reference receiver on a value type is legal and keeps its 'this'.
  .method public hidebysig static void Bump(valuetype Counter& c) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }

  // A by-value receiver of any type is legal too.
  .method public hidebysig static void Touch(class Builder b) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
    ret
  }
}

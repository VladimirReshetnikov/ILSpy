.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly RepeatedSingleUseAttribute
{
  .ver 1:0:0:0
}
.module RepeatedSingleUseAttribute.dll

// AllowMultiple = false (the default), yet applied twice - metadata allows it, C# has no syntax
// for it, and csc rejects a second application with CS0579.
.class public auto ansi beforefieldinit RepeatedNonMultiple
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 05 66 69 72 73 74 00 00 )
  .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 06 73 65 63 6F 6E 64 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// AllowMultiple = true, so both applications stay.
.class public auto ansi beforefieldinit RepeatedMultiple
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SuppressMessageAttribute::.ctor(string, string) = ( 01 00 03 63 61 74 02 69 64 00 00 )
  .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.SuppressMessageAttribute::.ctor(string, string) = ( 01 00 04 63 61 74 32 03 69 64 32 00 00 )

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

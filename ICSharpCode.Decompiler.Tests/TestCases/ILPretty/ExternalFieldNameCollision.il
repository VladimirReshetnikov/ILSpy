.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern ExternalFieldNameCollision.Library
{
  .ver 1:0:0:0
}
.assembly ExternalFieldNameCollision
{
  .ver 1:0:0:0
}
.module ExternalFieldNameCollision.dll

.class public auto ansi beforefieldinit ExternalFieldConsumer
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Read() cil managed
  {
    ldsfld int32 [ExternalFieldNameCollision.Library]ExternalFieldLibrary::Value
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly CrossRootMemberCollision
{
  .ver 1:0:0:0
}
.module CrossRootMemberCollision.dll

.class public auto ansi beforefieldinit CrossRootOwner
       extends [mscorlib]System.Object
{
  .class nested public auto ansi beforefieldinit a
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor()
      ret
    }
  }

  .field public int32 a

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit CrossRootConsumer
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Read(class CrossRootOwner owner) cil managed
  {
    ldarg.0
    ldfld int32 CrossRootOwner::a
    ret
  }
}

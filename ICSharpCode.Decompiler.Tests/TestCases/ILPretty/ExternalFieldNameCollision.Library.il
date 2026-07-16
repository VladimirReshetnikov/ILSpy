.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ExternalFieldNameCollision.Library
{
  .ver 1:0:0:0
}
.module ExternalFieldNameCollision.Library.dll

.class public auto ansi abstract sealed beforefieldinit ExternalFieldLibrary
       extends [mscorlib]System.Object
{
  .field public static int32 Value

  .method public hidebysig static int32 Value() cil managed
  {
    ldc.i4.m1
    ret
  }

  .method private hidebysig specialname rtspecialname static void .cctor() cil managed
  {
    ldc.i4.s 42
    stsfld int32 ExternalFieldLibrary::Value
    ret
  }
}

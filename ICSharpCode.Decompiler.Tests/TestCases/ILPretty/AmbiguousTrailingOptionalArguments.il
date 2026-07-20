.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly AmbiguousTrailingOptionalArguments
{
  .ver 1:0:0:0
}
.module AmbiguousTrailingOptionalArguments.dll

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void M(string a, [opt] int32[] rules) cil managed
  {
    .param [2] = nullref
    ret
  }

  .method public hidebysig instance void M(string a,
                                           [opt] class [mscorlib]System.Collections.Generic.IEnumerable`1<string> predefined,
                                           [opt] int32[] rules) cil managed
  {
    .param [2] = nullref
    .param [3] = nullref
    ret
  }

  .method public hidebysig instance void Test() cil managed
  {
    ldarg.0
    ldstr "x"
    ldnull
    ldnull
    call instance void C::M(string, class [mscorlib]System.Collections.Generic.IEnumerable`1<string>, int32[])
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

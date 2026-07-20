// ArrayUtil.Add has two overloads whose parameter names ("element", "array") are identical
// but in swapped positions, so named arguments cannot select between them. Test() computes the
// element while another value is already on the evaluation stack, producing an out-of-order
// temporary that the decompiler is tempted to fold into a named-argument call. Emitting named
// arguments here would be ambiguous (CS0121); the call must stay positional.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly NamedArgumentOverloadCollision
{
  .ver 1:0:0:0
}
.module NamedArgumentOverloadCollision.dll

.class public auto ansi abstract sealed beforefieldinit ArrayUtil
       extends [mscorlib]System.Object
{
  .method public hidebysig static !!0[] Add<T>(!!0 'element', !!0[] 'array') cil managed
  {
    ldarg.1
    ret
  }

  .method public hidebysig static !!0[] Add<T>(!!0[] 'array', !!0 'element') cil managed
  {
    ldarg.0
    ret
  }
}

.class public auto ansi beforefieldinit Consumer
       extends [mscorlib]System.Object
{
  .method public hidebysig static string[] GetArray() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig static string GetElement() cil managed
  {
    ldnull
    ret
  }

  .method public hidebysig static string[] Wrap(int32 tag, string[] arr) cil managed
  {
    ldarg.1
    ret
  }

  .method public hidebysig static string[] Test() cil managed
  {
    .locals init (string V_0)
    ldc.i4.1
    call string Consumer::GetElement()
    stloc.0
    call string[] Consumer::GetArray()
    ldloc.0
    call !!0[] ArrayUtil::Add<string>(!!0[], !!0)
    call string[] Consumer::Wrap(int32, string[])
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

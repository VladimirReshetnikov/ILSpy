.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ParameterizedProperty
{
  .ver 1:0:0:0
}
.module ParameterizedProperty.dll

// Visual Basic lets any property take arguments without being the type's default member. C# has
// only the indexer, so such a property has no declaration form: it has to be written as its
// accessor methods, which is how the call sites read anyway.
.class public abstract auto ansi beforefieldinit Symbolic
       extends [mscorlib]System.Object
{
  .method famorassem hidebysig newslot specialname abstract virtual
          instance string get_Described([opt] int32 depth) cil managed
  {
    .param [1] = int32(0)
  }

  .property instance string Described(int32)
  {
    .get instance string Symbolic::get_Described(int32)
  }

  // A parameterless property of the same name is a separate member and keeps its own form.
  .method famorassem hidebysig newslot specialname virtual instance string get_Name() cil managed
  {
    ldnull
    ret
  }

  .property instance string Name()
  {
    .get instance string Symbolic::get_Name()
  }

  .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Consumer
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string CallWithArgument(class Symbolic s) cil managed
  {
    ldarg.1
    ldc.i4.3
    callvirt instance string Symbolic::get_Described(int32)
    ret
  }

  // The argument is the parameter's own default, which is where the call would otherwise be
  // sugared back into a property access.
  .method public hidebysig instance string CallWithDefault(class Symbolic s) cil managed
  {
    ldarg.1
    ldc.i4.0
    callvirt instance string Symbolic::get_Described(int32)
    ret
  }

  .method public hidebysig instance string CallParameterless(class Symbolic s) cil managed
  {
    ldarg.1
    callvirt instance string Symbolic::get_Name()
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

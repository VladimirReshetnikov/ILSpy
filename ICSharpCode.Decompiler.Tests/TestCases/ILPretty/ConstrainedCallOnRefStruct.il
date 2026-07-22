#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly ConstrainedCallOnRefStruct { }
.module ConstrainedCallOnRefStruct.dll

// A constrained. call on a ref struct receiver whose resolution does not land on the referenced
// method (here: object.ToString is hidden by a non-virtual ToString). The receiver-cast fallback
// that disambiguates such calls must not be used: a ref struct value has no conversion to object
// (or any other type), so '((object)refLike).ToString()' can never compile.
.class public sequential ansi sealed beforefieldinit RefLike
       extends [CORE_ASSEMBLY]System.ValueType
{
  .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .pack 0
  .size 1

  .method public hidebysig instance string ToString() cil managed
  {
    .maxstack 8

    ldstr "RefLike"
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit Caller
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static string CallConstrained() cil managed
  {
    .maxstack 8
    .locals init (
      [0] valuetype RefLike
    )

    ldloca.s 0
    constrained. RefLike
    callvirt instance string [CORE_ASSEMBLY]System.Object::ToString()
    ret
  }
}

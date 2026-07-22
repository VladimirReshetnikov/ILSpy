#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern System.Collections
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly FakeMethodOnGenericInstance { }
.module FakeMethodOnGenericInstance.dll

// Member references that cannot be resolved against the target type's definition (here: methods
// that do not exist on Dictionary`2) are represented by fake methods built from the signature
// blob. The signature's class type parameters must be substituted with the declaring type's type
// arguments exactly once: 'AddMissing(!1)' on Dictionary`2<string, !T> takes a parameter of type
// T (the calling type's type parameter), not 'string'.
.class public auto ansi beforefieldinit Wrapper`1<T>
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig instance void CallMissing(class [System.Collections]System.Collections.Generic.Dictionary`2<string, !T> dict, !T item) cil managed
  {
    .maxstack 8

    ldarg.1
    ldarg.2
    callvirt instance void class [System.Collections]System.Collections.Generic.Dictionary`2<string, !T>::AddMissing(!1)
    ret
  }

  .method public hidebysig instance !T FetchMissing(class [System.Collections]System.Collections.Generic.Dictionary`2<string, !T> dict) cil managed
  {
    .maxstack 8

    ldarg.1
    callvirt instance !1 class [System.Collections]System.Collections.Generic.Dictionary`2<string, !T>::GetMissing()
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8

    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Object::.ctor()
    ret
  }
}

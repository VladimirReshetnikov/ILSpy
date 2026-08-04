#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly AttributeNotValidOnConstructor { }
.module AttributeNotValidOnConstructor.dll

// An attribute whose AttributeUsage covers Class and Method but NOT Constructor. A compiler can
// still put one on a .ctor: a constructor is a MethodDef in metadata, and synthesized attributes
// are written straight there without the usage check C# applies to source. Naming it on a C#
// constructor declaration does not compile (CS0592), so the decompiler has to hold it back.
.class public auto ansi sealed beforefieldinit OnlyClassAndMethodAttribute
       extends [CORE_ASSEMBLY]System.Attribute
{
  .custom instance void [CORE_ASSEMBLY]System.AttributeUsageAttribute::.ctor(valuetype [CORE_ASSEMBLY]System.AttributeTargets) = (
    01 00 44 00 00 00 00 00
  )

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8

    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Attribute::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit Holder
       extends [CORE_ASSEMBLY]System.Object
{
  // Carried by the type declaration, where Class is permitted: this one must survive.
  .custom instance void OnlyClassAndMethodAttribute::.ctor() = ( 01 00 00 00 )

  // Takes a parameter so the declaration is always printed: a trivial default constructor would be
  // elided once the attribute is held back, and the fixture could not tell the two apart.
  .method public hidebysig specialname rtspecialname instance void .ctor(int32 seed) cil managed
  {
    .custom instance void OnlyClassAndMethodAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8

    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Object::.ctor()
    ret
  }

  // The same attribute on a plain method, where Method is permitted: this one must survive too.
  .method public hidebysig instance void Run() cil managed
  {
    .custom instance void OnlyClassAndMethodAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8

    ret
  }
}

#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly OutParameterWithUnresolvedSignatureType { }
.module OutParameterWithUnresolvedSignatureType.dll

// A type whose name is also that of a type in the core assembly, so the two definitions differ
// only in which assembly declares them. Reference graphs really do disagree this way: a .NET
// Framework assembly calling into an assembly built for .NET names its BCL types through mscorlib
// while the callee names the same types through System.Runtime, and the decompiler resolves the
// two to separate definitions (mscorlib's and System.Private.CoreLib's).
.class public sequential ansi sealed beforefieldinit System.Guid
       extends [CORE_ASSEMBLY]System.ValueType
{
  .field public int32 Value
}

// The callee's first parameter is System.Guid as declared by the core assembly; the call site
// below names the one declared here. Matching the call against this declaration has to see past
// that disagreement, or the [out] flag on the second parameter is lost and the call site is
// rendered 'ref' - which does not compile against the real declaration (CS1620).
.class public auto ansi beforefieldinit Callee
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static void Load(valuetype [CORE_ASSEMBLY]System.Guid& key,
                                            [out] string& result) cil managed
  {
    .maxstack 8

    ldarg.1
    ldstr "value"
    stind.ref
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

.class public auto ansi beforefieldinit Caller
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static string Call(valuetype System.Guid& key) cil managed
  {
    .maxstack 2
    .locals init (
      [0] string result
    )

    ldarg.0
    ldloca.s result
    call void Callee::Load(valuetype System.Guid&, string&)
    ldloc.0
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

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly DuplicateParameterNames
{
  .ver 1:0:0:0
}
.module DuplicateParameterNames.dll

// Parameter names are not required to be unique in metadata. C# method declarations do require
// unique names, so the fallback output must repair collisions and update each parameter reference.
.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DuplicateParameterNames
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Add(int32 _, int32 _) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    add
    ret
  }
}

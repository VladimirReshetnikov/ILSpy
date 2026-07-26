.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly DiscardNameTakenByLocalFunction { .ver 1:0:0:0 }
.module DiscardNameTakenByLocalFunction.dll
.class public abstract auto ansi sealed beforefieldinit Probe extends [mscorlib]System.Object
{
  .method public hidebysig static void Run(class [mscorlib]System.Exception e) cil managed
  {
    .maxstack 2
    ldarg.0
    ldc.i4.1
    call void Probe::'<Run>g__Inner|0_0'(class [mscorlib]System.Exception, int32)
    ret
  }

  // The local function has a parameter of its own named '_', so the discarded property read cannot
  // be written as a discard assignment: that would assign to the parameter instead.
  .method assembly hidebysig static void '<Run>g__Inner|0_0'(class [mscorlib]System.Exception e, int32 _) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 1
    ldarg.0
    callvirt instance string [mscorlib]System.Exception::get_Message()
    pop
    ret
  }
}

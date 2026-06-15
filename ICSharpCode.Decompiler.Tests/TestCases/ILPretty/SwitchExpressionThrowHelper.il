// Type-pattern switch expression whose implicit default throws.
//
// Roslyn lowers a type-pattern switch expression with a throwing default
// to an isinst/brtrue chain (no 'switch' opcode) followed by a call to a
// compiler-generated throw helper on <PrivateImplementationDetails>.
// The helper call must be reconstructed into the switch expression's
// default arm so the un-nameable type never leaks into the output.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly SwitchExpressionThrowHelper
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module SwitchExpressionThrowHelper.dll

.class public auto ansi abstract sealed beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig static string Describe(object o) cil managed
  {
    .maxstack 1
    .locals init (
      [0] string
    )
    IL_0000: ldarg.0
    IL_0001: isinst [mscorlib]System.String
    IL_0006: brtrue.s IL_0019

    IL_0008: ldarg.0
    IL_0009: isinst [mscorlib]System.Exception
    IL_000e: brtrue.s IL_0021

    IL_0010: call void '<PrivateImplementationDetails>'::ThrowInvalidOperationException()

    IL_0015: ldloc.0
    IL_0016: stloc.0
    IL_0017: br.s IL_0029

    IL_0019: ldstr "string"
    IL_001e: stloc.0
    IL_001f: br.s IL_0029

    IL_0021: ldstr "exception"
    IL_0026: stloc.0
    IL_0027: br.s IL_0029

    IL_0029: ldloc.0
    IL_002a: ret
  }
}

.class private auto ansi sealed '<PrivateImplementationDetails>'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    01 00 00 00
  )
  .method assembly hidebysig static void ThrowInvalidOperationException() cil managed
  {
    .maxstack 8
    IL_0000: newobj instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_0005: throw
  }
}

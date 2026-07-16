// Regression test for a local-signature slot reused first as the compiler-generated lock
// temporary and then as an out-argument destination. The out argument starts a new live range;
// it must not merge with the lock temporary and prevent lock-statement reconstruction.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly LockOutVariableSlotReuse
{
  .ver 1:0:0:0
}
.module LockOutVariableSlotReuse.dll

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.LockOutVariableSlotReuse
       extends [mscorlib]System.Object
{
  .field private initonly object gate

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ldarg.0
    newobj instance void [mscorlib]System.Object::.ctor()
    stfld object ICSharpCode.Decompiler.Tests.TestCases.ILPretty.LockOutVariableSlotReuse::gate
    ret
  }

  .method private hidebysig static void Assign([out] object& 'value') cil managed
  {
    .maxstack 1
    ldarg.0
    ldnull
    stind.ref
    ret
  }

  .method public hidebysig instance void Run() cil managed
  {
    .maxstack 2
    .locals init (
      [0] object V_0,
      [1] bool V_1
    )

    ldarg.0
    ldfld object ICSharpCode.Decompiler.Tests.TestCases.ILPretty.LockOutVariableSlotReuse::gate
    stloc.0
    ldc.i4.0
    stloc.1
    .try
    {
      ldloc.0
      ldloca.s V_1
      call void [mscorlib]System.Threading.Monitor::Enter(object, bool&)
      call void [mscorlib]System.Console::WriteLine()
      leave.s AFTER_LOCK
    }
    finally
    {
      ldloc.1
      brfalse.s END_FINALLY
      ldloc.0
      call void [mscorlib]System.Threading.Monitor::Exit(object)
      END_FINALLY:
      endfinally
    }

    AFTER_LOCK:
    ldloca.s V_0
    call void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.LockOutVariableSlotReuse::Assign(object&)
    ldloc.0
    call void [mscorlib]System.Console::WriteLine(object)
    ret
  }
}

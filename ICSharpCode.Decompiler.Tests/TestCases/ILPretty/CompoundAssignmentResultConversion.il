#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly CompoundAssignmentResultConversion { }
.module CompoundAssignmentResultConversion.dll

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point
       extends [CORE_ASSEMBLY]System.ValueType
{
  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector op_Subtraction(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point left,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point right) cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector result)
    ldloca.s result
    initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector
    ldloc.0
    ret
  }

  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point op_Addition(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point left,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point right) cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point result)
    ldloca.s result
    initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point
    ldloc.0
    ret
  }
}

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector
       extends [CORE_ASSEMBLY]System.ValueType
{
  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point op_Explicit(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector 'value') cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point result)
    ldloca.s result
    initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point
    ldloc.0
    ret
  }

  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point op_CheckedExplicit(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector 'value') cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point result)
    ldloca.s result
    initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point
    ldloc.0
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.BaseNode
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 1
    ldarg.0
    call instance void [CORE_ASSEMBLY]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node
       extends ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.BaseNode
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 1
    ldarg.0
    call instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.BaseNode::.ctor()
    ret
  }

  .method public hidebysig specialname static
          class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.BaseNode op_Addition(
            class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node left,
            class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node right) cil managed
  {
    .maxstack 1
    ldnull
    ret
  }
}

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper
       extends [CORE_ASSEMBLY]System.ValueType
{
  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper op_Increment(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper 'value') cil managed
  {
    .maxstack 1
    ldarg.0
    ret
  }
}

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter
       extends [CORE_ASSEMBLY]System.ValueType
{
  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter op_Increment(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter 'value') cil managed
  {
    .maxstack 1
    ldarg.0
    ret
  }

  .method public hidebysig specialname instance void op_IncrementAssignment() cil managed
  {
    .maxstack 0
    ret
  }
}

.class public sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally
       extends [CORE_ASSEMBLY]System.ValueType
{
  .method public hidebysig specialname static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally op_Addition(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally left,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally right) cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally result)
    ldloca.s result
    initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally
    ldloc.0
    ret
  }

  .method public hidebysig specialname instance void op_AdditionAssignment(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally other) cil managed
  {
    .maxstack 0
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Screen
       extends [CORE_ASSEMBLY]System.Object
{
  .field public static valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter Total
  .field public static valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper Value

  .method public hidebysig static void BumpTotal() cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter tmp)
    ldsfld valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Screen::Total
    stloc.0
    ldloc.0
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter::op_Increment(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter)
    stsfld valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Counter ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Screen::Total
    ret
  }

  .method public hidebysig static void BumpValue() cil managed
  {
    .maxstack 1
    .locals init ([0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper tmp)
    ldsfld valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Screen::Value
    stloc.0
    ldloc.0
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper::op_Increment(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper)
    stsfld valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Bumper ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Screen::Value
    ret
  }

  .method public hidebysig static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point Convert(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point relativePosition,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point offset) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point::op_Subtraction(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point,
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point)
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector::op_Explicit(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector)
    starg.s relativePosition
    ldarg.0
    ret
  }

  .method public hidebysig static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point ConvertChecked(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point relativePosition,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point offset) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point::op_Subtraction(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point,
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point)
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector::op_CheckedExplicit(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Vector)
    starg.s relativePosition
    ldarg.0
    ret
  }

  .method public hidebysig static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point Add(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point relativePosition,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point offset) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point::op_Addition(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point,
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Point)
    starg.s relativePosition
    ldarg.0
    ret
  }

  .method public hidebysig static
          class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node Combine(
            class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node node,
            class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node other) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    call class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.BaseNode ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node::op_Addition(
      class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node,
      class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node)
    castclass ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Node
    starg.s node
    ldarg.0
    ret
  }

  .method public hidebysig static
          valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally Accumulate(
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally tally,
            valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally other) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally::op_Addition(
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally,
      valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CompoundAssignmentResultConversion.Tally)
    starg.s tally
    ldarg.0
    ret
  }

  .method public hidebysig static int16 AddSmall(int16 'value', int16 offset) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    add
    conv.i2
    starg.s 'value'
    ldarg.0
    ret
  }
}

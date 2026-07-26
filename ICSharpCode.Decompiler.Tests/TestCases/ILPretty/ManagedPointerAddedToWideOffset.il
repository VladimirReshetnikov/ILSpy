.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ManagedPointerAddedToWideOffset
{
  .ver 1:0:0:0
}
.module ManagedPointerAddedToWideOffset.dll

.class public abstract auto ansi sealed beforefieldinit Offsets
       extends [mscorlib]System.Object
{
  .field public static int32 Base

  // C++/CLI widens the offset to int64 before adding it to a tracked pointer. C# has no operator
  // for that, so the add has to be written as the intrinsic that advances a reference.
  .method public hidebysig static uint8& AdvanceByWideOffset(uint8& start) cil managed
  {
    .maxstack 2
    ldsfld int32 Offsets::Base
    conv.i8
    ldarg.0
    add
    ret
  }

  // Both operands are tracked pointers as far as the type system is concerned, though the second
  // one only ever holds a byte offset.
  .method public hidebysig static uint8& AdvanceByReference(uint8& start, uint8& offset) cil managed
  {
    .maxstack 2
    ldarg.0
    ldarg.1
    add
    ret
  }
}

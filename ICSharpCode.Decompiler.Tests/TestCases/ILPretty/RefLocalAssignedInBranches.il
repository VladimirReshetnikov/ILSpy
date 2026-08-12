.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly RefLocalAssignedInBranches
{
  .ver 1:0:0:0
}
.module RefLocalAssignedInBranches.dll

.class public sequential ansi sealed beforefieldinit Entry
       extends [mscorlib]System.ValueType
{
  .field public int32 Value
}

.class public abstract auto ansi sealed beforefieldinit Lookup
       extends [mscorlib]System.Object
{
  .field public static valuetype Entry First
  .field public static valuetype Entry Second

  .method public hidebysig static void Note() cil managed
  {
    ret
  }

  // The reference is assigned in either branch and never read before, so nothing in the IL
  // corresponds to a declaration. C# still insists a reference be bound where it is declared.
  // This assembly only references the .NET Framework mscorlib, where
  // System.Runtime.CompilerServices.Unsafe does not resolve, so the declaration cannot be bound
  // via 'Unsafe.NullRef<Entry>()'; it is bound to a placeholder local of the element type
  // instead.
  .method public hidebysig static int32 Read(bool flag) cil managed
  {
    .maxstack 2
    .locals init (
      [0] valuetype Entry& entry
    )
    ldarg.0
    brfalse.s IL_else

    call void Lookup::Note()
    ldsflda valuetype Entry Lookup::First
    stloc.0
    br.s IL_read

  IL_else:
    call void Lookup::Note()
    ldsflda valuetype Entry Lookup::Second
    stloc.0

  IL_read:
    ldloc.0
    ldfld int32 Entry::Value
    ldloc.0
    ldfld int32 Entry::Value
    add
    ret
  }
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly TupleBuiltFieldByField
{
  .ver 1:0:0:0
}
.module TupleBuiltFieldByField.dll

.class public sequential ansi serializable sealed beforefieldinit System.ValueTuple`3<T1,T2,T3>
       extends [mscorlib]System.ValueType
{
  .field public !T1 Item1
  .field public !T2 Item2
  .field public !T3 Item3

  .method public hidebysig specialname rtspecialname instance void .ctor(!T1 item1, !T2 item2, !T3 item3) cil managed
  {
    ldarg.0
    ldarg.1
    stfld !0 valuetype System.ValueTuple`3<!T1,!T2,!T3>::Item1
    ldarg.0
    ldarg.2
    stfld !1 valuetype System.ValueTuple`3<!T1,!T2,!T3>::Item2
    ldarg.0
    ldarg.3
    stfld !2 valuetype System.ValueTuple`3<!T1,!T2,!T3>::Item3
    ret
  }
}

.class public abstract auto ansi sealed beforefieldinit Builder
       extends [mscorlib]System.Object
{
  // A tuple filled in one field at a time rather than through its constructor. There is no "new"
  // form for a tuple type, so every element being accounted for is what makes this writable at all.
  .method public hidebysig static valuetype System.ValueTuple`3<string,int32,string> Complete(string s) cil managed
  {
    .maxstack 2
    .locals init (
      [0] valuetype System.ValueTuple`3<string,int32,string> tuple
    )
    ldloca.s 0
    initobj valuetype System.ValueTuple`3<string,int32,string>
    ldloca.s 0
    ldarg.0
    stfld !0 valuetype System.ValueTuple`3<string,int32,string>::Item1
    ldloca.s 0
    ldc.i4.7
    stfld !1 valuetype System.ValueTuple`3<string,int32,string>::Item2
    ldloca.s 0
    ldarg.0
    stfld !2 valuetype System.ValueTuple`3<string,int32,string>::Item3
    ldloc.0
    ret
  }

  // With an element left at its default the tuple form does not apply, and the type has to be named
  // the only way C# can name it.
  .method public hidebysig static valuetype System.ValueTuple`3<string,int32,string> Partial(string s) cil managed
  {
    .maxstack 2
    .locals init (
      [0] valuetype System.ValueTuple`3<string,int32,string> tuple
    )
    ldloca.s 0
    initobj valuetype System.ValueTuple`3<string,int32,string>
    ldloca.s 0
    ldarg.0
    stfld !0 valuetype System.ValueTuple`3<string,int32,string>::Item1
    ldloca.s 0
    ldarg.0
    stfld !2 valuetype System.ValueTuple`3<string,int32,string>::Item3
    ldloc.0
    ret
  }
}

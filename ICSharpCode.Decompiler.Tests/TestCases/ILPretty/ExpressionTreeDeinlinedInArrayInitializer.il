// Some compilers (e.g. the ReSharper-internal C# compiler) build an expression tree in a de-inlined,
// multi-statement shape: a ParameterExpression slot is reused across several trees and the lambda's body
// (Expression.Field) and its ParameterExpression array are spilled into separate locals before the
// Expression.Lambda call, which is then consumed in an array initializer. Roslyn never emits this shape,
// so the regression is captured as hand-written IL.
//
// Without the de-inlined handling in TransformExpressionTrees the body / parameter-array locals leak as
// MemberExpression / ParameterExpression[] together with an uncompilable LdMemberToken; the expected output
// below is the reconstructed lambda form.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern System.Core
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly ExpressionTreeDeinlinedInArrayInitializer { }

.class public auto ansi beforefieldinit KeyA
       extends [mscorlib]System.Object
{
  .field public bool FlagA
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [mscorlib]System.Object::.ctor()
    IL_0006: ret
  }
}

.class public auto ansi beforefieldinit KeyB
       extends [mscorlib]System.Object
{
  .field public bool FlagB
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [mscorlib]System.Object::.ctor()
    IL_0006: ret
  }
}

.class public auto ansi beforefieldinit Populator
       extends [mscorlib]System.Object
{
  .method public hidebysig instance class Populator
          Add<TKey>(class [System.Core]System.Linq.Expressions.Expression`1<class [mscorlib]System.Func`2<!!TKey,bool>> e,
                    bool v) cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [mscorlib]System.Object::.ctor()
    IL_0006: ret
  }
}

.class public auto ansi beforefieldinit ExpressionTreeDeinlinedInArrayInitializer
       extends [mscorlib]System.Object
{
  .field private initonly class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> '<ItemsA>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .field private initonly class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> '<ItemsB>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig specialname instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object>
          get_ItemsA() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: ldfld class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::'<ItemsA>k__BackingField'
    IL_0006: ret
  }

  .method public hidebysig specialname instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object>
          get_ItemsB() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: ldfld class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::'<ItemsB>k__BackingField'
    IL_0006: ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 10
    .locals init (
      [0] class Populator converter,
      [1] class [System.Core]System.Linq.Expressions.ParameterExpression p,
      [2] class [System.Core]System.Linq.Expressions.MemberExpression body,
      [3] class [System.Core]System.Linq.Expressions.ParameterExpression[] parameters
    )
    IL_0000: ldarg.0
    IL_0001: call instance void [mscorlib]System.Object::.ctor()

    // ItemsA: de-inlined tree over KeyA reusing parameter slot p (local 1).
    IL_0006: ldarg.0
    IL_0007: newobj instance void Populator::.ctor()
    IL_000c: stloc.0
    IL_000d: ldtoken KeyA
    IL_0012: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0017: ldstr "x"
    IL_001c: call class [System.Core]System.Linq.Expressions.ParameterExpression [System.Core]System.Linq.Expressions.Expression::Parameter(class [mscorlib]System.Type, string)
    IL_0021: stloc.1
    IL_0022: ldloc.1
    IL_0023: ldtoken field bool KeyA::FlagA
    IL_0028: call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_002d: call class [System.Core]System.Linq.Expressions.MemberExpression [System.Core]System.Linq.Expressions.Expression::Field(class [System.Core]System.Linq.Expressions.Expression, class [mscorlib]System.Reflection.FieldInfo)
    IL_0032: stloc.2
    IL_0033: ldc.i4.1
    IL_0034: newarr [System.Core]System.Linq.Expressions.ParameterExpression
    IL_0039: dup
    IL_003a: ldc.i4.0
    IL_003b: ldloc.1
    IL_003c: stelem.ref
    IL_003d: stloc.3
    IL_003e: ldc.i4.1
    IL_003f: newarr [mscorlib]System.Object
    IL_0044: dup
    IL_0045: ldc.i4.0
    IL_0046: ldloc.0
    IL_0047: ldloc.2
    IL_0048: ldloc.3
    IL_0049: call class [System.Core]System.Linq.Expressions.Expression`1<!!0> [System.Core]System.Linq.Expressions.Expression::Lambda<class [mscorlib]System.Func`2<class KeyA,bool>>(class [System.Core]System.Linq.Expressions.Expression, class [System.Core]System.Linq.Expressions.ParameterExpression[])
    IL_004e: ldc.i4.1
    IL_004f: callvirt instance class Populator Populator::Add<class KeyA>(class [System.Core]System.Linq.Expressions.Expression`1<class [mscorlib]System.Func`2<!!0,bool>>, bool)
    IL_0054: stelem.ref
    IL_0055: stfld class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::'<ItemsA>k__BackingField'

    // ItemsB: de-inlined tree over KeyB reusing the same parameter slot p (local 1).
    IL_005a: ldarg.0
    IL_005b: newobj instance void Populator::.ctor()
    IL_0060: stloc.0
    IL_0061: ldtoken KeyB
    IL_0066: call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_006b: ldstr "x"
    IL_0070: call class [System.Core]System.Linq.Expressions.ParameterExpression [System.Core]System.Linq.Expressions.Expression::Parameter(class [mscorlib]System.Type, string)
    IL_0075: stloc.1
    IL_0076: ldloc.1
    IL_0077: ldtoken field bool KeyB::FlagB
    IL_007c: call class [mscorlib]System.Reflection.FieldInfo [mscorlib]System.Reflection.FieldInfo::GetFieldFromHandle(valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0081: call class [System.Core]System.Linq.Expressions.MemberExpression [System.Core]System.Linq.Expressions.Expression::Field(class [System.Core]System.Linq.Expressions.Expression, class [mscorlib]System.Reflection.FieldInfo)
    IL_0086: stloc.2
    IL_0087: ldc.i4.1
    IL_0088: newarr [System.Core]System.Linq.Expressions.ParameterExpression
    IL_008d: dup
    IL_008e: ldc.i4.0
    IL_008f: ldloc.1
    IL_0090: stelem.ref
    IL_0091: stloc.3
    IL_0092: ldc.i4.1
    IL_0093: newarr [mscorlib]System.Object
    IL_0098: dup
    IL_0099: ldc.i4.0
    IL_009a: ldloc.0
    IL_009b: ldloc.2
    IL_009c: ldloc.3
    IL_009d: call class [System.Core]System.Linq.Expressions.Expression`1<!!0> [System.Core]System.Linq.Expressions.Expression::Lambda<class [mscorlib]System.Func`2<class KeyB,bool>>(class [System.Core]System.Linq.Expressions.Expression, class [System.Core]System.Linq.Expressions.ParameterExpression[])
    IL_00a2: ldc.i4.0
    IL_00a3: callvirt instance class Populator Populator::Add<class KeyB>(class [System.Core]System.Linq.Expressions.Expression`1<class [mscorlib]System.Func`2<!!0,bool>>, bool)
    IL_00a8: stelem.ref
    IL_00a9: stfld class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::'<ItemsB>k__BackingField'
    IL_00ae: ret
  }

  .property instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ItemsA()
  {
    .get instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::get_ItemsA()
  }
  .property instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ItemsB()
  {
    .get instance class [mscorlib]System.Collections.Generic.IReadOnlyList`1<object> ExpressionTreeDeinlinedInArrayInitializer::get_ItemsB()
  }
}

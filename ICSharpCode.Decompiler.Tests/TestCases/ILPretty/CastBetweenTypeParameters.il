#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern System.Linq.Expressions
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly CastBetweenTypeParameters { }

// A generic expression tree containing a single Expression.Convert node between
// two unconstrained type parameters. The reconstructed C# cast must go through
// object, because a direct (TTo)value cast between unconstrained type parameters
// is illegal (CS0030).
.class public abstract sealed auto ansi beforefieldinit CastBetweenTypeParameters`1<TTo>
    extends [CORE_ASSEMBLY]System.Object
{
    .class abstract sealed auto ansi nested private beforefieldinit Cache`1<TTo, T>
        extends [CORE_ASSEMBLY]System.Object
    {
        .field assembly static initonly class [CORE_ASSEMBLY]System.Func`2<!T, !TTo> Caster

        .method private hidebysig static
            class [CORE_ASSEMBLY]System.Func`2<!T, !TTo> Get () cil managed
        {
            .maxstack 5
            .locals init (
                [0] class [System.Linq.Expressions]System.Linq.Expressions.ParameterExpression
            )

            IL_0000: ldtoken !T
            IL_0005: call class [CORE_ASSEMBLY]System.Type [CORE_ASSEMBLY]System.Type::GetTypeFromHandle(valuetype [CORE_ASSEMBLY]System.RuntimeTypeHandle)
            IL_000a: ldstr "value"
            IL_000f: call class [System.Linq.Expressions]System.Linq.Expressions.ParameterExpression [System.Linq.Expressions]System.Linq.Expressions.Expression::Parameter(class [CORE_ASSEMBLY]System.Type, string)
            IL_0014: stloc.0
            IL_0015: ldloc.0
            IL_0016: ldtoken !TTo
            IL_001b: call class [CORE_ASSEMBLY]System.Type [CORE_ASSEMBLY]System.Type::GetTypeFromHandle(valuetype [CORE_ASSEMBLY]System.RuntimeTypeHandle)
            IL_0020: call class [System.Linq.Expressions]System.Linq.Expressions.UnaryExpression [System.Linq.Expressions]System.Linq.Expressions.Expression::Convert(class [System.Linq.Expressions]System.Linq.Expressions.Expression, class [CORE_ASSEMBLY]System.Type)
            IL_0025: ldc.i4.1
            IL_0026: newarr [System.Linq.Expressions]System.Linq.Expressions.ParameterExpression
            IL_002b: dup
            IL_002c: ldc.i4.0
            IL_002d: ldloc.0
            IL_002e: stelem.ref
            IL_002f: call class [System.Linq.Expressions]System.Linq.Expressions.Expression`1<!!0> [System.Linq.Expressions]System.Linq.Expressions.Expression::Lambda<class [CORE_ASSEMBLY]System.Func`2<!T, !TTo>>(class [System.Linq.Expressions]System.Linq.Expressions.Expression, class [System.Linq.Expressions]System.Linq.Expressions.ParameterExpression[])
            IL_0034: callvirt instance !0 class [System.Linq.Expressions]System.Linq.Expressions.Expression`1<class [CORE_ASSEMBLY]System.Func`2<!T, !TTo>>::Compile()
            IL_0039: ret
        }

        .method private hidebysig specialname rtspecialname static
            void .cctor () cil managed
        {
            .maxstack 8

            IL_0000: call class [CORE_ASSEMBLY]System.Func`2<!1, !0> class CastBetweenTypeParameters`1/Cache`1<!TTo, !T>::Get()
            IL_0005: stsfld class [CORE_ASSEMBLY]System.Func`2<!1, !0> class CastBetweenTypeParameters`1/Cache`1<!TTo, !T>::Caster
            IL_000a: ret
        }
    }

    .method public hidebysig static
        !TTo From<TFrom> (
            !!TFrom s
        ) cil managed
    {
        .maxstack 8

        IL_0000: ldsfld class [CORE_ASSEMBLY]System.Func`2<!1, !0> class CastBetweenTypeParameters`1/Cache`1<!TTo, !!TFrom>::Caster
        IL_0005: ldarg.0
        IL_0006: callvirt instance !1 class [CORE_ASSEMBLY]System.Func`2<!!TFrom, !TTo>::Invoke(!0)
        IL_000b: ret
    }
}

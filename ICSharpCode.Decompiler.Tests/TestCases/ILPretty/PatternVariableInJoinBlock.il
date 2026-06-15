.class public auto ansi abstract sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.PatternVariableInJoinBlock
    extends [System.Runtime]System.Object
{
    // The 'isinst string' result is stored in a pattern candidate (local 1) inside a
    // nested region (reached only when 'outer' is true), then read again in the join
    // block JOIN. JOIN has two incoming edges (from SET_FALSE and from the fall-through
    // after SET_TRUE), so condition detection cannot inline it into the pattern's 'if'.
    // Both edges originate inside the pattern's then-region, so JOIN is dominated by it,
    // but it is emitted at a shallower nesting level than the pattern test. Promoting
    // the read into a pattern local would move it outside the pattern variable's scope.
    .method public hidebysig static
        void Test (
            object o,
            bool outer,
            bool cond1,
            bool cond2
        ) cil managed
    {
        .maxstack 2
        .locals init (
            [0] bool flag,
            [1] string s
        )

        ldarg.1
        brfalse.s CONT

        ldarg.0
        isinst [System.Runtime]System.String
        stloc.1
        ldloc.1
        brfalse.s NOT_STRING

        ldloc.1
        callvirt instance int32 [System.Runtime]System.String::get_Length()
        ldc.i4.5
        ble.s CHECK2

        ldarg.2
        brfalse.s SET_FALSE

        br.s SET_TRUE

    CHECK2:
        ldarg.3
        brtrue.s SET_TRUE

    SET_FALSE:
        ldc.i4.0
        stloc.0
        br.s JOIN

    SET_TRUE:
        ldc.i4.1
        stloc.0

    JOIN:
        ldloc.0
        brfalse.s PRINT_S

        ldloc.1
        callvirt instance int32 [System.Runtime]System.String::get_Length()
        call void [System.Console]System.Console::WriteLine(int32)

    PRINT_S:
        ldloc.1
        call void [System.Console]System.Console::WriteLine(string)
        br.s CONT

    NOT_STRING:
        ldstr "not string"
        call void [System.Console]System.Console::WriteLine(string)
        br.s CONT

    CONT:
        ldstr "done"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    } // end of method PatternVariableInJoinBlock::Test

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.PatternVariableInJoinBlock

.assembly extern System.Runtime
{
	.publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
	.ver 4:0:0:0
}

.assembly InitializerConditionalValue
{
	.hash algorithm 0x00008004 // SHA1
	.ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions
	extends [System.Runtime]System.Object
{
	.field private bool _decompose
	.field private bool _remove
	.field private int32 _emphasis

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		.maxstack 8

		ldarg.0
		call instance void [System.Runtime]System.Object::.ctor()
		ret
	} // end of method EntryOptions::.ctor

	.method public hidebysig specialname
		instance bool get_Decompose () cil managed
	{
		.maxstack 8

		ldarg.0
		ldfld bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_decompose
		ret
	} // end of method EntryOptions::get_Decompose

	.method public hidebysig specialname
		instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) set_Decompose (
			bool 'value'
		) cil managed
	{
		.maxstack 8

		ldarg.0
		ldarg.1
		stfld bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_decompose
		ret
	} // end of method EntryOptions::set_Decompose

	.method public hidebysig specialname
		instance bool get_Remove () cil managed
	{
		.maxstack 8

		ldarg.0
		ldfld bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_remove
		ret
	} // end of method EntryOptions::get_Remove

	.method public hidebysig specialname
		instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) set_Remove (
			bool 'value'
		) cil managed
	{
		.maxstack 8

		ldarg.0
		ldarg.1
		stfld bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_remove
		ret
	} // end of method EntryOptions::set_Remove

	.method public hidebysig specialname
		instance int32 get_Emphasis () cil managed
	{
		.maxstack 8

		ldarg.0
		ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_emphasis
		ret
	} // end of method EntryOptions::get_Emphasis

	.method public hidebysig specialname
		instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) set_Emphasis (
			int32 'value'
		) cil managed
	{
		.maxstack 8

		ldarg.0
		ldarg.1
		stfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::_emphasis
		ret
	} // end of method EntryOptions::set_Emphasis

	.property instance bool Decompose ()
	{
		.get instance bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::get_Decompose()
		.set instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Decompose(bool)
	}
	.property instance bool Remove ()
	{
		.get instance bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::get_Remove()
		.set instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Remove(bool)
	}
	.property instance int32 Emphasis ()
	{
		.get instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::get_Emphasis()
		.set instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Emphasis(int32)
	}
} // end of class EntryOptions

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.Repro
	extends [System.Runtime]System.Object
{
	.method public hidebysig static
		class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions Build (
			object expression,
			bool condition,
			int32 emphasis
		) cil managed
	{
		.maxstack 5
		.locals init (
			[0] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions entryOptions,
			[1] bool flag
		)

		newobj instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::.ctor()
		stloc.0
		ldloc.0
		ldc.i4.1
		callvirt instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Decompose(bool)
		ldloc.0
		ldc.i4.1
		callvirt instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Remove(bool)
		ldloc.0
		ldarg.1
		brtrue.s SETTRUE
		ldarg.0
		brfalse.s SETFALSE
SETTRUE:
		ldc.i4.1
		stloc.1
		br.s AFTERFLAG
SETFALSE:
		ldc.i4.0
		stloc.1
AFTERFLAG:
		ldloc.1
		brtrue.s USEEMPH
		ldc.i4.2
		br.s DONE
USEEMPH:
		ldarg.2
DONE:
		callvirt instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) ICSharpCode.Decompiler.Tests.TestCases.ILPretty.InitializerConditionalValue.EntryOptions::set_Emphasis(int32)
		ldloc.0
		ret
	} // end of method Repro::Build
} // end of class Repro

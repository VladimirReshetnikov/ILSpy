.class private auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod
	extends [System.Runtime]System.Object
{
	// Fields
	.field private class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container container

	// Methods
	.method public hidebysig 
		instance bool CallSiteWithoutExtensionSyntax (
			string 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2050
		// Header size: 12
		// Code size: 46 (0x2e)
		.maxstack 2
		.locals init (
			[0] valuetype [System.Runtime]System.Nullable`1<bool>,
			[1] valuetype [System.Runtime]System.Nullable`1<bool>
		)

		IL_0000: ldarg.0
		IL_0001: ldfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::container
		IL_0006: dup
		IL_0007: brtrue.s IL_0015

		IL_0009: pop
		IL_000a: ldloca.s 1
		IL_000c: initobj valuetype [System.Runtime]System.Nullable`1<bool>
		IL_0012: ldloc.1
		IL_0013: br.s IL_0025

		IL_0015: call instance string[] ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container::GetItems()
		IL_001a: ldarg.1
		IL_001b: call bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::Contains<string>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, !!0)
		IL_0020: newobj instance void valuetype [System.Runtime]System.Nullable`1<bool>::.ctor(!0)

		IL_0025: stloc.0
		IL_0026: ldloca.s 0
		IL_0028: call instance !0 valuetype [System.Runtime]System.Nullable`1<bool>::GetValueOrDefault()
		IL_002d: ret
	} // end of method NullPropagationExtensionMethod::CallSiteWithoutExtensionSyntax

	.method public hidebysig 
		instance bool CallSiteWithoutExtensionSyntax2 (
			string 'value'
		) cil managed 
	{
		// Method begins at RVA 0x208c
		// Header size: 12
		// Code size: 60 (0x3c)
		.maxstack 2
		.locals init (
			[0] valuetype [System.Runtime]System.Nullable`1<bool>,
			[1] valuetype [System.Runtime]System.Nullable`1<bool>
		)

		IL_0000: ldarg.0
		IL_0001: ldfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::container
		IL_0006: dup
		IL_0007: brtrue.s IL_0015

		IL_0009: pop
		IL_000a: ldloca.s 1
		IL_000c: initobj valuetype [System.Runtime]System.Nullable`1<bool>
		IL_0012: ldloc.1
		IL_0013: br.s IL_0025

		IL_0015: call instance string[] ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container::GetItems()
		IL_001a: ldarg.1
		IL_001b: call bool ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::Contains<string>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>, !!0)
		IL_0020: newobj instance void valuetype [System.Runtime]System.Nullable`1<bool>::.ctor(!0)

		IL_0025: stloc.0
		IL_0026: ldloca.s 0
		IL_0028: call instance bool valuetype [System.Runtime]System.Nullable`1<bool>::get_HasValue()
		IL_002d: brfalse.s IL_003a

		IL_002f: ldloca.s 0
		IL_0031: call instance !0 valuetype [System.Runtime]System.Nullable`1<bool>::GetValueOrDefault()
		IL_0036: brfalse.s IL_003a

		IL_0038: ldc.i4.1
		IL_0039: ret

		IL_003a: ldc.i4.0
		IL_003b: ret
	} // end of method NullPropagationExtensionMethod::CallSiteWithoutExtensionSyntax2

	.method public hidebysig 
		instance int32 CallSiteWithExtensionSyntax () cil managed 
	{
		// Method begins at RVA 0x20d4
		// Header size: 12
		// Code size: 45 (0x2d)
		.maxstack 2
		.locals init (
			[0] valuetype [System.Runtime]System.Nullable`1<int32>,
			[1] valuetype [System.Runtime]System.Nullable`1<int32>
		)

		IL_0000: ldarg.0
		IL_0001: ldfld class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::container
		IL_0006: dup
		IL_0007: brtrue.s IL_0015

		IL_0009: pop
		IL_000a: ldloca.s 1
		IL_000c: initobj valuetype [System.Runtime]System.Nullable`1<int32>
		IL_0012: ldloc.1
		IL_0013: br.s IL_0024

		IL_0015: call instance string[] ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container::GetItems()
		IL_001a: call int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::Count<string>(class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!0>)
		IL_001f: newobj instance void valuetype [System.Runtime]System.Nullable`1<int32>::.ctor(!0)

		IL_0024: stloc.0
		IL_0025: ldloca.s 0
		IL_0027: call instance !0 valuetype [System.Runtime]System.Nullable`1<int32>::GetValueOrDefault()
		IL_002c: ret
	} // end of method NullPropagationExtensionMethod::CallSiteWithExtensionSyntax

	.method public hidebysig
		instance string BoxedValueTypeLocal () cil managed
	{
		.maxstack 1
		.locals init (
			[0] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver valueReceiver
		)

		IL_0000: call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::GetValueReceiver()
		IL_0005: box ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver
		IL_000a: stloc.0
		IL_000b: ldloc.0
		IL_000c: brtrue.s IL_0010

		IL_000e: ldnull
		IL_000f: ret

		IL_0010: ldloc.0
		IL_0011: call string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::FirstOrDefault(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver)
		IL_0016: ret
	} // end of method NullPropagationExtensionMethod::BoxedValueTypeLocal

	.method public hidebysig
		instance string BoxedValueTypeStack () cil managed
	{
		.maxstack 1

		IL_0000: call valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::GetValueReceiver()
		IL_0005: box ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver
		IL_000a: dup
		IL_000b: brtrue.s IL_0010

		IL_000d: pop
		IL_000e: ldnull
		IL_000f: ret

		IL_0010: call string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::FirstOrDefault(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver)
		IL_0015: ret
	} // end of method NullPropagationExtensionMethod::BoxedValueTypeStack

	.method public hidebysig
		instance string BoxedNullableValueType () cil managed
	{
		.maxstack 1

		IL_0000: call valuetype [System.Runtime]System.Nullable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod::GetNullableValueReceiver()
		IL_0005: box valuetype [System.Runtime]System.Nullable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver>
		IL_000a: castclass ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver
		IL_000f: dup
		IL_0010: brtrue.s IL_0015

		IL_0012: pop
		IL_0013: ldnull
		IL_0014: ret

		IL_0015: call string ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions::FirstOrDefault(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver)
		IL_001a: ret
	} // end of method NullPropagationExtensionMethod::BoxedNullableValueType

	.method private hidebysig static
		valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver GetValueReceiver () cil managed
	{
		.maxstack 1
		.locals init (
			[0] valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver
		IL_0008: ldloc.0
		IL_0009: ret
	} // end of method NullPropagationExtensionMethod::GetValueReceiver

	.method private hidebysig static
		valuetype [System.Runtime]System.Nullable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver> GetNullableValueReceiver () cil managed
	{
		.maxstack 1
		.locals init (
			[0] valuetype [System.Runtime]System.Nullable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver>
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj valuetype [System.Runtime]System.Nullable`1<valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver>
		IL_0008: ldloc.0
		IL_0009: ret
	} // end of method NullPropagationExtensionMethod::GetNullableValueReceiver

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x210d
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	} // end of method NullPropagationExtensionMethod::.ctor

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.NullPropagationExtensionMethod

.class private auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container
	extends [System.Runtime]System.Object
{
	// Methods
	.method public hidebysig 
		instance string[] GetItems () cil managed 
	{
		// Method begins at RVA 0x2115
		// Header size: 1
		// Code size: 2 (0x2)
		.maxstack 8

		IL_0000: ldnull
		IL_0001: ret
	} // end of method Container::GetItems

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x2118
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [System.Runtime]System.Object::.ctor()
		IL_0006: ret
	} // end of method Container::.ctor

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Container

.class private auto ansi abstract sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig static 
		bool Contains<T> (
			class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!T> source,
			!!T 'value'
		) cil managed 
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2120
		// Header size: 1
		// Code size: 2 (0x2)
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	} // end of method Extensions::Contains

	.method public hidebysig static 
		int32 Count<T> (
			class [System.Runtime]System.Collections.Generic.IEnumerable`1<!!T> source
		) cil managed 
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2123
		// Header size: 1
		// Code size: 2 (0x2)
		.maxstack 8

		IL_0000: ldc.i4.0
		IL_0001: ret
	} // end of method Extensions::Count

	.method public hidebysig static
		string FirstOrDefault (
			class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver source
		) cil managed
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)
		.maxstack 1

		IL_0000: ldnull
		IL_0001: ret
	} // end of method Extensions::FirstOrDefault

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.Extensions

.class private auto ansi abstract sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ArrayExtensions
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig static 
		bool Contains<T> (
			!!T[] source,
			!!T 'value'
		) cil managed 
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2126
		// Header size: 1
		// Code size: 2 (0x2)
		.maxstack 8

		IL_0000: ldc.i4.1
		IL_0001: ret
	} // end of method ArrayExtensions::Contains

} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ArrayExtensions

.class private interface abstract auto ansi ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver
{
} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver

.class private sequential ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver
	extends [System.Runtime]System.ValueType
	implements ICSharpCode.Decompiler.Tests.TestCases.ILPretty.IValueReceiver
{
} // end of class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ValueReceiver

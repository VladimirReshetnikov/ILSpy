#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly FileLocalTypes { }
.module FileLocalTypes.dll

// A type declared 'file' in C# 11 is emitted with the metadata name
// '<file>F<hash>__Name', which is how the compiler keeps same-named declarations in different
// files apart. Both file-local types here share one hash, so they came from one source file and
// can see each other. 'file' also replaces the accessibility keyword rather than joining it.

.class private auto ansi abstract sealed beforefieldinit '<Helpers_g>FAB12CD34__Support'
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig static int32 Compute (int32 'value') cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ret
	}
}

.class private auto ansi beforefieldinit '<Helpers_g>FAB12CD34__Holder'
	extends [CORE_ASSEMBLY]System.Object
{
	.field private int32 count

	.method public hidebysig instance int32 Next () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 '<Helpers_g>FAB12CD34__Holder'::count
		IL_0006: call int32 '<Helpers_g>FAB12CD34__Support'::Compute(int32)
		IL_000b: ret
	}

	.method public hidebysig specialname rtspecialname instance void .ctor () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public auto ansi beforefieldinit Consumer
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig instance int32 Use () cil managed
	{
		.maxstack 8
		IL_0000: newobj instance void '<Helpers_g>FAB12CD34__Holder'::.ctor()
		IL_0005: call instance int32 '<Helpers_g>FAB12CD34__Holder'::Next()
		IL_000a: ret
	}

	.method public hidebysig specialname rtspecialname instance void .ctor () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
		IL_0006: ret
	}
}

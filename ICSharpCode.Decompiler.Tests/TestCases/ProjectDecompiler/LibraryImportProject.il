.assembly extern mscorlib
{
}

.assembly LibraryImportProject
{
}

.module LibraryImportProject.dll

.class public auto ansi sealed beforefieldinit System.CodeDom.Compiler.GeneratedCodeAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (string tool, string version) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.LibraryImportAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (string libraryName) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit MarkerAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (string text) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi beforefieldinit NativeMethods
	extends [mscorlib]System.Object
{
	.method public hidebysig static
		int32 Materialized (int32 'value') cil managed
	{
		.custom instance void System.Runtime.InteropServices.LibraryImportAttribute::.ctor(string) = (
			01 00 0c 6d 61 74 65 72 69 61 6c 69 7a 65 64 00
			00
		)
		.custom instance void System.CodeDom.Compiler.GeneratedCodeAttribute::.ctor(string, string) = (
			01 00 28 4d 69 63 72 6f 73 6f 66 74 2e 49 6e 74
			65 72 6f 70 2e 4c 69 62 72 61 72 79 49 6d 70 6f
			72 74 47 65 6e 65 72 61 74 6f 72 03 31 2e 30 00
			00
		)
		.custom instance void MarkerAttribute::.ctor(string) = (
			01 00 04 6b 65 65 70 00 00
		)

		ldarg.0
		ret
	}

	.method public hidebysig static pinvokeimpl("native" as "AlreadyImported" winapi)
		int32 AlreadyImported (int32 'value') cil managed preservesig
	{
		.custom instance void System.Runtime.InteropServices.LibraryImportAttribute::.ctor(string) = (
			01 00 06 6e 61 74 69 76 65 00 00
		)
	}

	.method public hidebysig static
		int32 OrdinaryLibraryImport (int32 'value') cil managed
	{
		.custom instance void System.Runtime.InteropServices.LibraryImportAttribute::.ctor(string) = (
			01 00 08 6f 72 64 69 6e 61 72 79 00 00
		)

		ldarg.0
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

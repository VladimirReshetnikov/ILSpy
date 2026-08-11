.assembly extern mscorlib
{
}

.assembly GeneratedRegexProject
{
}

.module GeneratedRegexProject.dll

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

.class public auto ansi sealed beforefieldinit System.Text.RegularExpressions.GeneratedRegexAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (string pattern) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class private auto ansi sealed beforefieldinit 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper'
	extends [mscorlib]System.Object
{
	.custom instance void System.CodeDom.Compiler.GeneratedCodeAttribute::.ctor(string, string) = (
		01 00 28 53 79 73 74 65 6D 2E 54 65 78 74 2E 52
		65 67 75 6C 61 72 45 78 70 72 65 73 73 69 6F 6E
		73 2E 47 65 6E 65 72 61 74 6F 72 06 31 30 2E 30
		2E 30 00 00
	)

	.field public static initonly class 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper' Instance

	.method private hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method private hidebysig specialname rtspecialname static
		void .cctor () cil managed
	{
		newobj instance void 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper'::.ctor()
		stsfld class 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper' 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper'::Instance
		ret
	}
}

.class public auto ansi beforefieldinit Consumer
	extends [mscorlib]System.Object
{
	.method private hidebysig specialname static
		object get_Regex () cil managed
	{
		ldsfld class 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper' 'System.Text.RegularExpressions.Generated.<RegexGenerator_g>FAB12CD34__GeneratedRegexHelper'::Instance
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.property object Regex()
	{
		.custom instance void System.Text.RegularExpressions.GeneratedRegexAttribute::.ctor(string) = (
			01 00 01 61 00 00
		)
		.custom instance void System.CodeDom.Compiler.GeneratedCodeAttribute::.ctor(string, string) = (
			01 00 28 53 79 73 74 65 6D 2E 54 65 78 74 2E 52
			65 67 75 6C 61 72 45 78 70 72 65 73 73 69 6F 6E
			73 2E 47 65 6E 65 72 61 74 6F 72 06 31 30 2E 30
			2E 30 00 00
		)
		.get object Consumer::get_Regex()
	}
}

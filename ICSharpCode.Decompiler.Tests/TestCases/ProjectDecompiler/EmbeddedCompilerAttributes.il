.assembly extern mscorlib
{
}

.assembly EmbeddedCompilerAttributes
{
}

.module EmbeddedCompilerAttributes.dll

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
	extends [mscorlib]System.Attribute
{
	.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsByRefLikeAttribute
	extends [mscorlib]System.Attribute
{
	.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

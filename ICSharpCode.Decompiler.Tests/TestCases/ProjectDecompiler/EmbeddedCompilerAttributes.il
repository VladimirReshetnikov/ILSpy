.assembly extern mscorlib
{
}

.assembly EmbeddedCompilerAttributes
{
}

.module EmbeddedCompilerAttributes.dll

.custom instance void System.Runtime.CompilerServices.NullablePublicOnlyAttribute::.ctor(bool) = (
	01 00 00 00 00
)

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

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullablePublicOnlyAttribute
	extends [mscorlib]System.Attribute
{
	.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
		01 00 00 00
	)

	.field public initonly bool IncludesInternals

	.method public hidebysig specialname rtspecialname
		instance void .ctor (bool includesInternals) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ldarg.0
		ldarg.1
		stfld bool System.Runtime.CompilerServices.NullablePublicOnlyAttribute::IncludesInternals
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
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

.class public sequential ansi sealed beforefieldinit ReadOnlyValue
	extends [mscorlib]System.ValueType
{
	.custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
		01 00 00 00
	)

	.field public initonly int32 Value
}

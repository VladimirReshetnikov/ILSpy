.assembly extern mscorlib
{
}

.assembly CompilerGeneratedFileLocalProject
{
}

.module CompilerGeneratedFileLocalProject.dll

.class private auto ansi sealed beforefieldinit 'Example.<PrivateImplementationDetails>FABC1234__BytesToStringHelper'
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig static
		int32 GetValue () cil managed
	{
		ldc.i4.s 42
		ret
	}
}

.class private auto ansi sealed beforefieldinit 'Example.<PrivateImplementationDetails>FABC1234__SourceDeclaredHelper'
	extends [mscorlib]System.Object
{
	.method public hidebysig static
		int32 GetValue () cil managed
	{
		ldc.i4.s 43
		ret
	}
}

.class public auto ansi beforefieldinit Example.Consumer
	extends [mscorlib]System.Object
{
	.method public hidebysig static
		int32 Read () cil managed
	{
		call int32 'Example.<PrivateImplementationDetails>FABC1234__BytesToStringHelper'::GetValue()
		ret
	}

	.method public hidebysig static
		int32 ReadSourceDeclared () cil managed
	{
		call int32 'Example.<PrivateImplementationDetails>FABC1234__SourceDeclaredHelper'::GetValue()
		ret
	}
}

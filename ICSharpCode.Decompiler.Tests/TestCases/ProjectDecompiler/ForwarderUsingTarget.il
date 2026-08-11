.assembly extern mscorlib
{
	.ver 4:0:0:0
}

.assembly ForwarderUsingTarget
{
	.ver 1:0:0:0
}

.module ForwarderUsingTarget.dll

.class public auto ansi beforefieldinit Forwarded.Process
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.assembly extern mscorlib
{
	.ver 4:0:0:0
}

.assembly extern ForwarderUsingTarget
{
	.ver 1:0:0:0
}

.assembly ForwarderUsingProject
{
	.ver 1:0:0:0
	.custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = (
		01 00 02 00 00 00 00 00
	)
	.custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = (
		01 00 04 6b 65 65 70 00 00
	)
}

.module ForwarderUsingProject.dll

.class extern forwarder Forwarded.Process
{
	.assembly extern ForwarderUsingTarget
}

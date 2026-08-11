.assembly extern mscorlib
{
}

.assembly EmbeddedInteropEventProject
{
}

.module EmbeddedInteropEventProject.dll

.class interface public abstract auto ansi import Interop.EventSource
{
	.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = (
		01 00 24 31 31 31 31 31 31 31 31 2d 31 31 31 31 2d 31 31
		31 31 2d 31 31 31 31 2d 31 31 31 31 31 31 31 31 31 31 31
		31 00 00
	)
}

// Roslyn emits this metadata shape for an embedded No-PIA COM event interface.
// The Import flag has no matching GuidAttribute and therefore cannot be written in C#.
.class interface public abstract auto ansi import Interop.EventWrapper
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComEventInterfaceAttribute::.ctor(class [mscorlib]System.Type, class [mscorlib]System.Type) = (
		01 00 13 49 6e 74 65 72 6f 70 2e 45 76 65 6e 74 53 6f
		75 72 63 65 13 49 6e 74 65 72 6f 70 2e 45 76 65 6e 74
		53 6f 75 72 63 65 00 00
	)
	.custom instance void [mscorlib]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor(string, string) = (
		01 00 0d 66 69 78 74 75 72 65 2d 73 63 6f 70 65 14 49
		6e 74 65 72 6f 70 2e 45 76 65 6e 74 57 72 61 70 70 65
		72 00 00
	)
}

// A Guid-bearing embedded COM event interface is directly representable in C#.
.class interface public abstract auto ansi import Interop.GuidedEventWrapper
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = (
		01 00 24 32 32 32 32 32 32 32 32 2d 32 32 32 32 2d 32 32
		32 32 2d 32 32 32 32 2d 32 32 32 32 32 32 32 32 32 32 32
		32 00 00
	)
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComEventInterfaceAttribute::.ctor(class [mscorlib]System.Type, class [mscorlib]System.Type) = (
		01 00 13 49 6e 74 65 72 6f 70 2e 45 76 65 6e 74 53 6f
		75 72 63 65 13 49 6e 74 65 72 6f 70 2e 45 76 65 6e 74
		53 6f 75 72 63 65 00 00
	)
	.custom instance void [mscorlib]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor() = (
		01 00 00 00
	)
}

.class interface public abstract auto ansi import Interop.Application
	implements Interop.EventWrapper
{
	.custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = (
		01 00 24 33 33 33 33 33 33 33 33 2d 33 33 33 33 2d 33 33
		33 33 2d 33 33 33 33 2d 33 33 33 33 33 33 33 33 33 33 33
		33 00 00
	)
}

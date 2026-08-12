// Companion library for RefLocalUninitialized.il: a minimal stand-in for the
// System.Runtime.CompilerServices.Unsafe package. The main fixture references this assembly so
// that 'Unsafe.SkipInit<T>(out T)' and 'Unsafe.NullRef<T>()' resolve in the decompiler's type
// system: their emission is resolution-gated, and without a resolvable Unsafe type the
// declarations would fall back to zero/placeholder initializers instead of the spellings the
// fixture is guarding.

.assembly extern System.Runtime
{
	.ver 0:0:0:0
}
.assembly RefLocalUninitialized.Library
{
	.ver 1:0:0:0
}
.module RefLocalUninitialized.Library.dll

.class public abstract sealed auto ansi beforefieldinit System.Runtime.CompilerServices.Unsafe
	extends [System.Runtime]System.Object
{
	.method public hidebysig static void SkipInit<T> ([out] !!T& 'value') cil managed
	{
		ret
	}

	.method public hidebysig static !!T& NullRef<T> () cil managed
	{
		ldc.i4.0
		conv.u
		ret
	}
}

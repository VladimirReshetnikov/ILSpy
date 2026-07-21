// Regression test for a ref local whose managed-pointer slot is left unassigned by the IL
// (locals signature without 'init') and whose initial value is observed before any store.
// DeclareVariables classifies such a local as NeedsSkipInit. A ref local cannot be declared
// uninitialized in C# and cannot be passed as an 'out' argument, so the declaration must be
// rendered as 'scoped ref T reference = ref Unsafe.NullRef<T>();' -- not 'Unsafe.SkipInit(out
// ref T reference)', which is illegal (CS8388) and leaves the following 'reference = ref value;'
// reassignment unable to compile (CS8373).
//
// The 'scoped' modifier is required, not cosmetic: 'Unsafe.NullRef<T>()' has caller-context
// ref-safe-context, so without 'scoped' the reassignment of a method-scoped reference (here the
// address of the by-value parameter 'value') would be rejected with CS8374. The non-ref local in
// the same method still uses the valid 'Unsafe.SkipInit(out T result)' form, guarding that only
// the ref case changed.

.assembly extern System.Runtime
{
	.ver 0:0:0:0
}
.assembly RefLocalUninitialized { }
.module RefLocalUninitialized.dll

.class public abstract sealed auto ansi beforefieldinit RefLocalUninitialized
	extends [System.Runtime]System.Object
{
	.method public hidebysig static
		!!T LoopRef<T> (!!T 'value', int32 count) cil managed
	{
		.maxstack 3
		.locals (
			[0] !!T&,
			[1] !!T,
			[2] int32
		)

		ldc.i4.0
		stloc.2
		br.s CHECK

	LOOP:
		ldloc.0
		ldobj !!T
		stloc.1
		ldarga.s 'value'
		stloc.0
		ldloc.2
		ldc.i4.1
		add
		stloc.2

	CHECK:
		ldloc.2
		ldarg.1
		blt.s LOOP

		ldloc.1
		ret
	}
}

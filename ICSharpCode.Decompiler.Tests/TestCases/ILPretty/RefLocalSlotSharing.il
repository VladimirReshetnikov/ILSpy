// Regression test for a by-ref local sharing a single locals-signature slot across several
// disjoint scopes. Live-range splitting turns slot [0] into one ref local per store; each must
// keep its own 'ref T x = ref ...;' declaration. Previously all of them received the same name
// (because they share IL slot 0), which forced them to be merged into a single hoisted
// declaration 'ref T reference;' -- illegal for a ref local (CS8174: a ref local must be
// initialized at its declaration).

.class interface public abstract auto ansi IFlagged
{
	.method public hidebysig newslot specialname abstract virtual
		instance int32 get_Flags () cil managed
	{
	}

	.method public hidebysig newslot specialname abstract virtual
		instance void set_Flags (int32 'value') cil managed
	{
	}

	.property instance int32 Flags()
	{
		.get instance int32 IFlagged::get_Flags()
		.set instance void IFlagged::set_Flags(int32)
	}
}

.class public abstract sealed auto ansi beforefieldinit RefLocalSlotSharing
	extends [System.Runtime]System.Object
{
	.method public hidebysig static
		void SharedRefLocalSlot<class (IFlagged) T> (!!T context, bool a, bool b) cil managed
	{
		.maxstack 3
		.locals init (
			[0] !!T&
		)

		IL_0000: ldarg.1
		IL_0001: brfalse.s IL_0020

		IL_0003: ldarga.s context
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldloc.0
		IL_0008: constrained. !!T
		IL_000e: callvirt instance int32 IFlagged::get_Flags()
		IL_0013: ldc.i4.1
		IL_0014: or
		IL_0015: constrained. !!T
		IL_001b: callvirt instance void IFlagged::set_Flags(int32)

		IL_0020: ldarg.2
		IL_0021: brfalse.s IL_0040

		IL_0023: ldarga.s context
		IL_0025: stloc.0
		IL_0026: ldloc.0
		IL_0027: ldloc.0
		IL_0028: constrained. !!T
		IL_002e: callvirt instance int32 IFlagged::get_Flags()
		IL_0033: ldc.i4.2
		IL_0034: or
		IL_0035: constrained. !!T
		IL_003b: callvirt instance void IFlagged::set_Flags(int32)

		IL_0040: ldarga.s context
		IL_0042: stloc.0
		IL_0043: ldloc.0
		IL_0044: ldloc.0
		IL_0045: constrained. !!T
		IL_004b: callvirt instance int32 IFlagged::get_Flags()
		IL_0050: ldc.i4.4
		IL_0051: or
		IL_0052: constrained. !!T
		IL_0058: callvirt instance void IFlagged::set_Flags(int32)

		IL_005d: ret
	}
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ExplicitInterfacePropertyOverride
{
  .ver 1:0:0:0
}
.module ExplicitInterfacePropertyOverride.dll

.class interface public abstract auto ansi IHasToken
{
	.method public hidebysig specialname newslot abstract virtual
		instance object get_Token () cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance int32 get_Value () cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance void set_Value (int32 'value') cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance void add_Changed (class [mscorlib]System.EventHandler 'value') cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance void remove_Changed (class [mscorlib]System.EventHandler 'value') cil managed
	{
	}

	.property instance object Token ()
	{
		.get instance object IHasToken::get_Token()
	}

	.property instance int32 Value ()
	{
		.get instance int32 IHasToken::get_Value()
		.set instance void IHasToken::set_Value(int32)
	}

	.event [mscorlib]System.EventHandler Changed
	{
		.addon instance void IHasToken::add_Changed(class [mscorlib]System.EventHandler)
		.removeon instance void IHasToken::remove_Changed(class [mscorlib]System.EventHandler)
	}
}

.class public auto ansi beforefieldinit C
	extends [mscorlib]System.Object
	implements IHasToken
{
	// The class explicitly implements the interface accessors as methods carrying
	// a .override, but declares no property or event metadata rows of its own.

	.method private final hidebysig specialname newslot virtual
		instance object IHasToken.get_Token () cil managed
	{
		.override method instance object IHasToken::get_Token()
		ldnull
		ret
	}

	.method private final hidebysig specialname newslot virtual
		instance int32 IHasToken.get_Value () cil managed
	{
		.override method instance int32 IHasToken::get_Value()
		ldc.i4.0
		ldc.i4.0
		add
		ret
	}

	.method private final hidebysig specialname newslot virtual
		instance void IHasToken.set_Value (int32 'value') cil managed
	{
		.override method instance void IHasToken::set_Value(int32)
		nop
		nop
		ret
	}

	.method private final hidebysig specialname newslot virtual
		instance void IHasToken.add_Changed (class [mscorlib]System.EventHandler 'value') cil managed
	{
		.override method instance void IHasToken::add_Changed(class [mscorlib]System.EventHandler)
		nop
		nop
		ret
	}

	.method private final hidebysig specialname newslot virtual
		instance void IHasToken.remove_Changed (class [mscorlib]System.EventHandler 'value') cil managed
	{
		.override method instance void IHasToken::remove_Changed(class [mscorlib]System.EventHandler)
		nop
		nop
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

} // end of class C

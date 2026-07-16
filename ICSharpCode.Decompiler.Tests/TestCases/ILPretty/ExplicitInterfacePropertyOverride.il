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
	.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
		01 00 04 49 74 65 6D 00 00
	)

	.method public hidebysig specialname newslot abstract virtual
		instance object get_Token () cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance int32& get_Reference () cil managed
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
		instance string get_Item (int32 index) cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance void set_Item (int32 index, string 'value') cil managed
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

	.property instance int32& Reference ()
	{
		.get instance int32& IHasToken::get_Reference()
	}

	.property instance int32 Value ()
	{
		.get instance int32 IHasToken::get_Value()
		.set instance void IHasToken::set_Value(int32)
	}

	.property instance string Item (int32)
	{
		.get instance string IHasToken::get_Item(int32)
		.set instance void IHasToken::set_Item(int32, string)
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

.class public auto ansi beforefieldinit ForwardingC
	extends [mscorlib]System.Object
	implements IHasToken
{
	// The implementing methods have ordinary names and remain directly callable, so the decompiler
	// must retain them and add explicit property/event forwarders for their .override directives.
	.field private int32 'storage'

	.method private final hidebysig newslot virtual
		instance object ReadToken () cil managed
	{
		.override method instance object IHasToken::get_Token()
		ldnull
		ret
	}

	.method private final hidebysig newslot virtual
		instance int32& ReadReference () cil managed
	{
		.override method instance int32& IHasToken::get_Reference()
		ldarg.0
		ldflda int32 ForwardingC::'storage'
		ret
	}

	.method private final hidebysig newslot virtual
		instance int32 ReadValue () cil managed
	{
		.override method instance int32 IHasToken::get_Value()
		ldc.i4.1
		ret
	}

	.method private final hidebysig newslot virtual
		instance void WriteValue (int32 'value') cil managed
	{
		.override method instance void IHasToken::set_Value(int32)
		ret
	}

	.method private final hidebysig newslot virtual
		instance string ReadItem (int32 index) cil managed
	{
		.override method instance string IHasToken::get_Item(int32)
		ldnull
		ret
	}

	.method private final hidebysig newslot virtual
		instance void WriteItem (int32 index, string 'value') cil managed
	{
		.override method instance void IHasToken::set_Item(int32, string)
		ret
	}

	.method private final hidebysig newslot virtual
		instance void Subscribe (class [mscorlib]System.EventHandler 'value') cil managed
	{
		.override method instance void IHasToken::add_Changed(class [mscorlib]System.EventHandler)
		ret
	}

	.method private final hidebysig newslot virtual
		instance void Unsubscribe (class [mscorlib]System.EventHandler 'value') cil managed
	{
		.override method instance void IHasToken::remove_Changed(class [mscorlib]System.EventHandler)
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

// The type system adds a synthetic parameterless constructor to this nested generic struct.
// That FakeMethod has no MethodDefinition row, so MethodImpl lookup must ignore it.
.class public auto ansi beforefieldinit GenericContainer`1<class T>
	extends [mscorlib]System.Object
{
	.class nested public sequential ansi sealed beforefieldinit Nested<class T>
		extends [mscorlib]System.ValueType
		implements class [mscorlib]System.Collections.Generic.IEnumerator`1<!T>,
		           [mscorlib]System.IDisposable,
		           [mscorlib]System.Collections.IEnumerator
	{
		.field private !T 'value'

		.method public final hidebysig specialname newslot virtual
			instance !T get_Current () cil managed
		{
			ldarg.0
			ldfld !T valuetype GenericContainer`1/Nested<!T>::'value'
			ret
		}

		.property instance !T Current ()
		{
			.get instance !0 GenericContainer`1/Nested::get_Current()
		}

		.method private final hidebysig specialname newslot virtual
			instance object System.Collections.IEnumerator.get_Current () cil managed
		{
			.override method instance object [mscorlib]System.Collections.IEnumerator::get_Current()
			ldarg.0
			call instance !0 valuetype GenericContainer`1/Nested<!T>::get_Current()
			box !T
			ret
		}

		.property instance object System.Collections.IEnumerator.Current ()
		{
			.get instance object GenericContainer`1/Nested::System.Collections.IEnumerator.get_Current()
		}

		.method public final hidebysig newslot virtual
			instance bool MoveNext () cil managed
		{
			ldc.i4.0
			ret
		}

		.method public final hidebysig newslot virtual
			instance void Reset () cil managed
		{
			ret
		}

		.method public final hidebysig newslot virtual
			instance void Dispose () cil managed
		{
			ret
		}

		.method public hidebysig specialname rtspecialname
			instance void .ctor (!T 'value') cil managed
		{
			ldarg.0
			ldarg.1
			stfld !T valuetype GenericContainer`1/Nested<!T>::'value'
			ret
		}
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

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

.class public auto ansi beforefieldinit OrdinaryNamedPropertyAccessors
	extends [mscorlib]System.Object
{
	// Obfuscators may rename property accessors without setting SpecialName. The methods remain
	// callable metadata entry points even though C# cannot control a property's accessor names.
	.field private int32 'read'
	.field private int32 'write'

	.method public hidebysig
		instance int32 read () cil managed
	{
		ldarg.0
		ldfld int32 OrdinaryNamedPropertyAccessors::'read'
		ret
	}

	.method public hidebysig
		instance void write (int32 'value') cil managed
	{
		ldarg.0
		ldarg.1
		stfld int32 OrdinaryNamedPropertyAccessors::'write'
		ret
	}

	.property instance int32 Value ()
	{
		.get instance int32 OrdinaryNamedPropertyAccessors::read()
		.set instance void OrdinaryNamedPropertyAccessors::write(int32)
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class interface public abstract auto ansi IImplicitAccessors
{
	.method public hidebysig specialname newslot abstract virtual
		instance int32 get_Number () cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance void set_Number (int32 'value') cil managed
	{
	}

	.property instance int32 Number ()
	{
		.get instance int32 IImplicitAccessors::get_Number()
		.set instance void IImplicitAccessors::set_Number(int32)
	}
}

.class public auto ansi beforefieldinit ImplicitAccessors
	extends [mscorlib]System.Object
	implements IImplicitAccessors
{
	// CLR interface mapping uses the method names and signatures even though these methods are
	// not accessors and the implementing type has no property metadata row. C# requires a helper.
	.field private int32 'value'

	.method public final hidebysig newslot virtual
		instance int32 get_Number () cil managed
	{
		ldarg.0
		ldfld int32 ImplicitAccessors::'value'
		ret
	}

	.method public final hidebysig newslot virtual
		instance void set_Number (int32 'value') cil managed
	{
		ldarg.0
		ldarg.1
		stfld int32 ImplicitAccessors::'value'
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

.class interface public abstract auto ansi IOrdinaryAccessorNames
{
	.method public hidebysig newslot abstract virtual
		instance int32 get_Number () cil managed
	{
	}

	.method public hidebysig newslot abstract virtual
		instance void set_Number (int32 'value') cil managed
	{
	}
}

.class public auto ansi beforefieldinit PropertyAccessors
	extends [mscorlib]System.Object
	implements IOrdinaryAccessorNames
{
	// This is the mirror image of ImplicitAccessors: the implementing methods are accessors,
	// while the interface slots are ordinary methods. CLR accepts the mapping; C# needs helpers.
	.field private int32 'value'

	.method public final hidebysig specialname newslot virtual
		instance int32 get_Number () cil managed
	{
		ldarg.0
		ldfld int32 PropertyAccessors::'value'
		ret
	}

	.method public final hidebysig specialname newslot virtual
		instance void set_Number (int32 'value') cil managed
	{
		ldarg.0
		ldarg.1
		stfld int32 PropertyAccessors::'value'
		ret
	}

	.property instance int32 Number ()
	{
		.get instance int32 PropertyAccessors::get_Number()
		.set instance void PropertyAccessors::set_Number(int32)
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class public abstract auto ansi beforefieldinit BaseWithProperty
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname newslot abstract virtual
		instance string get_Name () cil managed
	{
	}

	.property instance string Name ()
	{
		.get instance string BaseWithProperty::get_Name()
	}
}

.class public auto ansi beforefieldinit DerivedAccessor
	extends BaseWithProperty
{
	// The virtual slot overrides a base property accessor but has no property row in this type.
	.method public hidebysig specialname virtual
		instance string get_Name () cil managed
	{
		ldstr "derived"
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void BaseWithProperty::.ctor()
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

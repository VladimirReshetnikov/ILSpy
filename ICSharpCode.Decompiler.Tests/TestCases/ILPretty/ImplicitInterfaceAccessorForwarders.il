.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ImplicitInterfaceAccessorForwarders
{
  .ver 1:0:0:0
}
.module ImplicitInterfaceAccessorForwarders.dll

// A tlbimp-style primary interop assembly. A parameterized property cannot be spelled in C#,
// so every derived interface re-declares the inherited COM members as ordinary methods with
// the accessor names, without SpecialName and without a MethodImpl row.
.class interface public abstract auto ansi import IComBase
{
	.method public hidebysig specialname newslot abstract virtual
		instance string get_Value () cil managed
	{
	}

	.method public hidebysig specialname newslot abstract virtual
		instance string get_TemplatePath (string projectType) cil managed
	{
	}

	.property instance string Value ()
	{
		.get instance string IComBase::get_Value()
	}

	.property instance string TemplatePath (string)
	{
		.get instance string IComBase::get_TemplatePath(string)
	}
}

.class interface public abstract auto ansi import IComDerived
	implements IComBase
{
	.method public hidebysig newslot abstract virtual
		instance string get_Value () cil managed
	{
	}

	.method public hidebysig newslot abstract virtual
		instance string get_TemplatePath (string projectType) cil managed
	{
	}
}

.class interface public abstract auto ansi IPlainBase
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
		.get instance int32 IPlainBase::get_Number()
		.set instance void IPlainBase::set_Number(int32)
	}
}

// The same shape without COM interop: an interface member still never implicitly implements
// a base interface member, so no default implementation may be synthesized here either.
.class interface public abstract auto ansi IPlainDerived
	implements IPlainBase
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

.class interface public abstract auto ansi IOrdinaryBase
{
	.method public hidebysig newslot abstract virtual
		instance int32 get_Flag () cil managed
	{
	}
}

// The mirror image: the derived interface declares a real property whose accessors match the
// base interface's ordinary methods by name and signature.
.class interface public abstract auto ansi IPropertyDerived
	implements IOrdinaryBase
{
	.method public hidebysig specialname newslot abstract virtual
		instance int32 get_Flag () cil managed
	{
	}

	.property instance int32 Flag ()
	{
		.get instance int32 IPropertyDerived::get_Flag()
	}
}

// A MethodImpl row inside an interface really does implement the base interface member, so the
// explicit forwarder for this default interface method must still be emitted.
.class interface public abstract auto ansi IOverridingDerived
	implements IPlainBase
{
	.method public hidebysig newslot virtual
		instance int32 ReadNumber () cil managed
	{
		.override method instance int32 IPlainBase::get_Number()
		ldc.i4.1
		ret
	}

	.method public hidebysig newslot virtual
		instance void WriteNumber (int32 'value') cil managed
	{
		.override method instance void IPlainBase::set_Number(int32)
		ret
	}
}

// Implicit interface mapping is a class concept, so the forwarders below are required here.
.class public auto ansi beforefieldinit ComConsumer
	extends [mscorlib]System.Object
	implements IComBase
{
	.method public final hidebysig newslot virtual
		instance string get_Value () cil managed
	{
		ldnull
		ret
	}

	// C# has no syntax for implementing a parameterized property, so this slot gets no forwarder.
	.method public final hidebysig newslot virtual
		instance string get_TemplatePath (string projectType) cil managed
	{
		ldnull
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

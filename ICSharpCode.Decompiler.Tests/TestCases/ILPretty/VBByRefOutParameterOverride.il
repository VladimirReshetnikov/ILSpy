.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly VBByRefOutParameterOverride
{
  .ver 1:0:0:0
}
.module VBByRefOutParameterOverride.dll

.class public abstract auto ansi beforefieldinit Base
	extends [mscorlib]System.Object
{
	// genuine 'ref': the byref parameter carries no [out] flag
	.method public hidebysig newslot virtual
		instance bool TryTransform (object expression, object& reference) cil managed
	{
		ldarg.2
		ldnull
		stind.ref
		ldc.i4.0
		ret
	}

	// genuine 'out': the byref parameter carries the [out] flag
	.method public hidebysig newslot virtual
		instance void Replace (object expression, [out] object& reference) cil managed
	{
		ldarg.2
		ldnull
		stind.ref
		ret
	}

	.method family hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class public auto ansi beforefieldinit Derived
	extends Base
{
	// Visual-Basic-style ByRef override of a 'ref': the byref parameter carries the
	// [out] flag even though the overridden base parameter is 'ref'. Rendering it 'out'
	// would break the override (CS0115), so the direction follows the base contract and
	// the [out] flag is surfaced as an explicit attribute.
	.method public hidebysig strict virtual
		instance bool TryTransform (object expression, [out] object& reference) cil managed
	{
		ldarg.2
		ldnull
		stind.ref
		ldc.i4.0
		ret
	}

	// override of a genuine 'out' with a matching [out] byref parameter: stays 'out'
	.method public hidebysig strict virtual
		instance void Replace (object expression, [out] object& reference) cil managed
	{
		ldarg.2
		ldnull
		stind.ref
		ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void Base::.ctor()
		ret
	}
}

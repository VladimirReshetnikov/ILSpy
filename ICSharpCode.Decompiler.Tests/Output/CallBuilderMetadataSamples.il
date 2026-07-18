.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 4:0:0:0
}
.assembly CallBuilderMetadataSamples
{
  .ver 1:0:0:0
}
.module CallBuilderMetadataSamples.dll

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (int32 priority) cil managed
	{
		.maxstack 1
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class interface public abstract auto ansi HighPriorityExtensionReceiver
{
}

.class interface public abstract auto ansi LowPriorityExtensionReceiver
	implements HighPriorityExtensionReceiver
{
}

.class public abstract auto ansi sealed beforefieldinit PriorityExtensions
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig static
		string Select (class HighPriorityExtensionReceiver highReceiver) cil managed
	{
		.maxstack 1
		ldstr "high"
		ret
	}

	.method public hidebysig static
		string Select (class LowPriorityExtensionReceiver lowReceiver) cil managed
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)
		.custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
			01 00 FF FF FF FF 00 00
		)
		.maxstack 1
		ldstr "low"
		ret
	}
}

.class public auto ansi beforefieldinit DivergentNameIndexer
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
		01 00 04 49 74 65 6D 00 00
	)

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		.maxstack 1
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public hidebysig specialname
		instance string get_Item (string lowName) cil managed
	{
		.maxstack 1
		ldarg.1
		ret
	}

	.method public hidebysig specialname
		instance void set_Item (string setterName, string 'value') cil managed
	{
		ret
	}

	.property instance string Item (string)
	{
		.get instance string DivergentNameIndexer::get_Item(string)
		.set instance void DivergentNameIndexer::set_Item(string, string)
	}

	.method public hidebysig specialname
		instance string get_Item (string hint, [opt] string other) cil managed
	{
		.param [2] = nullref
		.maxstack 1
		ldarg.1
		ret
	}

	.method public hidebysig specialname
		instance void set_Item (string setterHint, [opt] string setterOther, string 'value') cil managed
	{
		.param [2] = nullref
		ret
	}

	.property instance string Item (string, string)
	{
		.custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
			01 00 01 00 00 00 00 00
		)
		.get instance string DivergentNameIndexer::get_Item(string, string)
		.set instance void DivergentNameIndexer::set_Item(string, string, string)
	}
}

.class public auto ansi beforefieldinit CallBuilderMetadataSamples
	extends [mscorlib]System.Object
{
	.method public hidebysig static
		string SelectLowerPriorityExtensionConditionally (
			class LowPriorityExtensionReceiver receiver,
			string fallback
		) cil managed
	{
		.maxstack 2
		ldarg.0
		brtrue.s CALL_EXTENSION
		ldnull
		br.s COALESCE

	CALL_EXTENSION:
		ldarg.0
		call string PriorityExtensions::Select(class LowPriorityExtensionReceiver)

	COALESCE:
		dup
		brtrue.s RETURN
		pop
		ldarg.1

	RETURN:
		ret
	}

	.method public hidebysig
		instance void SetLowerPriorityIndexer () cil managed
	{
		.maxstack 3
		.locals init (
			[0] class DivergentNameIndexer indexer
		)

		newobj instance void DivergentNameIndexer::.ctor()
		stloc.0
		ldloc.0
		ldstr "file"
		ldstr "value"
		callvirt instance void DivergentNameIndexer::set_Item(string, string)
		ret
	}
}

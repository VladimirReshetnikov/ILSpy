.assembly extern System.Runtime
{
	.publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
	.ver 9:0:0:0
}

.assembly extern RefCastValueReceiver.External
{
	.ver 1:0:0:0
}

.assembly RefCastValueReceiver
{
}

.module RefCastValueReceiver.dll

.class public sequential ansi sealed beforefieldinit SourceReceiver
	extends [System.Runtime]System.ValueType
{
	.field public int32 Value
}

.class public sequential ansi sealed beforefieldinit TargetReceiver
	extends [System.Runtime]System.ValueType
{
	.field private int32 _value

	.method public hidebysig specialname instance int32 get_Value() cil managed
	{
		.maxstack 1

		ldarg.0
		ldfld int32 TargetReceiver::_value
		ret
	}

	.property instance int32 Value()
	{
		.get instance int32 TargetReceiver::get_Value()
	}
}

.class public abstract auto ansi sealed beforefieldinit RefCastValueReceiver
	extends [System.Runtime]System.Object
{
	.method public hidebysig static string ReadValue(
		valuetype [RefCastValueReceiver.External]External.ValueMatch 'value') cil managed
	{
		.maxstack 1

		ldarga.s 'value'
		call instance string [RefCastValueReceiver.External]External.ValueMatch::get_Path()
		ret
	}

	.method public hidebysig static string ReadIn(
		[in] valuetype [RefCastValueReceiver.External]External.ValueMatch& 'value') cil managed
	{
		.param [1]
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
			01 00 00 00
		)
		.maxstack 1

		ldarg.0
		call instance string [RefCastValueReceiver.External]External.ValueMatch::get_Path()
		ret
	}

	.method public hidebysig static int32 ReadSame(valuetype TargetReceiver& 'value') cil managed
	{
		.maxstack 1

		ldarg.0
		call instance int32 TargetReceiver::get_Value()
		ret
	}

	.method public hidebysig static int32 ReadReinterpreted(valuetype SourceReceiver& 'value') cil managed
	{
		.maxstack 1

		ldarg.0
		call instance int32 TargetReceiver::get_Value()
		ret
	}
}

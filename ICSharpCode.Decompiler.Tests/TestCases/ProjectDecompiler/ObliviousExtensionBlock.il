.assembly extern mscorlib
{
}

.assembly ObliviousExtensionBlock
{
}

.module ObliviousExtensionBlock.dll

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ExtensionMarkerAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor(string markerName) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi abstract sealed beforefieldinit ExtensionBlockTest.ObliviousExtensions
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
		01 00 00 00
	)

	.class nested public auto ansi sealed specialname '<G>$34505F560D9EACF86A87F3ED1F85E448'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
			01 00 00 00
		)

		.class nested public auto ansi abstract sealed specialname '<M>$A19AC9CD85DED852034952B941DE3351'
			extends [mscorlib]System.Object
		{
			.method public hidebysig specialname static
				void '<Extension>$'(string receiver) cil managed
			{
				ret
			}
		}

		.method public hidebysig specialname
			instance int32 get_Length() cil managed
		{
			.custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
				01 00 24 3c 4d 3e 24 41 31 39 41 43 39 43 44 38
				35 44 45 44 38 35 32 30 33 34 39 35 32 42 39 34
				31 44 45 33 33 35 31 00 00
			)
			newobj instance void [mscorlib]System.NotSupportedException::.ctor()
			throw
		}

		.property instance int32 Length()
		{
			.custom instance void System.Runtime.CompilerServices.ExtensionMarkerAttribute::.ctor(string) = (
				01 00 24 3c 4d 3e 24 41 31 39 41 43 39 43 44 38
				35 44 45 44 38 35 32 30 33 34 39 35 32 42 39 34
				31 44 45 33 33 35 31 00 00
			)
			.get instance int32 ExtensionBlockTest.ObliviousExtensions/'<G>$34505F560D9EACF86A87F3ED1F85E448'::get_Length()
		}
	}

	.method public hidebysig static int32 get_Length(string receiver) cil managed
	{
		ldarg.0
		callvirt instance int32 [mscorlib]System.String::get_Length()
		ret
	}
}

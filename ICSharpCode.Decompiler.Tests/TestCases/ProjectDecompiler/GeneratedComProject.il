.assembly extern mscorlib
{
}

.assembly GeneratedComProject
{
}

.module GeneratedComProject.dll

.class public auto ansi sealed beforefieldinit MarkerAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor (string text) cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute`2<TInterfaceInformation, TInterfaceImplementation>
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute`1<TComClassInformation>
	extends [mscorlib]System.Attribute
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}

.class private auto ansi sealed beforefieldinit InterfaceInformation
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class private auto ansi sealed beforefieldinit InterfaceImplementation
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class private auto ansi sealed beforefieldinit ComClassInformation
	extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class interface public abstract auto ansi MaterializedInterface
{
	.custom instance void System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void class System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute`2<class InterfaceInformation, class InterfaceImplementation>::.ctor() = (
		01 00 00 00
	)
	.custom instance void MarkerAttribute::.ctor(string) = (
		01 00 09 69 6e 74 65 72 66 61 63 65 00 00
	)

	.method public hidebysig newslot abstract virtual
		instance void Invoke () cil managed
	{
	}
}

.class public auto ansi sealed beforefieldinit MaterializedClass
	extends [mscorlib]System.Object
{
	.custom instance void System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void class System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute`1<class ComClassInformation>::.ctor() = (
		01 00 00 00
	)
	.custom instance void MarkerAttribute::.ctor(string) = (
		01 00 05 63 6c 61 73 73 00 00
	)

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

.class interface public abstract auto ansi OrdinaryInterface
{
	.custom instance void System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig newslot abstract virtual
		instance void Invoke () cil managed
	{
	}
}

.class public auto ansi sealed beforefieldinit OrdinaryClass
	extends [mscorlib]System.Object
{
	.custom instance void System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute::.ctor() = (
		01 00 00 00
	)

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}

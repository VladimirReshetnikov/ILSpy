#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly WindowsRuntimeEventSubscription { }
.module WindowsRuntimeEventSubscription.dll

// Roslyn compiles a WinRT event subscription 'x.E += h' into
// WindowsRuntimeMarshal.AddEventHandler(new Func<T, EventRegistrationToken>(x.add_E),
// new Action<EventRegistrationToken>(x.remove_E), h), and 'x.E -= h' into
// WindowsRuntimeMarshal.RemoveEventHandler(new Action<EventRegistrationToken>(x.remove_E), h).
// The delegate arguments name the event accessors, which C# cannot reference (CS0571), so the
// only legal rendering of these calls is the original event assignment.

.class public sequential ansi sealed beforefieldinit System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
	extends [CORE_ASSEMBLY]System.ValueType
{
	.field private int64 m_value
}

.class public abstract sealed auto ansi beforefieldinit System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig static void AddEventHandler<T> (
			class [CORE_ASSEMBLY]System.Func`2<!!0, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> addMethod,
			class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod,
			!!0 'handler'
		) cil managed
	{
		.maxstack 8
		IL_0000: ret
	}

	.method public hidebysig static void RemoveEventHandler<T> (
			class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod,
			!!0 'handler'
		) cil managed
	{
		.maxstack 8
		IL_0000: ret
	}
}

.class public auto ansi beforefieldinit FramePool
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig specialname
		instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken add_FrameArrived (
			class [CORE_ASSEMBLY]System.EventHandler 'value'
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		IL_0008: ldloc.0
		IL_0009: ret
	}

	.method public hidebysig specialname
		instance void remove_FrameArrived (
			valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'token'
		) cil managed
	{
		.maxstack 8
		IL_0000: ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
		IL_0006: ret
	}

	.event [CORE_ASSEMBLY]System.EventHandler FrameArrived
	{
		.addon instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken FramePool::add_FrameArrived(class [CORE_ASSEMBLY]System.EventHandler)
		.removeon instance void FramePool::remove_FrameArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
	}
}

.class public abstract sealed auto ansi beforefieldinit StaticEvents
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig specialname static
		valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken add_StaticArrived (
			class [CORE_ASSEMBLY]System.EventHandler 'value'
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		IL_0008: ldloc.0
		IL_0009: ret
	}

	.method public hidebysig specialname static
		void remove_StaticArrived (
			valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'token'
		) cil managed
	{
		.maxstack 8
		IL_0000: ret
	}

	.event [CORE_ASSEMBLY]System.EventHandler StaticArrived
	{
		.addon valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken StaticEvents::add_StaticArrived(class [CORE_ASSEMBLY]System.EventHandler)
		.removeon void StaticEvents::remove_StaticArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
	}
}

.class public auto ansi beforefieldinit VirtualPool
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig specialname newslot virtual
		instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken add_Arrived (
			class [CORE_ASSEMBLY]System.EventHandler 'value'
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		)

		IL_0000: ldloca.s 0
		IL_0002: initobj System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
		IL_0008: ldloc.0
		IL_0009: ret
	}

	.method public hidebysig specialname newslot virtual
		instance void remove_Arrived (
			valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'token'
		) cil managed
	{
		.maxstack 8
		IL_0000: ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
		IL_0006: ret
	}

	.event [CORE_ASSEMBLY]System.EventHandler Arrived
	{
		.addon instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken VirtualPool::add_Arrived(class [CORE_ASSEMBLY]System.EventHandler)
		.removeon instance void VirtualPool::remove_Arrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
	}
}

.class public auto ansi beforefieldinit FieldHolder
	extends [CORE_ASSEMBLY]System.Object
{
	.field private class VirtualPool pool

	.method public hidebysig instance void Unsubscribe (
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 4

		IL_0000: ldarg.0
		IL_0001: ldfld class VirtualPool FieldHolder::pool
		IL_0006: dup
		IL_0007: ldvirtftn instance void VirtualPool::remove_Arrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_000d: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_0012: ldarg.1
		IL_0013: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::RemoveEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_0018: ret
	}

	.method public hidebysig specialname rtspecialname
		instance void .ctor () cil managed
	{
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [CORE_ASSEMBLY]System.Object::.ctor()
		IL_0006: ret
	}
}

.class public abstract sealed auto ansi beforefieldinit EventConsumer
	extends [CORE_ASSEMBLY]System.Object
{
	.method public hidebysig static void SubscribeInstance (
			class FramePool pool,
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 4

		IL_0000: ldarg.0
		IL_0001: ldftn instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken FramePool::add_FrameArrived(class [CORE_ASSEMBLY]System.EventHandler)
		IL_0007: newobj instance void class [CORE_ASSEMBLY]System.Func`2<class [CORE_ASSEMBLY]System.EventHandler, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_000c: ldarg.0
		IL_000d: ldftn instance void FramePool::remove_FrameArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_0013: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_0018: ldarg.1
		IL_0019: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::AddEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Func`2<!!0, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_001e: ret
	}

	.method public hidebysig static void SubscribeVirtual (
			class VirtualPool pool,
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 5

		IL_0000: ldarg.0
		IL_0001: dup
		IL_0002: ldvirtftn instance valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken VirtualPool::add_Arrived(class [CORE_ASSEMBLY]System.EventHandler)
		IL_0008: newobj instance void class [CORE_ASSEMBLY]System.Func`2<class [CORE_ASSEMBLY]System.EventHandler, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_000d: ldarg.0
		IL_000e: dup
		IL_000f: ldvirtftn instance void VirtualPool::remove_Arrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_0015: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_001a: ldarg.1
		IL_001b: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::AddEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Func`2<!!0, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_0020: ret
	}

	.method public hidebysig static void UnsubscribeInstance (
			class FramePool pool,
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 3

		IL_0000: ldarg.0
		IL_0001: ldftn instance void FramePool::remove_FrameArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_0007: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_000c: ldarg.1
		IL_000d: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::RemoveEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_0012: ret
	}

	.method public hidebysig static void SubscribeStatic (
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 4

		IL_0000: ldnull
		IL_0001: ldftn valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken StaticEvents::add_StaticArrived(class [CORE_ASSEMBLY]System.EventHandler)
		IL_0007: newobj instance void class [CORE_ASSEMBLY]System.Func`2<class [CORE_ASSEMBLY]System.EventHandler, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_000c: ldnull
		IL_000d: ldftn void StaticEvents::remove_StaticArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_0013: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_0018: ldarg.0
		IL_0019: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::AddEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Func`2<!!0, valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_001e: ret
	}

	.method public hidebysig static void UnsubscribeStatic (
			class [CORE_ASSEMBLY]System.EventHandler 'handler'
		) cil managed
	{
		.maxstack 3

		IL_0000: ldnull
		IL_0001: ldftn void StaticEvents::remove_StaticArrived(valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
		IL_0007: newobj instance void class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>::.ctor(object, native int)
		IL_000c: ldarg.0
		IL_000d: call void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal::RemoveEventHandler<class [CORE_ASSEMBLY]System.EventHandler>(class [CORE_ASSEMBLY]System.Action`1<valuetype System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, !!0)
		IL_0012: ret
	}
}

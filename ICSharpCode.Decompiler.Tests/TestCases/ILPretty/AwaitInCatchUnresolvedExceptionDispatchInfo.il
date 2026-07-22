#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 5:0:0:0
}

// Deliberately not resolvable: the ExceptionDispatchInfo references below must stay unresolved,
// so the await-in-catch transform sees the Capture/Throw calls on a type it can only identify by
// name. Rebuilding 'catch { ... throw; }' must not depend on resolving ExceptionDispatchInfo to
// the same type definition the ambient core library provides.
.assembly extern FakeExceptionServices
{
  .ver 1:0:0:0
}

.assembly AwaitInCatchUnresolvedExceptionDispatchInfo { }
.module AwaitInCatchUnresolvedExceptionDispatchInfo.dll

.class public auto ansi abstract sealed beforefieldinit AwaitInCatch
	extends [CORE_ASSEMBLY]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<Run>d__0'
		extends [CORE_ASSEMBLY]System.ValueType
		implements [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine
	{
		.custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 '<>1__state'
		.field public valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder '<>t__builder'
		.field public class [CORE_ASSEMBLY]System.Threading.Tasks.Task work
		.field public class [CORE_ASSEMBLY]System.Threading.Tasks.Task cleanup
		.field private object '<>7__wrap1'
		.field private int32 '<>7__wrap2'
		.field private valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter '<>u__1'

		// Methods
		.method private final hidebysig newslot virtual
			instance void MoveNext () cil managed
		{
			.override method instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
			.maxstack 3
			.locals init (
				[0] int32,
				[1] valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter,
				[2] object,
				[3] int32,
				[4] class [CORE_ASSEMBLY]System.Exception
			)

			IL_0000: ldarg.0
			IL_0001: ldfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
			IL_0006: stloc.0
			.try
			{
				IL_0007: ldloc.0
				IL_0008: brfalse.s IL_0018

				IL_000a: ldloc.0
				IL_000b: ldc.i4.1
				IL_000c: beq IL_00ca

				IL_0011: ldarg.0
				IL_0012: ldc.i4.0
				IL_0013: stfld int32 AwaitInCatch/'<Run>d__0'::'<>7__wrap2'

				IL_0018: nop
				.try
				{
					IL_0019: ldloc.0
					IL_001a: brfalse.s IL_0054

					IL_001c: ldarg.0
					IL_001d: ldfld class [CORE_ASSEMBLY]System.Threading.Tasks.Task AwaitInCatch/'<Run>d__0'::work
					IL_0022: callvirt instance valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter [CORE_ASSEMBLY]System.Threading.Tasks.Task::GetAwaiter()
					IL_0027: stloc.1
					IL_0028: ldloca.s 1
					IL_002a: call instance bool [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
					IL_002f: brtrue.s IL_0070

					IL_0031: ldarg.0
					IL_0032: ldc.i4.0
					IL_0033: dup
					IL_0034: stloc.0
					IL_0035: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
					IL_003a: ldarg.0
					IL_003b: ldloc.1
					IL_003c: stfld valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
					IL_0041: ldarg.0
					IL_0042: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
					IL_0047: ldloca.s 1
					IL_0049: ldarg.0
					IL_004a: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter, valuetype AwaitInCatch/'<Run>d__0'>(!!0&, !!1&)
					IL_004f: leave IL_0141

					IL_0054: ldarg.0
					IL_0055: ldfld valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
					IL_005a: stloc.1
					IL_005b: ldarg.0
					IL_005c: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
					IL_0061: initobj [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter
					IL_0067: ldarg.0
					IL_0068: ldc.i4.m1
					IL_0069: dup
					IL_006a: stloc.0
					IL_006b: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'

					IL_0070: ldloca.s 1
					IL_0072: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
					IL_0077: leave.s IL_008a
				} // end .try
				catch [CORE_ASSEMBLY]System.Object
				{
					IL_0079: stloc.2
					IL_007a: ldarg.0
					IL_007b: ldloc.2
					IL_007c: stfld object AwaitInCatch/'<Run>d__0'::'<>7__wrap1'
					IL_0081: ldarg.0
					IL_0082: ldc.i4.1
					IL_0083: stfld int32 AwaitInCatch/'<Run>d__0'::'<>7__wrap2'
					IL_0088: leave.s IL_008a
				} // end handler

				IL_008a: ldarg.0
				IL_008b: ldfld int32 AwaitInCatch/'<Run>d__0'::'<>7__wrap2'
				IL_0090: stloc.3
				IL_0091: ldloc.3
				IL_0092: ldc.i4.1
				IL_0093: bne.un.s IL_010c

				IL_0095: ldarg.0
				IL_0096: ldfld class [CORE_ASSEMBLY]System.Threading.Tasks.Task AwaitInCatch/'<Run>d__0'::cleanup
				IL_009b: callvirt instance valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter [CORE_ASSEMBLY]System.Threading.Tasks.Task::GetAwaiter()
				IL_00a0: stloc.1
				IL_00a1: ldloca.s 1
				IL_00a3: call instance bool [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter::get_IsCompleted()
				IL_00a8: brtrue.s IL_00e6

				IL_00aa: ldarg.0
				IL_00ab: ldc.i4.1
				IL_00ac: dup
				IL_00ad: stloc.0
				IL_00ae: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
				IL_00b3: ldarg.0
				IL_00b4: ldloc.1
				IL_00b5: stfld valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
				IL_00ba: ldarg.0
				IL_00bb: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
				IL_00c0: ldloca.s 1
				IL_00c2: ldarg.0
				IL_00c3: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::AwaitUnsafeOnCompleted<valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter, valuetype AwaitInCatch/'<Run>d__0'>(!!0&, !!1&)
				IL_00c8: leave.s IL_0141

				IL_00ca: ldarg.0
				IL_00cb: ldfld valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
				IL_00d0: stloc.1
				IL_00d1: ldarg.0
				IL_00d2: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter AwaitInCatch/'<Run>d__0'::'<>u__1'
				IL_00d7: initobj [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter
				IL_00dd: ldarg.0
				IL_00de: ldc.i4.m1
				IL_00df: dup
				IL_00e0: stloc.0
				IL_00e1: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'

				IL_00e6: ldloca.s 1
				IL_00e8: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.TaskAwaiter::GetResult()
				IL_00ed: ldarg.0
				IL_00ee: ldfld object AwaitInCatch/'<Run>d__0'::'<>7__wrap1'
				IL_00f3: isinst [CORE_ASSEMBLY]System.Exception
				IL_00f8: dup
				IL_00f9: brtrue.s IL_0102

				IL_00fb: ldarg.0
				IL_00fc: ldfld object AwaitInCatch/'<Run>d__0'::'<>7__wrap1'
				IL_0101: throw

				IL_0102: call class [FakeExceptionServices]System.Runtime.ExceptionServices.ExceptionDispatchInfo [FakeExceptionServices]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Capture(class [CORE_ASSEMBLY]System.Exception)
				IL_0107: callvirt instance void [FakeExceptionServices]System.Runtime.ExceptionServices.ExceptionDispatchInfo::Throw()

				IL_010c: ldarg.0
				IL_010d: ldnull
				IL_010e: stfld object AwaitInCatch/'<Run>d__0'::'<>7__wrap1'
				IL_0113: leave.s IL_012e
			} // end .try
			catch [CORE_ASSEMBLY]System.Exception
			{
				IL_0115: stloc.s 4
				IL_0117: ldarg.0
				IL_0118: ldc.i4.s -2
				IL_011a: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
				IL_011f: ldarg.0
				IL_0120: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
				IL_0125: ldloc.s 4
				IL_0127: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetException(class [CORE_ASSEMBLY]System.Exception)
				IL_012c: leave.s IL_0141
			} // end handler

			IL_012e: ldarg.0
			IL_012f: ldc.i4.s -2
			IL_0131: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
			IL_0136: ldarg.0
			IL_0137: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
			IL_013c: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetResult()

			IL_0141: ret
		} // end of method '<Run>d__0'::MoveNext

		.method private final hidebysig newslot virtual
			instance void SetStateMachine (
				class [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
			) cil managed
		{
			.custom instance void [CORE_ASSEMBLY]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine)
			.maxstack 8

			IL_0000: ldarg.0
			IL_0001: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
			IL_0006: ldarg.1
			IL_0007: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::SetStateMachine(class [CORE_ASSEMBLY]System.Runtime.CompilerServices.IAsyncStateMachine)
			IL_000c: ret
		} // end of method '<Run>d__0'::SetStateMachine

	} // end of class <Run>d__0


	// Methods
	.method public hidebysig static
		class [CORE_ASSEMBLY]System.Threading.Tasks.Task Run (
			class [CORE_ASSEMBLY]System.Threading.Tasks.Task work,
			class [CORE_ASSEMBLY]System.Threading.Tasks.Task cleanup
		) cil managed
	{
		.custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [CORE_ASSEMBLY]System.Type) = (
			01 00 16 41 77 61 69 74 49 6e 43 61 74 63 68 2b
			3c 52 75 6e 3e 64 5f 5f 30 00 00
		)
		.maxstack 2
		.locals init (
			[0] valuetype AwaitInCatch/'<Run>d__0'
		)

		IL_0000: ldloca.s 0
		IL_0002: call valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Create()
		IL_0007: stfld valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
		IL_000c: ldloca.s 0
		IL_000e: ldarg.0
		IL_000f: stfld class [CORE_ASSEMBLY]System.Threading.Tasks.Task AwaitInCatch/'<Run>d__0'::work
		IL_0014: ldloca.s 0
		IL_0016: ldarg.1
		IL_0017: stfld class [CORE_ASSEMBLY]System.Threading.Tasks.Task AwaitInCatch/'<Run>d__0'::cleanup
		IL_001c: ldloca.s 0
		IL_001e: ldc.i4.m1
		IL_001f: stfld int32 AwaitInCatch/'<Run>d__0'::'<>1__state'
		IL_0024: ldloca.s 0
		IL_0026: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
		IL_002b: ldloca.s 0
		IL_002d: call instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::Start<valuetype AwaitInCatch/'<Run>d__0'>(!!0&)
		IL_0032: ldloca.s 0
		IL_0034: ldflda valuetype [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder AwaitInCatch/'<Run>d__0'::'<>t__builder'
		IL_0039: call instance class [CORE_ASSEMBLY]System.Threading.Tasks.Task [CORE_ASSEMBLY]System.Runtime.CompilerServices.AsyncTaskMethodBuilder::get_Task()
		IL_003e: ret
	} // end of method AwaitInCatch::Run

} // end of class AwaitInCatch

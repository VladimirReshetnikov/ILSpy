// A local function whose closure arrives only via a forwarded by-ref display-class struct
// parameter of an enclosing local function. The decompiler must declare it inside that
// enclosing function (where the display class lives); otherwise its captured fields leak as
// an undefined 'P_<n>.field' (CS0103). This is the IL Roslyn emits for the sibling .cs.

.assembly extern System.Runtime
{
  .ver 8:0:0:0
}
.assembly extern System.Console
{
  .ver 8:0:0:0
}
.assembly ForwardedClosureLocalFunction
{
}
.module ForwardedClosureLocalFunction.dll
.corflags 0x00000001

.class public auto ansi beforefieldinit Repro
       extends [System.Runtime]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass0_0'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public class ILogger logger
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass0_0'::.ctor

    .method assembly hidebysig instance native int 
            '<.ctor>g__WndProc|0'(int32 msg) cil managed
    {
      // Code size       52 (0x34)
      .maxstack  2
      .locals init (valuetype Repro/'<>c__DisplayClass0_1' V_0)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.0
      IL_0003:  conv.u
      IL_0004:  stfld      void* Repro/'<>c__DisplayClass0_1'::hwnd
      IL_0009:  ldloca.s   V_0
      IL_000b:  ldnull
      IL_000c:  stfld      string Repro/'<>c__DisplayClass0_1'::def
      IL_0011:  ldarg.1
      IL_0012:  ldc.i4.1
      IL_0013:  beq.s      IL_001b

      IL_0015:  ldarg.1
      IL_0016:  ldc.i4.2
      IL_0017:  beq.s      IL_0025

      IL_0019:  br.s       IL_002d

      IL_001b:  ldarg.0
      IL_001c:  ldloca.s   V_0
      IL_001e:  call       instance void Repro/'<>c__DisplayClass0_0'::'<.ctor>g__OnCreate|1'(valuetype Repro/'<>c__DisplayClass0_1'&)
      IL_0023:  br.s       IL_002d

      IL_0025:  ldarg.0
      IL_0026:  ldloca.s   V_0
      IL_0028:  call       instance void Repro/'<>c__DisplayClass0_0'::'<.ctor>g__OnDestroy|2'(valuetype Repro/'<>c__DisplayClass0_1'&)
      IL_002d:  ldloc.0
      IL_002e:  ldfld      void* Repro/'<>c__DisplayClass0_1'::hwnd
      IL_0033:  ret
    } // end of method '<>c__DisplayClass0_0'::'<.ctor>g__WndProc|0'

    .method assembly hidebysig instance void 
            '<.ctor>g__OnCreate|1'(valuetype Repro/'<>c__DisplayClass0_1'& A_1) cil managed
    {
      // Code size       54 (0x36)
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldfld      string Repro/'<>c__DisplayClass0_1'::def
      IL_0006:  brfalse.s  IL_0019

      IL_0008:  ldarg.0
      IL_0009:  ldfld      class ILogger Repro/'<>c__DisplayClass0_0'::logger
      IL_000e:  ldstr      "already created"
      IL_0013:  callvirt   instance void ILogger::Error(string)
      IL_0018:  ret

      IL_0019:  ldarg.1
      IL_001a:  ldstr      "created"
      IL_001f:  stfld      string Repro/'<>c__DisplayClass0_1'::def
      IL_0024:  ldarg.1
      IL_0025:  ldfld      void* Repro/'<>c__DisplayClass0_1'::hwnd
      IL_002a:  ldarg.0
      IL_002b:  ldfld      class ILogger Repro/'<>c__DisplayClass0_0'::logger
      IL_0030:  call       void Repro::Register(void*,
                                                class ILogger)
      IL_0035:  ret
    } // end of method '<>c__DisplayClass0_0'::'<.ctor>g__OnCreate|1'

    .method assembly hidebysig instance void 
            '<.ctor>g__OnDestroy|2'(valuetype Repro/'<>c__DisplayClass0_1'& A_1) cil managed
    {
      // Code size       33 (0x21)
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldfld      string Repro/'<>c__DisplayClass0_1'::def
      IL_0006:  brtrue.s   IL_0019

      IL_0008:  ldarg.0
      IL_0009:  ldfld      class ILogger Repro/'<>c__DisplayClass0_0'::logger
      IL_000e:  ldstr      "not created"
      IL_0013:  callvirt   instance void ILogger::Error(string)
      IL_0018:  ret

      IL_0019:  ldarg.1
      IL_001a:  ldnull
      IL_001b:  stfld      string Repro/'<>c__DisplayClass0_1'::def
      IL_0020:  ret
    } // end of method '<>c__DisplayClass0_0'::'<.ctor>g__OnDestroy|2'

  } // end of class '<>c__DisplayClass0_0'

  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass0_1'
         extends [System.Runtime]System.ValueType
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public string def
    .field public void* hwnd
  } // end of class '<>c__DisplayClass0_1'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class ILogger logger) cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  8
    IL_0000:  newobj     instance void Repro/'<>c__DisplayClass0_0'::.ctor()
    IL_0005:  dup
    IL_0006:  ldarg.1
    IL_0007:  stfld      class ILogger Repro/'<>c__DisplayClass0_0'::logger
    IL_000c:  ldarg.0
    IL_000d:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0012:  ldftn      instance native int Repro/'<>c__DisplayClass0_0'::'<.ctor>g__WndProc|0'(int32)
    IL_0018:  newobj     instance void class [System.Runtime]System.Func`2<int32,native int>::.ctor(object,
                                                                                                    native int)
    IL_001d:  ldc.i4.1
    IL_001e:  callvirt   instance !1 class [System.Runtime]System.Func`2<int32,native int>::Invoke(!0)
    IL_0023:  conv.i8
    IL_0024:  call       void [System.Console]System.Console::WriteLine(int64)
    IL_0029:  ret
  } // end of method Repro::.ctor

  .method private hidebysig static void  Register(void* hwnd,
                                                  class ILogger logger) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldstr      "register"
    IL_0006:  callvirt   instance void ILogger::Error(string)
    IL_000b:  ret
  } // end of method Repro::Register

  .method public hidebysig static void  Main() cil managed
  {
    .entrypoint
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  newobj     instance void ConsoleLogger::.ctor()
    IL_0005:  newobj     instance void Repro::.ctor(class ILogger)
    IL_000a:  pop
    IL_000b:  ret
  } // end of method Repro::Main

} // end of class Repro

.class interface public abstract auto ansi beforefieldinit ILogger
{
  .method public hidebysig newslot abstract virtual 
          instance void  Error(string message) cil managed
  {
  } // end of method ILogger::Error

} // end of class ILogger

.class public auto ansi beforefieldinit ConsoleLogger
       extends [System.Runtime]System.Object
       implements ILogger
{
  .method public hidebysig newslot virtual final 
          instance void  Error(string message) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0006:  ret
  } // end of method ConsoleLogger::Error

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ConsoleLogger::.ctor

} // end of class ConsoleLogger


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ForwardedClosureLocalFunction.res

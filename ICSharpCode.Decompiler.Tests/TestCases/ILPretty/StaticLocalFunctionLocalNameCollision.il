// A static local function has a loop-body local (V_5) whose generated name 'type2' happens to
// equal the name of a local ('type2', V_0) in the enclosing method. The two are distinct locals
// in distinct IL methods, so DeclareVariables must give each its own declaration. If they are
// instead merged into a single declaration in the enclosing method, the local function's uses
// bind to the enclosing local - illegal inside a static local function (CS8421). This is the
// shape Roslyn emits for the sibling .cs.

.assembly extern System.Runtime
{
  .ver 9:0:0:0
}
.assembly StaticLocalFunctionLocalNameCollision
{
}
.module StaticLocalFunctionLocalNameCollision.dll
.corflags 0x00000001

.class public auto ansi beforefieldinit Repro
       extends [System.Runtime]System.Object
{
  .method public hidebysig static string
          Present(class [System.Runtime]System.Type 'type') cil managed
  {
    .maxstack  3
    .locals init (class [System.Runtime]System.Type V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       class [System.Runtime]System.Type Repro::'<Present>g__FindInherited|0_0'(class [System.Runtime]System.Type)
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  callvirt   instance bool [System.Runtime]System.Type::get_IsGenericType()
    IL_000d:  brfalse.s  IL_0018

    IL_000f:  ldloc.0
    IL_0010:  brtrue.s   IL_0018

    IL_0012:  ldstr      "generic"
    IL_0017:  ret

    IL_0018:  ldarg.0
    IL_0019:  callvirt   instance bool [System.Runtime]System.Type::get_IsNested()
    IL_001e:  brfalse.s  IL_0041

    IL_0020:  ldloc.0
    IL_0021:  dup
    IL_0022:  brtrue.s   IL_002b

    IL_0024:  pop
    IL_0025:  ldarg.0
    IL_0026:  callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Reflection.MemberInfo::get_DeclaringType()
    IL_002b:  call       string Repro::Present(class [System.Runtime]System.Type)
    IL_0030:  ldstr      "."
    IL_0035:  ldarg.0
    IL_0036:  callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
    IL_003b:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_0040:  ret

    IL_0041:  ldarg.0
    IL_0042:  callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
    IL_0047:  ret
  } // end of method Repro::Present

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Repro::.ctor

  .method assembly hidebysig static class [System.Runtime]System.Type
          '<Present>g__FindInherited|0_0'(class [System.Runtime]System.Type 'type') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack  2
    .locals init (class [System.Runtime]System.Type V_0,
             class [System.Runtime]System.Type V_1,
             class [System.Runtime]System.Type[] V_2,
             class [System.Runtime]System.Type[] V_3,
             int32 V_4,
             class [System.Runtime]System.Type V_5,
             class [System.Runtime]System.Type V_6,
             class [System.Runtime]System.Type[] V_7)
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance bool [System.Runtime]System.Type::get_IsNested()
    IL_0006:  brtrue.s   IL_000a

    IL_0008:  ldnull
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  callvirt   instance bool [System.Runtime]System.Type::get_IsGenericType()
    IL_0010:  brtrue.s   IL_0014

    IL_0012:  ldnull
    IL_0013:  ret

    IL_0014:  ldarg.0
    IL_0015:  callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Type::GetGenericTypeDefinition()
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Reflection.MemberInfo::get_DeclaringType()
    IL_0021:  stloc.1
    IL_0022:  ldloc.1
    IL_0023:  brtrue.s   IL_0027

    IL_0025:  ldnull
    IL_0026:  ret

    IL_0027:  ldloc.0
    IL_0028:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericArguments()
    IL_002d:  stloc.2
    IL_002e:  ldloc.1
    IL_002f:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericArguments()
    IL_0034:  stloc.3
    IL_0035:  ldloc.2
    IL_0036:  ldlen
    IL_0037:  conv.i4
    IL_0038:  ldloc.3
    IL_0039:  ldlen
    IL_003a:  conv.i4
    IL_003b:  beq.s      IL_003f

    IL_003d:  ldnull
    IL_003e:  ret

    IL_003f:  ldc.i4.0
    IL_0040:  stloc.s    V_4
    IL_0042:  br.s       IL_0099

    IL_0044:  ldloc.2
    IL_0045:  ldloc.s    V_4
    IL_0047:  ldelem.ref
    IL_0048:  stloc.s    V_5
    IL_004a:  ldloc.3
    IL_004b:  ldloc.s    V_4
    IL_004d:  ldelem.ref
    IL_004e:  stloc.s    V_6
    IL_0050:  ldloc.s    V_5
    IL_0052:  callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
    IL_0057:  ldloc.s    V_6
    IL_0059:  callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
    IL_005e:  call       bool [System.Runtime]System.String::op_Inequality(string,
                                                                           string)
    IL_0063:  brfalse.s  IL_0067

    IL_0065:  ldnull
    IL_0066:  ret

    IL_0067:  ldloc.s    V_5
    IL_0069:  callvirt   instance valuetype [System.Runtime]System.Reflection.GenericParameterAttributes [System.Runtime]System.Type::get_GenericParameterAttributes()
    IL_006e:  ldloc.s    V_6
    IL_0070:  callvirt   instance valuetype [System.Runtime]System.Reflection.GenericParameterAttributes [System.Runtime]System.Type::get_GenericParameterAttributes()
    IL_0075:  beq.s      IL_0079

    IL_0077:  ldnull
    IL_0078:  ret

    IL_0079:  ldloc.s    V_5
    IL_007b:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericParameterConstraints()
    IL_0080:  ldloc.s    V_6
    IL_0082:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericParameterConstraints()
    IL_0087:  stloc.s    V_7
    IL_0089:  ldlen
    IL_008a:  conv.i4
    IL_008b:  ldloc.s    V_7
    IL_008d:  ldlen
    IL_008e:  conv.i4
    IL_008f:  beq.s      IL_0093

    IL_0091:  ldnull
    IL_0092:  ret

    IL_0093:  ldloc.s    V_4
    IL_0095:  ldc.i4.1
    IL_0096:  add
    IL_0097:  stloc.s    V_4
    IL_0099:  ldloc.s    V_4
    IL_009b:  ldloc.2
    IL_009c:  ldlen
    IL_009d:  conv.i4
    IL_009e:  blt.s      IL_0044

    IL_00a0:  ldloc.1
    IL_00a1:  ldarg.0
    IL_00a2:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Type::GetGenericArguments()
    IL_00a7:  callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Type::MakeGenericType(class [System.Runtime]System.Type[])
    IL_00ac:  ret
  } // end of method Repro::'<Present>g__FindInherited|0_0'

} // end of class Repro

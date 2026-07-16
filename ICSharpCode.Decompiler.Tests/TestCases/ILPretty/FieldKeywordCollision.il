// A semi-auto-property backing field whose declaring type ALSO has a member literally named
// 'field'. The C# compiler never emits this shape (a 'field'-keyword property cannot coexist with a
// member named 'field' referenced in the accessor), but it must not be mis-reconstructed: rewriting
// the backing field to the bare 'field' keyword would re-bind the member access. IntroduceFieldKeyword
// must therefore keep the explicit backing field here.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly FieldKeywordCollision
{
  .ver 1:0:0:0
}
.module FieldKeywordCollision.dll

.class private auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision
       extends [mscorlib]System.Object
{
  .field public int32 'field'
  .field private int32 '<Prop>k__BackingField'
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig specialname instance int32 get_Prop() cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 8
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::'<Prop>k__BackingField'
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::'field'
    add
    ret
  }

  .property instance int32 Prop()
  {
    .get instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.FieldKeywordCollision::get_Prop()
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.a`1<a>
       extends [mscorlib]System.Object
{
  .field public !0 Value
  .field public !0 a

  .method public hidebysig instance !0 Echo(!0 'value') cil managed
  {
    ldarg.1
    ret
  }

  .method public hidebysig instance !0 ReadMember() cil managed
  {
    ldarg.0
    ldfld !0 class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.a`1<!0>::a
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CollisionShapes
       extends [mscorlib]System.Object
{
  .class nested private auto ansi beforefieldinit Hidden
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor()
      ret
    }
  }

  .field private int32 CollisionShapes
  .field private int32 Same

  .method private hidebysig instance int32 Same() cil managed
  {
    ldarg.0
    ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CollisionShapes::CollisionShapes
    ret
  }

  .method public hidebysig instance int32 Read() cil managed
  {
    ldarg.0
    call instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CollisionShapes::Same()
    ret
  }

  .method private hidebysig instance int32 CollisionShapes(int32 'value') cil managed
  {
    ldarg.1
    ret
  }

  .method public hidebysig instance int32 ReadMethodName() cil managed
  {
    ldarg.0
    ldc.i4.1
    call instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CollisionShapes::CollisionShapes(int32)
    ret
  }

  .method private hidebysig instance void Overload(int32 'value') cil managed
  {
    ret
  }

  .method private hidebysig instance void Overload(string 'value') cil managed
  {
    ret
  }

  .method assembly hidebysig static void Leak(class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.CollisionShapes/Hidden 'value') cil managed
  {
    ret
  }

  .method public hidebysig static int32 MalformedBooleanSwitch(bool 'value') cil managed
  {
    ldarg.0
    switch (CASE_0, CASE_1, CASE_2)
    ldc.i4.m1
    ret
  CASE_0:
    ldc.i4.0
    ret
  CASE_1:
    ldc.i4.1
    ret
  CASE_2:
    ldc.i4.2
    ret
  }

  .method public hidebysig static int32 ConstantSwitchDefiniteAssignment() cil managed
  {
    .locals init ([0] int32 V_0, [1] int32 V_1)
    ldc.i4.0
    stloc.0
    ldc.i4.0
    switch (STORE_VALUE)
    br DISPATCH
  STORE_VALUE:
    ldc.i4.s 42
    stloc.1
    ldc.i4.1
    stloc.0
    br DISPATCH
  DISPATCH:
    ldloc.0
    switch (RETURN_DEFAULT, RETURN_VALUE)
    br RETURN_DEFAULT
  RETURN_VALUE:
    ldloc.1
    ret
  RETURN_DEFAULT:
    ldc.i4.m1
    ret
  }

  .method public hidebysig static int32 GotoCrossedLocals(object input) cil managed
  {
    .locals init ([0] object V_0, [1] int32 V_1, [2] int16 V_2)
    ldc.i4.s 10
    stloc.2
    ldloca.s V_2
    ldind.i2
    ldc.i4.s 10
    ceq
    switch (DISPATCH, NORMAL, DISPATCH)
  NORMAL:
    ldc.i4.0
    switch (SETUP)
  DISPATCH:
    ldloc.1
    switch (RETURN_DEFAULT, USE_OBJECT)
    br RETURN_DEFAULT
  SETUP:
    ldarg.0
    stloc.0
    ldc.i4.1
    stloc.1
    br DISPATCH
  USE_OBJECT:
    ldloc.0
    brfalse RETURN_DEFAULT
    ldc.i4.1
    ret
  RETURN_DEFAULT:
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner
       extends [mscorlib]System.Object
{
  .class nested private auto ansi beforefieldinit Closure
         extends [mscorlib]System.Object
  {
    .class nested private auto ansi beforefieldinit State
           extends [mscorlib]System.Object
    {
      .field public int32 Value

      .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
      {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
      }
    }

    .method assembly hidebysig instance int32 Helper() cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
      .locals init ([0] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure/State V_0)
      newobj instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure/State::.ctor()
      stloc.0
      ldloc.0
      ldc.i4.7
      stfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure/State::Value
      ldloc.0
      ldfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure/State::Value
      ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor()
      ret
    }
  }

  .method public hidebysig static class [mscorlib]System.Func`1<int32> Create() cil managed
  {
    newobj instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure::.ctor()
    dup
    ldvirtftn instance int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.DelegateOwner/Closure::Helper()
    newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

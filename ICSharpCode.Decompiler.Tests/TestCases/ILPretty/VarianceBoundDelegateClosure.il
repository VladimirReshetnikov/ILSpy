// A VB-style compiler-generated closure whose static "lambda" method takes a BASE type
// while the delegate it is bound to takes a more-derived type. CLR delegate creation is
// contravariant in the parameters, so 'newobj Func`2<Derived, Base>(ldnull, ldftn Base(Base))'
// is legal IL. A reconstructed C# lambda, however, must declare its parameter as the delegate's
// own Invoke parameter type (Derived): declaring it as the wider Base type is CS1678/CS1661.
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}
.assembly VarianceBoundDelegateClosure
{
  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56 65 72 73 69 6F 6E 3D 76 39 2E 30 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module VarianceBoundDelegateClosure.dll

.class public auto ansi beforefieldinit Base
       extends [System.Runtime]System.Object
{
  .field public object Value
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
    IL_0006: ret
  }
}

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void Base::.ctor()
    IL_0006: ret
  }
}

.class public auto ansi beforefieldinit C
       extends [System.Runtime]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '_Closure$__'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )

    // identity lambda: takes Base, returns Base; bound to Func<Derived, Base>
    .method public hidebysig static class Base
            '_Lambda$__0-0'(class Base e) cil managed
    {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: ret
    }

    // member-access lambda: reads a field declared on Base; bound to Func<Derived, object>
    .method public hidebysig static object
            '_Lambda$__1-0'(class Base b) cil managed
    {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: ldfld object Base::Value
      IL_0006: ret
    }
  }

  .method public hidebysig static class [System.Runtime]System.Func`2<class Derived,class Base>
          MakeIdentity() cil managed
  {
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ldftn class Base C/'_Closure$__'::'_Lambda$__0-0'(class Base)
    IL_0007: newobj instance void class [System.Runtime]System.Func`2<class Derived,class Base>::.ctor(object, native int)
    IL_000c: ret
  }

  .method public hidebysig static class [System.Runtime]System.Func`2<class Derived,object>
          MakeReader() cil managed
  {
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ldftn object C/'_Closure$__'::'_Lambda$__1-0'(class Base)
    IL_0007: newobj instance void class [System.Runtime]System.Func`2<class Derived,object>::.ctor(object, native int)
    IL_000c: ret
  }

  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
    IL_0006: ret
  }
}

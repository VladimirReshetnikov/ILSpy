// A closure-captured local whose only initializer is an anonymous method that reads the
// local itself (through the captured display-class field). Valid C# cannot express the
// merged single-initializer form 'Action a = delegate { a(); };': inside its own
// initializer the local is not definitely assigned, so the read is CS0165. A faithful
// decompilation must therefore split the declaration and give it a default value:
//     Action a = null;
//     a = delegate { a(); };
//     a();
// This IL mirrors what Roslyn emits for that source, but omits the explicit 'a = null'
// store, leaving the self-referencing delegate as the local's sole assignment - the shape
// that used to be merged into an uncompilable single initializer.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 9:0:0:0
}
.assembly SelfReferencingDelegate
{
}
.module SelfReferencingDelegate.dll
.corflags 0x00000001

.class public auto ansi beforefieldinit C
       extends [System.Runtime]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass0_0'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .field public class [System.Runtime]System.Action a

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass0_0'::.ctor

    .method assembly hidebysig instance void
            '<M>b__0'() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [System.Runtime]System.Action C/'<>c__DisplayClass0_0'::a
      IL_0006:  callvirt   instance void [System.Runtime]System.Action::Invoke()
      IL_000b:  ret
    } // end of method '<>c__DisplayClass0_0'::'<M>b__0'

  } // end of class '<>c__DisplayClass0_0'

  .method public hidebysig static void  M() cil managed
  {
    .maxstack  8
    .locals init (class C/'<>c__DisplayClass0_0' V_0)
    IL_0000:  newobj     instance void C/'<>c__DisplayClass0_0'::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldftn      instance void C/'<>c__DisplayClass0_0'::'<M>b__0'()
    IL_000e:  newobj     instance void [System.Runtime]System.Action::.ctor(object,
                                                                            native int)
    IL_0013:  stfld      class [System.Runtime]System.Action C/'<>c__DisplayClass0_0'::a
    IL_0018:  ldloc.0
    IL_0019:  ldfld      class [System.Runtime]System.Action C/'<>c__DisplayClass0_0'::a
    IL_001e:  callvirt   instance void [System.Runtime]System.Action::Invoke()
    IL_0023:  ret
  } // end of method C::M

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C

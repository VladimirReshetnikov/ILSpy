.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly AmbiguousIvtName.LibraryB
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 10 41 6D 62 69 67 75 6F 75 73 49 76 74 4E 61 6D 65 00 00 )
  .ver 1:0:0:0
}
.module AmbiguousIvtName.LibraryB.dll

// Internal copy of NsInternal.TextUtilities that IS visible to the consumer via
// the InternalsVisibleTo("AmbiguousIvtName") grant above.
.class private auto ansi abstract sealed beforefieldinit NsInternal.TextUtilities
       extends [mscorlib]System.Object
{
  .method assembly hidebysig static string Shorten(string text) cil managed
  {
    ldarg.0
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit NsInternal.TextDocument
       extends [mscorlib]System.Object
{
  .method public hidebysig static string Open() cil managed
  {
    ldstr "doc"
    ret
  }
}

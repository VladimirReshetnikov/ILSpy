.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly AmbiguousIvtName.LibraryA
{
  .ver 1:0:0:0
}
.module AmbiguousIvtName.LibraryA.dll

// Internal copy of NsInternal.TextUtilities that is NOT visible to the consumer:
// LibraryA grants no InternalsVisibleTo.
.class private auto ansi abstract sealed beforefieldinit NsInternal.TextUtilities
       extends [mscorlib]System.Object
{
  .method assembly hidebysig static string Shorten(string text) cil managed
  {
    ldarg.0
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit NsInternal.SourceText
       extends [mscorlib]System.Object
{
  .method public hidebysig static string Describe() cil managed
  {
    ldstr "text"
    ret
  }
}

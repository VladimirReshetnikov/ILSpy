.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern AmbiguousIvtName.LibraryA
{
  .ver 1:0:0:0
}
.assembly extern AmbiguousIvtName.LibraryB
{
  .ver 1:0:0:0
}
.assembly AmbiguousIvtName
{
  .ver 1:0:0:0
}
.module AmbiguousIvtName.dll

.class public auto ansi abstract sealed beforefieldinit NsPublic.TextUtilities
       extends [mscorlib]System.Object
{
  .method public hidebysig static string Shorten(string text) cil managed
  {
    ldarg.0
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit App.Consumer
       extends [mscorlib]System.Object
{
  .method public hidebysig static string Test() cil managed
  {
    call string [AmbiguousIvtName.LibraryA]NsInternal.SourceText::Describe()
    call string [AmbiguousIvtName.LibraryB]NsInternal.TextDocument::Open()
    call string [mscorlib]System.String::Concat(string, string)
    call string NsPublic.TextUtilities::Shorten(string)
    ret
  }
}

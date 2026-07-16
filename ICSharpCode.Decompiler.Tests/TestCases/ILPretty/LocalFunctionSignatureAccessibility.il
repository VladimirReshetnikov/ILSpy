.assembly extern System.Runtime
{
  .ver 9:0:0:0
}
.assembly LocalFunctionSignatureAccessibility
{
}
.module LocalFunctionSignatureAccessibility.dll
.corflags 0x00000001

.class public auto ansi beforefieldinit Repro
       extends [System.Runtime]System.Object
{
  .class nested private auto ansi beforefieldinit Hidden
         extends [System.Runtime]System.Object
  {
  }

  .method public hidebysig static void Run() cil managed
  {
    .maxstack 1
    IL_0000: ldnull
    IL_0001: call void Repro::'<Run>g__DoWork|0_0'(class Repro/Hidden)
    IL_0006: ret
  }

  .method assembly hidebysig static void
          '<Run>g__DoWork|0_0'(class Repro/Hidden 'value') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
    .maxstack 0
    IL_0000: ret
  }
}

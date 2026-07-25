.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly OverrideNarrowsAccessorAccessibility
{
  .ver 1:0:0:0
}
.module OverrideNarrowsAccessorAccessibility.dll

// The base declares the property and its accessor 'famorassem' (protected internal).
.class public abstract auto ansi beforefieldinit BaseWithProtectedInternal
       extends [mscorlib]System.Object
{
  .method famorassem hidebysig specialname newslot abstract virtual instance int32 get_Value() cil managed
  {
  }
  .property instance int32 Value()
  {
    .get instance int32 BaseWithProtectedInternal::get_Value()
  }
  .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// The override narrows the accessor to 'family' while the property stays protected internal.
// Repeating that narrowing in C# is rejected, because an override cannot change accessibility.
.class public auto ansi beforefieldinit DerivedNarrowsAccessor
       extends BaseWithProtectedInternal
{
  .method family hidebysig specialname virtual instance int32 get_Value() cil managed
  {
    ldc.i4.0
    ret
  }
  .property instance int32 Value()
  {
    .get instance int32 DerivedNarrowsAccessor::get_Value()
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void BaseWithProtectedInternal::.ctor()
    ret
  }
}

// A genuinely narrower accessor on a non-override still needs its modifier.
.class public auto ansi beforefieldinit NarrowerAccessorOnDeclaration
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname instance int32 get_Value() cil managed
  {
    ldc.i4.0
    ret
  }
  .method family hidebysig specialname instance void set_Value(int32 'value') cil managed
  {
    ret
  }
  .property instance int32 Value()
  {
    .get instance int32 NarrowerAccessorOnDeclaration::get_Value()
    .set instance void NarrowerAccessorOnDeclaration::set_Value(int32)
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

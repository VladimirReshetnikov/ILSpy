.assembly extern mscorlib { }
.module CyclicInterfaceRefKind.dll

// Metadata can name two interfaces that inherit from each other. Reading a byref parameter's
// direction consults the contract it implements, which is the other interface's parameter, whose
// direction consults this one back; without a stop the question never settles.
.class interface public abstract auto ansi CyclicInterfaceRefKindA
       implements CyclicInterfaceRefKindB
{
  .method public hidebysig newslot abstract virtual
          instance void  M(int32& x) cil managed
  {
  }
}

.class interface public abstract auto ansi CyclicInterfaceRefKindB
       implements CyclicInterfaceRefKindA
{
  .method public hidebysig newslot abstract virtual
          instance void  M(int32& x) cil managed
  {
  }
}

// Copyright (c) 2026 Vladimir Reshetnikov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

// Models a No-PIA local type pair: the embedding process drops TupleElementNamesAttribute from
// the embedded [TypeIdentifier] interface, while the implementing wrapper keeps its names.
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 9:0:0:0
}
.assembly extern System.Runtime.InteropServices
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 9:0:0:0
}
.assembly NoPiaTupleNames
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module NoPiaTupleNames.dll

.class interface public abstract auto ansi import ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ISecretsHandler
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
  .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 46 44 38 42 30 35 31 37 2D 37 31 46 34 2D 34 39 46 42 2D 41 41 33 30 2D 44 32 36 38 44 41 30 37 36 32 33 33 00 00 )
  .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.TypeIdentifierAttribute::.ctor() = ( 01 00 00 00 )

  .method public hidebysig newslot abstract virtual instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`3<string, string, object>>
          GetUserSecrets() cil managed
  {
  }

  .method public hidebysig newslot abstract virtual instance void
          AddUserSecret(valuetype [System.Runtime]System.ValueTuple`3<string, string, object> secret) cil managed
  {
  }

  .method public hidebysig newslot abstract virtual instance valuetype [System.Runtime]System.ValueTuple`2<string, string>
          GetNamedPair() cil managed
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6E 61 6D 65 05 76 61 6C 75 65 00 00 )
  }

  .method public hidebysig newslot abstract virtual instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`3<string, string, object>>
          GetExplicit() cil managed
  {
  }
}

.class public auto ansi beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.SecretsHandlerWrapper
       extends [System.Runtime]System.Object
       implements ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ISecretsHandler
{
  .method public final hidebysig newslot virtual instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`3<string, string, object>>
          GetUserSecrets() cil managed
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 03 00 00 00 0A 73 65 63 72 65 74 4E 61 6D 65 0B 73 65 63 72 65 74 56 61 6C 75 65 0C 6F 70 74 69 6F 6E 61 6C 44 61 74 61 00 00 )
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ret
  }

  .method public final hidebysig newslot virtual instance void
          AddUserSecret(valuetype [System.Runtime]System.ValueTuple`3<string, string, object> secret) cil managed
  {
    .param [1]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 03 00 00 00 0A 73 65 63 72 65 74 4E 61 6D 65 0B 73 65 63 72 65 74 56 61 6C 75 65 0C 6F 70 74 69 6F 6E 61 6C 44 61 74 61 00 00 )
    .maxstack 8
    IL_0000: ret
  }

  .method public final hidebysig newslot virtual instance valuetype [System.Runtime]System.ValueTuple`2<string, string>
          GetNamedPair() cil managed
  {
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 02 00 00 00 04 6E 61 6D 65 05 76 61 6C 75 65 00 00 )
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ldnull
    IL_0002: newobj instance void valuetype [System.Runtime]System.ValueTuple`2<string, string>::.ctor(!0, !1)
    IL_0007: ret
  }

  .method private final hidebysig newslot virtual instance class [System.Runtime]System.Collections.Generic.IEnumerable`1<valuetype [System.Runtime]System.ValueTuple`3<string, string, object>>
          'ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ISecretsHandler.GetExplicit'() cil managed
  {
    .override ICSharpCode.Decompiler.Tests.TestCases.ILPretty.ISecretsHandler::GetExplicit
    .param [0]
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.TupleElementNamesAttribute::.ctor(string[]) = ( 01 00 03 00 00 00 0A 73 65 63 72 65 74 4E 61 6D 65 0B 73 65 63 72 65 74 56 61 6C 75 65 0C 6F 70 74 69 6F 6E 61 6C 44 61 74 61 00 00 )
    .maxstack 8
    IL_0000: ldnull
    IL_0001: ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack 8
    IL_0000: ldarg.0
    IL_0001: call instance void [System.Runtime]System.Object::.ctor()
    IL_0006: ret
  }
}

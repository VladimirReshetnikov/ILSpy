.assembly extern mscorlib
{
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89)
	.ver 4:0:0:0
}
.assembly FieldSignatureAccessibility
{
}
.module FieldSignatureAccessibility.dll

// The CLR permits a field to be more accessible than the type appearing in its signature.
// C# does not, so the nested type must be widened when the field is emitted.
.class public auto ansi beforefieldinit FieldContainer
	extends [mscorlib]System.Object
{
	.class nested private sequential ansi sealed beforefieldinit FieldData
		extends [mscorlib]System.ValueType
	{
		.field public int32 Value
	}

	.field assembly static initonly valuetype FieldContainer/FieldData Data
}

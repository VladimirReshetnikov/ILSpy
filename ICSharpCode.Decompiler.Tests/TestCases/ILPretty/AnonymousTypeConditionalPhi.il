.assembly extern System.Runtime
{
	.publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
	.ver 4:0:0:0
}

.assembly AnonymousTypeConditionalPhi
{
	.hash algorithm 0x00008004 // SHA1
	.ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class private auto ansi sealed beforefieldinit '<>f__AnonymousType0`1'<'<Items>j__TPar'>
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)

	.field private initonly !'<Items>j__TPar' '<Items>i__Field'

	.method public hidebysig specialname
		instance !'<Items>j__TPar' get_Items () cil managed
	{
		.maxstack 8

		ldarg.0
		ldfld !0 class '<>f__AnonymousType0`1'<!'<Items>j__TPar'>::'<Items>i__Field'
		ret
	} // end of method <>f__AnonymousType0`1::get_Items

	.method public hidebysig specialname rtspecialname
		instance void .ctor (
			!'<Items>j__TPar' Items
		) cil managed
	{
		.maxstack 8

		ldarg.0
		call instance void [System.Runtime]System.Object::.ctor()
		ldarg.0
		ldarg.1
		stfld !0 class '<>f__AnonymousType0`1'<!'<Items>j__TPar'>::'<Items>i__Field'
		ret
	} // end of method <>f__AnonymousType0`1::.ctor

	.property instance !'<Items>j__TPar' Items ()
	{
		.get instance !0 '<>f__AnonymousType0`1'::get_Items()
	}
} // end of class <>f__AnonymousType0`1

.class private auto ansi sealed beforefieldinit '<>f__AnonymousType1`1'<'<Item>j__TPar'>
	extends [System.Runtime]System.Object
{
	.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)

	.field private initonly !'<Item>j__TPar' '<Item>i__Field'

	.method public hidebysig specialname
		instance !'<Item>j__TPar' get_Item () cil managed
	{
		.maxstack 8

		ldarg.0
		ldfld !0 class '<>f__AnonymousType1`1'<!'<Item>j__TPar'>::'<Item>i__Field'
		ret
	} // end of method <>f__AnonymousType1`1::get_Item

	.method public hidebysig specialname rtspecialname
		instance void .ctor (
			!'<Item>j__TPar' Item
		) cil managed
	{
		.maxstack 8

		ldarg.0
		call instance void [System.Runtime]System.Object::.ctor()
		ldarg.0
		ldarg.1
		stfld !0 class '<>f__AnonymousType1`1'<!'<Item>j__TPar'>::'<Item>i__Field'
		ret
	} // end of method <>f__AnonymousType1`1::.ctor

	.property instance !'<Item>j__TPar' Item ()
	{
		.get instance !0 '<>f__AnonymousType1`1'::get_Item()
	}
} // end of class <>f__AnonymousType1`1

.class public abstract auto ansi sealed beforefieldinit ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi
	extends [System.Runtime]System.Object
{
	.class nested private interface abstract auto ansi IMarker
	{
	} // end of class IMarker

	.class nested private sequential ansi sealed beforefieldinit MarkerStruct
		extends [System.Runtime]System.ValueType
		implements ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker
	{
	} // end of class MarkerStruct

	.class nested private auto ansi sealed beforefieldinit MarkerClass
		extends [System.Runtime]System.Object
		implements ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker
	{
		.method public hidebysig specialname rtspecialname
			instance void .ctor () cil managed
		{
			.maxstack 8

			ldarg.0
			call instance void [System.Runtime]System.Object::.ctor()
			ret
		} // end of method MarkerClass::.ctor
	} // end of class MarkerClass

	.class nested private sequential ansi sealed beforefieldinit SetterStruct
		extends [System.Runtime]System.ValueType
	{
		.method public hidebysig specialname
			instance void set_Value (
				int32 'value'
			) cil managed
		{
			.maxstack 0

			ret
		} // end of method SetterStruct::set_Value

		.property instance int32 Value ()
		{
			.set instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterStruct::set_Value(int32)
		}
	} // end of class SetterStruct

	.class nested private abstract auto ansi sealed SetterHolder
		extends [System.Runtime]System.Object
	{
		.field public static valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterStruct Field
		.field public static int32 InitializationCount

		.method private hidebysig specialname rtspecialname static
			void .cctor () cil managed
		{
			.maxstack 1

			ldc.i4.1
			stsfld int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterHolder::InitializationCount
			ret
		} // end of method SetterHolder::.cctor
	} // end of class SetterHolder

	.method private hidebysig static
		class [System.Runtime]System.Collections.Generic.IList`1<int32> GetList () cil managed
	{
		.maxstack 1

		ldnull
		ret
	} // end of method AnonymousTypeConditionalPhi::GetList

	.method private hidebysig static
		class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> GetEnumerable () cil managed
	{
		.maxstack 1

		ldnull
		ret
	} // end of method AnonymousTypeConditionalPhi::GetEnumerable

	.method private hidebysig static
		class [System.Runtime]System.Collections.Generic.IList`1<!!T> AsList<T> (
			!!T[] values
		) cil managed
	{
		.maxstack 1

		ldarg.0
		ret
	} // end of method AnonymousTypeConditionalPhi::AsList

	.method private hidebysig static
		class [System.Runtime]System.Collections.Generic.IReadOnlyCollection`1<!!T> AsReadOnlyCollection<T> (
			!!T[] values
		) cil managed
	{
		.maxstack 1

		ldarg.0
		ret
	} // end of method AnonymousTypeConditionalPhi::AsReadOnlyCollection

	.method public hidebysig static
		object Get (
			bool hasSource,
			bool recursive
		) cil managed
	{
		.maxstack 2
		.locals init (
			[0] class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> enumerable,
			[1] class '<>f__AnonymousType0`1'<class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32>> result
		)

		ldarg.0
		brfalse.s FALLBACK

		ldarg.1
		brtrue.s RECURSIVE

		call class [System.Runtime]System.Collections.Generic.IList`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::GetList()
		stloc.0
		ldloc.0
		br.s CREATE

RECURSIVE:
		call class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::GetEnumerable()

CREATE:
		newobj instance void class '<>f__AnonymousType0`1'<class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32>>::.ctor(!0)
		br.s RETURN

FALLBACK:
		call class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::GetEnumerable()
		newobj instance void class '<>f__AnonymousType0`1'<class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32>>::.ctor(!0)

RETURN:
		stloc.1
		ldloc.1
		ret
	} // end of method AnonymousTypeConditionalPhi::Get

	.method public hidebysig static
		object WideningBail (
			bool useEnumerable,
			object fallback
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> enumerable,
			[1] object result
		)

		ldarg.0
		brfalse.s FALLBACK

		call class [System.Runtime]System.Collections.Generic.IEnumerable`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::GetEnumerable()
		stloc.0
		ldloc.0
		br.s RETURN

FALLBACK:
		ldarg.1

RETURN:
		stloc.1
		ldloc.1
		ret
	} // end of method AnonymousTypeConditionalPhi::WideningBail

	// Boxing must remain branch-local. If the casts are removed, C# gives the conditional the
	// natural type long and changes the true branch from a boxed Int32 to a boxed Int64.
	.method private hidebysig static
		object BoxedNumericWidening (
			bool condition
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] object intMaterialized,
			[1] object longMaterialized,
			[2] object result
		)

		ldarg.0
		brfalse.s LONG_VALUE

		ldc.i4.1
		box [System.Runtime]System.Int32
		stloc.0
		ldloc.0
		br.s RETURN_BOXED

LONG_VALUE:
		ldc.i8 2
		box [System.Runtime]System.Int64
		stloc.1
		ldloc.1

RETURN_BOXED:
		stloc.2
		ldloc.2
		ret
	} // end of method AnonymousTypeConditionalPhi::BoxedNumericWidening

	// This direct stack conditional exercises target-context translation independently of phi
	// normalization. Moving either box outside the conditional changes the runtime object type.
	.method private hidebysig static
		object DirectBoxedNumericWidening (
			bool condition
		) cil managed
	{
		.maxstack 1

		ldarg.0
		brfalse.s DIRECT_LONG_VALUE

		ldc.i4.1
		box [System.Runtime]System.Int32
		br.s DIRECT_RETURN

DIRECT_LONG_VALUE:
		ldc.i8 2
		box [System.Runtime]System.Int64

DIRECT_RETURN:
		ret
	} // end of method AnonymousTypeConditionalPhi::DirectBoxedNumericWidening

	// Both materialization conversions are identities. The conditional retains the same natural
	// IMarker type, so the redundant branch locals can be removed without losing the named anchor.
	.method private hidebysig static
		object NamedIdentityConditional (
			bool condition,
			class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker left,
			class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker right
		) cil managed
	{
		.maxstack 1
		.locals init (
			[0] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker leftMaterialized,
			[1] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker rightMaterialized,
			[2] object result
		)

		ldarg.0
		brfalse.s RIGHT

		ldarg.1
		stloc.0
		ldloc.0
		br.s RETURN

RIGHT:
		ldarg.2
		stloc.1
		ldloc.1

RETURN:
		stloc.2
		ldloc.2
		ret
	} // end of method AnonymousTypeConditionalPhi::NamedIdentityConditional

	.method private hidebysig static
		object BoxedInterfaceConditional (
			bool condition,
			valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/MarkerStruct marker
		) cil managed
	{
		.maxstack 2
		.locals init (
			[0] class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker item,
			[1] class '<>f__AnonymousType1`1'<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker> result
		)

		ldarg.0
		brfalse.s CLASS_ITEM

		ldarg.1
		box valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/MarkerStruct
		stloc.0
		ldloc.0
		br.s CREATE_INTERFACE_ANONYMOUS

CLASS_ITEM:
		newobj instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/MarkerClass::.ctor()

CREATE_INTERFACE_ANONYMOUS:
		newobj instance void class '<>f__AnonymousType1`1'<class ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/IMarker>::.ctor(!0)
		stloc.1
		ldloc.1
		ret
	} // end of method AnonymousTypeConditionalPhi::BoxedInterfaceConditional

	.method private hidebysig static
		object AnonymousContainingIdentity (
			bool condition
		) cil managed
	{
		.maxstack 4
		.locals init (
			[0] class '<>f__AnonymousType1`1'<int32>[] first,
			[1] class '<>f__AnonymousType1`1'<int32>[] second,
			[2] class '<>f__AnonymousType1`1'<int32>[] materialized
		)

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.1
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.0

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.2
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.1

		ldarg.0
		brfalse.s IDENTITY_SECOND

		ldloc.0
		stloc.2
		ldloc.2
		br.s IDENTITY_RETURN

IDENTITY_SECOND:
		ldloc.1

IDENTITY_RETURN:
		ret
	} // end of method AnonymousTypeConditionalPhi::AnonymousContainingIdentity

	.method private hidebysig static
		object AnonymousContainingCallIdentity (
			bool condition
		) cil managed
	{
		.maxstack 4
		.locals init (
			[0] class '<>f__AnonymousType1`1'<int32>[] first,
			[1] class '<>f__AnonymousType1`1'<int32>[] second,
			[2] class [System.Runtime]System.Collections.Generic.IList`1<class '<>f__AnonymousType1`1'<int32>> materialized
		)

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.5
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.0

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.6
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.1

		ldarg.0
		brfalse.s CALL_SECOND

		ldloc.0
		call class [System.Runtime]System.Collections.Generic.IList`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::AsList<class '<>f__AnonymousType1`1'<int32>>(!!0[])
		stloc.2
		ldloc.2
		br.s CALL_RETURN

CALL_SECOND:
		ldloc.1
		call class [System.Runtime]System.Collections.Generic.IList`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::AsList<class '<>f__AnonymousType1`1'<int32>>(!!0[])

CALL_RETURN:
		ret
	} // end of method AnonymousTypeConditionalPhi::AnonymousContainingCallIdentity

	.method private hidebysig static
		object AnonymousContainingWideningBail (
			bool condition
		) cil managed
	{
		.maxstack 4
		.locals init (
			[0] class '<>f__AnonymousType1`1'<int32>[] first,
			[1] class '<>f__AnonymousType1`1'<int32>[] second,
			[2] class [System.Runtime]System.Collections.Generic.IEnumerable`1<class '<>f__AnonymousType1`1'<int32>> materialized
		)

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.3
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.0

		ldc.i4.1
		newarr class '<>f__AnonymousType1`1'<int32>
		dup
		ldc.i4.0
		ldc.i4.4
		newobj instance void class '<>f__AnonymousType1`1'<int32>::.ctor(!0)
		stelem.ref
		stloc.1

		ldarg.0
		brfalse.s WIDENING_SECOND

		ldloc.0
		call class [System.Runtime]System.Collections.Generic.IList`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::AsList<class '<>f__AnonymousType1`1'<int32>>(!!0[])
		stloc.2
		ldloc.2
		br.s WIDENING_RETURN

WIDENING_SECOND:
		ldloc.1
		call class [System.Runtime]System.Collections.Generic.IReadOnlyCollection`1<!!0> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::AsReadOnlyCollection<class '<>f__AnonymousType1`1'<int32>>(!!0[])

WIDENING_RETURN:
		ret
	} // end of method AnonymousTypeConditionalPhi::AnonymousContainingWideningBail

	// Structural negative for conditional-phi normalization. The ILFunction branches report
	// the delegate type through InferType, but their emitted lambdas still need that type as
	// a target. The conservative statement form remains unspellable and is not a compile-safety
	// oracle. AnonymousContainingIdentity and AnonymousContainingCallIdentity are the compiling
	// positives; AnonymousContainingWideningBail is the compiling negative.
	.method private hidebysig static
		int32 '<AnonymousContainingLambdaBail>b__0' (
			class '<>f__AnonymousType1`1'<int32> item
		) cil managed
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		.maxstack 1

		ldarg.0
		callvirt instance !0 class '<>f__AnonymousType1`1'<int32>::get_Item()
		ret
	} // end of method AnonymousTypeConditionalPhi::<AnonymousContainingLambdaBail>b__0

	.method private hidebysig static
		int32 '<AnonymousContainingLambdaBail>b__1' (
			class '<>f__AnonymousType1`1'<int32> item
		) cil managed
	{
		.custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		.maxstack 2

		ldarg.0
		callvirt instance !0 class '<>f__AnonymousType1`1'<int32>::get_Item()
		ldc.i4.1
		add
		ret
	} // end of method AnonymousTypeConditionalPhi::<AnonymousContainingLambdaBail>b__1

	.method private hidebysig static
		object AnonymousContainingLambdaBail (
			bool condition
		) cil managed
	{
		.maxstack 2
		.locals init (
			[0] class [System.Runtime]System.Func`2<class '<>f__AnonymousType1`1'<int32>, int32> materialized
		)

		ldarg.0
		brfalse.s LAMBDA_SECOND

		ldnull
		ldftn int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::'<AnonymousContainingLambdaBail>b__0'(class '<>f__AnonymousType1`1'<int32>)
		newobj instance void class [System.Runtime]System.Func`2<class '<>f__AnonymousType1`1'<int32>, int32>::.ctor(object, native int)
		stloc.0
		ldloc.0
		br.s LAMBDA_RETURN

LAMBDA_SECOND:
		ldnull
		ldftn int32 ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::'<AnonymousContainingLambdaBail>b__1'(class '<>f__AnonymousType1`1'<int32>)
		newobj instance void class [System.Runtime]System.Func`2<class '<>f__AnonymousType1`1'<int32>, int32>::.ctor(object, native int)

LAMBDA_RETURN:
		ret
	} // end of method AnonymousTypeConditionalPhi::AnonymousContainingLambdaBail

	.method public hidebysig static
		void GuardedSetterBail (
			bool condition,
			int32 val
		) cil managed
	{
		.maxstack 3

		ldarg.0
		brtrue.s SET_VALUE

		newobj instance void [System.Runtime]System.InvalidOperationException::.ctor()
		throw

SET_VALUE:
		call class [System.Runtime]System.Collections.Generic.IList`1<int32> ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi::GetList()
		ldc.i4.0
		ldarg.1
		callvirt instance void class [System.Runtime]System.Collections.Generic.IList`1<int32>::set_Item(int32, !0)
		ret
	} // end of method AnonymousTypeConditionalPhi::GuardedSetterBail

	.method private hidebysig static
		void GuardedStaticSetterBail (
			bool condition,
			int32 val
		) cil managed
	{
		.maxstack 2

		ldarg.0
		brtrue.s SET_STATIC_VALUE

		newobj instance void [System.Runtime]System.InvalidOperationException::.ctor()
		throw

SET_STATIC_VALUE:
		ldsflda valuetype ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterStruct ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterHolder::Field
		ldarg.1
		call instance void ICSharpCode.Decompiler.Tests.TestCases.ILPretty.AnonymousTypeConditionalPhi/SetterStruct::set_Value(int32)
		ret
	} // end of method AnonymousTypeConditionalPhi::GuardedStaticSetterBail
} // end of class AnonymousTypeConditionalPhi

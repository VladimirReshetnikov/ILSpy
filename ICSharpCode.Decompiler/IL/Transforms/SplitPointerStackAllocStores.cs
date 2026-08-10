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

#nullable enable

using System.Linq;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// A <c>stackalloc</c> whose result is a pointer (rather than a <c>Span&lt;T&gt;</c>) is only
	/// valid C# as the initializer of a pointer-typed local declaration; in an assignment position it
	/// is typed <c>Span&lt;T&gt;</c>, which does not convert to the pointer type (CS8346).
	///
	/// When the compiler stores such a <c>localloc</c> into a local that is assigned in more than one
	/// place (e.g. a pointer that gets its buffer from <c>stackalloc</c> in one branch and from a heap
	/// allocation in another), that local is declared separately from its stores, so the
	/// <c>stackalloc</c> would end up on the right-hand side of a plain assignment. This transform
	/// redirects the <c>stackalloc</c> into a fresh single-definition local (which becomes its
	/// declaration initializer) and copies that local into the original target, keeping the output
	/// compilable without reordering any side effects.
	/// </summary>
	public class SplitPointerStackAllocStores : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			foreach (var block in function.Descendants.OfType<Block>())
			{
				if (block.Kind != BlockKind.ControlFlow)
					continue;
				for (int i = 0; i < block.Instructions.Count; i++)
				{
					if (block.Instructions[i] is not StLoc store)
						continue;
					if (!IsPointerStackAlloc(store.Value))
						continue;
					var target = store.Variable;
					// A single-definition pointer local is normally declared together with this store,
					// so the stackalloc lands in a declaration initializer. SplitVariables may, however,
					// create another live variable for the same original IL local slot. Variable naming
					// can merge those live ranges again, causing this store to become an assignment.
					// Treat that case like a multi-store target as well.
					if (target.IsSingleDefinition && !SharesOriginalLocalSlot(function, target))
						continue;
					if (target.Kind != VariableKind.Local && target.Kind != VariableKind.StackSlot)
						continue;
					if (GetStackAllocPointerType(store.Value, target, context) is not { } pointerType)
						continue;

					context.Step($"Split pointer stackalloc store to '{target.Name}'", store);
					var tempVariable = function.RegisterVariable(VariableKind.Local, pointerType);
					var value = store.Value;
					var copyToTarget = new StLoc(target, new LdLoc(tempVariable));
					copyToTarget.AddILRange(store);
					store.ReplaceWith(copyToTarget);
					block.Instructions.Insert(i, new StLoc(tempVariable, value));
					i++;
				}
			}
		}

		static bool SharesOriginalLocalSlot(ILFunction function, ILVariable target)
		{
			return target.Index != null && function.Variables.Any(variable =>
				variable != target
				&& variable.Index == target.Index
				&& variable.Kind == target.Kind
				&& variable.Type.Equals(target.Type)
				&& variable.StoreCount > 0);
		}

		/// <summary>
		/// Returns the pointer type to declare the stackalloc under, or null where the store needs no
		/// splitting. A target that is a pointer keeps its own type. A target typed as a native
		/// integer - which happens where the buffer is only ever cast to the element type at each use
		/// - takes a byte pointer: the allocation size is already in bytes, so the two are the same
		/// buffer, and the casts at the uses are unaffected.
		/// </summary>
		static IType? GetStackAllocPointerType(ILInstruction value, ILVariable target, ILTransformContext context)
		{
			if (target.Type.Kind == TypeKind.Pointer)
				return target.Type;
			// An initializer block writes its elements through the target's own type, so it cannot be
			// re-typed; only a bare allocation can.
			if (value is not LocAlloc allocation || target.Type.GetStackType() != StackType.I)
				return null;
			// The allocation size is a count times sizeof(T) where the buffer has an element type;
			// declaring the pointer as T* is what lets the stackalloc be written without a cast, which
			// C# does not allow on one. A size that says nothing gives a byte buffer, which the size
			// is measured in anyway.
			// Shares the matcher with ExpressionBuilder.TranslateLocAlloc: the two decide the same
			// question and disagreeing means declaring a pointer of one type and stackallocating
			// another.
			var elementType = CSharp.ExpressionBuilder.MatchStackAllocSize(allocation.Argument, out var sizeOfElementType, out _)
					? sizeOfElementType
					: context.TypeSystem.FindType(KnownTypeCode.Byte);
			return new PointerType(elementType);
		}

		static bool IsPointerStackAlloc(ILInstruction value)
		{
			return value.ResultType == StackType.I
				&& (value is LocAlloc || value is Block { Kind: BlockKind.StackAllocInitializer });
		}
	}
}

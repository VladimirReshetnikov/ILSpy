// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Tests.Helpers;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class ILPrettyTestRunner
	{
		static readonly string TestCasePath = Tester.TestCasePath + "/ILPretty";

		[Test]
		public void AllFilesHaveTests()
		{
			var testNames = typeof(ILPrettyTestRunner).GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any())
				.Select(m => m.Name)
				.ToArray();
			foreach (var file in new DirectoryInfo(TestCasePath).EnumerateFiles())
			{
				if (file.Extension.Equals(".il", StringComparison.OrdinalIgnoreCase))
				{
					var testName = file.Name.Split('.')[0];
					Assert.That(testNames, Has.Member(testName));
					Assert.That(File.Exists(Path.Combine(TestCasePath, testName + ".cs")));
				}
			}
		}

		[Test, Ignore("Need to decide how to represent virtual methods without 'newslot' flag")]
		public async Task Issue379()
		{
			await Run();
		}

		[Test]
		public async Task VBByRefOutParameterOverride()
		{
			await Run();
		}

		[Test]
		public async Task FieldKeywordCollision()
		{
			await Run();
		}

		[Test]
		public async Task DuplicateParameterNames()
		{
			await Run();
		}

		[Test]
		public async Task AwaitInCatchUnresolvedExceptionDispatchInfo()
		{
			await Run();
		}

		[Test]
		public async Task ConstrainedCallOnRefStruct()
		{
			await Run();
		}

		[Test]
		public async Task FakeMethodOnGenericInstance()
		{
			await Run();
		}

		[Test]
		public async Task OutParameterWithUnresolvedSignatureType()
		{
			await Run();
		}

		[Test]
		public async Task RefCastValueReceiver()
		{
			await Run();
		}

		[Test]
		public async Task AttributeNotValidOnConstructor()
		{
			await Run();
		}

		[Test]
		public async Task AttributeNotValidOnMember()
		{
			await Run();
		}

		[Test]
		public async Task NamedArgumentOverloadCollision()
		{
			await Run();
		}

		[Test]
		public async Task AmbiguousTrailingOptionalArguments()
		{
			await Run();
		}

		[Test]
		public async Task ExternalFieldNameCollision()
		{
			var libraryFile = Path.Combine(TestCasePath, "ExternalFieldNameCollision.Library.il");
			var library = await Tester.AssembleIL(libraryFile, AssemblerOptions.Library).ConfigureAwait(false);
			try
			{
				await Run();
			}
			finally
			{
				Tester.RepeatOnIOError(() => File.Delete(library));
			}
		}

		[Test]
		public async Task ProtectedInternalCrossAssemblyOverride()
		{
			var libraryFile = Path.Combine(TestCasePath, "ProtectedInternalCrossAssemblyOverride.Library.il");
			var library = await Tester.AssembleIL(libraryFile, AssemblerOptions.Library).ConfigureAwait(false);
			try
			{
				await Run();
			}
			finally
			{
				Tester.RepeatOnIOError(() => File.Delete(library));
			}
		}

		[Test]
		public async Task ProtectedInternalFriendAssemblyOverride()
		{
			var libraryFile = Path.Combine(TestCasePath, "ProtectedInternalFriendAssemblyOverride.Library.il");
			var library = await Tester.AssembleIL(libraryFile, AssemblerOptions.Library).ConfigureAwait(false);
			try
			{
				await Run();
			}
			finally
			{
				Tester.RepeatOnIOError(() => File.Delete(library));
			}
		}

		[Test]
		public async Task AmbiguousIvtName()
		{
			var libraryA = await Tester.AssembleIL(Path.Combine(TestCasePath, "AmbiguousIvtName.LibraryA.il"), AssemblerOptions.Library).ConfigureAwait(false);
			var libraryB = await Tester.AssembleIL(Path.Combine(TestCasePath, "AmbiguousIvtName.LibraryB.il"), AssemblerOptions.Library).ConfigureAwait(false);
			try
			{
				await Run();
			}
			finally
			{
				Tester.RepeatOnIOError(() => File.Delete(libraryA));
				Tester.RepeatOnIOError(() => File.Delete(libraryB));
			}
		}

		[Test]
		public async Task CrossRootMemberCollision()
		{
			var ilFile = Path.Combine(TestCasePath, "CrossRootMemberCollision.il");
			var csFile = Path.Combine(TestCasePath, "CrossRootMemberCollision.cs");
			var executable = await Tester.AssembleIL(ilFile, AssemblerOptions.Library).ConfigureAwait(false);
			var decompiled = await Tester.DecompileCSharpType(executable,
				new ICSharpCode.Decompiler.TypeSystem.FullTypeName("CrossRootConsumer"),
				new DecompilerSettings { FileScopedNamespaces = false }).ConfigureAwait(false);
			CodeAssert.FilesAreEqual(csFile, decompiled, ["EXPECTED_OUTPUT"]);
			Tester.RepeatOnIOError(() => File.Delete(decompiled));
		}

		[Test]
		public void SharedLabelAllocatorAvoidsNestedFunctionCollisions()
		{
			var allocator = new ICSharpCode.Decompiler.CSharp.StatementBuilder.LabelAllocator();
			Assert.That(allocator.GetUniqueLabel("IL_0000"), Is.EqualTo("IL_0000"));
			Assert.That(allocator.GetUniqueLabel("IL_0000_2"), Is.EqualTo("IL_0000_2"));
			Assert.That(allocator.GetUniqueLabel("IL_0000"), Is.EqualTo("IL_0000_3"));
		}

		[Test]
		public void RemovesOnlyAdjacentGotosInNestedStatementLists()
		{
			var adjacentGoto = new GotoStatement { Label = "IL_0001" };
			var nonAdjacentGoto = new GotoStatement { Label = "IL_0002" };
			var adjacentLabel = new LabelStatement { Label = "IL_0001" };
			var nonAdjacentLabel = new LabelStatement { Label = "IL_0002" };
			var nestedBlock = new BlockStatement {
				adjacentGoto,
				adjacentLabel,
				nonAdjacentGoto,
				new EmptyStatement(),
				nonAdjacentLabel
			};
			var root = new BlockStatement {
				new IfElseStatement {
					Condition = new PrimitiveExpression(true),
					TrueStatement = nestedBlock
				}
			};

			ICSharpCode.Decompiler.CSharp.StatementBuilder.RemoveGotosToNextLabel(root);

			Assert.That(adjacentGoto.Parent, Is.Null);
			Assert.That(nonAdjacentGoto.Parent, Is.SameAs(nestedBlock));
			// The removed goto was the only reference to IL_0001, so the label must go too
			// (an unreferenced label is a CS0164 warning on recompilation); IL_0002 is still
			// referenced by the surviving goto and must stay.
			Assert.That(adjacentLabel.Parent, Is.Null);
			Assert.That(nonAdjacentLabel.Parent, Is.SameAs(nestedBlock));
		}

		[Test]
		public async Task FieldKeywordCtorBackingFieldWrite()
		{
			await Run();
		}

		[Test]
		public async Task AutoPropertyBackingFieldDebuggerBrowsable()
		{
			await Run();
		}

		[Test]
		public async Task PrimaryConstructorMethodAttributes()
		{
			await Run();
		}

		[Test]
		public async Task SwitchTupleNaturalType()
		{
			await Run();
		}

		[Test]
		public async Task InitializerConditionalValue()
		{
			await Run();
		}

		[Test]
		public async Task AnonymousTypeConditionalPhi()
		{
			await Run();
		}

		[Test]
		public async Task CastBetweenTypeParameters()
		{
			await Run();
		}

		[Test]
		public async Task Issue646()
		{
			await Run();
		}

		[Test]
		public async Task Issue684()
		{
			await Run();
		}

		[Test]
		public async Task Issue959()
		{
			await Run();
		}

		[Test]
		public async Task Issue982()
		{
			await Run();
		}

		[Test]
		public async Task Issue1038()
		{
			await Run();
		}

		[Test]
		public async Task Issue1047()
		{
			await Run();
		}

		[Test]
		public async Task Issue1389()
		{
			await Run();
		}

		[Test]
		public async Task Issue1918()
		{
			await Run();
		}

		[Test]
		public async Task Issue1922()
		{
			await Run();
		}

		[Test]
		public async Task FSharpUsing_Debug()
		{
			await Run(settings: new DecompilerSettings { RemoveDeadStores = true, UseEnhancedUsing = false, FileScopedNamespaces = false });
		}

		[Test]
		public async Task FSharpUsing_Release()
		{
			await Run(settings: new DecompilerSettings { RemoveDeadStores = true, UseEnhancedUsing = false, FileScopedNamespaces = false });
		}

		[Test]
		public async Task DirectCallToExplicitInterfaceImpl()
		{
			await Run();
		}

		[Test]
		public async Task TruncatedAccessorBody()
		{
			await Run();
		}

		[Test]
		public async Task SealedRecordProtectedCopyCtor()
		{
			await Run();
		}

		[Test]
		public async Task ExplicitInterfacePropertyOverride()
		{
			await Run();
		}

		[Test]
		public async Task ImplicitInterfaceAccessorForwarders()
		{
			await Run();
		}

		[Test]
		public async Task EvalOrder()
		{
			await Run();
		}

		[Test]
		public async Task TailCall()
		{
			await Run();
		}

		[Test]
		public async Task CS1xSwitch_Debug()
		{
			await Run(settings: new DecompilerSettings { SwitchExpressions = false, FileScopedNamespaces = false });
		}

		[Test]
		public async Task CS1xSwitch_Release()
		{
			await Run(settings: new DecompilerSettings { SwitchExpressions = false, FileScopedNamespaces = false });
		}

		[Test]
		public async Task UnknownTypes()
		{
			await Run();
		}

		[Test]
		public async Task NoAccessorProperties()
		{
			await Run();
		}

		[Test]
		public async Task Issue1145()
		{
			await Run();
		}

		[Test]
		public async Task Issue1157()
		{
			await Run();
		}

		[Test]
		public async Task Issue1256()
		{
			await Run();
		}

		[Test]
		public async Task Issue1323()
		{
			await Run();
		}

		[Test]
		public async Task Issue1325()
		{
			await Run();
		}

		[Test]
		public async Task Issue1681()
		{
			await Run();
		}

		[Test]
		public async Task Issue1454()
		{
			await Run();
		}

		[Test]
		public async Task StackAllocIntoNativeIntLocal()
		{
			await Run();
		}

		[Test]
		public async Task ManagedPointerAddedToWideOffset()
		{
			await Run();
		}

		[Test]
		public async Task ProtectedSetterOnGrandparent()
		{
			await Run();
		}

		[Test]
		public async Task RefLocalAssignedInBranches()
		{
			await Run();
		}

		[Test]
		public async Task TupleBuiltFieldByField()
		{
			await Run();
		}

		[Test]
		public async Task DiscardNameTakenByLocalFunction()
		{
			await Run();
		}

		[Test]
		public async Task InitOnlySettersAfterInitializer()
		{
			await Run();
		}

		[Test]
		public async Task OperatorTrueOutsideDirectCondition()
		{
			await Run();
		}

		[Test]
		public async Task CompoundAssignmentResultConversion()
		{
			await Run();
		}

		[Test]
		public async Task ParameterizedProperty()
		{
			await Run();
		}

		[Test]
		public async Task ByRefExtensionOnReferenceType()
		{
			await Run();
		}

		[Test]
		public async Task UninitializedPointerLocal()
		{
			await Run();
		}

		[Test]
		public async Task OptionalNullDefaultOnValueType()
		{
			await Run();
		}

		[Test]
		public async Task OverrideNarrowsAccessibility()
		{
			await Run();
		}

		[Test]
		public async Task RepeatedSingleUseAttribute()
		{
			await Run();
		}

		[Test]
		public async Task InheritedAllowMultipleAttribute()
		{
			await Run();
		}

		[Test]
		public async Task CyclicInterfaceRefKind()
		{
			await Run();
		}

		[Test]
		public async Task Issue1638()
		{
			await Run();
		}

		[Test]
		public async Task Issue2104()
		{
			await Run();
		}

		[Test]
		public async Task Issue2443()
		{
			await Run();
		}

		[Test]
		public async Task Issue3344CkFinite()
		{
			await Run();
		}

		[Test]
		public async Task Issue3421()
		{
			await Run();
		}

		[Test]
		public async Task Issue3442()
		{
			await Run();
		}

		[Test]
		public async Task Issue3465()
		{
			await Run();
		}

		[Test]
		public async Task Issue3466()
		{
			await Run();
		}

		[Test]
		public async Task Issue3504()
		{
			await Run();
		}

		[Test]
		public async Task Issue3524()
		{
			await Run();
		}

		[Test]
		public async Task NullPropagationExtensionMethod()
		{
			await Run();
		}

		[Test]
		public async Task Issue3552()
		{
			await Run();
		}

		[Test]
		public async Task Issue2260SwitchString()
		{
			await Run();
		}

		[Test]
		public async Task PatternVariableInJoinBlock()
		{
			await Run();
		}

		[Test]
		public async Task HoistedBaseCtorArg()
		{
			await Run();
		}

		[Test]
		public async Task SwitchOnStringNegativeCharIndex()
		{
			await Run();
		}

		[Test]
		public async Task CachedReadOnlySpanFromLazyCache()
		{
			await Run();
		}

		[Test]
		public async Task ConstantBlobs()
		{
			await Run();
		}

		[Test]
		public async Task SequenceOfNestedIfs()
		{
			await Run();
		}

		[Test]
		public async Task StackAllocDuplicateStore()
		{
			await Run();
		}

		[Test]
		public async Task StackAllocConditionalPointer()
		{
			await Run();
		}

		[Test, Platform("Win")] // UseLegacyAssembler requires the .NET Framework ilasm
		public async Task Unsafe()
		{
			await Run(assemblerOptions: AssemblerOptions.Library | AssemblerOptions.UseLegacyAssembler);
		}

		[Test]
		public async Task CallIndirect()
		{
			await Run();
		}

		[Test]
		public async Task ConstructorInitializerAfterLeadingStatements()
		{
			await Run();
		}

		[Test]
		public async Task ForwardedClosureLocalFunction()
		{
			await Run();
		}

		[Test]
		public async Task QueryExpressionInParameter()
		{
			await Run();
		}

		[Test]
		public async Task TupleNamesFromConsumerLambdas()
		{
			await Run();
		}

		[Test]
		public async Task NoPiaTupleNames()
		{
			await Run();
		}

		[Test]
		public async Task OrderByThenByRangeVariable()
		{
			await Run();
		}

		[Test]
		public async Task FieldKeywordPatternVariableCollision()
		{
			await Run();
		}

		[Test]
		public async Task StaticLocalFunctionLocalNameCollision()
		{
			await Run();
		}

		[Test]
		public async Task LocalFunctionSignatureAccessibility()
		{
			await Run();
		}

		[Test]
		public async Task FieldSignatureAccessibility()
		{
			await Run();
		}

		[Test]
		public async Task FSharpLoops_Debug()
		{
			CopyFSharpCoreDll();
			await Run(settings: new DecompilerSettings { RemoveDeadStores = true, FileScopedNamespaces = false });
		}

		[Test]
		public async Task FSharpLoops_Release()
		{
			CopyFSharpCoreDll();
			await Run(settings: new DecompilerSettings { RemoveDeadStores = true, FileScopedNamespaces = false });
		}

		[Test]
		public async Task WeirdEnums()
		{
			await Run();
		}

		[Test]
		public async Task GuessAccessors()
		{
			await Run();
		}

		[Test]
		public async Task RefLocalSlotSharing()
		{
			await Run();
		}

		[Test]
		public async Task RefLocalUninitialized()
		{
			var libraryFile = Path.Combine(TestCasePath, "RefLocalUninitialized.Library.il");
			var library = await Tester.AssembleIL(libraryFile, AssemblerOptions.Library).ConfigureAwait(false);
			try
			{
				await Run();
			}
			finally
			{
				Tester.RepeatOnIOError(() => File.Delete(library));
			}
		}

		[Test]
		public async Task SkipInitUnavailable()
		{
			await Run();
		}

		[Test]
		public async Task LockOutVariableSlotReuse()
		{
			await Run();
		}

		[Test]
		public async Task VarianceBoundDelegateClosure()
		{
			await Run();
		}

		[Test]
		public async Task EmptyBodies()
		{
			await Run();
		}

		[Test]
		public async Task ExpressionTreeDeinlinedInArrayInitializer()
		{
			await Run();
		}

		[Test]
		public async Task MonoFixed()
		{
			await Run();
		}

		[Test]
		public async Task ExtensionEncodingV1()
		{
			// uses Microsoft.Net.Compilers.Toolset 5.0.0-2.25380.108
			// see ExtensionEncodingV1.il for details
			await Run();
		}

		[Test]
		public async Task ExtensionEncodingV2()
		{
			// uses Microsoft.Net.Compilers.Toolset 5.0.0-2.25451.107
			// see ExtensionEncodingV2.il for details
			await Run();
		}

		[Test]
		public async Task SortSwitchSections()
		{
			await Run(settings: new DecompilerSettings { SortSwitchSections = true, FileScopedNamespaces = false });
		}

		[Test]
		public async Task SwitchExpressionThrowHelper()
		{
			await Run();
		}

		[Test]
		public async Task ScopedRefStructLocal()
		{
			await Run();
		}

		[Test]
		public async Task LegacyScopedInParameter()
		{
			await Run();
		}

		[Test]
		public async Task ModernRefSafetyScopedInParameter()
		{
			await Run();
		}

		[Test]
		public async Task SelfReferencingDelegate()
		{
			await Run();
		}

		[Test]
		public async Task WindowsRuntimeEventSubscription()
		{
			await Run();
		}

		[Test]
		public async Task FileLocalTypes()
		{
			await Run();
		}

		async Task Run([CallerMemberName] string testName = null, DecompilerSettings settings = null,
			AssemblerOptions assemblerOptions = AssemblerOptions.Library)
		{
			if (settings == null)
			{
				// never use file-scoped namespaces, unless explicitly specified
				settings = new DecompilerSettings { FileScopedNamespaces = false };
			}
			var ilFile = Path.Combine(TestCasePath, testName + ".il");
			var csFile = Path.Combine(TestCasePath, testName + ".cs");

			var executable = await Tester.AssembleIL(ilFile, assemblerOptions).ConfigureAwait(false);
			var decompiled = await Tester.DecompileCSharp(executable, settings).ConfigureAwait(false);

			CodeAssert.FilesAreEqual(csFile, decompiled, ["EXPECTED_OUTPUT"]);
			Tester.RepeatOnIOError(() => File.Delete(decompiled));
		}

		static readonly object copyLock = new object();

		static void CopyFSharpCoreDll()
		{
			lock (copyLock)
			{
				if (File.Exists(Path.Combine(TestCasePath, "FSharp.Core.dll")))
					return;
				string fsharpCoreDll = Path.Combine(TestCasePath, "..", "..", "..", "ILSpy-tests", "FSharp", "FSharp.Core.dll");
				if (!File.Exists(fsharpCoreDll))
					Assert.Ignore("Ignored because of missing ILSpy-tests repo. Must be checked out separately from https://github.com/icsharpcode/ILSpy-tests!");
				File.Copy(fsharpCoreDll, Path.Combine(TestCasePath, "FSharp.Core.dll"));
			}
		}
	}
}

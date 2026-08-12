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

using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.Decompiler.TypeSystem;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class VBPrettyTestRunner
	{
		static readonly string TestCasePath = Tester.TestCasePath + "/VBPretty";

		[Test]
		public void AllFilesHaveTests()
		{
			var testNames = typeof(VBPrettyTestRunner).GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Any())
				.Select(m => m.Name)
				.ToArray();
			foreach (var file in new DirectoryInfo(TestCasePath).EnumerateFiles())
			{
				if (file.Extension.Equals(".vb", StringComparison.OrdinalIgnoreCase))
				{
					var testName = file.Name.Split('.')[0];
					Assert.That(testNames, Has.Member(testName));
					Assert.That(File.Exists(Path.Combine(TestCasePath, testName + ".cs")));
				}
			}
		}

		static readonly CompilerOptions[] defaultOptions = Tester.SupportedOnCurrentPlatform(new[]
		{
			CompilerOptions.None,
			CompilerOptions.Optimize,
			CompilerOptions.UseRoslyn1_3_2 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn1_3_2 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn2_10_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn2_10_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn3_11_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn3_11_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslynLatest | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslynLatest | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn1_3_2,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn1_3_2,
			CompilerOptions.UseRoslyn2_10_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn2_10_0,
			CompilerOptions.UseRoslyn3_11_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn3_11_0,
			CompilerOptions.UseRoslyn4_14_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn4_14_0,
			CompilerOptions.UseRoslynLatest,
			CompilerOptions.Optimize | CompilerOptions.UseRoslynLatest,
		});

		static readonly CompilerOptions[] roslynOnlyOptions = Tester.SupportedOnCurrentPlatform(new[]
		{
			CompilerOptions.UseRoslyn1_3_2 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn1_3_2 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn2_10_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn2_10_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn3_11_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn3_11_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn4_14_0 | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslynLatest | CompilerOptions.TargetNet40,
			CompilerOptions.Optimize | CompilerOptions.UseRoslynLatest | CompilerOptions.TargetNet40,
			CompilerOptions.UseRoslyn1_3_2,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn1_3_2,
			CompilerOptions.UseRoslyn2_10_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn2_10_0,
			CompilerOptions.UseRoslyn3_11_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn3_11_0,
			CompilerOptions.UseRoslyn4_14_0,
			CompilerOptions.Optimize | CompilerOptions.UseRoslyn4_14_0,
			CompilerOptions.UseRoslynLatest,
			CompilerOptions.Optimize | CompilerOptions.UseRoslynLatest,
		});

		[Test]
		public async Task Async([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test] // TODO: legacy VB compound assign
		public async Task VBCompoundAssign([ValueSource(nameof(roslynOnlyOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task SelectEmbeddedRuntime([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			// Only the program type is compared: what is under test is the string-switch fold
			// through the embedded EmbeddedOperators.CompareString helper, while the embedded
			// runtime types themselves are compiler plumbing whose exact shape varies with the
			// compiler version.
			await Run(options: options | CompilerOptions.Library | CompilerOptions.EmbedVisualBasicRuntime,
				typeName: "EmbeddedProgram");
		}

		[Test]
		public async Task ParameterizedProperties([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task Select([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBAnonymousTypes([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			IgnoreIfVbRuntimeSubstituted(options);
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task Issue1906([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task Issue2192([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			IgnoreIfVbRuntimeSubstituted(options);
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task OutParameterDefiniteAssignment([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBPropertiesTest([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBAutomaticEvents([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBExplicitInterfaceImplementation([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBNonGenericForEach([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			IgnoreIfVbRuntimeSubstituted(options);
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBClosures([ValueSource(nameof(roslynOnlyOptions))] CompilerOptions options)
		{
			// The Roslyn VB compiler emits copy-constructor closures; the legacy (pre-Roslyn) compiler
			// uses a different closure shape, so this fixture targets the Roslyn compilers only.
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBAnonymousType([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task YieldReturn([ValueSource(nameof(defaultOptions))] CompilerOptions options)
		{
			await Run(options: options | CompilerOptions.Library);
		}

		[Test]
		public async Task VBYieldInTryCatch()
		{
			await Run(options: CompilerOptions.Optimize | CompilerOptions.UseRoslynLatest, verifyRoundTrip: true);
		}

		static void IgnoreIfVbRuntimeSubstituted(CompilerOptions options)
		{
			if (!OperatingSystem.IsWindows()
				&& (options & CompilerOptions.UseRoslynMask) == CompilerOptions.UseRoslyn2_10_0
				&& (options & CompilerOptions.TargetNet40) == 0)
			{
				Assert.Ignore("The non-Windows netcore-2.2 configuration substitutes the legacy " +
					"Microsoft.VisualBasic as -vbruntime (see Tester.CompileVB); the resulting " +
					"reference graph changes this test's decompiled output.");
			}
		}

		async Task Run([CallerMemberName] string testName = null, CompilerOptions options = CompilerOptions.UseDebug, DecompilerSettings settings = null, string typeName = null, bool verifyRoundTrip = false)
		{
			if (verifyRoundTrip && (options & CompilerOptions.UseRoslynMask) != 0)
			{
				options |= CompilerOptions.UseTestRunner;
			}
			var vbFile = Path.Combine(TestCasePath, testName + ".vb");
			var csFile = Path.Combine(TestCasePath, testName + ".cs");
			var exeFile = TestsAssemblyOutput.GetFilePath(TestCasePath, testName, Tester.GetSuffix(options) + ".exe");
			if (options.HasFlag(CompilerOptions.Library))
			{
				exeFile = Path.ChangeExtension(exeFile, ".dll");
			}

			CompilerResults recompiled = null;
			string decompiled = null;
			bool succeeded = false;
			try
			{
				var executable = await Tester.CompileVB(vbFile, options | CompilerOptions.ReferenceVisualBasic, exeFile).ConfigureAwait(false);
				var decompilerSettings = settings ?? new DecompilerSettings { FileScopedNamespaces = false };
				decompiled = typeName == null
					? await Tester.DecompileCSharp(executable.PathToAssembly, decompilerSettings).ConfigureAwait(false)
					: await Tester.DecompileCSharpType(executable.PathToAssembly, new FullTypeName(typeName), decompilerSettings).ConfigureAwait(false);

				if (verifyRoundTrip)
				{
					recompiled = await Tester.CompileCSharp(decompiled, options | CompilerOptions.ReferenceVisualBasic).ConfigureAwait(false);
					await Tester.RunAndCompareOutput(testName + ".vb", executable.PathToAssembly, recompiled.PathToAssembly, decompiled,
						(options & CompilerOptions.UseTestRunner) != 0, (options & CompilerOptions.Force32Bit) != 0).ConfigureAwait(false);
				}

				CodeAssert.FilesAreEqual(csFile, decompiled, Tester.GetPreprocessorSymbols(options).ToArray());
				succeeded = true;
			}
			finally
			{
				if (succeeded && decompiled != null)
				{
					Tester.RepeatOnIOError(() => File.Delete(decompiled));
				}
				recompiled?.DeleteTempFiles();
			}
		}
	}
}

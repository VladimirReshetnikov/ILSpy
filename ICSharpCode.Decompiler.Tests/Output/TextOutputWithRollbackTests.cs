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

using System.Collections.Generic;
using System.Reflection.Metadata;


using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Output
{
	[TestFixture]
	public class TextOutputWithRollbackTests
	{
		/// <summary>
		/// Records every call it receives, so a test can assert on the arguments a replay forwarded.
		/// </summary>
		class RecordingOutput : ITextOutput
		{
			public List<string> Calls { get; } = new List<string>();

			public string IndentationString { get; set; } = "\t";

			public void Indent() => Calls.Add("Indent");
			public void Unindent() => Calls.Add("Unindent");
			public void Write(char ch) => Calls.Add($"Write({ch})");
			public void Write(string text) => Calls.Add($"Write({text})");
			public void WriteLine() => Calls.Add("WriteLine");

			public void WriteReference(OpCodeInfo opCode, bool omitSuffix = false)
				=> Calls.Add($"WriteReference(opCode:{opCode.Name}, omitSuffix:{omitSuffix})");

			public void WriteReference(MetadataFile metadata, Handle handle, string text, string protocol = "decompile", bool isDefinition = false)
				=> Calls.Add($"WriteReference(handle, {text}, {protocol}, isDefinition:{isDefinition})");

			public void WriteReference(IType type, string text, bool isDefinition = false)
				=> Calls.Add($"WriteReference(type, {text}, isDefinition:{isDefinition})");

			public void WriteReference(IMember member, string text, bool isDefinition = false)
				=> Calls.Add($"WriteReference(member, {text}, isDefinition:{isDefinition})");

			public void WriteLocalReference(string text, object reference, bool isDefinition = false, bool isHoverOnly = false)
				=> Calls.Add($"WriteLocalReference({text}, isDefinition:{isDefinition}, isHoverOnly:{isHoverOnly})");

			public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false, bool isDefinition = false)
				=> Calls.Add($"MarkFoldStart({collapsedText}, defaultCollapsed:{defaultCollapsed}, isDefinition:{isDefinition})");

			public void MarkFoldEnd() => Calls.Add("MarkFoldEnd");
		}

		[Test]
		public void CommitForwardsIsHoverOnly()
		{
			var target = new RecordingOutput();
			var rollback = new TextOutputWithRollback(target);

			rollback.WriteLocalReference("loc", new object(), isDefinition: true, isHoverOnly: true);
			rollback.Commit();

			Assert.That(target.Calls, Is.EqualTo(new[] {
				"WriteLocalReference(loc, isDefinition:True, isHoverOnly:True)"
			}));
		}

		[Test]
		public void CommitForwardsOmitSuffix()
		{
			var target = new RecordingOutput();
			var rollback = new TextOutputWithRollback(target);

			rollback.WriteReference(new OpCodeInfo(ILOpCode.Ldarg, "ldarg.s"), omitSuffix: true);
			rollback.Commit();

			Assert.That(target.Calls, Has.Count.EqualTo(1));
			Assert.That(target.Calls[0], Does.Contain("omitSuffix:True"));
		}

		[Test]
		public void CommitForwardsIsDefinitionOnFoldStart()
		{
			var target = new RecordingOutput();
			var rollback = new TextOutputWithRollback(target);

			rollback.MarkFoldStart("...", defaultCollapsed: true, isDefinition: true);
			rollback.Commit();

			Assert.That(target.Calls, Is.EqualTo(new[] {
				"MarkFoldStart(..., defaultCollapsed:True, isDefinition:True)"
			}));
		}

		[Test]
		public void NothingReachesTheTargetBeforeCommit()
		{
			var target = new RecordingOutput();
			var rollback = new TextOutputWithRollback(target);

			rollback.Write("discarded");
			rollback.WriteLine();

			Assert.That(target.Calls, Is.Empty);
		}
	}
}

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Disassembler
{
	[TestFixture]
	public class OpCodeInfoTests
	{
		/// <summary>
		/// The opcode display names the disassembler can produce, paired with the field name
		/// <see cref="OpCodeInfo.EncodedName"/> derives from each.
		/// </summary>
		static IEnumerable<(ILOpCode Code, string DisplayName)> KnownOpCodes()
		{
			foreach (ILOpCode code in Enum.GetValues<ILOpCode>())
			{
				string name = code.GetDisplayName();
				if (!string.IsNullOrEmpty(name))
					yield return (code, name);
			}
		}

		static readonly HashSet<string> OpCodesFieldNames =
			typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(OpCode))
				.Select(f => f.Name)
				.ToHashSet(StringComparer.Ordinal);

		[Test]
		public void EncodedNameMatchesAFieldOnSystemReflectionEmitOpCodes()
		{
			var unmatched = new List<string>();
			foreach (var (code, displayName) in KnownOpCodes())
			{
				string encoded = new OpCodeInfo(code, displayName).EncodedName;
				if (!OpCodesFieldNames.Contains(encoded))
					unmatched.Add($"{displayName} -> {encoded}");
			}

			Assert.That(unmatched, Is.Empty,
				"EncodedName feeds the documentation URL, so every value must name a real OpCodes field.");
		}

		/// <remarks>
		/// <see cref="OpCodeInfo.EncodedName"/> keys off the display name alone, so these cases pass the
		/// name that carries the trailing dot rather than a matching <see cref="ILOpCode"/>.
		/// </remarks>
		[TestCase("constrained.", "Constrained")]
		[TestCase("readonly.", "Readonly")]
		[TestCase("tail.", "Tailcall")]
		[TestCase("unaligned.", "Unaligned")]
		[TestCase("volatile.", "Volatile")]
		public void PrefixOpCodesEncodeToTheirDocumentedFieldNames(string displayName, string expected)
		{
			var info = new OpCodeInfo(default, displayName);

			Assert.That(info.EncodedName, Is.EqualTo(expected));
			Assert.That(OpCodesFieldNames, Does.Contain(expected));
		}

		/// <summary>
		/// The <c>no.</c> prefix is the one special case with no counterpart: neither
		/// <see cref="ILOpCode"/> nor <see cref="OpCodes"/> declares it, so no display name the
		/// disassembler produces reaches this branch.
		/// </summary>
		[Test]
		public void TheNoPrefixHasNoOpCodesCounterpart()
		{
			Assert.That(new OpCodeInfo(default, "no.").EncodedName, Is.EqualTo("No"));
			Assert.That(OpCodesFieldNames, Does.Not.Contain("No"));
			Assert.That(Enum.GetNames<ILOpCode>(), Does.Not.Contain("No"));
		}

		[Test]
		public void LinkIsBuiltFromTheEncodedName()
		{
			var info = new OpCodeInfo(ILOpCode.Readonly, "readonly.");

			Assert.That(info.Link, Is.EqualTo(
				"https://docs.microsoft.com/dotnet/api/system.reflection.emit.opcodes.readonly"));
		}
	}
}

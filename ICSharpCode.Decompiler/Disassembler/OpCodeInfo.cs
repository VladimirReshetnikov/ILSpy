// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Describes one IL opcode and helper metadata used by disassembly output.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="Name"/> preserves the textual mnemonic emitted in disassembly, including trailing dots for prefix opcodes.
	/// <see cref="EncodedName"/> converts that mnemonic into the naming style used by
	/// <see href="https://learn.microsoft.com/dotnet/api/system.reflection.emit.opcodes">System.Reflection.Emit.OpCodes</see>
	/// documentation URLs.
	/// </para>
	/// <para>
	/// This is a value type so opcode metadata can be passed and compared without allocations in hot disassembly paths.
	/// </para>
	/// </remarks>
	public struct OpCodeInfo : IEquatable<OpCodeInfo>
	{
		/// <summary>
		/// The numeric opcode value.
		/// </summary>
		public readonly ILOpCode Code;

		/// <summary>
		/// The IL mnemonic used in textual output.
		/// </summary>
		public readonly string Name;

		string encodedName;

		/// <summary>
		/// Initializes a new opcode descriptor.
		/// </summary>
		/// <param name="code">The numeric opcode value.</param>
		/// <param name="name">The textual IL mnemonic.</param>
		public OpCodeInfo(ILOpCode code, string name)
		{
			this.Code = code;
			this.Name = name ?? "";
			this.encodedName = null;
		}

		/// <summary>
		/// Compares this opcode descriptor with another instance.
		/// </summary>
		/// <param name="other">The instance to compare against.</param>
		/// <returns>
		/// <see langword="true"/> when both <see cref="Code"/> and <see cref="Name"/> are equal; otherwise <see langword="false"/>.
		/// </returns>
		public bool Equals(OpCodeInfo other)
		{
			return other.Code == this.Code && other.Name == this.Name;
		}

		public static bool operator ==(OpCodeInfo lhs, OpCodeInfo rhs) => lhs.Equals(rhs);
		public static bool operator !=(OpCodeInfo lhs, OpCodeInfo rhs) => !(lhs == rhs);

		public override bool Equals(object obj)
		{
			if (obj is OpCodeInfo opCode)
				return Equals(opCode);
			return false;
		}

		public override int GetHashCode()
		{
			return unchecked(982451629 * Code.GetHashCode() + 982451653 * Name.GetHashCode());
		}

		/// <summary>
		/// Gets the reference URL for this opcode in .NET API documentation.
		/// </summary>
		/// <value>
		/// A URL under <c>system.reflection.emit.opcodes</c> built from <see cref="EncodedName"/>.
		/// </value>
		public string Link => "https://docs.microsoft.com/dotnet/api/system.reflection.emit.opcodes." + EncodedName.ToLowerInvariant();

		/// <summary>
		/// Gets the opcode name encoded in the <see cref="System.Reflection.Emit.OpCodes"/> naming style.
		/// </summary>
		/// <value>
		/// The cached encoded name used for documentation links and interop with reflection opcode naming.
		/// </value>
		public string EncodedName {
			get {
				if (encodedName != null)
					return encodedName;
				switch (Name)
				{
					case "constrained.":
						encodedName = "Constrained";
						return encodedName;
					case "no.":
						encodedName = "No";
						return encodedName;
					case "readonly.":
						encodedName = "Reaonly";
						return encodedName;
					case "tail.":
						encodedName = "Tailcall";
						return encodedName;
					case "unaligned.":
						encodedName = "Unaligned";
						return encodedName;
					case "volatile.":
						encodedName = "Volatile";
						return encodedName;
				}
				string text = "";
				bool toUpperCase = true;
				foreach (var ch in Name)
				{
					if (ch == '.')
					{
						text += '_';
						toUpperCase = true;
					}
					else if (toUpperCase)
					{
						text += char.ToUpperInvariant(ch);
						toUpperCase = false;
					}
					else
					{
						text += ch;
					}
				}
				encodedName = text;
				return encodedName;
			}
		}
	}
}

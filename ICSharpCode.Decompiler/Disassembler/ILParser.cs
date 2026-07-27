// Copyright (c) 2018 Siegfried Pammer
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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Provides low-level IL bytecode decoding helpers used by the disassembler and control-flow scanners.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Methods on this type operate directly on <see cref="BlobReader"/> and advance its offset as they decode operands.
	/// Most helpers are intentionally tolerant of truncated method bodies so malformed metadata can still be inspected.
	/// </para>
	/// <para>
	/// The API assumes callers provide the correct opcode for follow-up decoding calls such as <see cref="DecodeIndex(ref BlobReader, ILOpCode)"/>.
	/// Supplying a mismatched opcode results in undefined decoding semantics and may throw.
	/// </para>
	/// </remarks>
	public static class ILParser
	{
		/// <summary>
		/// Decodes one IL opcode from the current reader position.
		/// </summary>
		/// <param name="blob">The method body reader.</param>
		/// <returns>The decoded one-byte or two-byte opcode value.</returns>
		public static ILOpCode DecodeOpCode(this ref BlobReader blob)
		{
			byte opCodeByte = blob.ReadByte();
			if (opCodeByte == 0xFE && blob.RemainingBytes >= 1)
			{
				return (ILOpCode)(0xFE00 + blob.ReadByte());
			}
			else
			{
				return (ILOpCode)opCodeByte;
			}
		}

		internal static int OperandSize(this OperandType opType)
		{
			switch (opType)
			{
				// 64-bit
				case OperandType.I8:
				case OperandType.R:
					return 8;
				// 32-bit
				case OperandType.BrTarget:
				case OperandType.Field:
				case OperandType.Method:
				case OperandType.I:
				case OperandType.Sig:
				case OperandType.String:
				case OperandType.Tok:
				case OperandType.Type:
				case OperandType.ShortR:
					return 4;
				// (n + 1) * 32-bit
				case OperandType.Switch:
					return 4; // minimum 4, usually more
				case OperandType.Variable: // 16-bit
					return 2;
				// 8-bit
				case OperandType.ShortVariable:
				case OperandType.ShortBrTarget:
				case OperandType.ShortI:
					return 1;
				default:
					return 0;
			}
		}

		/// <summary>
		/// Skips the operand bytes of the specified opcode.
		/// </summary>
		/// <param name="blob">The method body reader.</param>
		/// <param name="opCode">The opcode whose operand should be skipped.</param>
		/// <remarks>
		/// For malformed or truncated bodies, this method advances to the end of the blob instead of throwing.
		/// </remarks>
		public static void SkipOperand(this ref BlobReader blob, ILOpCode opCode)
		{
			var opType = opCode.GetOperandType();
			int operandSize;
			if (opType == OperandType.Switch)
			{
				uint n = blob.RemainingBytes >= 4 ? blob.ReadUInt32() : uint.MaxValue;
				if (n < int.MaxValue / 4)
				{
					operandSize = (int)(n * 4);
				}
				else
				{
					operandSize = int.MaxValue;
				}
			}
			else
			{
				operandSize = opType.OperandSize();
			}
			if (operandSize <= blob.RemainingBytes)
			{
				blob.Offset += operandSize;
			}
			else
			{
				// ignore missing/partial operand at end of body
				blob.Offset = blob.Length;
			}
		}

		/// <summary>
		/// Decodes a branch target operand and returns the absolute target offset.
		/// </summary>
		/// <param name="blob">The method body reader positioned at the branch operand.</param>
		/// <param name="opCode">A branch opcode with either short or long branch operand width.</param>
		/// <returns>
		/// The absolute target offset when enough bytes are available; otherwise <see cref="int.MinValue"/>.
		/// </returns>
		public static int DecodeBranchTarget(this ref BlobReader blob, ILOpCode opCode)
		{
			int opSize = opCode.GetBranchOperandSize();
			if (opSize <= blob.RemainingBytes)
			{
				int relOffset = opSize == 4 ? blob.ReadInt32() : blob.ReadSByte();
				return unchecked(relOffset + blob.Offset);
			}
			else
			{
				return int.MinValue;
			}
		}

		/// <summary>
		/// Decodes switch branch targets and returns absolute offsets.
		/// </summary>
		/// <param name="blob">The method body reader positioned at the switch operand.</param>
		/// <returns>An array of absolute target offsets. The array can be empty for malformed input.</returns>
		/// <remarks>
		/// When the encoded target count exceeds the remaining bytes, decoding is truncated to the available payload.
		/// </remarks>
		public static int[] DecodeSwitchTargets(this ref BlobReader blob)
		{
			if (blob.RemainingBytes < 4)
			{
				blob.Offset += blob.RemainingBytes;
				return new int[0];
			}
			uint numTargets = blob.ReadUInt32();
			bool numTargetOverflow = false;
			if (numTargets > blob.RemainingBytes / 4)
			{
				numTargets = (uint)(blob.RemainingBytes / 4);
				numTargetOverflow = true;
			}
			int[] targets = new int[numTargets];
			int offset = blob.Offset + 4 * targets.Length;
			for (int i = 0; i < targets.Length; i++)
			{
				targets[i] = unchecked(blob.ReadInt32() + offset);
			}
			if (numTargetOverflow)
			{
				blob.Offset += blob.RemainingBytes;
			}
			return targets;
		}

		/// <summary>
		/// Decodes a user-string token operand and resolves it to the corresponding metadata string.
		/// </summary>
		/// <param name="blob">The method body reader positioned at the token operand.</param>
		/// <param name="metadata">The metadata reader used to resolve the token.</param>
		/// <returns>The decoded user string.</returns>
		public static string DecodeUserString(this ref BlobReader blob, MetadataReader metadata)
		{
			return metadata.GetUserString(MetadataTokens.UserStringHandle(blob.ReadInt32()));
		}

		/// <summary>
		/// Decodes a local-variable or parameter index operand.
		/// </summary>
		/// <param name="blob">The method body reader positioned at the index operand.</param>
		/// <param name="opCode">The opcode that determines operand width.</param>
		/// <returns>The decoded index value.</returns>
		/// <exception cref="ArgumentException">
		/// <paramref name="opCode"/> does not carry a variable-index operand.
		/// </exception>
		public static int DecodeIndex(this ref BlobReader blob, ILOpCode opCode)
		{
			switch (opCode.GetOperandType())
			{
				case OperandType.ShortVariable:
					return blob.ReadByte();
				case OperandType.Variable:
					return blob.ReadUInt16();
				default:
					throw new ArgumentException($"{opCode} not supported!");
			}
		}

		/// <summary>
		/// Determines whether the opcode semantically terminates execution of the current method region.
		/// </summary>
		/// <param name="opCode">The opcode to inspect.</param>
		/// <returns>
		/// <see langword="true"/> for <c>ret</c>, <c>endfilter</c>, and <c>endfinally</c>; otherwise <see langword="false"/>.
		/// </returns>
		public static bool IsReturn(this ILOpCode opCode)
		{
			return opCode == ILOpCode.Ret || opCode == ILOpCode.Endfilter || opCode == ILOpCode.Endfinally;
		}

		/// <summary>
		/// Computes the method-header size in bytes for the specified method body reader.
		/// </summary>
		/// <param name="bodyBlockReader">A reader positioned at the start of a method body header.</param>
		/// <returns>The tiny-header size (<c>1</c>) or the decoded fat-header size in bytes.</returns>
		public static int GetHeaderSize(BlobReader bodyBlockReader)
		{
			byte header = bodyBlockReader.ReadByte();
			if ((header & 3) == 3)
			{
				// fat
				ushort largeHeader = (ushort)((bodyBlockReader.ReadByte() << 8) | header);
				return (byte)(largeHeader >> 12) * 4;
			}
			else
			{
				// tiny
				return 1;
			}
		}

		/// <summary>
		/// Scans a method body and marks all statically encoded branch targets.
		/// </summary>
		/// <param name="blob">The method body reader.</param>
		/// <param name="branchTargets">A bit set that receives marked branch target offsets.</param>
		/// <remarks>
		/// This routine only marks targets that fall inside the current method-body blob.
		/// </remarks>
		public static void SetBranchTargets(ref BlobReader blob, BitSet branchTargets)
		{
			while (blob.RemainingBytes > 0)
			{
				var opCode = DecodeOpCode(ref blob);
				if (opCode == ILOpCode.Switch)
				{
					foreach (var target in DecodeSwitchTargets(ref blob))
					{
						if (target >= 0 && target < blob.Length)
							branchTargets.Set(target);
					}
				}
				else if (opCode.IsBranch())
				{
					int target = DecodeBranchTarget(ref blob, opCode);
					if (target >= 0 && target < blob.Length)
						branchTargets.Set(target);
				}
				else
				{
					SkipOperand(ref blob, opCode);
				}
			}
		}
	}
}

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
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Describes IL type-name rendering modes used by the disassembler.
	/// </summary>
	public enum ILNameSyntax
	{
		/// <summary>
		/// class/valuetype + TypeName (built-in types use keyword syntax)
		/// </summary>
		Signature,
		/// <summary>
		/// Like signature, but always refers to type parameters using their position
		/// </summary>
		SignatureNoNamedTypeParameters,
		/// <summary>
		/// [assembly]Full.Type.Name (even for built-in types)
		/// </summary>
		TypeName,
		/// <summary>
		/// Name (but built-in types use keyword syntax)
		/// </summary>
		ShortTypeName
	}

	/// <summary>
	/// Provides utility methods used while writing textual IL disassembly.
	/// </summary>
	public static class DisassemblerHelpers
	{
		static readonly char[] _validNonLetterIdentifierCharacter = new char[] { '_', '$', '@', '?', '`', '.' };

		/// <summary>
		/// Formats an instruction offset using IL label syntax.
		/// </summary>
		/// <param name="offset">The byte offset within the method body.</param>
		/// <returns>The label text in <c>IL_xxxx</c> form.</returns>
		public static string OffsetToString(int offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}

		/// <summary>
		/// Formats an instruction offset using IL label syntax.
		/// </summary>
		/// <param name="offset">The byte offset within the method body.</param>
		/// <returns>The label text in <c>IL_xxxx</c> form.</returns>
		public static string OffsetToString(long offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}

		/// <summary>
		/// Writes a local reference to an IL instruction offset.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="offset">
		/// The target offset, or <see langword="null"/> to emit the literal <c>null</c>.
		/// </param>
		public static void WriteOffsetReference(ITextOutput writer, int? offset)
		{
			if (offset == null)
				writer.Write("null");
			else
				writer.WriteLocalReference(OffsetToString(offset.Value), offset);
		}

		/// <summary>
		/// Writes one exception region clause in ILAsm-compatible form.
		/// </summary>
		/// <param name="exceptionHandler">The metadata exception region to format.</param>
		/// <param name="module">The module containing type references used by the clause.</param>
		/// <param name="context">Generic context used when rendering catch types.</param>
		/// <param name="writer">The output sink.</param>
		public static void WriteTo(this ExceptionRegion exceptionHandler, MetadataFile module, MetadataGenericContext context, ITextOutput writer)
		{
			writer.Write(".try ");
			WriteOffsetReference(writer, exceptionHandler.TryOffset);
			writer.Write('-');
			WriteOffsetReference(writer, exceptionHandler.TryOffset + exceptionHandler.TryLength);
			writer.Write(' ');
			writer.Write(exceptionHandler.Kind.ToString().ToLowerInvariant());
			if (exceptionHandler.FilterOffset != -1)
			{
				writer.Write(' ');
				WriteOffsetReference(writer, exceptionHandler.FilterOffset);
				writer.Write(" handler ");
			}
			if (!exceptionHandler.CatchType.IsNil)
			{
				writer.Write(' ');
				exceptionHandler.CatchType.WriteTo(module, writer, context);
			}
			writer.Write(' ');
			WriteOffsetReference(writer, exceptionHandler.HandlerOffset);
			writer.Write('-');
			WriteOffsetReference(writer, exceptionHandler.HandlerOffset + exceptionHandler.HandlerLength);
		}

		static string ToInvariantCultureString(object value)
		{
			IConvertible convertible = value as IConvertible;
			return (null != convertible)
				? convertible.ToString(System.Globalization.CultureInfo.InvariantCulture)
				: value.ToString();
		}

		static bool IsValidIdentifierCharacter(char c)
			=> char.IsLetterOrDigit(c) || _validNonLetterIdentifierCharacter.IndexOf(c) >= 0;

		static bool IsValidIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return false;

			if (char.IsDigit(identifier[0]))
				return false;

			// As a special case, .ctor and .cctor are valid despite starting with a dot
			if (identifier[0] == '.')
				return identifier == ".ctor" || identifier == ".cctor";

			if (identifier.Contains(".."))
				return false;

			if (Metadata.ILOpCodeExtensions.ILKeywords.Contains(identifier))
				return false;

			return identifier.All(IsValidIdentifierCharacter);
		}

		/// <summary>
		/// Escapes an identifier using ILAsm single-quote escaping when required.
		/// </summary>
		/// <param name="identifier">The raw identifier text.</param>
		/// <returns>
		/// The original identifier when it is already IL-legal; otherwise a quoted and escaped representation.
		/// </returns>
		public static string Escape(string identifier)
		{
			if (IsValidIdentifier(identifier))
			{
				return identifier;
			}

			// The ECMA specification says that ' inside SQString should be ecaped using an octal escape sequence,
			// but we follow Microsoft's ILDasm and use \'.
			return $"'{EscapeString(identifier).Replace("'", "\\'")}'";
		}

		/// <summary>
		/// Writes a method parameter reference token for an IL instruction operand.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="metadata">The metadata reader used to resolve parameter names.</param>
		/// <param name="handle">The method definition that owns the parameter.</param>
		/// <param name="index">The zero-based method signature parameter index.</param>
		public static void WriteParameterReference(ITextOutput writer, MetadataReader metadata, MethodDefinitionHandle handle, int index)
		{
			string name = GetParameterName(index);
			if (name == null)
			{
				writer.WriteLocalReference(index.ToString(), "param_" + index);
			}
			else
			{
				writer.WriteLocalReference(name, "param_" + index);
			}

			string GetParameterName(int parameterNumber)
			{
				var methodDefinition = metadata.GetMethodDefinition(handle);
				if ((methodDefinition.Attributes & System.Reflection.MethodAttributes.Static) != 0)
				{
					parameterNumber++;
				}
				foreach (var p in methodDefinition.GetParameters())
				{
					var param = metadata.GetParameter(p);
					if (param.SequenceNumber < parameterNumber)
					{
						continue;
					}
					else if (param.SequenceNumber == parameterNumber)
					{
						if (param.Name.IsNil)
							return null;
						return Escape(metadata.GetString(param.Name));
					}
					else
					{
						break;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Writes a local-variable reference token for an IL instruction operand. The output is the numeric
		/// slot index of the local.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="metadata">Accepted for signature compatibility; currently unused.</param>
		/// <param name="handle">Accepted for signature compatibility; currently unused.</param>
		/// <param name="index">The local variable index.</param>
		public static void WriteVariableReference(ITextOutput writer, MetadataReader metadata, MethodDefinitionHandle handle, int index)
		{
			writer.WriteLocalReference(index.ToString(), "loc_" + index);
		}

		/// <summary>
		/// Writes an IL operand using ILAsm-compatible literal formatting.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="operand">The operand value to format.</param>
		/// <exception cref="ArgumentNullException"><paramref name="operand"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// <para>
		/// This overload dispatches to the strongly typed overloads for strings and floating-point values so special
		/// cases such as NaN, infinity, and escaped strings stay consistent with ILDasm-like output.
		/// </para>
		/// <para>
		/// Character operands are emitted as their numeric UTF-16 code unit value, matching IL constant syntax.
		/// </para>
		/// </remarks>
		public static void WriteOperand(ITextOutput writer, object operand)
		{
			if (operand == null)
				throw new ArgumentNullException(nameof(operand));

			string s = operand as string;
			if (s != null)
			{
				WriteOperand(writer, s);
			}
			else if (operand is char)
			{
				writer.Write(((int)(char)operand).ToString());
			}
			else if (operand is float)
			{
				WriteOperand(writer, (float)operand);
			}
			else if (operand is double)
			{
				WriteOperand(writer, (double)operand);
			}
			else if (operand is bool)
			{
				writer.Write((bool)operand ? "true" : "false");
			}
			else
			{
				s = ToInvariantCultureString(operand);
				writer.Write(s);
			}
		}

		/// <summary>
		/// Writes an integral operand using invariant-culture formatting.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="val">The integral value to write.</param>
		public static void WriteOperand(ITextOutput writer, long val)
		{
			writer.Write(ToInvariantCultureString(val));
		}

		/// <summary>
		/// Writes a <see cref="float"/> operand using IL-compatible syntax.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="val">The floating-point value to write.</param>
		/// <remarks>
		/// <para>
		/// Finite numbers use round-trip formatting (<c>R</c>) so re-parsing can recover the same IEEE-754 value.
		/// </para>
		/// <para>
		/// NaN and infinities are emitted as raw byte tuples because ILAsm does not accept textual NaN/Infinity literals.
		/// Negative zero is preserved explicitly.
		/// </para>
		/// </remarks>
		public static void WriteOperand(ITextOutput writer, float val)
		{
			if (val == 0)
			{
				if (1 / val == float.NegativeInfinity)
				{
					// negative zero is a special case
					writer.Write('-');
				}
				writer.Write("0.0");
			}
			else if (float.IsInfinity(val) || float.IsNaN(val))
			{
				byte[] data = BitConverter.GetBytes(val);
				writer.Write('(');
				for (int i = 0; i < data.Length; i++)
				{
					if (i > 0)
						writer.Write(' ');
					writer.Write(data[i].ToString("X2"));
				}
				writer.Write(')');
			}
			else
			{
				writer.Write(val.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		/// <summary>
		/// Writes a <see cref="double"/> operand using IL-compatible syntax.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="val">The floating-point value to write.</param>
		/// <remarks>
		/// Behavior mirrors <see cref="WriteOperand(ITextOutput, float)"/> at 64-bit precision.
		/// </remarks>
		public static void WriteOperand(ITextOutput writer, double val)
		{
			if (val == 0)
			{
				if (1 / val == double.NegativeInfinity)
				{
					// negative zero is a special case
					writer.Write('-');
				}
				writer.Write("0.0");
			}
			else if (double.IsInfinity(val) || double.IsNaN(val))
			{
				byte[] data = BitConverter.GetBytes(val);
				writer.Write('(');
				for (int i = 0; i < data.Length; i++)
				{
					if (i > 0)
						writer.Write(' ');
					writer.Write(data[i].ToString("X2"));
				}
				writer.Write(')');
			}
			else
			{
				writer.Write(val.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		/// <summary>
		/// Writes a string operand enclosed in IL string literal quotes.
		/// </summary>
		/// <param name="writer">The output sink.</param>
		/// <param name="operand">The string value to escape and emit.</param>
		public static void WriteOperand(ITextOutput writer, string operand)
		{
			writer.Write('"');
			writer.Write(EscapeString(operand));
			writer.Write('"');
		}

		/// <summary>
		/// Escapes text for inclusion in an IL string literal.
		/// </summary>
		/// <param name="str">The raw string to escape.</param>
		/// <returns>The escaped text without surrounding quote characters.</returns>
		/// <remarks>
		/// Control characters, surrogate code units, and non-space whitespace are emitted as <c>\uXXXX</c> escapes so
		/// the resulting literal is explicit and round-trippable.
		/// </remarks>
		public static string EscapeString(string str)
		{
			var sb = new StringBuilder();
			foreach (char ch in str)
			{
				switch (ch)
				{
					case '"':
						sb.Append("\\\"");
						break;
					case '\\':
						sb.Append("\\\\");
						break;
					case '\0':
						sb.Append("\\0");
						break;
					case '\a':
						sb.Append("\\a");
						break;
					case '\b':
						sb.Append("\\b");
						break;
					case '\f':
						sb.Append("\\f");
						break;
					case '\n':
						sb.Append("\\n");
						break;
					case '\r':
						sb.Append("\\r");
						break;
					case '\t':
						sb.Append("\\t");
						break;
					case '\v':
						sb.Append("\\v");
						break;
					default:
						// print control characters and uncommon white spaces as numbers
						if (char.IsControl(ch) || char.IsSurrogate(ch) || (char.IsWhiteSpace(ch) && ch != ' '))
						{
							sb.AppendFormat("\\u{0:x4}", (int)ch);
						}
						else
						{
							sb.Append(ch);
						}
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Maps fully-qualified BCL primitive type names to IL keyword type names.
		/// </summary>
		/// <param name="fullName">The fully-qualified runtime type name (for example <c>System.Int32</c>).</param>
		/// <returns>
		/// The IL primitive keyword (for example <c>int32</c>), or <see langword="null"/> when
		/// <paramref name="fullName"/> is not one of the recognized primitive aliases.
		/// </returns>
		public static string PrimitiveTypeName(string fullName)
		{
			switch (fullName)
			{
				case "System.SByte":
					return "int8";
				case "System.Int16":
					return "int16";
				case "System.Int32":
					return "int32";
				case "System.Int64":
					return "int64";
				case "System.Byte":
					return "uint8";
				case "System.UInt16":
					return "uint16";
				case "System.UInt32":
					return "uint32";
				case "System.UInt64":
					return "uint64";
				case "System.Single":
					return "float32";
				case "System.Double":
					return "float64";
				case "System.Void":
					return "void";
				case "System.Boolean":
					return "bool";
				case "System.String":
					return "string";
				case "System.Char":
					return "char";
				case "System.Object":
					return "object";
				case "System.IntPtr":
					return "native int";
				default:
					return null;
			}
		}
	}
}

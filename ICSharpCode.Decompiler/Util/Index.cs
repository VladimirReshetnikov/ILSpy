// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	/// <summary>Represents an index into a sequence, counted either from the start or from the end.</summary>
	/// <remarks>
	/// Index is used by the C# compiler to support the new index syntax
	/// <code>
	/// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
	/// int lastElement = someArray[^1]; // lastElement = 5
	/// </code>
	/// </remarks>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
	internal
#endif
	readonly struct Index : IEquatable<Index>
	{
		private readonly int _value;

		/// <summary>Initializes a new <see cref="Index"/> with an explicit origin.</summary>
		/// <param name="value">Zero-based index value. The value must be non-negative.</param>
		/// <param name="fromEnd"><see langword="true"/> to count from the end; <see langword="false"/> to count from the start.</param>
		/// <remarks>
		/// When counting from the end, <c>1</c> refers to the last element and <c>0</c> refers to the position just past the last element.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than zero.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Index(int value, bool fromEnd = false)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			if (fromEnd)
				_value = ~value;
			else
				_value = value;
		}

		// The following private constructors mainly created for perf reason to avoid the checks
		private Index(int value)
		{
			_value = value;
		}

		/// <summary>Gets an index that points at the first element.</summary>
		public static Index Start => new Index(0);

		/// <summary>Gets an index that points one position past the last element.</summary>
		public static Index End => new Index(~0);

		/// <summary>Creates an index counted from the start.</summary>
		/// <param name="value">Zero-based offset from the start. Must be non-negative.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than zero.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromStart(int value)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			return new Index(value);
		}

		/// <summary>Creates an index counted from the end.</summary>
		/// <param name="value">Offset from the end, where <c>1</c> addresses the last element. Must be non-negative.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than zero.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Index FromEnd(int value)
		{
			if (value < 0)
			{
				ThrowValueArgumentOutOfRange_NeedNonNegNumException();
			}

			return new Index(~value);
		}

		/// <summary>Gets the non-negative index magnitude.</summary>
		public int Value {
			get {
				if (_value < 0)
					return ~_value;
				else
					return _value;
			}
		}

		/// <summary>Gets whether this index is interpreted relative to the end of a sequence.</summary>
		public bool IsFromEnd => _value < 0;

		/// <summary>Computes the absolute zero-based offset for a sequence of a specified length.</summary>
		/// <param name="length">Length of the target sequence.</param>
		/// <remarks>
		/// For performance reasons, this method does not validate <paramref name="length"/> or the computed offset.
		/// It also does not check whether the offset is greater than <paramref name="length"/>.
		/// Callers are expected to pass sensible sequence lengths. If an invalid offset is produced and then
		/// used for indexing, the consuming collection is expected to throw an out-of-range exception.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetOffset(int length)
		{
			int offset = _value;
			if (IsFromEnd)
			{
				// offset = length - (~value)
				// offset = length + (~(~value) + 1)
				// offset = length + value + 1

				offset += length + 1;
			}
			return offset;
		}

		/// <summary>Determines whether this value equals another object.</summary>
		/// <param name="value">Object to compare.</param>
		public override bool Equals([NotNullWhen(true)] object? value) => value is Index && _value == ((Index)value)._value;

		/// <summary>Determines whether this value equals another <see cref="Index"/>.</summary>
		/// <param name="other">Other index value to compare.</param>
		public bool Equals(Index other) => _value == other._value;

		/// <summary>Returns a hash code for this instance.</summary>
		public override int GetHashCode() => _value;

		/// <summary>Converts a non-negative integer to an index counted from the start.</summary>
		public static implicit operator Index(int value) => FromStart(value);

		/// <summary>Converts this index to a string representation.</summary>
		public override string ToString()
		{
			if (IsFromEnd)
				return ToStringFromEnd();

			return ((uint)Value).ToString();
		}

		private static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
		{
#if SYSTEM_PRIVATE_CORELIB
            throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
#else
			throw new ArgumentOutOfRangeException("value", "value must be non-negative");
#endif
		}

		private string ToStringFromEnd()
		{
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            Span<char> span = stackalloc char[11]; // 1 for ^ and 10 for longest possible uint value
            bool formatted = ((uint)Value).TryFormat(span.Slice(1), out int charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
#else
			return '^' + Value.ToString();
#endif
		}
	}
}
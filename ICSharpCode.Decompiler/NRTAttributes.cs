// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies that the listed members are not null when the annotated member returns a specific boolean value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		/// <summary>
		/// Gets the return value that guarantees the target members are not null.
		/// </summary>
		public bool ReturnValue { get; }

		/// <summary>
		/// Gets the members that are guaranteed to be not null when <see cref="ReturnValue"/> matches.
		/// </summary>
		public string[] Members { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class for a single member.
		/// </summary>
		/// <param name="returnValue">The method/property return value that implies not-null state.</param>
		/// <param name="member">The member that is guaranteed to be not null.</param>
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			ReturnValue = returnValue;
			Members = new string[1] { member };
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class for multiple members.
		/// </summary>
		/// <param name="returnValue">The method/property return value that implies not-null state.</param>
		/// <param name="members">The members that are guaranteed to be not null.</param>
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			ReturnValue = returnValue;
			Members = members;
		}
	}
}

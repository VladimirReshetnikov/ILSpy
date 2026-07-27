// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Nullable-annotation attribute polyfills from dotnet/runtime, for target frameworks
// that lack them.

#if !NETCORE

#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Backport of <c>NotNullIfNotNullAttribute</c> for target frameworks where the BCL does not expose it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		/// <summary>
		/// Gets the parameter name whose nullability determines the annotated member's nullability.
		/// </summary>
		public string ParameterName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NotNullIfNotNullAttribute"/> class.
		/// </summary>
		/// <param name="parameterName">The parameter whose null-state drives the annotated return value or property.</param>
		public NotNullIfNotNullAttribute(string parameterName)
		{
			ParameterName = parameterName;
		}
	}

	/// <summary>
	/// Backport of <c>NotNullWhenAttribute</c> for target frameworks where the BCL does not expose it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotNullWhenAttribute"/> class.
		/// </summary>
		/// <param name="returnValue">The boolean return value for which the annotated parameter is guaranteed non-null.</param>
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}

		/// <summary>
		/// Gets the return value that implies the annotated parameter is not <see langword="null"/>.
		/// </summary>
		public bool ReturnValue { get; }
	}

	/// <summary>
	/// Backport of <c>DoesNotReturnIfAttribute</c> for target frameworks where the BCL does not expose it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class DoesNotReturnIfAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DoesNotReturnIfAttribute"/> class.
		/// </summary>
		/// <param name="parameterValue">The parameter value that indicates the enclosing method never returns.</param>
		public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

		/// <summary>
		/// Gets the parameter value that indicates the enclosing method does not return.
		/// </summary>
		public bool ParameterValue { get; }
	}
}
#endif

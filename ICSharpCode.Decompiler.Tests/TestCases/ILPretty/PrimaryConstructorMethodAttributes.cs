using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	[method: ConstructorMarker("class")]
	internal class AttributedPrimaryConstructorClass(string value)
	{
		public string Value { get; } = value;
	}

	[method: ConstructorMarker("struct")]
	internal struct AttributedPrimaryConstructorStruct(int value)
	{
		public int Value { get; } = value;
	}

	[AttributeUsage(AttributeTargets.Constructor)]
	internal sealed class ConstructorMarkerAttribute : Attribute
	{
		public string Value { get; }

		public ConstructorMarkerAttribute(string value)
		{
			Value = value;
		}
	}
}

using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class InaccessibleParameterTypes
	{
		// The metadata declares Hidden as a private nested type; the public Handler delegate's
	// signature references it, so accessibility widening raises it to keep the declarations
	// compilable (CS0059). The anonymous methods below still stay 'delegate {}' because the
	// inaccessibility check reads the metadata, not the widened declaration.
	public class Hidden
		{
		}

		public delegate void Handler(Hidden h);

		public static void Register(Action<Hidden> callback)
		{
		}
	}
	public class InaccessibleParameterTypesConsumer
	{
		public InaccessibleParameterTypes.Handler Create()
		{
			return delegate {
			};
		}

		public void Run()
		{
			InaccessibleParameterTypes.Register(delegate {
			});
		}
	}
}

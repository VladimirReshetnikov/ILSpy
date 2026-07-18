namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public class Comparisons
	{
		private class A
		{
		}

		private class B
		{
		}

		private interface IMarker
		{
		}

		private sealed class HasEqualityOperator : IMarker
		{
			public static bool operator ==(HasEqualityOperator a, HasEqualityOperator b)
			{
				return false;
			}

			public static bool operator !=(HasEqualityOperator a, HasEqualityOperator b)
			{
				return false;
			}

			public override bool Equals(object obj)
			{
				return false;
			}

			public override int GetHashCode()
			{
				return 0;
			}
		}

		private bool CompareUnrelatedNeedsCast(A a, B b)
		{
			return (object)a == b;
		}

		private bool CompareObjectToTypeWithEqualityOperator(object a, HasEqualityOperator b)
		{
			return a == (object)b;
		}

		private bool CompareTypeWithEqualityOperatorToObject(HasEqualityOperator a, object b)
		{
			return (object)a == b;
		}

		private bool CompareObjectToTypeWithInequalityOperator(object a, HasEqualityOperator b)
		{
			return a != (object)b;
		}

		private bool CompareTypeWithInequalityOperatorToObject(HasEqualityOperator a, object b)
		{
			return (object)a != b;
		}

		private bool CompareInterfaceToTypeWithEqualityOperator(IMarker a, HasEqualityOperator b)
		{
			return a == (object)b;
		}

		private bool CompareTypeWithEqualityOperatorToInterface(HasEqualityOperator a, IMarker b)
		{
			return (object)a == b;
		}

		private bool CompareInterfaceToTypeWithInequalityOperator(IMarker a, HasEqualityOperator b)
		{
			return a != (object)b;
		}

		private bool CompareTypeWithInequalityOperatorToInterface(HasEqualityOperator a, IMarker b)
		{
			return (object)a != b;
		}

		private bool CompareTypesWithEqualityOperator(HasEqualityOperator a, HasEqualityOperator b)
		{
			return (object)a == (object)b;
		}

		private bool CompareTypesWithInequalityOperator(HasEqualityOperator a, HasEqualityOperator b)
		{
			return (object)a != (object)b;
		}
	}
}

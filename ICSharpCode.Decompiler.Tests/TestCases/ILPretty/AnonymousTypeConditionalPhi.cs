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
using System.Runtime.InteropServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public static class AnonymousTypeConditionalPhi
	{
		private interface IMarker
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MarkerStruct : IMarker
		{
		}

		private sealed class MarkerClass : IMarker
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SetterStruct
		{
			public int Value {
				set {
				}
			}
		}

		private static class SetterHolder
		{
			public static SetterStruct Field;

			public static int InitializationCount;

			static SetterHolder()
			{
				InitializationCount = 1;
			}
		}

		private static IList<int> GetList()
		{
			return null;
		}

		private static IEnumerable<int> GetEnumerable()
		{
			return null;
		}

		private static IList<T> AsList<T>(T[] values)
		{
			return values;
		}

		private static IReadOnlyCollection<T> AsReadOnlyCollection<T>(T[] values)
		{
			return values;
		}

		public static object Get(bool hasSource, bool recursive)
		{
			return (!hasSource) ? new {
				Items = GetEnumerable()
			} : new {
				Items = (recursive ? GetEnumerable() : GetList())
			};
		}

		public static object WideningBail(bool useEnumerable, object fallback)
		{
			object result;
			if (useEnumerable)
			{
				IEnumerable<int> enumerable = GetEnumerable();
				result = enumerable;
			}
			else
			{
				result = fallback;
			}
			return result;
		}

		private static object BoxedNumericWidening(bool condition)
		{
			object result;
			if (condition)
			{
				object obj = 1;
				result = obj;
			}
			else
			{
				object obj2 = 2L;
				result = obj2;
			}
			return result;
		}

		private static object DirectBoxedNumericWidening(bool condition)
		{
			return (!condition) ? ((object)2L) : ((object)1);
		}

		private static object NamedIdentityConditional(bool condition, IMarker left, IMarker right)
		{
			return (!condition) ? right : left;
		}

		private static object BoxedInterfaceConditional(bool condition, MarkerStruct marker)
		{
			return new {
				Item = ((!condition) ? new MarkerClass() : ((IMarker)marker))
			};
		}

		private static object AnonymousContainingIdentity(bool condition)
		{
			var array = new[] {
				new {
					Item = 1
				}
			};
			var array2 = new[] {
				new {
					Item = 2
				}
			};
			return (!condition) ? array2 : array;
		}

		private static object AnonymousContainingCallIdentity(bool condition)
		{
			var values = new[] {
				new {
					Item = 5
				}
			};
			var values2 = new[] {
				new {
					Item = 6
				}
			};
			return (!condition) ? AsList(values2) : AsList(values);
		}

		private static object AnonymousContainingWideningBail(bool condition)
		{
			var values = new[] {
				new {
					Item = 3
				}
			};
			var values2 = new[] {
				new {
					Item = 4
				}
			};
			object result;
			if (condition)
			{
				var enumerable = AsList(values);
				result = enumerable;
			}
			else
			{
				result = AsReadOnlyCollection(values2);
			}
			return result;
		}

		private static object AnonymousContainingLambdaBail(bool condition)
		{
			Func<_003C_003Ef__AnonymousType1<int>, int> result;
			if (condition)
			{
				var func = item => item.Item;
				result = func;
			}
			else
			{
				result = item => item.Item + 1;
			}
			return result;
		}

		public static void GuardedSetterBail(bool condition, int val)
		{
			if (!condition)
			{
				throw new InvalidOperationException();
			}
			GetList()[0] = val;
		}

		private static void GuardedStaticSetterBail(bool condition, int val)
		{
			if (!condition)
			{
				throw new InvalidOperationException();
			}
			SetterHolder.Field.Value = val;
		}
	}
}

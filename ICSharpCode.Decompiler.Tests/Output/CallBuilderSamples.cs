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

using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.CallBuilder
{
	internal class CallBuilderSamples
	{
		private sealed class PriorityConstructor
		{
			public PriorityConstructor(string callerFilePath = null, string callerMemberName = null)
			{
			}

			[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
			public PriorityConstructor(string hint, string callerFilePath = null, string callerMemberName = null)
			{
			}
		}

		private sealed class PriorityIndexer
		{
			public string this[string callerFilePath = null, string callerMemberName = null] {
				get { return callerFilePath + callerMemberName; }
			}

			[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
			public string this[string hint, string callerFilePath = null, string callerMemberName = null] {
				get { return hint + callerFilePath + callerMemberName; }
			}
		}

		private sealed class ReorderedPriorityIndexer
		{
			public string this[string a, int b, int c] {
				get { return a + b + c; }
				set { }
			}

			[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
			public string this[object x, int b, int c] {
				get { return x + b.ToString() + c; }
				set { }
			}
		}

		private static string CreateByCurrentContext(string callerFilePath = null, string callerMemberName = null)
		{
			return callerFilePath + callerMemberName;
		}

		[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
		private static string CreateByCurrentContext(string hint, string callerFilePath = null, string callerMemberName = null)
		{
			return hint + callerFilePath + callerMemberName;
		}

		private static string ReorderedPriority(string a, int b, int c)
		{
			return a + b + c;
		}

		[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
		private static string ReorderedPriority(object x, int b, int c)
		{
			return x + b.ToString() + c;
		}

		private static string GetText(string value)
		{
			return value;
		}

		private static string OrdinaryOverload(string text)
		{
			return text;
		}

		private static string OrdinaryOverload(object value)
		{
			return value.ToString();
		}

		private static string ParamsPriority(params string[] values)
		{
			return string.Join("", values);
		}

		[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
		private static string ParamsPriority(params object[] items)
		{
			return string.Join("", items);
		}

		private static int GetKey(int value)
		{
			return value;
		}

		private static IDictionary<int, (int value, int fallback)> GetDictionary(
			IDictionary<int, (int value, int fallback)> dictionary)
		{
			return dictionary;
		}

		private TValue ValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
		{
			return defaultValue;
		}

		public void ReorderedGenericTupleDefault(IDictionary<int, (int value, int fallback)> dictionary)
		{
			ValueOrDefault(key: GetKey(0), dictionary: GetDictionary(dictionary), defaultValue: default((int value, int fallback)));
		}

		public string PreserveLowerPriorityOverload()
		{
			return CreateByCurrentContext(callerFilePath: "file", callerMemberName: "member");
		}

		public object PreserveLowerPriorityConstructor()
		{
			return new PriorityConstructor(callerFilePath: "file", callerMemberName: "member");
		}

		public string PreserveLowerPriorityIndexer()
		{
			var indexer = new PriorityIndexer();
			return indexer[callerFilePath: "file", callerMemberName: "member"];
		}

		public string PreserveReorderedLowerPriorityOverload()
		{
			return ReorderedPriority(a: GetText("a"), c: GetKey(3), b: GetKey(2));
		}

		public string PreserveReorderedLowerPriorityIndexer()
		{
			var indexer = new ReorderedPriorityIndexer();
			return indexer[a: GetText("a"), c: GetKey(3), b: GetKey(2)];
		}

		public void PreserveReorderedLowerPriorityIndexerSetter()
		{
			var indexer = new ReorderedPriorityIndexer();
			indexer[a: GetText("a"), c: GetKey(3), b: GetKey(2)] = "value";
		}

		public string PreserveLowerPriorityParamsOverload()
		{
			return ParamsPriority(values: new string[1] { "value" });
		}

		public string PreserveOrdinaryObjectOverload()
		{
			return OrdinaryOverload((object)GetText("value"));
		}
	}
}

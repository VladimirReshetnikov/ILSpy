// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq.Expressions;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	// An expression-tree lambda compiled inline as the value of a switch statement.
	// The C# compiler lowers 'key => key.EnableOverride' to
	// Expression.Lambda(Expression.Field(parameter, FieldInfo.GetFieldFromHandle(ldtoken field)), ...),
	// which appears as the dispatch value of the switch. The expression-tree transform must
	// reconstruct the lambda even though that value lives inside a control-flow block.
	public class ExpressionTreeInSwitch
	{
		public enum Override
		{
			None,
			Enable,
			Disable
		}

		public class Key
		{
			public Override EnableOverride;
		}

		public interface IStore
		{
			TVal GetValue<TKey, TVal>(Expression<Func<TKey, TVal>> selector);
		}

		private IStore store;

		public void SwitchOnFieldSelector(Action onEnable, Action onDisable, Action onDefault)
		{
			switch (store.GetValue((Key key) => key.EnableOverride))
			{
			case Override.Enable:
				onEnable();
				break;
			case Override.Disable:
				onDisable();
				break;
			default:
				onDefault();
				break;
			}
		}
	}
}

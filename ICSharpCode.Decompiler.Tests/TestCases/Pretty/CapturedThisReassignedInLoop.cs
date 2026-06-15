using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class CapturedThisReassignedInLoop
	{
		public CapturedThisReassignedInLoop Parent;

		public int Value;

		public Dictionary<int, CapturedThisReassignedInLoop> Children = new Dictionary<int, CapturedThisReassignedInLoop>();

		public Func<int> WalkParentChain(int threshold)
		{
			CapturedThisReassignedInLoop current = this;
			while (current.Value < threshold)
			{
				current = current.Parent;
				if (current == null)
				{
					return null;
				}
			}

			return () => current.Value;
		}

		public Func<int> WalkNestedChain(int key)
		{
			CapturedThisReassignedInLoop current = this;
			do
			{
				int snapshot = current.Value;
				if (current.Children.TryGetValue(key, out var value))
				{
					current = value;
					return () => current.Value + snapshot;
				}

				current = current.Parent;
			} while (current != null);
			return null;
		}
	}
}

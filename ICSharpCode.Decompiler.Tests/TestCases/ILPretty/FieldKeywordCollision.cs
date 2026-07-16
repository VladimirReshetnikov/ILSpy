using System;
using System.Runtime.CompilerServices;

namespace ICSharpCode.Decompiler.Tests.TestCases.ILPretty
{
	public class a<a2>
	{
		public a2 Value;
		public a2 a3;

		public a2 Echo(a2 value)
		{
			return value;
		}

		public a2 ReadMember()
		{
			return this.a3;
		}
	}

	public enum CollisionMode
	{
		None,
		CollisionMode
	}

	public static class CollisionModeConsumer
	{
		public static CollisionMode Read()
		{
			return CollisionMode.CollisionMode;
		}
	}

	public class CollisionShapes
	{
		internal class Hidden
		{
		}

		private int CollisionShapes2;

		private int m_Same;

		private int Same()
		{
			return this.CollisionShapes2;
		}

		public int Read()
		{
			return Same();
		}

		private int CollisionShapes3(int value)
		{
			return value;
		}

		public int ReadMethodName()
		{
			return this.CollisionShapes3(1);
		}

		private void Overload(int value)
		{
		}

		private void Overload(string value)
		{
		}

		internal static void Leak(Hidden value)
		{
		}

		public static int MalformedBooleanSwitch(bool value)
		{
			return (value ? 1 : 0) switch {
				0 => 0,
				1 => 1,
				2 => 2,
				_ => -1,
			};
		}

		public static int ConstantSwitchDefiniteAssignment()
		{
			int num = 0;
			return 42;
		}

		public static int GotoCrossedLocals(object input)
		{
			short num = 10;
			object obj = default(object);
			int num2 = default(int);
			switch ((num == 10) ? 1 : 0)
			{
				case 0:
				case 2:
					break;
				default:
					obj = input;
					num2 = 1;
					break;
			}
			switch (num2)
			{
				case 1:
					if (obj != null)
					{
						return 1;
					}
					break;
			}
			return 0;
		}
	}

	public class DelegateOwner
	{
		private class Closure
		{
			private class State
			{
				public int Value;
			}

			[CompilerGenerated]
			internal int Helper()
			{
				return new State {
					Value = 7
				}.Value;
			}
		}

		public static Func<int> Create()
		{
			return new Closure().Helper;
		}
	}

	internal class FieldKeywordCollision
	{
		public int field;

		[CompilerGenerated]
		private int _003CProp_003Ek__BackingField;

		public int Prop {
			[CompilerGenerated]
			get {
				return _003CProp_003Ek__BackingField + @field;
			}
		}
	}
}

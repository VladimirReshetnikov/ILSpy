using System;
using System.Runtime.InteropServices.WindowsRuntime;

public static class EventConsumer
{
	public static void SubscribeInstance(FramePool pool, EventHandler handler)
	{
		pool.FrameArrived += handler;
	}

	public static void SubscribeVirtual(VirtualPool pool, EventHandler handler)
	{
		pool.Arrived += handler;
	}

	public static void UnsubscribeInstance(FramePool pool, EventHandler handler)
	{
		pool.FrameArrived -= handler;
	}

	public static void SubscribeStatic(EventHandler handler)
	{
		StaticEvents.StaticArrived += handler;
	}

	public static void UnsubscribeStatic(EventHandler handler)
	{
		StaticEvents.StaticArrived -= handler;
	}
}
public class FieldHolder
{
	private VirtualPool pool;

	public void Unsubscribe(EventHandler handler)
	{
		pool.Arrived -= handler;
	}
}
public class FramePool
{
	public event EventHandler FrameArrived {
		add {
			return default(EventRegistrationToken);
		}
		remove {
		}
	}
}
public static class StaticEvents
{
	public static event EventHandler StaticArrived {
		add {
			return default(EventRegistrationToken);
		}
		remove {
		}
	}
}
public class VirtualPool
{
	public virtual event EventHandler Arrived {
		add {
			return default(EventRegistrationToken);
		}
		remove {
		}
	}
}
namespace System.Runtime.InteropServices.WindowsRuntime
{
	public struct EventRegistrationToken
	{
		private long m_value;
	}
	public static class WindowsRuntimeMarshal
	{
		public static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod, Action<EventRegistrationToken> removeMethod, T handler)
		{
		}

		public static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
		{
		}
	}
}
